using Microsoft.AspNetCore.Mvc;
using System;
using DBDiffer;
using DBCDumpHost.Utils;

namespace DBCDumpHost.Controllers
{
    [Route("api/diff")]
    [ApiController]
    public class DiffController : ControllerBase
    {
        public string Diff(string name, string build1, string build2)
        {
            if(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(build1) || string.IsNullOrEmpty(build2))
            {
                return "Invalid arguments! Require name, build1, build2";
            }

            Logger.WriteLine("Serving diff for " + name + " between " + build1 + " and " + build2);

            var dbc1 = DBCManager.GetOrLoad(name, build1);
            var dbc2 = DBCManager.GetOrLoad(name, build2);

            var comparer = new DBComparer(dbc1, dbc2);
            var diff = comparer.Diff(DiffType.WoWTools);

            return diff.ToJSONString();
        }
    }
}
