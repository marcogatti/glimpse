using System;
using NUnit.Framework;
using System.IO;

namespace glimpse.Tests.global
{
    [TestFixture]
    public class AutoTests
    {

        private static String PROJECT_ROOT_DIRECTORY_NAME = "glimpse.Tests";


        public static String getProjectRootDirectory()
        {
            return Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;
        }

        [Test]
        public void projectRootDirectoryNameIsExpected()
        {
            Assert.AreEqual(PROJECT_ROOT_DIRECTORY_NAME, new DirectoryInfo(getProjectRootDirectory()).Name);
        }
    }
}
