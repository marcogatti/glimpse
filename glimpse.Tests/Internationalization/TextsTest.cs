using System;
using NUnit.Framework;
using System.Resources;
using System.Globalization;
using System.Threading;
using glimpse.Helpers;

namespace glimpse.Tests.Internationalization
{
    [TestFixture]
    public class TextsTest
    {
        [Test]
        public void Espaniol()
        {
            LocalizationHelper loc = new LocalizationHelper("Texts.xml");
        }
    }
}
