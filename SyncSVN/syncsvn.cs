//using ConfigLib;
using SharpSvn;
using System;
using System.Collections.Generic;

using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;


// подключить nuget-пакет SharpSvn.1.9-x64
namespace RepositoryLib
{
    [DisplayName("Настройки хранилища файлов (SVN)")]
    public class SVNFileRepositoryConfig
    {
        public SVNFileRepositoryConfig()
        {
            configData = new SortedDictionary<string, string>();
            //AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", configPath)
            parseConfigFile();
        }

        public void parseConfigFile()
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;

                if (appSettings.Count == 0)
                {
                    throw new ConfigurationErrorsException("Application settings file doesn't exists");
                }
                else
                {
                    foreach (var key in appSettings.AllKeys)
                    {
                        configData.Add(key, appSettings[key].ToString());
                    }
                }
            }
            catch (ConfigurationException e)
            {
                throw new ConfigurationErrorsException("Error reading app settings: " + e.Message);
            }
        }

        [Category("Место хранения")]
        [DisplayName("Локальная папка")]
        //public string RootPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "RVRFiles");[Category("Место хранения")]
        //[DisplayName("Удаленная папка")]
        //public string SvnUrl { get; set; } = @"https://DESKTOP-TR8E3OJ/svn/test_repo/";
        //[Category("Авторизация")]
        //[DisplayName("Имя пользователя")]
        //public string SvnUser { get; set; } = "aleksey";
        //[Category("Авторизация")]
        //[DisplayName("Пароль пользователя")]
        //public string SvnPassword { get; set; } = "82348234";
        public SortedDictionary<string, string> configData { get; set; }
    }

    public class SVNFileRepository// : IFileRepository
    {
        private SvnClient client;

        public static string GenerateFilename(string path)
        {
            string fileName = Path.GetFileName(path);
            string result = $"{DateTime.Now:yyyyMMddHHmmss}_{fileName}";
            return result;
        }

        public SVNFileRepositoryConfig Config { get; private set; }
        /*public SVNFileRepository(IConfigManager manager) : this(manager.Get<SVNFileRepositoryConfig>(ConfigType.SVNFileRepository)) 
        {
        
        }*/

        //protected SVNFileRepository(SVNFileRepositoryConfig config)
        public SVNFileRepository(SVNFileRepositoryConfig config)

        {
            Config = config;
            client = InitSvnClient();
        }

        private string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                folder += Path.DirectorySeparatorChar;

            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public bool ContainsSubPath(string pathToFile, string subPath)
        {
            return pathToFile.Contains(string.Format(@"{0}\", subPath));
        }

        private List<string> GetAllFilesAndDirs(string dirPath)
        {
            string[] entries = Directory.GetFileSystemEntries(dirPath, "*", SearchOption.AllDirectories);
            return new List<string>(entries);
        }

        public string Download(string id)
        {
            lock (client)
            {
                var svnFolder = Config.configData["RootPath"];
                var svnUrl = Config.configData["SvnUrl"];
                if (!Directory.Exists(svnFolder))
                    Directory.CreateDirectory(svnFolder);
                var svnPath = Path.Combine(svnFolder, id);
                if (File.Exists(svnPath))
                    return svnPath;

                var checkoutArgs = new SvnCheckOutArgs {/* Depth = SvnDepth.Empty*/ };
                //using (var client = InitSvnClient())
                //{
                try
                {
                    if (client.GetUrl(svnFolder).IndexOf(svnUrl) < 0)
                    {
                        client.CheckOut(new SvnUriTarget(svnUrl), svnFolder, checkoutArgs);
                    }
                    client.Update(svnPath);
                }
                catch
                {
                    throw new Exception("Unable to fetch content from repository");
                }
                return svnPath;
                //}
            }
        }
        public SvnClient InitSvnClient()
        {
            SvnClient client = null;
            try
            {
                client = new SvnClient();
                client.Authentication.ForceCredentials(Config.configData["SvnUser"], Config.configData["SvnPassword"]);
                client.Authentication.SslServerTrustHandlers += (s, e) =>
                {
                    e.AcceptedFailures = e.Failures;
                    e.Save = true;
                };
            }
            catch (Exception e)
            {
                throw new Exception("Unable to connect to svn server: " + e.Message);
            }

            return client;
        }

        public string Upload(string path)
        {
            lock (client)
            {
                var svnFolder = Config.configData["RootPath"];
                var svnUrl = Config.configData["SvnUrl"];
                if (!Directory.Exists(svnFolder))
                    Directory.CreateDirectory(svnFolder);
                if (!File.Exists(path))
                    return string.Empty;
                var fileName = GenerateFilename(path);
                var svnPath = Path.Combine(svnFolder, fileName);
                var checkoutArgs = new SvnCheckOutArgs { Depth = SvnDepth.Empty };
                var commitArgs = new SvnCommitArgs { LogMessage = $"Add {path} to {svnPath}" };
                File.Copy(path, svnPath);
                //using (var client = InitSvnClient())
                //{
                try
                {
                    if (client.GetUrl(svnFolder).IndexOf(svnUrl) < 0)
                        client.CheckOut(new SvnUriTarget(svnUrl), svnFolder, checkoutArgs);
                    client.Add(svnPath);
                    client.Commit(svnFolder, commitArgs);
                }
                catch { }
                return fileName;
                //}
            }
        }

        public void Pull()
        {
            // TODO: if status is empty do nothing
            lock (client)
            {
                var svnFolder = Config.configData["RootPath"];
                var svnUrl = Config.configData["SvnUrl"];
                if (!Directory.Exists(svnFolder))
                    Directory.CreateDirectory(svnFolder);

                var checkoutArgs = new SvnCheckOutArgs { /*Depth = SvnDepth.Empty*/ };
                try {
                    if (client.GetUrl(svnFolder).IndexOf(svnUrl) < 0)
                    {
                        //                        client.CheckOut(new SvnUriTarget(svnUrl), svnFolder, checkoutArgs);

                        SvnUpdateArgs args = new SvnUpdateArgs();
                        args.Revision = SvnRevision.Head;
                        Console.Write("Pull changes to directory " + svnFolder + " with revision "
                            + args.Revision + "\n");


                        client.CheckOut(new SvnUriTarget(svnUrl), svnFolder, checkoutArgs);
                    }

                    bool gotList;
                    List<string> files = new List<string>();
                    System.Collections.ObjectModel.Collection<SvnListEventArgs> list;
                    gotList = client.GetList(svnFolder, out list);
                    if (gotList) {
                        foreach (SvnListEventArgs item in list)
                        {
                            client.Update(Path.Combine(svnFolder, item.Path), new SvnUpdateArgs() 
                            { UpdateParents = true });
                            files.Add(item.Path);
                        }
                    }
                    client.Update(svnFolder, new SvnUpdateArgs() { UpdateParents = true });
                }
                catch
                {
                    throw new Exception("Unable to pull content from repository");
                }
            }
        }

        public void Push()
        {
            lock (client) {
                var svnFolder = Config.configData["RootPath"];
                var svnUrl = Config.configData["SvnUrl"];
                if (!Directory.Exists(svnFolder))
                    Directory.CreateDirectory(svnFolder);

                Console.Write("Local directory: " + svnFolder);
                var checkoutArgs = new SvnCheckOutArgs { /*Depth = SvnDepth.Empty*/ };
                try {
                    if (client.GetUrl(svnFolder).IndexOf(svnUrl) < 0) {
                        SvnUpdateArgs args = new SvnUpdateArgs();
                        args.Revision = SvnRevision.Head;
                        Console.Write("Push changes to directory remote repository" + "\n");
                        client.CheckOut(new SvnUriTarget(svnUrl), svnFolder, checkoutArgs);                
                    }


                    // TODO: customize commit message
                    var commitArgs = new SvnCommitArgs { LogMessage = $"Commit message" };

                    List<string> localEntries = GetAllFilesAndDirs(svnFolder);
                    localEntries.RemoveAll(dir => dir.Trim().EndsWith(".svn"));
                    localEntries.RemoveAll(dir => ContainsSubPath(dir, ".svn"));
                    //localDirs.Sort();
                    //localFiles.Sort();

                    try {
                        if (client.GetUrl(svnFolder).IndexOf(svnUrl) < 0)
                            client.CheckOut(new SvnUriTarget(svnUrl), svnFolder, checkoutArgs);
                        foreach (var dir in localEntries)
                            if (!underSvnControl(dir))
                                client.Add(dir);

                        foreach (var file in localEntries)
                            if (!underSvnControl(file))
                                client.Add(file);

                        client.Commit(svnFolder, commitArgs);
                    } catch (Exception e) {
                        throw new Exception("Unable to push content to repository: " + e.Message);
                    }
                }
                catch (Exception e){
                    throw new Exception("Unable to push content to repository: " + e.Message);
                }
            }
        }

        public bool underSvnControl(string filePath)
        {
            // use ThrowOnError = false to avoid exception in case the path does
            // not point to a versioned item
            SvnInfoArgs svnInfoArgs = new SvnInfoArgs() { ThrowOnError = false };
            System.Collections.ObjectModel.Collection<SvnInfoEventArgs> svnInfo;
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
            if (client != null)
            {
                var svnUri = client.GetUriFromWorkingCopy(folder);
                if (svnUri == null) return string.Empty;
                return svnUri.AbsoluteUri;
            }
            return "";
        }
    }
}
