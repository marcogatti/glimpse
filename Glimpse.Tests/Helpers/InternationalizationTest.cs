using System;
using NUnit.Framework;
using Glimpse.Tests.global;
using Glimpse.Helpers;
using Glimpse.Exceptions.Internationalization;

namespace Glimpse.Tests.Helpers
{
    [TestFixture]
    public class InternationalizationTest
    {
        private static String SUBPATH_TO_LAGUAGE_FILE = "/Internationalization/LanguageElements.xml";

        private String testProjectRootDirectory;
        private InternationalizationHelper helperSpanish;
        private InternationalizationHelper helperEnglish;
        private InternationalizationHelper helperNoDefaultLang;

        [SetUp]
        public void findtestProjectRootDirectory()
        {
            this.testProjectRootDirectory = AutoTests.getProjectRootDirectory();
            this.helperSpanish = InternationalizationHelper.buildForLanguage(this.languageFilePath(), InternationalizationHelper.SPANISH);
            this.helperEnglish = InternationalizationHelper.buildForLanguage(this.languageFilePath(), InternationalizationHelper.ENGLISH);
            this.helperNoDefaultLang = InternationalizationHelper.buildForLanguage(languageFilePath(), InternationalizationHelper.NO_LANG);
        }

        [Test]
        public void oneLanguageTextIsFoundWithDefaultLanguageES()
        {
            Assert.AreEqual("Por favor iniciá sesión", this.helperSpanish.getLanguageElement("dummy", "Login"));
        }

        [Test]
        [ExpectedException(typeof(DefaultLanguageNotSettedException))]
        public void searchFailsBecauseNoDefaultLanguageIsPresetted()
        {
            this.helperNoDefaultLang.getLanguageElement("dummy", "Login");
        }

        [Test]
        [ExpectedException(typeof(LanguageElementNotFoundException))]
        public void searchFailsBecauseNotLanguageElementISFound()
        {
            this.helperSpanish.getLanguageElement("dummy", "zarazaza");
        }

        [Test]
        public void oneSpanishLanguageTextIsFoundWithNoDefaultLanguage()
        {
            Assert.AreEqual("Por favor iniciá sesión", this.helperNoDefaultLang.getLanguageElement("dummy", "Login", InternationalizationHelper.SPANISH));
        }

        [Test]
        public void oneEnglishLanguageTextIsFoundWithNoDefaultLanguage()
        {
            Assert.AreEqual("Please log in", this.helperNoDefaultLang.getLanguageElement("pepe", "Login", InternationalizationHelper.ENGLISH));
        }


        private string languageFilePath()
        {
            return this.testProjectRootDirectory + SUBPATH_TO_LAGUAGE_FILE;
        }
    }
}
