using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using WoWTools.SpellDescParser;

namespace Tests
{
    [TestClass]
    public class TestAllSpellDescs
    {
        [TestMethod]
        public void JustDontCrash()
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "SpellDescs.txt")))
            {
                Assert.Inconclusive("Unable to find SpellDescs.txt");
                return;
            }

            foreach (var line in File.ReadAllLines("SpellDescs.txt"))
            {
                var parser = new SpellDescParser(line);
                //Console.WriteLine("--------");
                parser.Parse();
                //Console.WriteLine(parser.root.ToString());
            }
        }
    }
}
