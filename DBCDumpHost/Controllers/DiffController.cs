using DBCDumpHost.Services;
using DBCDumpHost.Utils;
using DBDiffer;
using Microsoft.AspNetCore.Mvc;
using System.Collections;

namespace DBCDumpHost.Controllers
{
    [Route("api/diff")]
    [ApiController]
    public class DiffController : ControllerBase
    {
        private readonly DBCManager dbcManager;

        public DiffController(IDBCManager dbcManager)
        {
            this.dbcManager = dbcManager as DBCManager;
        }

        public string Diff(string name, string build1, string build2)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(build1) || string.IsNullOrEmpty(build2))
            {
                return "Invalid arguments! Require name, build1, build2";
            }

            Logger.WriteLine("Serving diff for " + name + " between " + build1 + " and " + build2);

            var dbc1 = dbcManager.GetOrLoad(name, build1) as IDictionary;
            var dbc2 = dbcManager.GetOrLoad(name, build2) as IDictionary;

            var comparer = new DBComparer(dbc1, dbc2);
            var diff = comparer.Diff(DiffType.WoWTools);

            return diff.ToJSONString();
        }
    }
}
