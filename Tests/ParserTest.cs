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
        public void ExternalDescription()
        {
            var spellDescParser = new SpellDescParser("Conjures a Mana Gem that can be used to instantly restore $5405s1% mana, and holds up to $s2 charges. $@spellname118812 $@spelldesc118812");
            spellDescParser.Parse();
            var expectedOutput = new Root(new List<INode>()
            {
                new PlainText("Conjures a Mana Gem that can be used to instantly restore "),
                new Property(PropertyType.Effect, 1, 5405),
                new PlainText("% mana, and holds up to "),
                new Property(PropertyType.Effect, 2, null),
                new PlainText(" charges. "),
                new Property(PropertyType.SpellName, null, 118812),
                new PlainText(" "),
                new Property(PropertyType.SpellDescription, null, 118812)
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
