using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using WoWTools.SpellDescParser;

namespace Tests
{
    [TestClass]
    public class RootTest
    {
        [TestMethod]
        public void RootEquals()
        {
            var expectedOutput1 = new Root(new List<INode>()
            {
                new PlainText("Hello this is a test string with a "),
                new Property(PropertyType.Unknown, null, null),
                new PlainText(" variable in the middle of it.")
            });

            var expectedOutput2 = new Root(new List<INode>()
            {
                new PlainText("Hello this is a test string with a "),
                new Property(PropertyType.Unknown, null, null),
                new PlainText(" variable in the middle of it.")
            });

            var expectedOutput3 = new Root(new List<INode>()
            {
                new PlainText("Hello this was a test string with a "),
                new Property(PropertyType.Duration, null, null),
                new PlainText(" variable in the middle of it.")
            });

            Assert.AreEqual(expectedOutput1, expectedOutput2);
            Assert.AreNotEqual(expectedOutput1, expectedOutput3);
        }
    }
}
