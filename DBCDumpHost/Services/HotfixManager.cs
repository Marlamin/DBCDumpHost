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

        public static Dictionary<uint, List<string>> GetHotfixDBsPerBuild(uint targetBuild = 0)
        {
            var filesPerBuild = new Dictionary<uint, List<string>>();

            foreach (var file in Directory.GetFiles("caches", "*.bin"))
            {
                using (var stream = File.OpenRead(file))
                using (var bin = new BinaryReader(stream))
                {
                    bin.BaseStream.Position = 8;
                    var build = bin.ReadUInt32();

                    // If only requesting files for 1 build, skip others
                    if (targetBuild != 0 && targetBuild != build)
                        continue;

                    if (!filesPerBuild.ContainsKey(build))
                    {
                        filesPerBuild.Add(build, new List<string>());
                    }

                    filesPerBuild[build].Add(file);
                }
            }

            return filesPerBuild;
        }

        public static void LoadCaches(uint targetBuild = 0)
        {
            var filesPerBuild = GetHotfixDBsPerBuild(targetBuild);

            if (targetBuild != 0)
            {
                Logger.WriteLine("Reloading hotfixes for build " + targetBuild + "..");
                hotfixReaders.Remove(targetBuild);
            }
            else
            {
                Logger.WriteLine("Reloading all hotfixes..");
                hotfixReaders.Clear();
            }

            foreach (var fileList in filesPerBuild)
            {
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
            LoadCaches(build);
        }
    }
}
