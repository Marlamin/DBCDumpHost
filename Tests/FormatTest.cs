using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using WoWTools.SpellDescParser;

namespace Tests
{
    [TestClass]
    public class FormatTest
    {
        [TestMethod]
        public void EffectTest()
        {
            // ID 11 - Frostbolt of Ages
            var spellDescParser = new SpellDescParser("Deals $s1 Frost damage to the target.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 11, new FakeSupplier());

            Assert.AreEqual("Deals 649 Frost damage to the target.", sb.ToString());
        }

        [TestMethod]
        public void Duration()
        {
            // ID 2871 - Nullify Disease
            var spellDescParser = new SpellDescParser("Target is immune to disease for $d and is cured of any existing diseases.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 2871, new FakeSupplier());

            Assert.AreEqual("Target is immune to disease for 30 sec and is cured of any existing diseases.", sb.ToString());
        }

        [TestMethod]
        public void SpellEffectAndDuration()
        {
            // ID 871 - Shield Wall
            var spellDescParser = new SpellDescParser("Reduces all damage you take by $s1% for $d.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 871, new FakeSupplier());

            Assert.AreEqual("Reduces all damage you take by 40% for 8 sec.", sb.ToString());
        }

        [TestMethod]
        public void Radius()
        {
            // ID 22012 - Spirit Heal
            var spellDescParser = new SpellDescParser("Resurrects all friends within $a1 yards.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 22012, new FakeSupplier());

            Assert.AreEqual("Resurrects all friends within 20 yards.", sb.ToString());
        }
    }

    public class FakeSupplier : ISupplier
    {
        public double? SupplyEffectPoint(int spellID, uint? effectIndex)
        {
            if (spellID == 11 && effectIndex == 1)
            {
                return 649.35065;
            }
            else if (spellID == 871 && effectIndex == 1)
            {
                return -40;
            }

            return 0;
        }

        public int? SupplyDuration(int spellID, uint? effectIndex)
        {
            if (spellID == 871)
            {
                return 8;
            }
            else if (spellID == 2871)
            {
                return 30;
            }

            return 0;
        }

        public double? SupplyRadius(int spellID, uint? effectIndex, int radiusIndex)
        {
            if (spellID == 22012)
            {
                return 20;
            }

            return 0;
        }
    }
}
