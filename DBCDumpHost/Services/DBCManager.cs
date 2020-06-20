using DBCD;
using DBCD.Providers;
using DBCDumpHost.Controllers;
using DBCDumpHost.Utils;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DBCDumpHost.Services
{
    public class DBCManager : IDBCManager
    {
        private readonly DBDProvider dbdProvider;
        private readonly DBCProvider dbcProvider;

        private MemoryCache Cache;
        private ConcurrentDictionary<(string, string, bool), SemaphoreSlim> Locks;

        public DBCManager(IDBDProvider dbdProvider, IDBCProvider dbcProvider)
        {
            this.dbdProvider = dbdProvider as DBDProvider;
            this.dbcProvider = dbcProvider as DBCProvider;

            Cache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 350 });
            Locks = new ConcurrentDictionary<(string, string, bool), SemaphoreSlim>();
        }

        public async Task<IDBCDStorage> GetOrLoad(string name, string build)
        {
            return await GetOrLoad(name, build, false);
        }

        public async Task<IDBCDStorage> GetOrLoad(string name, string build, bool useHotfixes = false, LocaleFlags locale = LocaleFlags.All_WoW)
        {
            if (locale != LocaleFlags.All_WoW)
            {
                return LoadDBC(name, build, useHotfixes, locale);
            }

            if (!Cache.TryGetValue((name, build, useHotfixes), out IDBCDStorage cachedDBC))
            {
                SemaphoreSlim mylock = Locks.GetOrAdd((name, build, useHotfixes), k => new SemaphoreSlim(1, 1));

                await mylock.WaitAsync();

                try
                {
                    if (!Cache.TryGetValue((name, build, useHotfixes), out cachedDBC))
                    {
                        // Key not in cache, load DBC
                        Logger.WriteLine("DBC " + name + " for build " + build + " (hotfixes: " + useHotfixes + ") is not cached, loading!");
                        cachedDBC = LoadDBC(name, build, useHotfixes);
                        Cache.Set((name, build, useHotfixes), cachedDBC, new MemoryCacheEntryOptions().SetSize(1));
                    }
                }
                finally
                {
                    mylock.Release();
                }
            }

            return cachedDBC;
        }

        private IDBCDStorage LoadDBC(string name, string build, bool useHotfixes = false, LocaleFlags locale = LocaleFlags.All_WoW)
        {
            if(locale != LocaleFlags.All_WoW)
            {
                dbcProvider.localeFlags = locale;
            }

            var dbcd = new DBCD.DBCD(dbcProvider, dbdProvider);
            var storage = dbcd.Load(name, build);

            dbcProvider.localeFlags = LocaleFlags.All_WoW;

            var splitBuild = build.Split('.');

            if (splitBuild.Length != 4)
            {
                throw new Exception("Invalid build!");
            }

            var buildNumber = uint.Parse(splitBuild[3]);

            if (useHotfixes)
            {
                if (!HotfixManager.hotfixReaders.ContainsKey(buildNumber))
                    HotfixManager.LoadCaches(buildNumber);

                if (HotfixManager.hotfixReaders.ContainsKey(buildNumber))
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
            // TODO: Only clear hotfix caches? :(
            Cache.Dispose();
            Cache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 350 });
        }
    }
}
