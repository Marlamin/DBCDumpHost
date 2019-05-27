using DBCD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DBCDumpHost.Services
{
    public interface IDBCManager
    {
        IDBCDStorage GetOrLoad(string name, string build);
    }
}
