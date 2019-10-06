using DBCDumpHost.Utils;
using DBFileReaderLib;
using System;
using System.IO;
namespace DBCDumpHost.Services
{
    public static class HotfixManager
    {
        public static HotfixReader hotfixReader;

        static HotfixManager()
        {
            LoadCaches();
        }

        private static void LoadCaches()
        {
            Logger.WriteLine("Loading hotfixes..");
            hotfixReader = new HotfixReader("DBCache.bin");
            hotfixReader.CombineCaches(Directory.GetFiles("caches", "*.bin"));
            Logger.WriteLine("Loaded hotfixes!");
        }

        public static void AddCache(MemoryStream cache)
        {
            var filename = Path.Combine("caches", "DBCache-" + ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() + "-" + DateTime.Now.Millisecond + ".bin");
            using (var stream = File.Create(filename))
            {
                cache.CopyTo(stream);
            }
            LoadCaches();
        }
    }
}
