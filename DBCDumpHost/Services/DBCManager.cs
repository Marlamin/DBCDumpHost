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
        private MemoryCache HotfixCache;
        private ConcurrentDictionary<(string, string), SemaphoreSlim> Locks;
        private ConcurrentDictionary<(string, string), SemaphoreSlim> HotfixLocks;

        public DBCManager(IDBDProvider dbdProvider, IDBCProvider dbcProvider)
        {
            this.dbdProvider = dbdProvider as DBDProvider;
            this.dbcProvider = dbcProvider as DBCProvider;

            Cache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 350 });
            HotfixCache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 50 });
            Locks = new ConcurrentDictionary<(string, string), SemaphoreSlim>();
            HotfixLocks = new ConcurrentDictionary<(string, string), SemaphoreSlim>();
        }

        public IDBCDStorage GetOrLoad(string name, string build)
        {
            return GetOrLoad(name, build, false);
        }

        public IDBCDStorage GetOrLoad(string name, string build, bool useHotfixes = false)
        {
            ref ConcurrentDictionary<(string, string), SemaphoreSlim> targetLocks = ref Locks;
            ref MemoryCache targetCache = ref Cache;

            if (useHotfixes)
            {
                targetCache = ref HotfixCache;
                targetLocks = ref HotfixLocks;
            }

            if (!targetCache.TryGetValue((name, build), out IDBCDStorage cachedDBC))
            {
                SemaphoreSlim mylock = targetLocks.GetOrAdd((name, build), k => new SemaphoreSlim(1, 1));

                mylock.Wait();

                try
                {
                    if (!targetCache.TryGetValue((name, build), out cachedDBC))
                    {
                        // Key not in cache, load DBC
                        Logger.WriteLine("DBC " + name + " for build " + build + " (hotfixes: " + useHotfixes + ") is not cached, loading!");
                        cachedDBC = LoadDBC(name, build, useHotfixes);
                        targetCache.Set((name, build), cachedDBC, new MemoryCacheEntryOptions().SetSize(1));
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

            if (useHotfixes)
            {
                if(!HotfixManager.hotfixReaders.ContainsKey(buildNumber))
                    HotfixManager.LoadCaches(buildNumber);

                if(HotfixManager.hotfixReaders.ContainsKey(buildNumber))
                    storage = storage.ApplyingHotfixes(HotfixManager.hotfixReaders[buildNumber]);
            }

            return storage;
        }

        public void ClearCache()
        {
            Cache.Dispose();
            Cache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 350 });
        }

        public void ClearHotfixCache()
        {
            HotfixCache.Dispose();
            HotfixCache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 50 });
        }
    }
}
