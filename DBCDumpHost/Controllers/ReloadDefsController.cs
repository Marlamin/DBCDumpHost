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

        public ReloadDefsController(IDBDProvider dbdProvider)
        {
            this.dbdProvider = dbdProvider as DBDProvider;
        }


        // GET: api/ReloadDefs
        [HttpGet]
        public string Get()
        {
            int count = dbdProvider.LoadDefinitions();
            return "Reloaded " + count + " definitions!";
        }
    }
}
