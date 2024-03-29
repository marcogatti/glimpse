﻿using System;
using NUnit.Framework;
using System.IO;

namespace Glimpse.Tests.global
{
    [TestFixture]
    public class AutoTests
    {
        private static String PROJECT_ROOT_DIRECTORY_NAME = "Glimpse.Tests";
        
        public static String getProjectRootDirectory()
        {
            return Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;
        }

        [Test]
        public void projectRootDirectoryNameIsExpected()
        {
            Assert.AreEqual(PROJECT_ROOT_DIRECTORY_NAME.ToLower(), new DirectoryInfo(getProjectRootDirectory()).Name.ToLower());
        }
    }
}
