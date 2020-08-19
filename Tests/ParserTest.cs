using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using WoWTools.SpellDescParser;

namespace Tests
{
    [TestClass]
    public class ParserTest
    {
        [TestMethod]
        public void SimpleDescription()
        {
            var spellDescParser = new SpellDescParser("Hello this is a test string with a $d variable ${test} in the middle of it.");
            spellDescParser.Parse();
            var expectedOutput = new Root(new List<INode>()
            {
                new PlainText("Hello this is a test string with a "),
                new Property(PropertyType.Duration, null, null),
                new PlainText(" variable "),
                new Expression("test"),
                new PlainText(" in the middle of it.")
            });

            Assert.AreEqual(expectedOutput, spellDescParser.root);
        }

        [TestMethod]
        public void ReadIntTest()
        {
            Assert.AreEqual(0, new SpellDescParser("0").ReadInt());
            Assert.AreEqual(100, new SpellDescParser("100=positive").ReadInt());
            Assert.AreEqual(-100, new SpellDescParser("-100 is negative").ReadInt());
        }
    }
}
