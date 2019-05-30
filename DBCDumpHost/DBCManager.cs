using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DB2FileReaderLib.NET;
using DBCDumpHost.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace DBCDumpHost
{
    public class DBCManager
    {
        private static MemoryCache Cache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 100 });
        private static ConcurrentDictionary<(string, string), SemaphoreSlim> Locks = new ConcurrentDictionary<(string, string), SemaphoreSlim>();

        public static IDictionary GetOrLoad(string name, string build)
        {
            IDictionary cachedDBC;

            if (!Cache.TryGetValue((name, build), out cachedDBC))
            {
                SemaphoreSlim mylock = Locks.GetOrAdd((name, build), k => new SemaphoreSlim(1, 1));

                mylock.Wait();

                try
                {
                    if (!Cache.TryGetValue((name, build), out cachedDBC))
                    {
                        // Key not in cache, load DBC
                        Logger.WriteLine("DBC " + name + " for build " + build + " is not cached, loading! (Cache currently at " + Cache.Count + " entries!)");
                        cachedDBC = LoadDBC(name, build);
                        Cache.Set((name, build), cachedDBC, new MemoryCacheEntryOptions().SetSize(1));
                    }
                }
                finally
                {
                    mylock.Release();
                }
            }

            return cachedDBC;
        }

        private static IDictionary LoadDBC(string name, string build)
        {
            if (name.Contains("."))
            {
                throw new Exception("Invalid DBC name!");
            }

            if (string.IsNullOrEmpty(build))
            {
                throw new Exception("No build given!");
            }

            // Find file
            var filename = "";

            foreach(var file in Directory.GetFiles(Path.Combine(SettingManager.dbcDir, build), "*.*", SearchOption.AllDirectories))
            {
                if(Path.GetFileNameWithoutExtension(file).ToLower() == name)
                {
                    filename = file;
                }
            }

            DBReader reader = new DBReader(filename);

            var rawType = DefinitionManager.CompileDefinition(filename, build, reader.LayoutHash);
            var generic = typeof(DBReader).GetMethod("GetRecords").MakeGenericMethod(rawType);
            var instance = (IDictionary)generic.Invoke(reader, null);

            return instance;
        }
    }
}
