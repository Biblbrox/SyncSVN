using Microsoft.VisualStudio.TestTools.UnitTesting;
using RepositoryLib;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpSvn;

namespace RepositoryLib.Tests
{
    [TestClass()]
    public class SVNFileRepositoryTests
    {
        private SVNFileRepository repo { get; set; }
        private SVNFileRepositoryConfig config { get; set; }

        private void cleanRepo()
        {
            // Delete all files in a working directory    
            string[] files = Directory.GetFiles(config.configData["RootPath"]);
            foreach (string file in files)
                File.Delete(file);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            try {
                config = new SVNFileRepositoryConfig();

                Console.WriteLine("Config loaded");
                // Used config
                Console.WriteLine("User: " + config.configData["SvnUser"]);
                Console.WriteLine("Password: " + config.configData["SvnPassword"]);
                Console.WriteLine("Server: " + config.configData["SvnUrl"]);
                Console.WriteLine("Working directory: " + config.configData["RootPath"]);


                // Delete all files in a working directory    
                cleanRepo();

                repo = new RepositoryLib.SVNFileRepository(config);
            } catch (Exception e) {
                Assert.Fail("Unable to init repository: " + e.Message + "\n");
            }
        }

        private void setAttributesNormal(DirectoryInfo dir)
        {
            foreach (var subDir in dir.GetDirectories())
                setAttributesNormal(subDir);
            foreach (var file in dir.GetFiles()) {
                file.Attributes = FileAttributes.Normal;
            }
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


        public void PullTestRec(string DirUrl, string DirLocalPath)
        {
            List<string> files = new List<string>();
            List<string> directories = new List<string>();
            using (SvnClient svnClient = new SvnClient()) {
                SvnInfoEventArgs info;
                Uri repos = new Uri(DirUrl);
                svnClient.GetInfo(repos, out info);

                Collection<SvnListEventArgs> contents;
                SvnListArgs arg = new SvnListArgs();
                arg.Revision = new SvnRevision(info.Revision); //the revision you want to check
                arg.RetrieveEntries = SvnDirEntryItems.AllFieldsV15;
                if (svnClient.GetList(new Uri(DirUrl), arg, out contents)) {
                    foreach (SvnListEventArgs item in contents) {
                        if (item.Entry.NodeKind == SvnNodeKind.Directory)
                            directories.Add(item.Path);
                        else if (item.Entry.NodeKind == SvnNodeKind.File)
                            files.Add(item.Path);
                    }
                }
            }

            directories = directories.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            directories.Sort();
            foreach (var dir in directories)
                PullTestRec(Path.Combine(DirUrl, dir), Path.Combine(DirLocalPath, dir));

            files.Sort();

            repo.Pull();

            string rootPath = DirLocalPath;
            List<string> localFiles = Directory.GetFiles(rootPath)
                    .OfType<string>().ToList();
            List<string> localDirectories = Directory.GetDirectories(rootPath)
                .OfType<string>().ToList();

            for (int i = 0; i < localFiles.Count; ++i)
                localFiles[i] = GetRelativePath(localFiles[i], rootPath);

            for (int i = 0; i < localDirectories.Count; ++i)
                localDirectories[i] = GetRelativePath(localDirectories[i], rootPath);

            localDirectories.RemoveAll(dir => dir == ".svn");
            localDirectories.Sort();
            localFiles.Sort();


            Console.WriteLine("Remote files");
            foreach (var file in files)
                Console.WriteLine(file);
            Console.WriteLine("Remote directories");
            foreach (var dir in directories)
                Console.WriteLine(dir);


            Console.WriteLine("Local files");
            foreach (var file in localFiles)
                Console.WriteLine(file);
            Console.WriteLine("Local directories");
            foreach (var dir in localDirectories)
                Console.WriteLine(dir);

            Assert.AreEqual(localFiles.Count, files.Count);
            Assert.AreEqual(localDirectories.Count, directories.Count);
            CollectionAssert.AreEqual(localFiles, files);
            CollectionAssert.AreEqual(localDirectories, directories);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            var dir = new System.IO.DirectoryInfo(config.configData["RootPath"]);
            if (dir.Exists) {
                setAttributesNormal(dir);
                dir.Delete(true);
                dir.Create();
            }
        }


        /// <summary>
        /// Test download specific file
        /// </summary>
        [TestMethod()]
        public void DownloadTest()
        {

            List<string> files = new List<string> { "asd.txt", "New_File.txt" };
            try {
                // Test downoad file
                foreach (var file in files)
                    repo.Download(file);

            } catch (Exception e) {
                Assert.Fail("Exception occured: " + e.Message + "\n");
            }
        }

        /// <summary>
        /// Test pull content from remote repository recursively
        /// Test compare each directory or file name in remote repository with local
        /// </summary>
        [TestMethod()]
        public void PullTest()
        {
            PullTestRec(config.configData["SvnUrl"], config.configData["RootPath"]);
        }

        /// <summary>
        /// Test push content to remote repository recursively
        /// 
        /// </summary>
        [TestMethod()]
        public void PushTest()
        {
            Console.WriteLine("PushTest");
            string rootPath = config.configData["RootPath"];
            string svnPath = config.configData["SvnUrl"];

            // Fetch Repository con
            repo.Pull();

            // Create directory
            string dirName = "dir_name_that_must_be_free_for_usage";
            string dirPath = Path.Combine(rootPath, dirName);
            Directory.CreateDirectory(dirPath);
            // Add file in that directory
            File.Create(Path.Combine(dirPath, "file_name_that_must_be_free_for_usage.txt")).Close();

            // Push changes
            repo.Push();


            // Remember local dirs and files
            List<string> localFiles = Directory.GetFiles(rootPath).OfType<string>().ToList();
            List<string> localDirectories = Directory.GetDirectories(rootPath)
                .OfType<string>().ToList();

            for (int i = 0; i < localFiles.Count; ++i)
                localFiles[i] = GetRelativePath(localFiles[i], rootPath);

            for (int i = 0; i < localDirectories.Count; ++i)
                localDirectories[i] = GetRelativePath(localDirectories[i], rootPath);

            localDirectories.RemoveAll(dir => dir == ".svn");
            localDirectories.Sort();
            localFiles.Sort();


            // Clean local files
            cleanRepo();

            // Fetch repo with new dir and file
            repo.Pull();


            // Store new lical dirs and files
            List<string> newLocalFiles = Directory.GetFiles(rootPath).OfType<string>().ToList();
            List<string> newLocalDirectories = Directory.GetDirectories(rootPath)
                .OfType<string>().ToList();

            for (int i = 0; i < newLocalFiles.Count; ++i)
                newLocalFiles[i] = GetRelativePath(newLocalFiles[i], rootPath);

            for (int i = 0; i < newLocalDirectories.Count; ++i)
                newLocalDirectories[i] = GetRelativePath(newLocalDirectories[i], rootPath);

            newLocalDirectories.RemoveAll(dir => dir == ".svn");
            newLocalDirectories.Sort();
            newLocalFiles.Sort();


            // Check that content before commit and after clean pull is the same

            Assert.AreEqual(localFiles.Count, newLocalFiles.Count);
            Assert.AreEqual(localDirectories.Count, newLocalDirectories.Count);
            CollectionAssert.AreEqual(localFiles, newLocalFiles);
            CollectionAssert.AreEqual(localDirectories, newLocalDirectories);
        }
    }
}