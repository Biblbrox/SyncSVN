﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using RepositoryLib;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SharpSvn;
using static SyncSVNTests.TestUtils;

namespace SyncSVNTests
{
    [TestClass()]
    public class SVNFileRepositoryTests
    {
        private SVNFileRepository repo { get; set; }
        private SVNFileRepositoryConfig config { get; set; }

        private void cleanRepo()
        {
            // Delete all files in a working directory    
            string[] files = Directory.GetFiles(config.ConfigData["RootPath"]);
            foreach (string file in files)
                File.Delete(file);
        }

        [STAThread]
        [TestInitialize]
        public void TestInitialize()
        {
            try {
                config = new SVNFileRepositoryConfig();

                Console.WriteLine("Config loaded");
                // Used config
                Console.WriteLine("User: " + config.ConfigData["SvnUser"]);
                Console.WriteLine("Password: " + config.ConfigData["SvnPassword"]);
                Console.WriteLine("Server: " + config.ConfigData["SvnUrl"]);
                Console.WriteLine("Working directory: " + config.ConfigData["RootPath"]);


                // Delete all files in a working directory    
                cleanRepo();

                repo = new SVNFileRepository(config);
            } catch (Exception e) {
                Assert.Fail("Unable to init repository: " + e.Message + "\n");
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Directory.Delete(repo.RootPath, true);
            Directory.CreateDirectory(repo.RootPath);
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

        /// <summary>
        /// Test download specific file
        /// </summary>
        [TestMethod()]
        public void DownloadTest()
        {
            List<string> files = new List<string> { "asd.txt", "New_File.txt" };

            // Download files
            files.ForEach(file => repo.Download(file));

            // Check they downloaded
            files.ForEach(file => Assert.IsTrue(File.Exists(Path.Combine(repo.RootPath, file))));
        }

        /// <summary>
        /// Test pull content from remote repository recursively
        /// Test compare each directory or file name in remote repository with local
        /// </summary>
        [TestMethod()]
        public void PullTest()
        {
            PullTestRec(config.ConfigData["SvnUrl"], config.ConfigData["RootPath"]);
        }

        /// <summary>
        /// Test push content to remote repository recursively
        /// 
        /// </summary>
        [TestMethod()]
        public void PushTest()
        {
            Console.WriteLine("PushTest");

            // Fetch Repository con
            repo.Pull();

            // Create directory
            string dirName = "dir_name_that_must_be_free_for_usage";
            string dirPath = Path.Combine(repo.RootPath, dirName);
            Directory.CreateDirectory(dirPath);
            // Add file in that directory
            File.Create(Path.Combine(dirPath, "file_name_that_must_be_free_for_usage.txt")).Close();

            // Push changes
            repo.Push();


            // Remember local dirs and files
            List<string> localFiles = Directory.GetFiles(repo.RootPath).OfType<string>().ToList();
            List<string> localDirectories = Directory.GetDirectories(repo.RootPath)
                .OfType<string>().ToList();

            for (int i = 0; i < localFiles.Count; ++i)
                localFiles[i] = GetRelativePath(localFiles[i], repo.RootPath);

            for (int i = 0; i < localDirectories.Count; ++i)
                localDirectories[i] = GetRelativePath(localDirectories[i], repo.RootPath);

            localDirectories.RemoveAll(dir => dir == ".svn");
            localDirectories.Sort();
            localFiles.Sort();


            // Clean local files
            cleanRepo();

            // Fetch repo with new dir and file
            repo.Pull();


            // Store new lical dirs and files
            List<string> newLocalFiles = Directory.GetFiles(repo.RootPath).OfType<string>().ToList();
            List<string> newLocalDirectories = Directory.GetDirectories(repo.RootPath)
                .OfType<string>().ToList();

            for (int i = 0; i < newLocalFiles.Count; ++i)
                newLocalFiles[i] = GetRelativePath(newLocalFiles[i], repo.RootPath);

            for (int i = 0; i < newLocalDirectories.Count; ++i)
                newLocalDirectories[i] = GetRelativePath(newLocalDirectories[i], repo.RootPath);

            newLocalDirectories.RemoveAll(dir => dir == ".svn");
            newLocalDirectories.Sort();
            newLocalFiles.Sort();

            // Check that content before commit and after clean pull is the same

            Assert.AreEqual(localFiles.Count, newLocalFiles.Count);
            Assert.AreEqual(localDirectories.Count, newLocalDirectories.Count);
            CollectionAssert.AreEqual(localFiles, newLocalFiles);
            CollectionAssert.AreEqual(localDirectories, newLocalDirectories);
        }

        /// <summary>
        /// Test resolving conflicts while pull changes from repository
        /// </summary>
        [TestMethod()]
        public void TestConflictPull()
        {
            string rootPath = config.ConfigData["RootPath"];
            string svnPath = config.ConfigData["SvnUrl"];

            repo.Pull();


            // Simulate another user
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            repo.RootPath = testRootPath;

            // Delete all files in a test working directory    
            cleanRepo();

            var anotherRepo = new SVNFileRepository(config);

            anotherRepo.Pull();

            List<string> localFiles =
                Directory.GetFiles(repo.RootPath).OfType<string>().ToList();
            List<string> localDirectories = Directory.GetDirectories(repo.RootPath)
                .OfType<string>().ToList();

            // Modify content of some file
            string conflictFile = localFiles[0];
            WriteToFile(conflictFile, "Add new line to file\n");

            anotherRepo.Push();
            // End of another user session


            // Make changes via default user
            repo.RootPath = rootPath;
            localFiles = Directory.GetFiles(repo.RootPath).OfType<string>().ToList();
            localDirectories = Directory.GetDirectories(repo.RootPath).OfType<string>().ToList();

            // Modify content of some file
            conflictFile = localFiles[0];
            WriteToFile(conflictFile, "Add another new line to file\n");

            // Now this file changed by another user and local user both.
            // Try to pull changes from repo
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            repo.Pull((List<string> list) => ResolveConflictsPull(msg, list));

            // Check that conflict resolved
            Assert.IsTrue(!File.Exists(conflictFile + ".mine"), "Conflicts must be resolved");
        }

        [TestMethod()]
        public void TestConflictPullMultiple()
        {
            //            string rootPath = config.configData["RootPath"];
            //            string svnPath = config.configData["SvnUrl"];

            string user1Root = repo.RootPath;
            repo.Pull();

            // Simulate another user
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            repo.RootPath = testRootPath;

            // Delete all files in a test working directory    
            cleanRepo();

            var anotherRepo = new SVNFileRepository(config);

            anotherRepo.Pull();

            List<string> localFiles =
                Directory.GetFiles(config.ConfigData["RootPath"]).OfType<string>().ToList();
            List<string> localDirectories = Directory.GetDirectories(repo.RootPath)
                .OfType<string>().ToList();

            // Modify content of some file
            List<string> conflictFiles = new List<string>() {
                localFiles[0], localFiles[1], localFiles[2]
            };

            conflictFiles.ForEach(file => WriteToFile(file, "Add new line to file\n"));

            anotherRepo.Push();
            // End of another user session


            // Make changes via default user
            repo.RootPath = user1Root;
            localFiles = Directory.GetFiles(repo.RootPath).OfType<string>().ToList();
            localDirectories = Directory.GetDirectories(repo.RootPath).OfType<string>().ToList();

            // Modify content of some file
            conflictFiles = new List<string>() {
                localFiles[0], localFiles[1], localFiles[2]
            };

            conflictFiles.ForEach(file => WriteToFile(file, "Add another new line to file\n"));

            // Now this file changed by another user and local user both.
            // Try to pull changes from repo
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            repo.Pull((List<string> list) => ResolveConflictsPull(msg, list));

            // Check that conflict resolved
            conflictFiles.ForEach(file => {
                Assert.IsTrue(!File.Exists(file + ".mine"), "Conflicts must be resolved");
            });
        }

        /// <summary>
        /// Test resolving conflicts while push changes from repository
        /// </summary>
        [TestMethod()]
        public void TestConflictPush()
        {
            string user1Root = repo.RootPath;
            repo.Pull();

            // Simulate another user
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            repo.RootPath = testRootPath;

            // Delete all files in test working directory    
            cleanRepo();

            var anotherRepo = new SVNFileRepository(config);

            anotherRepo.Pull();

            List<string> localFiles =
                Directory.GetFiles(repo.RootPath).OfType<string>().ToList();
            List<string> localDirectories = Directory.GetDirectories(repo.RootPath)
                .OfType<string>().ToList();

            // Modify content of some file
            var conflictFiles = new List<string>() {
                localFiles[0], localFiles[1],localFiles[2]
            };
            conflictFiles.ForEach(file => WriteToFile(file, "Add new line to file\n"));

            anotherRepo.Push();
            // End of another user session


            // Make changes via default user
            config.ConfigData["RootPath"] = user1Root;
            localFiles = Directory.GetFiles(repo.RootPath).OfType<string>().ToList();

            // Modify content of some file
            conflictFiles = new List<string>() {
                localFiles[0], localFiles[1],localFiles[2]
            };
            conflictFiles.ForEach(file => WriteToFile(file, "Add another new line to file\n"));

            // Now this file changed by another user and local user both.
            // Try to pull changes from repo
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            repo.Push((List<string> list) => ResolveConflictsPull(msg, list));

            // Check that conflict resolved
            conflictFiles.ForEach(file => {
                Assert.IsTrue(!File.Exists(file + ".mine"), "Conflicts must be resolved");
            });
        }


        [TestMethod()]
        public void TestConflictPushMultiple()
        {
            string user1Root = repo.RootPath;
            repo.Pull();

            // Simulate another user
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            config.ConfigData["RootPath"] = testRootPath;

            // Delete all files in test working directory    
            cleanRepo();

            var anotherRepo = new SVNFileRepository(config);

            anotherRepo.Pull();

            List<string> localFiles =
                Directory.GetFiles(config.ConfigData["RootPath"]).OfType<string>().ToList();
            List<string> localDirectories = Directory.GetDirectories(config.ConfigData["RootPath"])
                .OfType<string>().ToList();

            // Modify content of some file
            string conflictFile = localFiles[0];
            WriteToFile(conflictFile, "Add new line to file\n");

            anotherRepo.Push();
            // End of another user session


            // Make changes via default user
            repo.RootPath = user1Root;
            localFiles = Directory.GetFiles(repo.RootPath).OfType<string>().ToList();

            // Modify content of some file
            conflictFile = localFiles[0];
            WriteToFile(conflictFile, "Add another new line to file\n");

            // Now this file changed by another user and local user both.
            // Try to pull changes from repo
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            repo.Push((List<string> list) => ResolveConflictsPull(msg, list));

            // Check that conflict resolved
            Assert.IsTrue(!File.Exists(conflictFile + ".mine"), "Conflicts must be resolved");
        }


        [TestMethod()]
        public void UploadAndDeleteTest()
        {
            // Init repo
            repo.Checkout();

            // Create local file
            string uniqueFileName = $@"{Guid.NewGuid()}.txt";
            string uniqueFilePath = Path.Combine(repo.RootPath, uniqueFileName);

            var f = File.Create(uniqueFilePath);
            f.Close();
            // Push local file
            repo.Upload(uniqueFilePath);

            // Remove file from server
            repo.Delete(uniqueFilePath);

            Assert.IsFalse(File.Exists(uniqueFilePath));
        }

        /// <summary>
        /// Check for conflicts when edit deleted remotely local file
        /// </summary>
        [TestMethod()]
        public void UploadAndDeleteConflictEditDeletedRemoteTest()
        {
            // User 1 actions
            // Init repo
            string user1Root = repo.RootPath;

            repo.Checkout();

            // Create local file
            string uniqueFileName = $@"{Guid.NewGuid()}.txt";
            string uniqueFilePath = Path.Combine(repo.RootPath, uniqueFileName);

            var f = File.Create(uniqueFilePath);
            f.Close();
            // Push local file
            repo.Upload(uniqueFilePath);
            // User 1 actions done


            // User 2 actions
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            repo.RootPath = testRootPath;
            repo.Checkout();
            // Edit created file
            WriteToFile(Path.Combine(testRootPath, uniqueFileName), "Local edit content\n");
            // User 2 actions done


            // Again, user 1 actions
            repo.RootPath = user1Root;
            // Delete created file 
            repo.Delete(uniqueFilePath);
            // User 1 actions done


            // Again, user 2 actions
            config.ConfigData["RootPath"] = testRootPath;
            // Pull changes
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            repo.Pull((List<string> list) => ResolveConflictsPull(msg, list));

//            Assert.IsFalse(File.Exists(uniqueFilePath));
        }

        /// <summary>
        /// Check for conflicts when edit deleted remotely local file
        /// </summary>
        [TestMethod()]
        public void UploadAndDeleteConflictEditMultipleDeletedRemoteTest()
        {
            // User 1 actions
            // Init repo
            string user1Root = repo.RootPath;

            repo.Checkout();

            // Create local file
            List<string> uniqueFileNames = new List<string>() {
                $@"{Guid.NewGuid()}.txt", $@"{Guid.NewGuid()}.txt",
                $@"{Guid.NewGuid()}.txt"
            };

            List<string> uniqueFilePaths = new List<string>();
            // TODO: fix this
            uniqueFileNames.ForEach(f => uniqueFilePaths.Add(Path.Combine(repo.RootPath, f)));

            foreach (var path in uniqueFilePaths) {
                var f = File.Create(path);
                f.Close();
                // Push local file
                repo.Upload(path);
            }

            // User 1 actions done


            // User 2 actions
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            repo.RootPath = testRootPath;
            repo.Checkout();
            // Edit created file
            uniqueFileNames.ForEach(f => {
                WriteToFile(Path.Combine(repo.RootPath, f), "Local edit content\n");
            });
            // User 2 actions done


            // Again, user 1 actions
            repo.RootPath = user1Root;
            // Delete created file 
            uniqueFilePaths.ForEach(f => repo.Delete(f));
            // User 1 actions done


            // Again, user 2 actions
            config.ConfigData["RootPath"] = testRootPath;
            // Pull changes
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            repo.Pull((List<string> list) => ResolveConflictsPull(msg, list));

            //            Assert.IsFalse(File.Exists(uniqueFilePath));
        }
    }
}