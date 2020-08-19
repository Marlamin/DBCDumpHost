using DBCD;
using System.Threading.Tasks;

namespace DBCDumpHost.Services
{
    public interface IDBCManager
    {
        Task<IDBCDStorage> GetOrLoad(string name, string build);
    }
}
