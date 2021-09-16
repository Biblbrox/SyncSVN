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
                string[] files = Directory.GetFiles(config.configData["RootPath"]);
                foreach (string file in files)
                    File.Delete(file);

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
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public void PullTestRec(string DirUrl, string DirLocalPath)
        {

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

        /**
         * Test download specific file
         */
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

        /**
         * Test pull content from remove repository
         */
        [TestMethod()]
        public void PullTest()
        {
            List<string> files = new List<string>();
            List<string> directories = new List<string>();
            using (SvnClient svnClient = new SvnClient()) {
                SvnInfoEventArgs info;
                Uri repos = new Uri(config.configData["SvnUrl"]);
                svnClient.GetInfo(repos, out info);
                
                Collection<SvnListEventArgs> contents;
                SvnListArgs arg = new SvnListArgs();
                arg.Revision = new SvnRevision(info.Revision); //the revision you want to check
                arg.RetrieveEntries = SvnDirEntryItems.AllFieldsV15;
                if (svnClient.GetList(new Uri(config.configData["SvnUrl"]), arg, out contents)) {
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
            files.Sort();
            
            repo.Pull();
            
            string rootPath = config.configData["RootPath"];
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
            foreach(var file in files)
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
        
        [TestMethod()]
        public void PushTest()
        {
            try {
                // Test pull repository
                repo.Push();
            } catch (Exception e) {
                Assert.Fail("Exception occured: " + e.Message + "\n");  
            }
        }
    }
}