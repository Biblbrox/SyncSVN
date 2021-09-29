using SharpSvn;
using System;
using System.Collections.Generic;

using System.ComponentModel;
using System.IO;
using System.Configuration;
using System.Collections.ObjectModel;


// подключить nuget-пакет SharpSvn.1.9-x64
namespace RepositoryLib
{
//    using ConflictCallback = Func<List<string>, List<bool>>;
    using ConflictCallback = Func<List<string>, Dictionary<string, bool>>;

    [DisplayName("Настройки хранилища файлов (SVN)")]
    public class SVNFileRepositoryConfig
    {
        public SVNFileRepositoryConfig()
        {
            configData = new SortedDictionary<string, string>();
            //AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", configPath)
            ParseConfigFile();
        }

        /// <summary>
        /// Parse configures from App.Config
        /// </summary>
        public void ParseConfigFile()
        {
            try {
                var appSettings = ConfigurationManager.AppSettings;

                if (appSettings.Count == 0)
                    throw new ConfigurationErrorsException("Application settings file doesn't exists");
                else
                    foreach (var key in appSettings.AllKeys)
                        configData.Add(key, appSettings[key].ToString());
            } catch (ConfigurationException e) {
                throw new ConfigurationErrorsException("Error reading app settings: " + e.Message);
            }
        }

        public SortedDictionary<string, string> configData { get; set; }
    }

    public class ConflictData
    {
        // Indicate whether conflict occured.
        public bool IsConflict;
        public SvnConflictType Type;
        public SvnConflictAction Action;
        public SvnConflictReason Reason;
        public List<string> ConflictEntries;

        public ConflictData()
        {
            IsConflict = false;
            ConflictEntries = new List<string>();
        }
    }

    public class SVNFileRepository// : IFileRepository
    {
        private SvnClient client;

        public SVNFileRepositoryConfig Config { get; private set; }

