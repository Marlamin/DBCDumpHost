using DBCD;
using DBCDumpHost.Services;
using DBCDumpHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DBCDumpHost.Controllers
{
    [Route("api/peek")]
    [ApiController]
    public class PeekController : ControllerBase
    {
        private readonly DBCManager dbcManager;

        public PeekController(IDBCManager dbcManager)
        {
            this.dbcManager = dbcManager as DBCManager;
        }

        public struct PeekResult
        {
            public Dictionary<string, string> values { get; set; }
            public int offset { get; set; }
        }

        // GET: peek/
        [HttpGet]
        public string Get()
        {
            return "No DBC selected!";
        }

        // GET: peek/name
        [HttpGet("{name}")]
        public async Task<PeekResult> Get(string name, string build, string col, int val, bool useHotfixes = false, bool calcOffset = true)
        {
            Logger.WriteLine("Serving foreign key row for " + name + "::" + col + " (" + build + ", hotfixes: " + useHotfixes + ") value " + val);

            var storage = await dbcManager.GetOrLoad(name, build, useHotfixes);

            var result = new PeekResult();
            result.values = new Dictionary<string, string>();

            if (!storage.Values.Any())
            {
                return result;
            }

            var offset = 0;
            var recordFound = false;

            if(!calcOffset && col == "ID")
            {
                if (storage.TryGetValue(val, out DBCDRow row))
                {
                    for (var i = 0; i < storage.AvailableColumns.Length; ++i)
                    {
                        string fieldName = storage.AvailableColumns[i];

                        if (fieldName != col)
                            continue;

                        var field = row[fieldName];

                        // Don't think FKs to arrays are possible, so only check regular value
                        if (field.ToString() == val.ToString())
                        {
                            for (var j = 0; j < storage.AvailableColumns.Length; ++j)
                            {
                                string subfieldName = storage.AvailableColumns[j];
                                var subfield = row[subfieldName];

                                if (subfield is Array a)
                                {
                                    for (var k = 0; k < a.Length; k++)
                                    {
                                        result.values.Add(subfieldName + "[" + k + "]", a.GetValue(k).ToString());
                                    }
                                }
                                else
                                {
                                    result.values.Add(subfieldName, subfield.ToString());
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (DBCDRow row in storage.Values)
                {
                    if (recordFound)
                        continue;

                    offset++;

                    for (var i = 0; i < storage.AvailableColumns.Length; ++i)
                    {
                        string fieldName = storage.AvailableColumns[i];

                        if (fieldName != col)
                            continue;

                        var field = row[fieldName];

                        // Don't think FKs to arrays are possible, so only check regular value
                        if (field.ToString() == val.ToString())
                        {
                            for (var j = 0; j < storage.AvailableColumns.Length; ++j)
                            {
                                string subfieldName = storage.AvailableColumns[j];
                                var subfield = row[subfieldName];

                                if (subfield is Array a)
                                {
                                    for (var k = 0; k < a.Length; k++)
                                    {
                                        result.values.Add(subfieldName + "[" + k + "]", a.GetValue(k).ToString());
                                    }
                                }
                                else
                                {
                                    result.values.Add(subfieldName, subfield.ToString());
                                }
                            }

                            recordFound = true;
                        }
                    }
                }
            }

            result.offset = offset;

            return result;
        }
    }
}
