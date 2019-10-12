using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DBCD;
using DBCD.Providers;
using DBCDumpHost.Utils;
using DBFileReaderLib;
using Microsoft.Extensions.Caching.Memory;

namespace DBCDumpHost.Services
{
    public class DBCManager : IDBCManager
    {
        private readonly DBDProvider dbdProvider;
        private readonly DBCProvider dbcProvider;

        private MemoryCache Cache;
        private ConcurrentDictionary<(string, string), SemaphoreSlim> Locks;

        public DBCManager(IDBDProvider dbdProvider, IDBCProvider dbcProvider)
        {
            this.dbdProvider = dbdProvider as DBDProvider;
            this.dbcProvider = dbcProvider as DBCProvider;

            Cache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 100 });
            Locks = new ConcurrentDictionary<(string, string), SemaphoreSlim>();
        }

        public IDBCDStorage GetOrLoad(string name, string build)
        {
            return GetOrLoad(name, build, false);
        }

        public IDBCDStorage GetOrLoad(string name, string build, bool useHotfixes = false)
        {
            if (useHotfixes)
            {
                return LoadDBC(name, build, useHotfixes);
            }

            if (!Cache.TryGetValue((name, build), out IDBCDStorage cachedDBC))
            {
                SemaphoreSlim mylock = Locks.GetOrAdd((name, build), k => new SemaphoreSlim(1, 1));

                mylock.Wait();

                try
                {
                    if (!Cache.TryGetValue((name, build), out cachedDBC))
                    {
                        // Key not in cache, load DBC
                        Logger.WriteLine("DBC " + name + " for build " + build + " is not cached, loading! (Cache currently at " + Cache.Count + " entries!)");
                        cachedDBC = LoadDBC(name, build, false);
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

        private IDBCDStorage LoadDBC(string name, string build, bool useHotfixes = false)
        {
            var dbcd = new DBCD.DBCD(dbcProvider, dbdProvider);
            var storage = dbcd.Load(name, build);
            var splitBuild = build.Split('.');

            if (splitBuild.Length != 4)
            {
                throw new Exception("Invalid build!");
            }

            var buildNumber = uint.Parse(splitBuild[3]);

            if (useHotfixes && HotfixManager.hotfixReaders.ContainsKey(buildNumber))
            {
                var countBefore = storage.Count;
                storage = storage.ApplyingHotfixes(HotfixManager.hotfixReaders[buildNumber]);
                var countAfter = storage.Count;

                Logger.WriteLine("Applied hotfixes to table " + name + ", count before = " + countBefore + ", after = " + countAfter);
            }
            
            return storage;
        }

        public void ClearCache()
        {
            Cache.Dispose();
            Cache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 100 });
        }
    }
}
