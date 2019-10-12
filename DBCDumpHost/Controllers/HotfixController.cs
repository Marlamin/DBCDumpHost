using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DBCDumpHost.Services;
using DBCDumpHost.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DBCDumpHost.Controllers
{
    [Route("api/cache")]
    [ApiController]
    public class HotfixController : ControllerBase
    {
        private bool CheckUserToken(string token)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var result = client.GetStringAsync("https://wow.tools/api.php?type=token&token=" + token).Result;
                    if(result == "1")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }catch(Exception e)
            {
                Logger.WriteLine("Error checking user token: " + e.Message);
                return false;
            }
        }

        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            if (!Request.Headers.ContainsKey("WT-UserToken") || !CheckUserToken(Request.Headers["WT-UserToken"]))
            {
                Logger.WriteLine("Got cache upload with unknown/bad usertoken, ignoring..");
                return Unauthorized();
            }

            Logger.WriteLine("Cache upload: " + files[0].Length);
            long size = files.Sum(f => f.Length);

            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    if (formFile.Length > 10485760)
                        return BadRequest("File too large!");

                    var filePath = Path.GetTempFileName();

                    using (var stream = new MemoryStream())
                    {
                        await formFile.CopyToAsync(stream);
                        ProcessCache(stream);
                    }
                }
            }

            return Ok();
        }

        [HttpGet]
        [Route("refresh")]
        public string Get()
        {
            HotfixManager.LoadCaches();
            return "Refreshed hotfixes!";
        }

        private void ProcessCache(MemoryStream stream)
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
                if(version != 7)
                {
                    Logger.WriteLine("Unsupported DBCache version: " + version);
                    return;
                }

                var build = bin.ReadUInt32();
                Logger.WriteLine("Cache is for build " + build);

                // TODO: Check SHA to prevent malformed data being used
                var sha = bin.ReadBytes(32);

                stream.Position = 0;
                HotfixManager.AddCache(stream, build);
            }
        }
    }
}