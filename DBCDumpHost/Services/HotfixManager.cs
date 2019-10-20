using DBCDumpHost.Utils;
using DBFileReaderLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace DBCDumpHost.Services
{
    public static class HotfixManager
    {
        public static Dictionary<uint, HotfixReader> hotfixReaders = new Dictionary<uint, HotfixReader>();

        static HotfixManager()
        {
            LoadCaches();
        }

        public static Dictionary<uint, List<string>> GetHotfixDBsPerBuild()
        {
            var filesPerBuild = new Dictionary<uint, List<string>>();

            foreach (var file in Directory.GetFiles("caches", "*.bin"))
            {
                using (var stream = File.OpenRead(file))
                using (var bin = new BinaryReader(stream))
                {
                    bin.BaseStream.Position = 8;
                    var build = bin.ReadUInt32();
                    if (!filesPerBuild.ContainsKey(build))
                    {
                        filesPerBuild.Add(build, new List<string>());
                    }

                    filesPerBuild[build].Add(file);
                }
            }

            return filesPerBuild;
        }

        public static void LoadCaches()
        {
            Logger.WriteLine("Loading hotfixes..");

            var filesPerBuild = GetHotfixDBsPerBuild();

            foreach (var fileList in filesPerBuild)
            {
                hotfixReaders.Clear();
                hotfixReaders.Add(fileList.Key, new HotfixReader(fileList.Value[0]));
                hotfixReaders[fileList.Key].CombineCaches(fileList.Value.ToArray());
                Logger.WriteLine("Loaded " + fileList.Value.Count + " hotfix DBs for build " + fileList.Key + "!");
            }
        }

        public static void AddCache(MemoryStream cache, uint build, int userID)
        {
            var filename = Path.Combine("caches", "DBCache-" + build + "-" + userID + "-" + ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() + "-" + DateTime.Now.Millisecond + ".bin");
            using (var stream = File.Create(filename))
            {
                cache.CopyTo(stream);
            }
            LoadCaches();
        }
    }
}
