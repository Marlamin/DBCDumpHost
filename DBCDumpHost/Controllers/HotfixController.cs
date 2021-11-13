using DBCDumpHost.Services;
using DBCDumpHost.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DBCDumpHost.Controllers
{
    [Route("api/cache")]
    [ApiController]
    public class HotfixController : ControllerBase
    {
        private readonly DBCManager dbcManager;

        public HotfixController(IDBCManager dbcManager)
        {
            this.dbcManager = dbcManager as DBCManager;
        }

        private int GetUserIDByToken(string token)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var result = client.GetStringAsync("https://wow.tools/api.php?type=token&token=" + token).Result;
                    return int.Parse(result);
                }
            }
            catch (Exception e)
            {
                Logger.WriteLine("Error checking user token: " + e.Message);
                return 0;
            }
        }

        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            if (!Request.Headers.ContainsKey("WT-UserToken"))
            {
                Logger.WriteLine("No user token given!");
                return Unauthorized();
            }

            var userID = GetUserIDByToken(Request.Headers["WT-UserToken"]);

            if (userID == 0)
            {
                Logger.WriteLine("No user token given!");
                return Unauthorized();
            }

            Logger.WriteLine("Cache upload: " + files[0].Length);

            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    if (formFile.Length > 26214400) // 25MB
                        return BadRequest("File too large!");

                    var filePath = Path.GetTempFileName();

                    using (var stream = new MemoryStream())
                    {
                        await formFile.CopyToAsync(stream);
                        ProcessCache(stream, userID);
                    }
                }
            }

            return Ok();
        }

        [HttpPost]
        [Route("uploadzip")]
        public async Task<IActionResult> UploadZip(List<IFormFile> files)
        {
            if (!Request.Headers.ContainsKey("WT-UserToken"))
            {
                Logger.WriteLine("No user token given!");
                return Unauthorized();
            }

            var userID = GetUserIDByToken(Request.Headers["WT-UserToken"]);

            if (userID == 0)
            {
                Logger.WriteLine("No user token given!");
                return Unauthorized();
            }

            Logger.WriteLine("ZIP cache upload: " + files[0].Length);

            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    if (formFile.Length > 68157440) // 65 MB
                        return BadRequest("ZIP too large!");

                    var filePath = Path.GetTempFileName();

                    using (var fs = new FileStream(Path.Combine("zips", userID + "-" + ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() + "-" + DateTime.Now.Millisecond + ".zip"), FileMode.CreateNew))
                    using (var stream = new MemoryStream())
                    {
                        await formFile.CopyToAsync(stream);
                        await formFile.CopyToAsync(fs);

                        foreach (var file in new ZipArchive(stream).Entries)
                        {
                            // if file == dbcache
                            if (file.Name == "DBCache.bin")
                            {
                                using (var entryMs = new MemoryStream())
                                {
                                    file.Open().CopyTo(entryMs);
                                    ProcessCache(entryMs, userID);
                                }
                            }
                            else if (file.Name.EndsWith(".wdb"))
                            {
                                Logger.WriteLine("Got WDB file in ZIP: " + file.Name);
                                using (var entryMs = new MemoryStream())
                                {
                                    file.Open().CopyTo(entryMs);
                                    ProcessWDB(entryMs, userID);
                                }
                            }
                            else
                            {
                                Logger.WriteLine("Got unknown file in ZIP: " + file.Name);
                            }
                        }
                    }
                }
            }

            return Ok();
        }

        [HttpGet]
        [Route("refresh")]
        public string Get()
        {
            //HotfixManager.LoadCaches();
            return "NOT SUPPORTED";
        }

        private void ProcessCache(MemoryStream stream, int userID)
        {
            Logger.WriteLine("New cache of size " + stream.Length + " received!");
            stream.Position = 0;
            using (var bin = new BinaryReader(stream))
            {
                var xfthMagic = 'X' << 0 | 'F' << 8 | 'T' << 16 | 'H' << 24;

                if (bin.ReadUInt32() != xfthMagic)
                {
                    Logger.WriteLine("Invalid cache header!");
                    return;
                }

                var version = bin.ReadUInt32();
                if (version != 7 && version != 8)
                {
                    Logger.WriteLine("Unsupported DBCache version: " + version);
                    return;
                }

                var build = bin.ReadUInt32();
                Logger.WriteLine("Cache is for build " + build);

                // TODO: Check SHA to prevent malformed data being used
                var sha = bin.ReadBytes(32);

                stream.Position = 0;
                HotfixManager.AddCache(stream, build, userID);
            }

            dbcManager.ClearHotfixCache();
        }

        private void ProcessWDB(MemoryStream stream, int userID)
        {
            Logger.WriteLine("New WDB of size " + stream.Length + " received!");
            if (stream.Length < 33)
            {
                Logger.WriteLine("Skipping saving of WDB, is 32 bytes or less (empty)");
                return;
            }

            stream.Position = 0;
            using (var bin = new BinaryReader(stream))
            {
                var identifier = Encoding.ASCII.GetString(bin.ReadBytes(4).Reverse().ToArray());
                var clientBuild = bin.ReadUInt32();
                var clientLocale = Encoding.ASCII.GetString(bin.ReadBytes(4).Reverse().ToArray());
                var recordSize = bin.ReadUInt32();
                var recordVersion = bin.ReadUInt32();
                var formatVersion = bin.ReadUInt32();

                // Arbitrary sanity checking
                if (clientBuild < 34220 || clientBuild > 98765)
                {
                    Logger.WriteLine("Ignoring cache, invalid build (" + clientBuild + ")");
                    return;
                }

                if (clientLocale != "enUS")
                {
                    Logger.WriteLine("Ignoring cache, invalid locale (" + clientLocale + ")");
                    return;
                }

                var filename = "";

                switch (identifier)
                {
                    case "WMOB": // Creature
                        filename = "creaturecache";
                        break;
                    case "WGOB": // Gameobject
                        filename = "gameobjectcache";
                        break;
                    case "WPTX": // PageText
                        filename = "pagetextcache";
                        break;
                    case "WQST": // Quest
                        filename = "questcache";
                        break;
                    case "WNPC": // NPC
                        filename = "npccache";
                        break;
                    case "WPTN": // Petition
                        filename = "petitioncache";
                        break;
                    default:
                        Logger.WriteLine("Unknown cache file identifier: " + identifier);
                        break;
                }

                if (!string.IsNullOrEmpty(filename))
                {
                    stream.Position = 0;

                    var targetFilename = Path.Combine("caches", filename + "-" + clientBuild + "-" + userID + "-" + ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() + "-" + DateTime.Now.Millisecond + ".wdb");
                    using (var targetStream = System.IO.File.Create(targetFilename))
                    {
                        stream.CopyTo(targetStream);
                    }

                    Logger.WriteLine("Saved WDB to " + targetFilename);
                }
            }

            dbcManager.ClearHotfixCache();
        }
    }
}