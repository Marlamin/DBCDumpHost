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

        [TestMethod]
        public void AuraPeriod()
        {
            // ID 201867 - Call of the Void
            var spellDescParser = new SpellDescParser("Your soul is drawn into the void, dealing Shadow damage every $t1.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 201867, new FakeSupplier());

            Assert.AreEqual("Your soul is drawn into the void, dealing Shadow damage every 2.", sb.ToString()); // Yes, every 2.
        }

        [TestMethod]
        public void MaxStacks()
        {
            // ID 149587 - Total Time
            var spellDescParser = new SpellDescParser("Bell Tollssss: $u1");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 149587, new FakeSupplier());

            Assert.AreEqual("Bell Tollssss: 9999", sb.ToString());
        }

        [TestMethod]
        public void ProcChargesAndNewLine()
        {
            // ID 35399 - Spell Reflection
            var spellDescParser = new SpellDescParser("Magical spells will be reflected.\n $n charges.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 35399, new FakeSupplier());

            Assert.AreEqual("Magical spells will be reflected.\n 4 charges.", sb.ToString());
        }

        [TestMethod]
        public void ChainTargets()
        {
            // ID 245131 - Chain Lightning
            var spellDescParser = new SpellDescParser("Strikes an enemy with a lightning bolt that arcs to another nearby enemy. The spell affects up to $x1 targets.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 245131, new FakeSupplier());

            Assert.AreEqual("Strikes an enemy with a lightning bolt that arcs to another nearby enemy. The spell affects up to 3 targets.", sb.ToString());
        }

        [TestMethod]
        public void MaxTargetLevel()
        {
            // ID 334809 - Spiritual Knowledge
            var spellDescParser = new SpellDescParser("Experience gained from killing monsters and completing quests in the Shadowlands increased by $s1%. Lasts $d. Does not function above level $V.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 334809, new FakeSupplier());

            Assert.AreEqual("Experience gained from killing monsters and completing quests in the Shadowlands increased by 5%. Lasts 1 hour. Does not function above level 60.", sb.ToString());
        }

        [TestMethod]
        public void MaxTargets()
        {
            // ID 245235 - From the Void
            var spellDescParser = new SpellDescParser("Calls to the void, summoning $i Waning Voids.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 334809, new FakeSupplier());

            Assert.AreEqual("Calls to the void, summoning 3 Waning Voids.", sb.ToString());
        }
        
        [TestMethod]
        public void ProcChance()
        {
            // ID 3424 - Tainted Howl
            var spellDescParser = new SpellDescParser("Gives nearby allies $h% chance to poison an enemy on hit.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 3424, new FakeSupplier());

            Assert.AreEqual("Gives nearby allies 35% chance to poison an enemy on hit.", sb.ToString());
        }

        [TestMethod]
        public void Range()
        {
            // ID 340484 - Allaying
            var spellDescParser = new SpellDescParser("Puts fallen anima-starved creatures to rest within $r yds.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 340484, new FakeSupplier());

            Assert.AreEqual("Puts fallen anima-starved creatures to rest within 20 yds.", sb.ToString());
        }

        [TestMethod]
        public void EffectAmplitude()
        {
            // ID 322516 - Singe Mana
            var spellDescParser = new SpellDescParser("Hits an enemy with an anti-mana bolt. For each point of mana consumed by the bolt, the target takes $e1 damage.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 322516, new FakeSupplier());

            Assert.AreEqual("Hits an enemy with an anti-mana bolt. For each point of mana consumed by the bolt, the target takes 3 damage.", sb.ToString());
        }

        [TestMethod]
        public void ExternalName()
        {
            // ID 50464 - Nourish
            var spellDescParser = new SpellDescParser("Receives triple bonus from $@spellname77495.");
            spellDescParser.Parse();

            var sb = new StringBuilder();
            spellDescParser.root.Format(sb, 50464, new FakeSupplier());

            Assert.AreEqual("Receives triple bonus from Mastery: Harmony.", sb.ToString());
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
            else if (spellID == 334809)
            {
                return 5;
            }

            return 0;
        }

        public int? SupplyDuration(int spellID, uint? effectIndex)
        {
            if (spellID == 871)
            {
                return 8000;
            }
            else if (spellID == 2871)
            {
                return 30000;
            }
            else if (spellID == 334809)
            {
                return 3600000;
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

        public int? SupplyMaxStacks(int spellID)
        {
            return 9999;
        }

        public int? SupplyAuraPeriod(int spellID, uint? effectIndex)
        {
            return 2000;
        }

        public int? SupplyProcCharges(int spellID)
        {
            return 4;
        }

        public int? SupplyChainTargets(int spellID, uint? effectIndex)
        {
            return 3;
        }

        public int? SupplyMaxTargetLevel(int spellID)
        {
            return 60;
        }

        public int? SupplyMaxTargets(int spellID)
        {
            return 3;
        }

        public int? SupplyProcChance(int spellID)
        {
            if (spellID == 3424)
            {
                return 35;
            }

            return null;
        }

        public int? SupplyMinRange(int spellID)
        {
            if (spellID == 340484)
            {
                return 20;
            }

            return null;
        }

        public int? SupplyMaxRange(int spellID)
        {
            if (spellID == 340484)
            {
                return 20;
            }

            return null;
        }

        public int? SupplyEffectAmplitude(int spellID, uint? effectIndex)
        {
            if (spellID == 322516)
            {
                return 3;
            }

            return null;
        }

        public string? SupplySpellName(int spellID)
        {
            if (spellID == 77495)
            {
                return "Mastery: Harmony";
            }

            return null;
        }
    }
}
