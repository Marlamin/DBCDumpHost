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

        // GET/POST: data/name
        [HttpGet("{name}"), HttpPost("{name}")]
        public DataTablesResult Get(string name, string build, int draw, int start, int length)
        {
            var searching = false;

            var parameters = new Dictionary<string, string>();

            if(Request.Method == "POST")
            {
                Console.WriteLine("POST: " + Request.Form.Count);
                // POST, what site uses
                foreach (var post in Request.Form)
                {
                    parameters.Add(post.Key, post.Value);
                }

                if (parameters.ContainsKey("draw"))
                    draw = int.Parse(parameters["draw"]);

                if (parameters.ContainsKey("start"))
                    start = int.Parse(parameters["start"]);

                if (parameters.ContainsKey("length"))
                    length = int.Parse(parameters["length"]);
            }
            else
            {
                Console.WriteLine("GET: " + Request.QueryString);
                // GET, backwards compatibility for scripts/users using this
                foreach (var get in Request.Query)
                {
                    parameters.Add(get.Key, get.Value);
                }
            }

            var searchValue = parameters["search[value]"];

            if (string.IsNullOrWhiteSpace(searchValue))
            {
                Logger.WriteLine("Serving data " + start + "," + length + " for dbc " + name + " (" + build + ") for draw " + draw);
            }
            else
            {
                searching = true;
                Logger.WriteLine("Serving data " + start + "," + length + " for dbc " + name + " (" + build + ") for draw " + draw + " with search " + searchValue);
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

                var filtering = false;
                var filters = new Dictionary<int, (string, bool)>();

                var siteColIndex = 0;
                if(storage.Values.Count > 0)
                {
                    DBCDRow firstItem = storage.Values.First();

                    for (var i = 0; i < storage.AvailableColumns.Length; ++i)
                    {
                        var field = firstItem[storage.AvailableColumns[i]];
                        if (field is Array a)
                        {
                            for (var j = 0; j < a.Length; j++)
                            {
                                if (parameters.ContainsKey("columns[" + siteColIndex + "][search][value]") && !string.IsNullOrWhiteSpace(parameters["columns[" + siteColIndex + "][search][value]"]))
                                {
                                    var searchVal = parameters["columns[" + siteColIndex + "][search][value]"];
                                    searching = true;
                                    filtering = true;
                                    if(searchVal.Length > 6 && searchVal.Substring(0, 6) == "exact:")
                                    {
                                        filters.Add(siteColIndex, (searchVal.Remove(0, 6), true));
                                    }
                                    else
                                    {
                                        filters.Add(siteColIndex, (searchVal, false));
                                    }
                                }

                                siteColIndex++;
                            }
                        }
                        else
                        {
                            if (parameters.ContainsKey("columns[" + siteColIndex + "][search][value]") && !string.IsNullOrWhiteSpace(parameters["columns[" + siteColIndex + "][search][value]"]))
                            {
                                var searchVal = parameters["columns[" + siteColIndex + "][search][value]"];
                                searching = true;
                                filtering = true;
                                if (searchVal.Length > 6 && searchVal.Substring(0, 6) == "exact:")
                                {
                                    filters.Add(siteColIndex, (searchVal.Remove(0, 6), true));
                                }
                                else
                                {
                                    filters.Add(siteColIndex, (searchVal, false));
                                }
                            }

                            siteColIndex++;
                        }
                    }
                }

                var resultCount = 0;
                foreach (DBCDRow item in storage.Values)
                {
                    siteColIndex = 0;

                    var rowList = new List<string>();
                    var matches = false;
                    var allMatch = true;

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
                                    if (filtering)
                                    {
                                        if (filters.ContainsKey(siteColIndex))
                                        {
                                            if (filters[siteColIndex].Item2)
                                            {
                                                if (val == filters[siteColIndex].Item1)
                                                {
                                                    matches = true;
                                                }
                                                else
                                                {
                                                    Console.WriteLine(val + " does not match " + filters[siteColIndex].Item1 + ", filtering out");
                                                    allMatch = false;
                                                }
                                            }
                                            else
                                            {
                                                if (val.Contains(filters[siteColIndex].Item1, StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    Console.WriteLine(val + " matches column query " + filters[siteColIndex].Item1);

                                                    matches = true;
                                                }
                                                else
                                                {
                                                    Console.WriteLine(val + " does not match " + filters[siteColIndex].Item1 + ", filtering out");
                                                    allMatch = false;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (val.Contains(searchValue, StringComparison.InvariantCultureIgnoreCase))
                                            matches = true;
                                    }
                                }

                                val = System.Web.HttpUtility.HtmlEncode(val);

                                rowList.Add(val);

                                siteColIndex++;
                            }
                        }
                        else
                        {
                            var val = field.ToString();
                            if (searching)
                            {
                                if (filtering)
                                {
                                    if (filters.ContainsKey(siteColIndex))
                                    {
                                        if (filters[siteColIndex].Item2)
                                        {
                                            if (val == filters[siteColIndex].Item1)
                                            {
                                                matches = true;
                                            }
                                            else
                                            {
                                                Console.WriteLine(val + " does not match " + filters[siteColIndex].Item1 + ", filtering out");
                                                allMatch = false;
                                            }
                                        }
                                        else
                                        {
                                            if (val.Contains(filters[siteColIndex].Item1, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                Console.WriteLine(val + " matches column query " + filters[siteColIndex].Item1);

                                                matches = true;
                                            }
                                            else
                                            {
                                                Console.WriteLine(val + " does not match " + filters[siteColIndex].Item1 + ", filtering out");
                                                allMatch = false;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (val.Contains(searchValue, StringComparison.InvariantCultureIgnoreCase))
                                        matches = true;
                                }
                            }

                            val = System.Web.HttpUtility.HtmlEncode(val);

                            rowList.Add(val);
                            siteColIndex++;
                        }
                    }

                    if (searching)
                    {
                        if (matches && allMatch)
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
