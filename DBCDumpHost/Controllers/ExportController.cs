using DBCD;
using DBCDumpHost.Services;
using DBCDumpHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Collections.Generic;
using System.Net;

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

        private async Task<byte[]> GenerateCSVStream(string name, string build, bool useHotfixes = false, bool newLinesInStrings = true, LocaleFlags locale = LocaleFlags.All_WoW)
        {
            var storage = await dbcManager.GetOrLoad(name, build, useHotfixes, locale);
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
                                value = StringToCSVCell((string)value, newLinesInStrings);

                            exportWriter.Write(value);
                        }

                        if (!isEndOfRecord)
                            exportWriter.Write(",");
                    }

                    exportWriter.WriteLine();
                }

                exportWriter.Dispose();

                return exportStream.ToArray();
            }
        }

        [Route("")]
        [Route("csv")]
        [HttpGet]
        public async Task<ActionResult> ExportCSV(string name, string build, bool useHotfixes = false, bool newLinesInStrings = true, LocaleFlags locale = LocaleFlags.All_WoW)
        {
            Logger.WriteLine("Exporting DBC " + name + " for build " + build + " and locale " + locale);
            try
            {
                return new FileContentResult(await GenerateCSVStream(name, build, useHotfixes, newLinesInStrings, locale), "application/octet-stream")
                {
                    FileDownloadName = Path.ChangeExtension(name, ".csv")
                };
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

        private async Task<string[]> GetDBListForBuild(string build)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://wow.tools/api.php?type=dblist&build=" + build);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                var result = await reader.ReadToEndAsync();
                return result.Split(',');
            }
        }

        [Route("all")]
        [HttpGet]
        public async Task<ActionResult> ExportAllCSV(string build, bool useHotfixes = false, bool newLinesInStrings = true, LocaleFlags locale = LocaleFlags.All_WoW)
        {
            Logger.WriteLine("Exporting all DBCs build " + build + " and locale " + locale);

            using (var zip = new MemoryStream())
            {
                using (var archive = new ZipArchive(zip, ZipArchiveMode.Create))
                {
                    // TODO: Get list of DBCs for a specific build
                    foreach (var dbname in await GetDBListForBuild(build))
                    {
                        try
                        {
                            var cleanName = dbname.ToLower().Replace("dbfilesclient/", "").Replace(".db2", "");
                            using (var exportStream = new MemoryStream(await GenerateCSVStream(cleanName, build, useHotfixes, newLinesInStrings, locale)))
                            {
                                var entryname = cleanName + ".csv";
                                var entry = archive.CreateEntry(entryname);
                                using (var entryStream = entry.Open())
                                {
                                    exportStream.CopyTo(entryStream);
                                }
                            }
                        }
                        catch (FileNotFoundException)
                        {
                            Logger.WriteLine("Table " + dbname + " not found in build " + build);
                        }
                        catch (Exception e)
                        {
                            Logger.WriteLine("Error " + e.Message + " occured when getting table " + dbname + " of build " + build);
                        }
                    }
                }

                return new FileContentResult(zip.ToArray(), "application/octet-stream")
                {
                    FileDownloadName = "alldbc-" + build + ".zip"
                };
            }
        }

        public static string StringToCSVCell(string str, bool newLinesInStrings)
        {
            if (!newLinesInStrings)
            {
                str = str.Replace("\n", "").Replace("\r", "");
            }

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
