using SyncSVN;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SharpSvn;
using static SyncSVNTests.TestUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SyncSVNTests
{
    [TestClass()]
    public class SVNFileRepositoryTests
    {
        private SVNFileRepository repo { get; set; }
        private SVNFileRepositoryConfig config { get; set; }

        private void cleanRepo()
        {
            // Delete all dirs and files in a working directory    
            if (Directory.Exists(repo.RootPath)) { 
                Directory.Delete(repo.RootPath, true);
                Directory.CreateDirectory(repo.RootPath);
            }
        }

        [STAThread]
        [TestInitialize]
        public void TestInitialize()
        {
            try {
                config = new SVNFileRepositoryConfig();

                Console.WriteLine("Config loaded");

                repo = new SVNFileRepository(config);

                // Used config
                Console.WriteLine("User: " + repo.SvnUser);
                Console.WriteLine("Password: " + repo.SvnPassword);
                Console.WriteLine("Server: " + repo.SvnUrl);
                Console.WriteLine("Working directory: " + repo.RootPath);

                // Delete all files in a working directory    
                cleanRepo();
            } catch (Exception e) {
                Assert.Fail("Unable to init repository: " + e.Message + "\n");
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cleanRepo();
        }


        public void PullTestRec(string DirUrl, string DirLocalPath)
        {
            List<string> files = new List<string>();
            List<string> directories = new List<string>();
            SvnInfoEventArgs info;
            Uri repos = new Uri(DirUrl);
            repo.client.GetInfo(repos, out info);
            
            Collection<SvnListEventArgs> contents;
            SvnListArgs arg = new SvnListArgs();
            arg.Revision = new SvnRevision(info.Revision); //the revision you want to check
            arg.RetrieveEntries = SvnDirEntryItems.AllFieldsV15;
            if (repo.client.GetList(new Uri(DirUrl), arg, out contents)) {
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

            repo.Pull();

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
            PullTestRec(repo.SvnUrl, repo.RootPath);
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
            string dirName = $@"{Guid.NewGuid()}";
            string dirPath = Path.Combine(repo.RootPath, dirName);
            Directory.CreateDirectory(dirPath);
            // Add file in that directory
            File.Create(Path.Combine(dirPath, $@"{Guid.NewGuid()}.txt")).Close();

            // Push changes
            repo.Push();


            // Remember local dirs and files
            List<string> localEntries = repo.GetAllFilesAndDirs(repo.RootPath);
            for (int i = 0; i < localEntries.Count; ++i)
                localEntries[i] = GetRelativePath(localEntries[i], repo.RootPath);

            localEntries.RemoveAll(dir => dir == ".svn");
            localEntries.Sort();

            // Clean local files
            cleanRepo();

            // Fetch repo with created directory and file
            repo.Pull();


            // Store new lical dirs and files
            var newLocalEntries = repo.GetAllFilesAndDirs(repo.RootPath);
            for (int i = 0; i < newLocalEntries.Count; ++i)
                newLocalEntries[i] = GetRelativePath(newLocalEntries[i], repo.RootPath);

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
            string user1Root = repo.RootPath;

            repo.Pull();


            // Simulate another user
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            repo.RootPath = testRootPath;

            // Delete all files in a test working directory    
            cleanRepo();

            repo.Pull();

            List<string> localFiles =
                Directory.GetFiles(repo.RootPath).OfType<string>().ToList();
            List<string> localDirectories = Directory.GetDirectories(repo.RootPath)
                .OfType<string>().ToList();

            // Modify content of some file
            string conflictFile = localFiles[0];
            WriteToFile(conflictFile, "Add new line to file\n");

            repo.Push();
            // End of another user session


            // Make changes via default user
            repo.RootPath = user1Root;
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
                Directory.GetFiles(repo.RootPath).OfType<string>().ToList();
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
        public void TestConflictPushMultiple()
        {// TODO: strange
            string user1Root = repo.RootPath;
            repo.Pull();

            // Simulate another user
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(testRootPath);
            repo.RootPath = testRootPath;

            // Delete all files in test working directory    
            cleanRepo();


            repo.Pull();

            List<string> localFiles =
                Directory.GetFiles(repo.RootPath).OfType<string>().ToList();
            List<string> localDirectories = Directory.GetDirectories(repo.RootPath)
                .OfType<string>().ToList();

            // Modify content of some file
            var conflictFiles = new List<string>() { localFiles[0], localFiles[1],localFiles[2] };
            conflictFiles.ForEach(file => WriteToFile(file, "Add new line to file\n"));

            repo.Push();
            // End of another user session


            // Make changes via default user
            repo.RootPath = user1Root;
            localFiles = Directory.GetFiles(repo.RootPath).OfType<string>().ToList();

            // Modify content of some file
            conflictFiles = new List<string>() { localFiles[0], localFiles[1], localFiles[2] };
            conflictFiles.ForEach(file => WriteToFile(file, "Add another new line to file\n"));

            // Now this file changed by another user and local user both.
            // Try to pull changes from repo
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            repo.Push((List<string> list) => ResolveConflictsPull(msg, list));

            // Check that conflict resolved
            conflictFiles.ForEach(file => {
                Assert.IsFalse(File.Exists(file + ".mine"), "Conflicts must be resolved");
            });
        }


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

            repo.Pull();

            List<string> localFiles =
                Directory.GetFiles(repo.RootPath).OfType<string>().ToList();
            List<string> localDirectories = Directory.GetDirectories(repo.RootPath)
                .OfType<string>().ToList();

            // Modify content of some file
            string conflictFile = localFiles[0];
            WriteToFile(conflictFile, "Add new line to file\n");

            repo.Push();
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
            repo.RootPath = testRootPath;
            // Pull changes
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";

            repo.Pull((List<string> list) => ResolveConflictsPull(msg, list));


            string[] files = Directory.GetFiles(repo.RootPath, "*.mine", 
                SearchOption.TopDirectoryOnly);
            Assert.IsFalse(files.Length > 0, "Conflicts must be resolved");
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
                $@"{Guid.NewGuid()}.txt", $@"{Guid.NewGuid()}.txt", $@"{Guid.NewGuid()}.txt"
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
            repo.RootPath = testRootPath;
            // Pull changes
            string msg = "В следующих удаленных файлах были сделаны изменения."
                + "Отметьте локальные файлы, которые вы хотите заменить удаленными";
            repo.Pull((List<string> list) => ResolveConflictsPull(msg, list));

            string[] files = Directory.GetFiles(repo.RootPath, "*.mine",
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
            repo.Pull();
            var user1Root = repo.RootPath;
            // Create local file
            string uniqueFileName = $@"{Guid.NewGuid()}.txt";
            string uniqueFilePath = Path.Combine(repo.RootPath, uniqueFileName);

            var f = File.Create(uniqueFilePath);
            f.Close();
            // Push local file
            repo.Upload(uniqueFilePath);

            // Second user
            repo.RootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            CreateEmptyFolder(repo.RootPath);
            repo.Checkout();
            uniqueFilePath = Path.Combine(repo.RootPath, uniqueFileName);
            WriteToFile(uniqueFilePath, "Local edit content\n");
            repo.Upload(uniqueFilePath);

            // First user
            repo.RootPath = user1Root;
            uniqueFilePath = Path.Combine(repo.RootPath, uniqueFileName);
            string msg = "Select files";
            repo.Delete(uniqueFilePath, (List<string> list) => ResolveConflictsPull(msg, list));
            // Check for conflicts

            string[] files = Directory.GetFiles(repo.RootPath, "*.mine",
                SearchOption.TopDirectoryOnly);
            Assert.IsFalse(files.Length > 0, "Conflicts must be resolved");
        }


        /// <summary>
        /// Download folder test
        /// </summary>
        [TestMethod]
        public void DownloadFolderTest()
        {
            // User 1 actions
            string testRootPath = Path.Combine(Directory.GetCurrentDirectory(), "testRootPath");
            string defaultPath = repo.RootPath;
            repo.RootPath = testRootPath;
            repo.Checkout();

            // User 2 actions
            repo.RootPath = defaultPath;
            repo.Checkout();
            var folderName = $@"{Guid.NewGuid()}";
            var folder = Directory.CreateDirectory(Path.Combine(repo.RootPath, folderName));

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
            var extraFile = File.Create(Path.Combine(repo.RootPath, extraFileName));
            extraFile.Close();

            repo.Upload(folder.FullName);
            repo.Upload(extraFile.Name);

            // User 1 actions. He want to download directory without that file
            repo.RootPath = testRootPath;
            repo.Download(Path.Combine(repo.RootPath, folderName));

            string dirPath = Path.Combine(repo.RootPath, folderName);
            Assert.IsTrue(Directory.Exists(dirPath), "Updated directory must be exists");
            fileNames.ForEach(name => {
                Assert.IsTrue(File.Exists(Path.Combine(dirPath, name)),
                    "Files in updated folder must be exists");
            });
            Assert.IsFalse(File.Exists(Path.Combine(repo.RootPath, extraFileName)),
                "That file must not be updated");
        }

        /// <summary>
        /// Check if remote file created early exists
        /// </summary>
        [TestMethod]
        public void RemoteExistsTest()
        {
            repo.Checkout();
            string uniqueFile = $@"{Guid.NewGuid()}.txt";
            var f = File.Create(Path.Combine(repo.RootPath, uniqueFile));
            f.Close();

            repo.Upload(Path.Combine(repo.RootPath, uniqueFile));

            // Check if method works at all
            Assert.IsFalse(SvnExt.RemoteExists(repo.client, repo.SvnUrl, uniqueFile + "aaaa"),
                "This should not be happened");

            Assert.IsTrue(SvnExt.RemoteExists(repo.client, repo.SvnUrl, uniqueFile),
                $@"File {uniqueFile} doens't exists remotely");
        }
    }
}