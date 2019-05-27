using DBCD;
using DBCDumpHost.Services;
using DBCDumpHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

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
            public List<(string, string)> values;
            public int offset;
        }

        // GET: peek/
        [HttpGet]
        public string Get()
        {
            return "No DBC selected!";
        }

        // GET: peek/name
        [HttpGet("{name}")]
        public PeekResult Get(string name, string build, string bc, string col, int val)
        {
            Logger.WriteLine("Serving foreign key row for " + name + "::" + col + " (" + build + "/" + bc + ") value " + val);

            var storage = dbcManager.GetOrLoad(name, build);

            if (!storage.Values.Any())
            {
                throw new Exception("No rows found!");
            }

            var result = new PeekResult();
            result.values = new List<(string, string)>();

            var offset = 0;
            var recordFound = false;
            foreach (DBCDRow item in storage.Values)
            {
                if (recordFound)
                    continue;

                offset++;

                for (var i = 0; i < storage.AvailableColumns.Length; ++i)
                {
                    string fieldName = storage.AvailableColumns[i];
                    var field = item[fieldName];

                    if (fieldName != col)
                        continue;

                    // Don't think FKs to arrays are possible, so only check regular value
                    if (field.ToString() == val.ToString())
                    {
                        for (var j = 0; j < storage.AvailableColumns.Length; ++j)
                        {
                            string subfieldName = storage.AvailableColumns[j];
                            var subfield = item[subfieldName];

                            if (subfield is Array a)
                            {
                                for (var k = 0; k < a.Length; k++)
                                {
                                    result.values.Add((subfieldName + "[" + k + "]", a.GetValue(k).ToString()));
                                }
                            }
                            else
                            {
                                result.values.Add((subfieldName, subfield.ToString()));
                            }
                        }

                        recordFound = true;
                    }
                }
            }

            result.offset = offset;

            return result;
        }
    }
}
