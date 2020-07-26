using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DBCD.Providers;
using DBCDumpHost.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DBCDumpHost.Controllers
{
    [Route("api/relations")]
    [ApiController]
    public class RelationshipController : ControllerBase
    {
        private readonly DBDProvider dbdProvider;

        public RelationshipController(IDBDProvider dbdProvider, IDBCManager dbcManager)
        {
            this.dbdProvider = dbdProvider as DBDProvider;
        }

        [HttpGet]
        public Dictionary<string, List<string>> Get()
        {
            return dbdProvider.GetAllRelations();
        }

        [HttpGet("{foreignColumn}")]
        public List<string> Get(string foreignColumn)
        {
            return dbdProvider.GetRelationsToColumn(foreignColumn);
        }
    }
}