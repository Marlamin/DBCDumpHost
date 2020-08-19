using DBCD.Providers;
using DBCDumpHost.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DBCDumpHost.Controllers
{
    [Route("api/sectioninfo")]
    [ApiController]
    public class EncryptedSectionController : ControllerBase
    {
        private readonly DBDProvider dbdProvider;
        private readonly DBCManager dbcManager;

        public EncryptedSectionController(IDBDProvider dbdProvider, IDBCManager dbcManager)
        {
            this.dbdProvider = dbdProvider as DBDProvider;
            this.dbcManager = dbcManager as DBCManager;
        }

        [HttpGet]
        public async Task<string> Get(string name, string build, bool useHotfixes = false)
        {
            if (name == null || build == null)
            {
                return "Not enough variables";
            }

            var storage = await dbcManager.GetOrLoad(name, build, useHotfixes);
            if (!storage.Values.Any())
            {
                throw new Exception("No rows found!");
            }

            var returnString = "";
            foreach (var encryptedSection in storage.GetEncryptedSections())
            {
                returnString += encryptedSection.Key.ToString("X16") + " " + encryptedSection.Value + "\n";
            }

            return returnString;
        }
    }
}