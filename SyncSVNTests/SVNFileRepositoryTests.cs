using Microsoft.VisualStudio.TestTools.UnitTesting;
using RepositoryLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLib.Tests
{
    [TestClass()]
    public class SVNFileRepositoryTests
    {
        /**
         * Test download specific file
         */
        [TestMethod()]
        public void DownloadTest()
        {
            try
            {
                var config = new RepositoryLib.SVNFileRepositoryConfig();

                System.Diagnostics.Trace.WriteLine("Config loaded");
                // Used config
                Console.WriteLine("User: " + config.configData["SvnUser"]);
                Console.WriteLine("Password: " + config.configData["SvnPassword"]);
                Console.WriteLine("Server: " + config.configData["SvnUrl"]);


                RepositoryLib.SVNFileRepository repo = null;
                try
                {
                    repo = new RepositoryLib.SVNFileRepository(config);
                }
                catch (Exception e)
                {
                    Assert.Fail("Unable to init repository: " + e.Message + "\n");
                }

                System.Diagnostics.Trace.WriteLine("Succesfully init svn client");

                // Test downoad file
                repo.Download("asd.txt");
                repo.Download("New_File.txt");
            }
            catch (Exception e)
            {
                Assert.Fail("Exception occured: " + e.Message + "\n");
            }
        }

        /**
         * Test pull content from remove repository
         */
        [TestMethod()]
        public void PullTest()
        {
            Assert.Fail();
        }
    }
}