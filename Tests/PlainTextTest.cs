using Microsoft.VisualStudio.TestTools.UnitTesting;
using WoWTools.SpellDescParser;

namespace Tests
{
    [TestClass]
    public class PlainTextTest
    {
        [TestMethod]
        public void PlainTextEquals()
        {
            var expectedOutput1 = new PlainText("Hello world!");
            var expectedOutput2 = new PlainText("Hello world!");
            var expectedOutput3 = new PlainText("Goodbye world!");
            Assert.AreEqual(expectedOutput1, expectedOutput2);
            Assert.AreNotEqual(expectedOutput1, expectedOutput3);
        }
    }
}
