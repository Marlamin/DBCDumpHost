using DBCD.Providers;
using DBCDumpHost.Services;
using DBCDumpHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DBCDumpHost.Controllers
{
    [Route("api/data")]
    [ApiController]
    public class DataController : ControllerBase
    {
        public class DataTablesResult
        {
            public int draw { get; set; }
            public int recordsFiltered { get; set; }
            public int recordsTotal { get; set; }
            public List<string[]> data { get; set; }
            public string error { get; set; }
        }

        private readonly DBDProvider dbdProvider;
        private readonly DBCManager dbcManager;

        public DataController(IDBDProvider dbdProvider, IDBCManager dbcManager)
        {
            this.dbdProvider = dbdProvider as DBDProvider;
            this.dbcManager = dbcManager as DBCManager;
        }

        // GET: data/
        [HttpGet]
        public string Get()
        {
            return "No DBC selected!";
        }

        // GET/POST: data/name
        [HttpGet("{name}"), HttpPost("{name}")]
        public async Task<DataTablesResult> Get(CancellationToken cancellationToken, string name, string build, int draw, int start, int length, bool useHotfixes = false, LocaleFlags locale = LocaleFlags.All_WoW)
        {
            var parameters = new Dictionary<string, string>();

            if (Request.Method == "POST")
            {
                // POST, what site uses
                foreach (var post in Request.Form)
                    parameters.Add(post.Key, post.Value);

                if (parameters.ContainsKey("draw"))
                    draw = int.Parse(parameters["draw"]);

                if (parameters.ContainsKey("start"))
                    start = int.Parse(parameters["start"]);

                if (parameters.ContainsKey("length"))
                    length = int.Parse(parameters["length"]);
            }
            else
            {
                // GET, backwards compatibility for scripts/users using this
                foreach (var get in Request.Query)
                    parameters.Add(get.Key, get.Value);
            }

            if (!parameters.TryGetValue("search[value]", out var searchValue) || string.IsNullOrWhiteSpace(searchValue))
            {
                Logger.WriteLine("Serving data " + start + "," + length + " for dbc " + name + " (" + build + ") for draw " + draw);
            }
            else
            {
                Logger.WriteLine("Serving data " + start + "," + length + " for dbc " + name + " (" + build + ") for draw " + draw + " with search " + searchValue);
            }

            var result = new DataTablesResult
            {
                draw = draw
            };

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var storage = await dbcManager.GetOrLoad(name, build, useHotfixes, locale);

                if (storage == null)
                {
                    throw new Exception("Definitions for this DB and version combination not found in definition cache!");
                }

                result.recordsTotal = storage.Values.Count();
                result.data = new List<string[]>();

                if (storage.Values.Count == 0 || storage.AvailableColumns.Length == 0)
                    return result;

                var viewFilter = new DBCViewFilter(storage, parameters, WebUtility.HtmlEncode);

                result.data = viewFilter.GetRecords(cancellationToken).ToList();
                result.recordsFiltered = result.data.Count;

                var takeLength = length;
                if ((start + length) > result.recordsFiltered)
                {
                    takeLength = result.recordsFiltered - start;
                }

                // Temp hackfix: If requested count is higher than the amount of filtered records an error occurs and all rows are returned crashing tabs for large DBs.
                if(takeLength < 0)
                {
                    start = 0;
                    takeLength = 0;
                }

                result.data = result.data.GetRange(start, takeLength);
            }
            catch (Exception e)
            {
                Logger.WriteLine("Error occured during serving data: " + e.Message);
                result.error = e.Message.Replace(SettingManager.dbcDir, "");
            }

            return result;
        }
    }

    [Flags]
    public enum LocaleFlags : uint
    {
        All = 0xFFFFFFFF,
        None = 0,
        //Unk_1 = 0x1,
        enUS = 0x2,
        koKR = 0x4,
        //Unk_8 = 0x8,
        frFR = 0x10,
        deDE = 0x20,
        zhCN = 0x40,
        esES = 0x80,
        zhTW = 0x100,
        enGB = 0x200,
        enCN = 0x400,
        enTW = 0x800,
        esMX = 0x1000,
        ruRU = 0x2000,
        ptBR = 0x4000,
        itIT = 0x8000,
        ptPT = 0x10000,
        enSG = 0x20000000, // custom
        plPL = 0x40000000, // custom
        All_WoW = enUS | koKR | frFR | deDE | zhCN | esES | zhTW | enGB | esMX | ruRU | ptBR | itIT | ptPT
    }
}
