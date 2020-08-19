using Microsoft.VisualStudio.TestTools.UnitTesting;
using WoWTools.SpellDescParser;

namespace Tests
{
    [TestClass]
    public class VariableTest
    {
        [TestMethod]
        public void VariableEquals()
        {
            var expectedOutput1 = new Property(PropertyType.Duration, 1, null);
            var expectedOutput2 = new Property(PropertyType.Duration, 1, null);
            var expectedOutput3 = new Property(PropertyType.Unknown, 1, null);
            Assert.AreEqual(expectedOutput1, expectedOutput2);
            Assert.AreNotEqual(expectedOutput1, expectedOutput3);
        }

        [TestMethod]
        public void PropertyParse()
        {
            Assert.AreEqual(new Property(PropertyType.Duration, null, null), new SpellDescParser("d seconds").ReadVariable());
            Assert.AreEqual(new Property(PropertyType.Effect, 1, 100), new SpellDescParser("100s1poop").ReadVariable());
            Assert.AreEqual(new Property(PropertyType.Radius0, 50, null), new SpellDescParser("a50").ReadVariable());
        }

        [TestMethod]
        public void ExpressionParse()
        {
            Assert.AreEqual(new Expression("$s2*$<mult>"), new SpellDescParser("{$s2*$<mult>}").ReadExpression());
            Assert.AreEqual(new Expression(""), new SpellDescParser("{}").ReadExpression());
            Assert.AreEqual(new Expression("$s2*$<mult>"), new SpellDescParser("{$s2*$<mult>").ReadExpression());
        }
    }
}