        private ConflictData Conflict { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GenerateFilename(string path)
        {
            string fileName = Path.GetFileName(path);
            string result = $"{DateTime.Now:yyyyMMddHHmmss}_{fileName}";
            return result;
        }

        /// <summary>
        /// Init repository classx
        /// </summary>
        /// <param name="config"></param>
        //protected SVNFileRepository(SVNFileRepositoryConfig config)
        public SVNFileRepository(SVNFileRepositoryConfig config)
        {
            Config = config;
            client = InitSvnClient();

            Conflict = new ConflictData();
        }

        /// <summary>
        /// Get relative path to entry folder
        /// </summary>
        /// <param name="filespec"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        private string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                folder += Path.DirectorySeparatorChar;

            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Check if path contain subpath
        /// </summary>
        /// <param name="pathToFile"></param>
        /// <param name="subPath"></param>
        /// <returns></returns>
        public bool ContainsSubPath(string pathToFile, string subPath)
        {
            return pathToFile.Contains(string.Format(@"{0}\", subPath));
        }

        /// <summary>
        /// Get all files and directories from folder dirPath
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        private List<string> GetAllFilesAndDirs(string dirPath)
        {
            string[] entries = Directory.GetFileSystemEntries(dirPath, "*", SearchOption.AllDirectories);
            return new List<string>(entries);
        }

        /// <summary>
        /// Return last revision number
        /// </summary>
        /// <returns></returns>
        private SvnRevision getLatestRevision()
        {
            SvnInfoEventArgs info;
            var svnUrl = Config.configData["SvnUrl"];
            Uri repos = new Uri(svnUrl);

            client.GetInfo(repos, out info);

            return info.Revision;
        }

        /// <summary>
        /// Set info about conflict to Conflict member
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void setConflict(object sender, SvnConflictEventArgs e)
        {
            Conflict.IsConflict = true;
            if (e.ConflictAction == SvnConflictAction.Delete &&
                e.ConflictReason == SvnConflictReason.Edited) {
                Conflict.ConflictEntries.Add(Path.Combine(Config.configData["RootPath"], e.Path));
                Conflict.Action = SvnConflictAction.Delete;
                Conflict.Reason = SvnConflictReason.Edited;
                return;
            }

            Conflict.IsConflict = true;
            Conflict.ConflictEntries.Add(e.MergedFile);
        }

        /// <summary>
        /// Return modified entries(files and dirs)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private List<string> getModifiedEntries(string path)
        {
            List<string> entries = new List<string>();

            lock (client) {
                Collection<SvnStatusEventArgs> changedFiles = new Collection<SvnStatusEventArgs>();
                client.GetStatus(path, out changedFiles);

                //delete files from subversion that are not in filesystem
                //add files to suversion , that are new in filesystem

                foreach (SvnStatusEventArgs changedFile in changedFiles)
                    if (changedFile.LocalContentStatus == SvnStatus.Modified)
                        entries.Add(changedFile.Path);
            }

            return entries;
        }

        public void Checkout()
        {
            var svnFolder = Config.configData["RootPath"];
            var svnUrl = Config.configData["SvnUrl"];

            var checkoutArgs = new SvnCheckOutArgs {/* Depth = SvnDepth.Empty*/ };
            client.CheckOut(new SvnUriTarget(svnUrl), svnFolder, checkoutArgs);
        }

        /// <summary>
        /// Download file from repository
        /// If onConflict is null all local changes will be overwriten by remote changes
        /// If onConflict is set then all entries marked as true will not be overwriten 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onConflict"></param>
        /// <returns></returns>
        public string Download(string id, ConflictCallback onConflict = null)
        {
            lock (client) {
                var svnFolder = Config.configData["RootPath"];
                var svnUrl = Config.configData["SvnUrl"];
                if (!Directory.Exists(svnFolder))
                    Directory.CreateDirectory(svnFolder);

                var svnPath = Path.Combine(svnFolder, id);
                if (File.Exists(svnPath))
                    return svnPath;

                try {
                    if (!client.IsWorkingCopy(svnFolder))
                        Checkout();

                    SvnUpdateArgs updateArgs = new SvnUpdateArgs();
                    updateArgs.Conflict += new EventHandler<SvnConflictEventArgs>(setConflict);

                    // Clear conflict state
                    Conflict.IsConflict = false;
                    Conflict.ConflictEntries.Clear();

                    client.Update(svnFolder, updateArgs);

                    if (Conflict.IsConflict) {
                        Dictionary<string, bool> resolve = new Dictionary<string, bool>();
                        if (onConflict != null) {
                            resolve = onConflict(Conflict.ConflictEntries);
                        } else { // Force local changes by default
                            foreach (var entry in Conflict.ConflictEntries)
                                resolve.Add(entry, true);
                        }

                        foreach (string entry in Conflict.ConflictEntries)
                            client.Resolve(entry, resolve[entry] ? SvnAccept.MineFull
                                : SvnAccept.TheirsFull);
                    }
                } catch {
                    throw new Exception("Unable to fetch content from repository");
                }
                return svnPath;
            }
        }

        /// <summary>
        /// Initialize svn client
        /// </summary>
        /// <returns></returns>
        public SvnClient InitSvnClient()
        {
            SvnClient client = null;
            try {
                client = new SvnClient();
                client.Authentication.ForceCredentials(Config.configData["SvnUser"],
                    Config.configData["SvnPassword"]);
                client.Authentication.SslServerTrustHandlers += (s, e) => {
                    e.AcceptedFailures = e.Failures;
                    e.Save = true;
                };
            } catch (Exception e) {
                throw new Exception("Unable to connect to svn server: " + e.Message);
            }

            return client;
        }

        /// <summary>
        /// Delete file entry from repository 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="onConflict"></param>
        public void Delete(string path, ConflictCallback onConflict = null)
        {
            if (!Directory.Exists(path) && !File.Exists(path))
                return;

            lock (client) {
                var svnFolder = Config.configData["RootPath"];
                var svnUrl = Config.configData["SvnUrl"];

                client.Delete(path, new SvnDeleteArgs { Force = true });

                var args = new SvnCommitArgs();
                args.LogMessage = "File " + path + " deleted";
                client.Commit(svnFolder, args);
            }
        }


        /// <summary>
        /// Upload file to svn server
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="onConflict"></param>
        /// <returns></returns>
        public string Upload(string filePath, ConflictCallback onConflict = null)
        {
            lock (client) {
                var svnFolder = Config.configData["RootPath"];
                var svnUrl = Config.configData["SvnUrl"];

                if (!Directory.Exists(svnFolder)) {
                    Directory.CreateDirectory(svnFolder);
                    return string.Empty;
                }

                if (!File.Exists(filePath))
                    return string.Empty;

                // TODO: Check if file under repository path
                // if (!ContainsSubPath(path,  svnFoler))

                var checkoutArgs = new SvnCheckOutArgs { Depth = SvnDepth.Empty };
                var commitArgs = new SvnCommitArgs {
                    LogMessage = $"Add file {filePath} to repository" 
                };

                try {
                    if (!client.IsWorkingCopy(svnFolder))
                        Checkout();

                    Conflict.IsConflict = false;
                    Conflict.ConflictEntries.Clear();

                    SvnUpdateArgs updateArgs = new SvnUpdateArgs();
                    updateArgs.Conflict += new EventHandler<SvnConflictEventArgs>(setConflict);

                    client.Update(svnFolder, updateArgs);


                    if (Conflict.IsConflict) {
                        Dictionary<string, bool> resolve = new Dictionary<string, bool>();
                        if (onConflict != null) {
                            resolve = onConflict(Conflict.ConflictEntries);
                        } else { // Force local changes by default
                            foreach (var entry in Conflict.ConflictEntries)
                                resolve.Add(entry, true);
                        }

                        foreach (string entry in Conflict.ConflictEntries)
                            client.Resolve(entry, resolve[entry] ? SvnAccept.MineFull
                                : SvnAccept.TheirsFull);
                    }

                    if (!underSvnControl(filePath))
                        client.Add(filePath);

                    client.Commit(svnFolder, commitArgs);
                } catch (Exception e) {
                    throw new Exception("Unable to upload file: " + e.Message);
                }

                return filePath;
            }
        }

        /// <summary>
        /// Pull changes from svn server.
        /// If onConflict is null all local changes will be overwriten by remote changes
        /// If onConflict is set then all entries which marked as true will not be overwriten 
        /// by remote changes.
        /// </summary>
        /// <param name="onConflict"></param>
        public void Pull(ConflictCallback onConflict = null)
        {
            lock (client) {
                var svnFolder = Config.configData["RootPath"];
                var svnUrl = Config.configData["SvnUrl"];
                if (!Directory.Exists(svnFolder))
                    Directory.CreateDirectory(svnFolder);

                try {
                    // Check if folder doesn't contain repository.
                    // If it is true - clone repository
                    if (!client.IsWorkingCopy(svnFolder)) {
                        var checkoutArgs = new SvnCheckOutArgs {/* Depth = SvnDepth.Empty*/ };
                        client.CheckOut(new SvnUriTarget(svnUrl), svnFolder, checkoutArgs);
                    }
                    
                    Conflict.IsConflict = false;
                    Conflict.ConflictEntries.Clear();
                    
                    SvnUpdateArgs updateArgs = new SvnUpdateArgs();
                    updateArgs.Conflict += new EventHandler<SvnConflictEventArgs>(setConflict);
                    
                    client.Update(svnFolder, updateArgs);


                    if (Conflict.IsConflict) {
                        Dictionary<string, bool> resolve = new Dictionary<string, bool>();
                        if (onConflict != null) {
                            resolve = onConflict(Conflict.ConflictEntries);
                        } else { // Force local changes by default
                            foreach (var entry in Conflict.ConflictEntries)
                                resolve.Add(entry, true);
                        }

                        foreach (string entry in Conflict.ConflictEntries) {
                            if (Conflict.Action == SvnConflictAction.Delete &&
                                Conflict.Reason == SvnConflictReason.Edited) {
                                if (resolve[entry])
                                    client.Resolve(entry, SvnAccept.Working);
                                else
                                    SvnExt.DeleteEntry(entry);
                            } else {
                                client.Resolve(entry,
                                    resolve[entry] ? SvnAccept.MineFull : SvnAccept.TheirsFull);
                            }
                        }
                    }
                    
                } catch (Exception e) {
                    throw new Exception("Unable to pull content from repository: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Push changes to svn repository. 
        /// If conflicts occurs onConflict will be called.
        /// All local entities marked as true will overwrite remote entities.
        /// </summary>
        /// <param name="onConflict"></param>
        public void Push(ConflictCallback onConflict = null)
        {
            lock (client) {
                var svnFolder = Config.configData["RootPath"];
                var svnUrl = Config.configData["SvnUrl"];
                if (!Directory.Exists(svnFolder))
                    Directory.CreateDirectory(svnFolder);

                Console.Write("Local directory: " + svnFolder);
                try {
                    // Check if folder doesn't contain repository.
                    // If it is true - clone repository
                    // TODO: does this need when push?
                    if (!client.IsWorkingCopy(svnFolder)) {
                        var checkoutArgs = new SvnCheckOutArgs {/* Depth = SvnDepth.Empty*/ };
                        client.CheckOut(new SvnUriTarget(svnUrl), svnFolder, checkoutArgs); 
                    }

                    Console.Write("Push changes to directory remote repository" + "\n");
                    SvnUpdateArgs updateArgs = new SvnUpdateArgs();
                    updateArgs.Conflict += new EventHandler<SvnConflictEventArgs>(setConflict);
                    
                    // Clear conflict state
                    Conflict.IsConflict = false;
                    Conflict.ConflictEntries.Clear();
                    
                    client.Update(svnFolder, updateArgs);


                    if (Conflict.IsConflict) {
                        Dictionary<string, bool> resolve = new Dictionary<string, bool>();
                        if (onConflict != null) {
                            resolve = onConflict(Conflict.ConflictEntries);
                        } else { // Force local changes by default
                            foreach (var entry in Conflict.ConflictEntries)
                                resolve.Add(entry, true);
                        }

                        foreach (string entry in Conflict.ConflictEntries)
                            client.Resolve(entry, resolve[entry] ? SvnAccept.MineFull
                                : SvnAccept.TheirsFull);
                    }

                    List<string> localEntries = GetAllFilesAndDirs(svnFolder);
                    localEntries.RemoveAll(dir => dir.Trim().EndsWith(".svn"));
                    localEntries.RemoveAll(dir => ContainsSubPath(dir, ".svn"));
                    
                    // Add to version control all uncontrolled files
                    foreach (var dir in localEntries)
                        if (!underSvnControl(dir))
                            client.Add(dir);
                    
                    // TODO: customize commit message
                    var changedFiles = getModifiedEntries(svnFolder);
                    string commitMsg = "Next files was modified: \n";
                    foreach (var entry in changedFiles)
                        commitMsg += entry + "\n";
                    
                    commitMsg += "\n";
                    var commitArgs = new SvnCommitArgs { LogMessage = commitMsg};
                    client.Commit(svnFolder, commitArgs);
                } catch (Exception e) {
                    throw new Exception("Unable to push content to repository: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Check if file is under svn
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool underSvnControl(string filePath)
        {
            // use ThrowOnError = false to avoid exception in case the path does
            // not point to a versioned item
            SvnInfoArgs svnInfoArgs = new SvnInfoArgs() { ThrowOnError = false };
            Collection<SvnInfoEventArgs> svnInfo;
            return client.GetInfo(SvnTarget.FromString(filePath), svnInfoArgs, out svnInfo);
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
    public static class SvnExt
    {
        public static string GetUrl(this SvnClient client, string folder)
        {
            if (client != null) {
                var svnUri = client.GetUriFromWorkingCopy(folder);
                if (svnUri == null)
                    return string.Empty;

                return svnUri.AbsoluteUri;
            }

            return "";
        }

        /// <summary>
        /// Delete entry(file or directory)
        /// </summary>
        /// <param name="entryPath"></param>
        public static void DeleteEntry(string entryPath)
        {
            FileAttributes attr = File.GetAttributes(entryPath);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                Directory.Delete(entryPath, true);
            else
                File.Delete(entryPath);
        }

        /// <summary>
        /// Check if local folder is working copy of repository
        /// </summary>
        /// <param name="client"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsWorkingCopy(this SvnClient client, string path)
        {
            var uri = client.GetUriFromWorkingCopy(path);
            return uri != null;
        }

        /// <summary>
        /// Check if remote file exists
        /// </summary>
        /// <param name="client"></param>
        /// <param name="relPath"></param>
        /// <returns></returns>
        public static bool RemoteExists(this SvnClient client, SVNFileRepositoryConfig Config, string relPath)
        {
            Uri targetUri = new Uri(new Uri(Config.configData["SvnUrl"]), relPath);
            var target = SvnTarget.FromUri(targetUri);
            Collection<SvnInfoEventArgs> info;
            bool result = client.GetInfo(target, new SvnInfoArgs { ThrowOnError = false }, out info);

            return result && info.Count != 0;
        }
    }
}
