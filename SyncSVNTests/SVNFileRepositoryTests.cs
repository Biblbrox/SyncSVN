using SyncSVN;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SharpSvn;
using static SyncSVNTests.TestUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace SyncSVNTests
{
    [TestClass()]
    public class SVNFileRepositoryTests
    {
        private SVNFileRepository Repo { get; set; }
        private SVNFileRepositoryConfig Config { get; set; }

        private void CleanRepo()
        {
            // Delete all dirs and files in a working directory    
            if (Directory.Exists(Repo.RootPath)) { 
                Directory.Delete(Repo.RootPath, true);
                Directory.CreateDirectory(Repo.RootPath);
            }
        }

        [STAThread]
        [TestInitialize]
        public void TestInitialize()
        {
            try {
                Config = new SVNFileRepositoryConfig();

                Console.WriteLine("Config loaded");

                Repo = new SVNFileRepository(Config);

                // Used config
                Console.WriteLine("User: " + Repo.SvnUser);
                Console.WriteLine("Password: " + Repo.SvnPassword);
                Console.WriteLine("Server: " + Repo.SvnUrl);
                Console.WriteLine("Working directory: " + Repo.RootPath);

                // Delete all files in a working directory    
                CleanRepo();
            } catch (Exception e) {
                Assert.Fail("Unable to init repository: " + e.Message + "\n");
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CleanRepo();
        }


        public void PullTestRec(string DirUrl, string DirLocalPath)
        {
            List<string> files = new List<string>();
            List<string> directories = new List<string>();
            SvnInfoEventArgs info;
            Uri repos = new Uri(DirUrl);
            Repo.Client.GetInfo(repos, out info);
            
            Collection<SvnListEventArgs> contents;
            SvnListArgs arg = new SvnListArgs();
            arg.Revision = new SvnRevision(info.Revision); //the revision you want to check
            arg.RetrieveEntries = SvnDirEntryItems.AllFieldsV15;
            if (Repo.Client.GetList(new Uri(DirUrl), arg, out contents)) {
                foreach (SvnListEventArgs item in contents) {
                    if (item.Entry.NodeKind == SvnNodeKind.Directory)
                        directories.Add(item.Path);
                    else if (item.Entry.NodeKind == SvnNodeKind.File)
                        files.Add(item.Path);
                }
            }

            directories = directories.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            directories.Sort();
            foreach (var dir in directories)
                PullTestRec(Path.Combine(DirUrl, dir), Path.Combine(DirLocalPath, dir));

            files.Sort();

            Repo.Pull();

            string rootPath = DirLocalPath;
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

            Console.WriteLine("Remote files");
            files.ForEach(file => Console.WriteLine(file));
            Console.WriteLine("Remote directories");
            directories.ForEach(dir => Console.WriteLine(dir));


            Console.WriteLine("Local files");
            localDirectories.ForEach(file => Console.WriteLine(file));
            Console.WriteLine("Local directories");
            localDirectories.ForEach(dir => Console.WriteLine(dir));

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
            files.ForEach(file => Repo.Download(file));

            // Check they downloaded
            files.ForEach(file => Assert.IsTrue(File.Exists(Path.Combine(Repo.RootPath, file))));
        }

        /// <summary>
        /// Test pull content from remote repository recursively
        /// Test compare each directory or file name in remote repository with local
        /// </summary>
        [TestMethod()]
        public void PullTest()
        {
            PullTestRec(Repo.SvnUrl, Repo.RootPath);
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
            Repo.Pull();

            // Create directory
            string dirName = $@"{Guid.NewGuid()}";
            string dirPath = Path.Combine(Repo.RootPath, dirName);
            Directory.CreateDirectory(dirPath);
            // Add file in that directory
            File.Create(Path.Combine(dirPath, $@"{Guid.NewGuid()}.txt")).Close();

            // Push changes
            Repo.Push();


            // Remember local dirs and files
            List<string> localEntries = Repo.GetAllFilesAndDirs(Repo.RootPath);
            for (int i = 0; i < localEntries.Count; ++i)
                localEntries[i] = GetRelativePath(localEntries[i], Repo.RootPath);

            localEntries.RemoveAll(dir => dir == ".svn");
            localEntries.Sort();

            // Clean local files
            CleanRepo();

            // Fetch repo with created directory and file
            Repo.Pull();


            // Store new lical dirs and files
            var newLocalEntries = Repo.GetAllFilesAndDirs(Repo.RootPath);
            for (int i = 0; i < newLocalEntries.Count; ++i)
                newLocalEntries[i] = GetRelativePath(newLocalEntries[i], Repo.RootPath);

            newLocalEntries.RemoveAll(dir => dir == ".svn");
            newLocalEntries.Sort();

            // Check that content before commit and after clean pull is the same
            Assert.AreEqual(localEntries.Count, newLocalEntries.Count);
            CollectionAssert.AreEqual(localEntries, newLocalEntries);
        }

        /// <summary>
        /// Test resolving conflicts while pull changes from repository
        /// </summary>
        [TestMethod()]
        public void TestConflictPull()
        { 
            string user1Root = Repo.RootPath;

            Repo.Pull();


            // Simulate another user
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            Repo.RootPath = testRootPath;

            // Delete all files in a test working directory    
            CleanRepo();

            Repo.Pull();

            List<string> localFiles =
                Directory.GetFiles(Repo.RootPath).OfType<string>().ToList();
            List<string> localDirectories = Directory.GetDirectories(Repo.RootPath)
                .OfType<string>().ToList();

            // Modify content of some file
            string conflictFile = localFiles[0];
            WriteToFile(conflictFile, "Add new line to file\n");

            Repo.Push();
            // End of another user session


            // Make changes via default user
            Repo.RootPath = user1Root;
            localFiles = Directory.GetFiles(Repo.RootPath).OfType<string>().ToList();
            localDirectories = Directory.GetDirectories(Repo.RootPath).OfType<string>().ToList();

            // Modify content of some file
            conflictFile = localFiles[0];
            WriteToFile(conflictFile, "Add another new line to file\n");

            // Now this file changed by another user and local user both.
            // Try to pull changes from repo
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            Repo.Pull((List<string> list) => ResolveConflictsPull(msg, list));

            // Check that conflict resolved
            Assert.IsTrue(!File.Exists(conflictFile + ".mine"), "Conflicts must be resolved");
        }

        [TestMethod()]
        public void TestConflictPullMultiple()
        {
            string user1Root = Repo.RootPath;
            Repo.Pull();

            // Simulate another user
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            Repo.RootPath = testRootPath;

            // Delete all files in a test working directory    
            CleanRepo();

            var anotherRepo = new SVNFileRepository(Config);

            anotherRepo.Pull();

            var localFiles = Directory.GetFiles(Repo.RootPath).OfType<string>().ToList();
            var localDirectories = Directory.GetDirectories(Repo.RootPath)
                .OfType<string>().ToList();

            // Modify content of some file
            List<string> conflictFiles = new List<string>() {
                localFiles[0], localFiles[1], localFiles[2]
            };

            conflictFiles.ForEach(file => WriteToFile(file, "Add new line to file\n"));

            anotherRepo.Push();
            // End of another user session


            // Make changes via default user
            Repo.RootPath = user1Root;
            localFiles = Directory.GetFiles(Repo.RootPath).OfType<string>().ToList();
            localDirectories = Directory.GetDirectories(Repo.RootPath).OfType<string>().ToList();

            // Modify content of some file
            conflictFiles = new List<string>() {
                localFiles[0], localFiles[1], localFiles[2]
            };

            conflictFiles.ForEach(file => WriteToFile(file, "Add another new line to file\n"));

            // Now this file changed by another user and local user both.
            // Try to pull changes from repo
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            Repo.Pull((List<string> list) => ResolveConflictsPull(msg, list));

            // Check that conflict resolved
            conflictFiles.ForEach(file => {
                Assert.IsTrue(!File.Exists(file + ".mine"), "Conflicts must be resolved");
            });
        }

        /// <summary>
        /// Test resolving conflicts while push changes from repository
        /// </summary>
        [TestMethod()]
        public void TestConflictPushMultiple()
        {// TODO: strange
            string user1Root = Repo.RootPath;
            Repo.Pull();

            // Simulate another user
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            Repo.RootPath = testRootPath;

            // Delete all files in test working directory    
            CleanRepo();


            Repo.Pull();

            var localFiles = Directory.GetFiles(Repo.RootPath).OfType<string>().ToList();
            var localDirectories = Directory.GetDirectories(Repo.RootPath)
                .OfType<string>().ToList();

            // Modify content of some file
            var conflictFiles = new List<string>() { localFiles[0], localFiles[1],localFiles[2] };
            conflictFiles.ForEach(file => WriteToFile(file, "Add new line to file\n"));

            Repo.Push();
            // End of another user session


            // Make changes via default user
            Repo.RootPath = user1Root;
            localFiles = Directory.GetFiles(Repo.RootPath).OfType<string>().ToList();

            // Modify content of some file
            conflictFiles = new List<string>() { localFiles[0], localFiles[1], localFiles[2] };
            conflictFiles.ForEach(file => WriteToFile(file, "Add another new line to file\n"));

            // Now this file changed by another user and local user both.
            // Try to pull changes from repo
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            Repo.Push((List<string> list) => ResolveConflictsPull(msg, list));

            // Check that conflict resolved
            conflictFiles.ForEach(file => {
                Assert.IsFalse(File.Exists(file + ".mine"), "Conflicts must be resolved");
            });
        }


        [TestMethod()]
        public void TestConflictPush()
        {
            string user1Root = Repo.RootPath;
            Repo.Pull();

            // Simulate another user
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            Repo.RootPath = testRootPath;

            // Delete all files in test working directory    
            CleanRepo();

            Repo.Pull();

            var localFiles = Directory.GetFiles(Repo.RootPath).OfType<string>().ToList();
            var localDirectories = Directory.GetDirectories(Repo.RootPath)
                .OfType<string>().ToList();

            // Modify content of some file
            string conflictFile = localFiles[0];
            WriteToFile(conflictFile, "Add new line to file\n");

            Repo.Push();
            // End of another user session


            // Make changes via default user
            Repo.RootPath = user1Root;
            localFiles = Directory.GetFiles(Repo.RootPath).OfType<string>().ToList();

            // Modify content of some file
            conflictFile = localFiles[0];
            WriteToFile(conflictFile, "Add another new line to file\n");

            // Now this file changed by another user and local user both.
            // Try to pull changes from repo
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            Repo.Push((List<string> list) => ResolveConflictsPull(msg, list));

            // Check that conflict resolved
            Assert.IsTrue(!File.Exists(conflictFile + ".mine"), "Conflicts must be resolved");
        }


        [TestMethod()]
        public void UploadAndDeleteTest()
        {
            // Init repo
            Repo.Checkout();

            // Create local file
            string uniqueFileName = $@"{Guid.NewGuid()}.txt";
            string uniqueFilePath = Path.Combine(Repo.RootPath, uniqueFileName);

            var f = File.Create(uniqueFilePath);
            f.Close();
            // Push local file
            Repo.Upload(uniqueFilePath);

            // Remove file from server
            Repo.Delete(uniqueFilePath);

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
            string user1Root = Repo.RootPath;

            Repo.Checkout();

            // Create local file
            string uniqueFileName = $@"{Guid.NewGuid()}.txt";
            string uniqueFilePath = Path.Combine(Repo.RootPath, uniqueFileName);

            var f = File.Create(uniqueFilePath);
            f.Close();
            // Push local file
            Repo.Upload(uniqueFilePath);
            // User 1 actions done


            // User 2 actions
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            Repo.RootPath = testRootPath;
            Repo.Checkout();
            // Edit created file
            WriteToFile(Path.Combine(testRootPath, uniqueFileName), "Local edit content\n");
            // User 2 actions done


            // Again, user 1 actions
            Repo.RootPath = user1Root;
            // Delete created file 
            Repo.Delete(uniqueFilePath);
            // User 1 actions done


            // Again, user 2 actions
            Repo.RootPath = testRootPath;
            // Pull changes
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";

            Repo.Pull((List<string> list) => ResolveConflictsPull(msg, list));


            string[] files = Directory.GetFiles(Repo.RootPath, "*.mine",
                SearchOption.TopDirectoryOnly);
            Assert.IsFalse(files.Length > 0, "Conflicts must be resolved");
        }

        /// <summary>
        /// Check for conflicts when edit deleted remotely local file
        /// </summary>
        [TestMethod()]
        public void UploadAndDeleteConflictEditMultipleDeletedRemoteTest()
        {
            // User 1 actions
            // Init repo
            string user1Root = Repo.RootPath;

            Repo.Checkout();

            // Create local file
            var uniqueFileNames = new List<string>() {
                $@"{Guid.NewGuid()}.txt", $@"{Guid.NewGuid()}.txt", $@"{Guid.NewGuid()}.txt"
            };

            var uniqueFilePaths = new List<string>();
            // TODO: fix this
            uniqueFileNames.ForEach(f => uniqueFilePaths.Add(Path.Combine(Repo.RootPath, f)));

            foreach (var path in uniqueFilePaths) {
                var f = File.Create(path);
                f.Close();
                // Push local file
                Repo.Upload(path);
            }

            // User 1 actions done


            // User 2 actions
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            Repo.RootPath = testRootPath;
            Repo.Checkout();
            // Edit created file
            uniqueFileNames.ForEach(f => {
                WriteToFile(Path.Combine(Repo.RootPath, f), "Local edit content\n");
            });
            // User 2 actions done


            // Again, user 1 actions
            Repo.RootPath = user1Root;
            // Delete created file 
            uniqueFilePaths.ForEach(f => Repo.Delete(f));
            // User 1 actions done


            // Again, user 2 actions
            Repo.RootPath = testRootPath;
            // Pull changes
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            Repo.Pull((List<string> list) => ResolveConflictsPull(msg, list));

            string[] files = Directory.GetFiles(Repo.RootPath, "*.mine",
                SearchOption.TopDirectoryOnly);
            Assert.IsFalse(files.Length > 0, "Conflicts must be resolved");
        }

        /// <summary>
        /// Check for conflicts when delete edited remotely local file
        /// </summary>
        [TestMethod()]
        public void DeleteModifiedRemotlyTest()
        {
            // First user
            Repo.Pull();
            var user1Root = Repo.RootPath;
            // Create local file
            string uniqueFileName = $@"{Guid.NewGuid()}.txt";
            string uniqueFilePath = Path.Combine(Repo.RootPath, uniqueFileName);

            var f = File.Create(uniqueFilePath);
            f.Close();
            // Push local file
            Repo.Upload(uniqueFilePath);

            // Second user
            Repo.RootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(Repo.RootPath);
            Repo.Checkout();
            uniqueFilePath = Path.Combine(Repo.RootPath, uniqueFileName);
            WriteToFile(uniqueFilePath, "Local edit content\n");
            Repo.Upload(uniqueFilePath);

            // First user
            Repo.RootPath = user1Root;
            uniqueFilePath = Path.Combine(Repo.RootPath, uniqueFileName);
            string msg = "Select files";
            Repo.Delete(uniqueFilePath, (List<string> list) => ResolveConflictsPull(msg, list));
            // Check for conflicts

            string[] files = Directory.GetFiles(Repo.RootPath, "*.mine",
                SearchOption.TopDirectoryOnly);
            Assert.IsFalse(files.Length > 0, "All conflicts must be resolved");
        }


        /// <summary>
        /// Download folder test
        /// </summary>
        [TestMethod]
        public void DownloadFolderTest()
        {
            // User 1 actions
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            string defaultPath = Repo.RootPath;
            Repo.RootPath = testRootPath;
            Repo.Checkout();

            // User 2 actions
            Repo.RootPath = defaultPath;
            Repo.Checkout();
            var folderName = $@"{Guid.NewGuid()}";
            var folder = Directory.CreateDirectory(Path.Combine(Repo.RootPath, folderName));

            var fileNames = new List<string> {
                $@"{Guid.NewGuid()}.txt",
                $@"{Guid.NewGuid()}.txt",
                $@"{Guid.NewGuid()}.txt"
            };

            var files = new List<string>{
                Path.Combine(folder.FullName, fileNames[0]),
                Path.Combine(folder.FullName, fileNames[1]),
                Path.Combine(folder.FullName, fileNames[2])
            };

            files.ForEach(path => {
                var file = File.Create(path);
                file.Close();
            });

            // Create extra file not in created early directory
            var extraFileName = $@"{Guid.NewGuid()}.txt"; 
            var extraFile = File.Create(Path.Combine(Repo.RootPath, extraFileName));
            extraFile.Close();

            Repo.Upload(folder.FullName);
            Repo.Upload(extraFile.Name);

            // User 1 actions. He want to download directory without that file
            Repo.RootPath = testRootPath;
            Repo.Download(Path.Combine(Repo.RootPath, folderName));

            string dirPath = Path.Combine(Repo.RootPath, folderName);
            Assert.IsTrue(Directory.Exists(dirPath), "Updated directory must be exists");
            fileNames.ForEach(name => {
                Assert.IsTrue(File.Exists(Path.Combine(dirPath, name)),
                    "Files in updated folder must be exists");
            });
            Assert.IsFalse(File.Exists(Path.Combine(Repo.RootPath, extraFileName)),
                "That file must not be updated");
        }

        /// <summary>
        /// Check if remote file created early exists
        /// </summary>
        [TestMethod]
        public void RemoteExistsTest()
        {
            Repo.Checkout();
            string uniqueFile = $@"{Guid.NewGuid()}.txt";
            var f = File.Create(Path.Combine(Repo.RootPath, uniqueFile));
            f.Close();

            Repo.Upload(Path.Combine(Repo.RootPath, uniqueFile));

            // Check if method works at all
            Assert.IsFalse(SvnExt.RemoteExists(Repo.Client, Repo.SvnUrl, uniqueFile + "aaaa"),
                "This should not be happened");

            Assert.IsTrue(SvnExt.RemoteExists(Repo.Client, Repo.SvnUrl, uniqueFile),
                $@"File {uniqueFile} doesn't exists remotely");
        }

        /// <summary>
        /// Generic test for binary file
        /// </summary>
        [TestMethod]
        public void CheckBinaryFilesPush()
        {
            Repo.Checkout();

            string binFileName = "binFile.bin";
            string binaryPath = Path.Combine(Repo.RootPath, binFileName);
            
            using (var client = new WebClient()) {
                string link = "http://downloads.sourceforge.net/gnuwin32/wget-1.11.4-1-setup.exe";
                client.DownloadFile(link, binaryPath);
            }


            string user1Root = Repo.RootPath;
            Repo.Pull();

            // Simulate another user
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            Repo.RootPath = testRootPath;

            // Delete all files in a test working directory    
            CleanRepo();

            var anotherRepo = new SVNFileRepository(Config);

            anotherRepo.Pull();

            var localFiles = Directory.GetFiles(Repo.RootPath).OfType<string>().ToList();
            var localDirectories = Directory.GetDirectories(Repo.RootPath)
                .OfType<string>().ToList();

            // Modify content of some file
            string conflictFile = Path.Combine(Repo.RootPath, binFileName);
            WriteToFile(conflictFile, "Add new line to file\n");

            anotherRepo.Push();
            // End of another user session


            // Make changes via default user
            Repo.RootPath = user1Root;
            localFiles = Directory.GetFiles(Repo.RootPath).OfType<string>().ToList();
            localDirectories = Directory.GetDirectories(Repo.RootPath).OfType<string>().ToList();

            // Modify content of some file
            conflictFile = Path.Combine(Repo.RootPath, binFileName);
            WriteToFile(conflictFile, "Add another new line to file\n");

            // Now this file changed by another user and local user both.
            // Try to pull changes from repo
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            Repo.Pull((List<string> list) => ResolveConflictsPull(msg, list));

            // Check that conflict resolved
            Assert.IsTrue(!File.Exists(conflictFile + ".mine"), "Conflicts must be resolved");
        }
    }
}