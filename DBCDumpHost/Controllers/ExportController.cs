using DBCD;
using DBCDumpHost.Services;
using DBCDumpHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Collections.Generic;
using System.Net;

namespace DBCDumpHost.Controllers
{
    using Parameters = IReadOnlyDictionary<string, string>;

    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private static readonly Parameters DefaultParameters = new Dictionary<string, string>();
        private static readonly char[] QuoteableChars = new char[] { ',', '"', '\r', '\n' };

        private readonly DBCManager dbcManager;

        public ExportController(IDBCManager dbcManager)
        {
            this.dbcManager = dbcManager as DBCManager;
        }

        private async Task<byte[]> GenerateCSVStream(IDBCDStorage storage, Parameters parameters, bool newLinesInStrings = true)
        {
            if (storage.AvailableColumns.Length == 0)
            {
                throw new Exception("No columns found!");
            }

            // NOTE: if newLinesInStrings is obsolete then use StringToCSVCell in ctor
            Func<string, string> formatter = newLinesInStrings switch
            {
                true => StringToCSVCell,
                _ => StringToCSVCellSingleLine
            };

            var viewFilter = new DBCViewFilter(storage, parameters, formatter);

            using var exportStream = new MemoryStream();
            using var exportWriter = new StreamWriter(exportStream);

            // write header
            await exportWriter.WriteLineAsync(string.Join(",", GetColumnNames(storage)));

            // write records
            foreach (var item in viewFilter.GetRecords())
                await exportWriter.WriteLineAsync(string.Join(",", item));

            exportWriter.Flush();

            return exportStream.ToArray();
        }

        [Route("")]
        [Route("csv")]
        [HttpGet, HttpPost]
        public async Task<ActionResult> ExportCSV(string name, string build, bool useHotfixes = false, bool newLinesInStrings = true, LocaleFlags locale = LocaleFlags.All_WoW)
        {
            Logger.WriteLine("Exporting DBC " + name + " as CSV for build " + build + " and locale " + locale);

            var parameters = DefaultParameters;

            if (Request.Method == "POST")
                parameters = Request.Form.ToDictionary(x => x.Key, x => (string)x.Value);

            try
            {
                var storage = await GetStorage(name, build, useHotfixes, locale);

                if (storage.Count == 0)
                {
                    return NoContent();
                }

                return new FileContentResult(await GenerateCSVStream(storage, parameters, newLinesInStrings), "application/octet-stream")
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
                            var storage = await GetStorage(cleanName, build, useHotfixes, locale);

                            using (var exportStream = new MemoryStream(await GenerateCSVStream(storage, DefaultParameters, newLinesInStrings)))
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

        [Route("db2")]
        [HttpGet]
        public async Task<ActionResult> GetDB2ByTableName(string tableName, string fullBuild)
        {
            var provider = new DBCProvider();
            
            Logger.WriteLine("Serving DB2 \"" + tableName + "\" for build " + fullBuild);

            try
            {
                var extension = "";
                
                string fileName = Path.Combine(SettingManager.dbcDir, fullBuild, "dbfilesclient", $"{tableName}.db2");

                if (System.IO.File.Exists(fileName))
                {
                    extension = "db2";
                }
                else
                {
                    fileName = Path.ChangeExtension(fileName, ".dbc");
                    
                    if (!System.IO.File.Exists(fileName))
                        throw new FileNotFoundException($"Unable to find {tableName}");

                    extension = "dbc";
                }

                using (var stream = provider.StreamForTableName(tableName, fullBuild))
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    return new FileContentResult(ms.ToArray(), "application/octet-stream")
                    {
                        FileDownloadName = Path.GetFileName(tableName.ToLower() + "." + extension)
                    };
                }
            }
            catch (FileNotFoundException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Logger.WriteLine("Table " + tableName + " not found for build " + fullBuild);
                Console.ResetColor();
                return NotFound();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Logger.WriteLine("Error " + e.Message + " occured when serving file " + tableName + " for build " + fullBuild);
                Console.ResetColor();
            }

            return NotFound();
        }
        private async Task<IDBCDStorage> GetStorage(string name, string build, bool useHotfixes = false, LocaleFlags locale = LocaleFlags.All_WoW)
        {
            return await dbcManager.GetOrLoad(name, build, useHotfixes, locale);
        }

        private IEnumerable<string> GetColumnNames(IDBCDStorage storage)
        {
            var record = storage.Values.FirstOrDefault();

            if (record == null)
                yield break;

            for (var i = 0; i < storage.AvailableColumns.Length; ++i)
            {
                var name = storage.AvailableColumns[i];

                if (record[name] is Array array)
                {
                    // explode arrays by suffixing the ordinal
                    for (var j = 0; j < array.Length; j++)
                        yield return name + $"[{j}]";
                }
                else
                {
                    yield return name;
                }
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

        private static string StringToCSVCell(string str)
        {
            var mustQuote = str.IndexOfAny(QuoteableChars) > -1;
            if (mustQuote)
                return '"' + str.Replace("\"", "\"\"") + '"';

            return str;
        }

        private static string StringToCSVCellSingleLine(string str)
        {
            return StringToCSVCell(str.Replace("\n", "").Replace("\r", ""));
        }
    }
}
