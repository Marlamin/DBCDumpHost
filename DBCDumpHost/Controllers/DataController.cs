using DBCD;
using DBCD.Providers;
using DBCDumpHost.Services;
using DBCDumpHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBCDumpHost.Controllers
{
    [Route("api/data")]
    [ApiController]
    public class DataController : ControllerBase
    {
        public struct DataTablesResult
        {
            public int draw;
            public int recordsFiltered;
            public int recordsTotal;
            public List<List<string>> data;
            public string error;
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

        // GET: data/name
        [HttpGet("{name}")]
        public DataTablesResult Get(string name, string build, int draw, int start, int length)
        {
            var searching = false;
            var searchValue = Request.Query["search[value]"];

            if (string.IsNullOrWhiteSpace(searchValue))
            {
                Logger.WriteLine("Serving data " + start + "," + length + " for dbc " + name + " (" + build + ") for draw " + draw);
            }
            else
            {
                searching = true;
                Logger.WriteLine("Serving data " + start + "," + length + " for dbc " + name + " (" + build + ") for draw " + draw + " with filter " + searchValue);
            }

            var result = new DataTablesResult
            {
                draw = draw
            };

            try
            {
                var storage = dbcManager.GetOrLoad(name, build);

                if(storage == null)
                {
                    throw new Exception("Definitions for this DB/version combo not found in definition cache!");
                }

                result.recordsTotal = storage.Values.Count();

                result.data = new List<List<string>>();

                var resultCount = 0;
                foreach (DBCDRow item in storage.Values)
                {
                    var rowList = new List<string>();
                    var matches = false;

                    for (var i = 0; i < storage.AvailableColumns.Length; ++i)
                    {
                        var field = item[storage.AvailableColumns[i]];

                        if (field is Array a)
                        {
                            for (var j = 0; j < a.Length; j++)
                            {
                                var val = a.GetValue(j).ToString();
                                if (searching)
                                {
                                    if (val.Contains(searchValue, StringComparison.InvariantCultureIgnoreCase))
                                        matches = true;
                                }

                                rowList.Add(val);
                            }
                        }
                        else
                        {
                            var val = field.ToString();
                            if (searching)
                            {
                                if (val.Contains(searchValue, StringComparison.InvariantCultureIgnoreCase))
                                    matches = true;
                            }

                            rowList.Add(val);
                        }
                    }

                    if (searching)
                    {
                        if (matches)
                        {
                            resultCount++;
                            result.data.Add(rowList);
                        }
                    }
                    else
                    {
                        resultCount++;
                        result.data.Add(rowList);
                    }
                }

                result.recordsFiltered = resultCount;

                var takeLength = length;
                if ((start + length) > resultCount)
                {
                    takeLength = resultCount - start;
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
}
