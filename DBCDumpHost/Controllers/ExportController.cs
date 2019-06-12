using DBCD;
using DBCDumpHost.Services;
using DBCDumpHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace DBCDumpHost.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private readonly DBCManager dbcManager;

        public ExportController(IDBCManager dbcManager)
        {
            this.dbcManager = dbcManager as DBCManager;
        }

        [Route("")]
        [Route("csv")]
        [HttpGet]
        public ActionResult ExportCSV(string name, string build)
        {
            Logger.WriteLine("Exporting DBC " + name + " for build " + build);
            try
            {
                var storage = dbcManager.GetOrLoad(name, build);
                if (!storage.Values.Any())
                {
                    throw new Exception("No rows found!");
                }

                var headerWritten = false;

                using (var exportStream = new MemoryStream())
                using (var exportWriter = new StreamWriter(exportStream))
                {
                    foreach (DBCDRow item in storage.Values)
                    {
                        // Write CSV header
                        if (!headerWritten)
                        {
                            for (var j = 0; j < storage.AvailableColumns.Length; ++j)
                            {
                                string fieldname = storage.AvailableColumns[j];
                                var field = item[fieldname];

                                var isEndOfRecord = j == storage.AvailableColumns.Length - 1;

                                if (field is Array a)
                                {
                                    for (var i = 0; i < a.Length; i++)
                                    {
                                        var isEndOfArray = a.Length - 1 == i;

                                        exportWriter.Write($"{fieldname}[{i}]");
                                        if (!isEndOfArray)
                                            exportWriter.Write(",");
                                    }
                                }
                                else
                                {
                                    exportWriter.Write(fieldname);
                                }

                                if (!isEndOfRecord)
                                    exportWriter.Write(",");
                            }
                            headerWritten = true;
                            exportWriter.WriteLine();
                        }

                        for (var i = 0; i < storage.AvailableColumns.Length; ++i)
                        {
                            var field = item[storage.AvailableColumns[i]];

                            var isEndOfRecord = i == storage.AvailableColumns.Length - 1;

                            if (field is Array a)
                            {
                                for (var j = 0; j < a.Length; j++)
                                {
                                    var isEndOfArray = a.Length - 1 == j;
                                    exportWriter.Write(a.GetValue(j));

                                    if (!isEndOfArray)
                                        exportWriter.Write(",");
                                }
                            }
                            else
                            {
                                var value = field;
                                if (value.GetType() == typeof(string))
                                    value = StringToCSVCell((string)value);

                                exportWriter.Write(value);
                            }

                            if (!isEndOfRecord)
                                exportWriter.Write(",");
                        }

                        exportWriter.WriteLine();
                    }

                    exportWriter.Dispose();

                    return new FileContentResult(exportStream.ToArray(), "application/octet-stream")
                    {
                        FileDownloadName = Path.ChangeExtension(name, ".csv")
                    };
                }
            }
            catch (FileNotFoundException e)
            {
                Logger.WriteLine("DBC " + name + " for build " + build + " not found: " + e.Message);
                return NotFound();
            }
            catch (Exception e)
            {
                Logger.WriteLine("Error during CSV generation for DBC " + name + " for build " + build + ": " + e.Message);
                return BadRequest();
            }
        }

        public static string StringToCSVCell(string str)
        {
            var mustQuote = (str.Contains(",") || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"));
            if (mustQuote)
            {
                var sb = new StringBuilder();
                sb.Append("\"");
                foreach (var nextChar in str)
                {
                    sb.Append(nextChar);
                    if (nextChar == '"')
                        sb.Append("\"");
                }
                sb.Append("\"");
                return sb.ToString();
            }

            return str;
        }
    }
}
