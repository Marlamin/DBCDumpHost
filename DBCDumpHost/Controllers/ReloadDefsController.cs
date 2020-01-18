using DBCD.Providers;
using DBCDumpHost.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBCDumpHost.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReloadDefsController : ControllerBase
    {
        private readonly DBDProvider dbdProvider;
        private readonly DBCManager dbcManager;

        public ReloadDefsController(IDBDProvider dbdProvider, IDBCManager dbcManager)
        {
            this.dbdProvider = dbdProvider as DBDProvider;
            this.dbcManager = dbcManager as DBCManager;
        }

        // GET: api/ReloadDefs
        [HttpGet]
        public string Get()
        {
            int count = dbdProvider.LoadDefinitions();
            dbcManager.ClearCache();
            dbcManager.ClearHotfixCache();
            return "Reloaded " + count + " definitions and cleared DBC cache!";
        }
    }
}
