using DBCD.Providers;
using DBCDumpHost.Controllers;
using System;
using System.IO;
using System.Net.Http;

namespace DBCDumpHost.Services
{
    public class DBCProvider : IDBCProvider
    {
        public LocaleFlags localeFlags = LocaleFlags.All_WoW;

        public Stream StreamForTableName(string tableName, string build)
        {
            if (tableName.Contains("."))
                throw new Exception("Invalid DBC name!");

            if (string.IsNullOrEmpty(build))
                throw new Exception("No build given!");

            if (short.Parse(build[0].ToString()) > 5)
            {
                var ms = new MemoryStream();
                // Try CASC webservice
                try
                {
                    using (var client = new HttpClient())
                    {
                        var output = client.GetStreamAsync(SettingManager.cascToolHost + "/casc/file/db2?tableName=" + tableName + "&fullBuild=" + build + "&locale=" + localeFlags).Result;
                        output.CopyTo(ms);
                        ms.Position = 0;
                        return ms;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to retrieve DB2 from web CASC backend: " + e.Message);
                }
            }

            // Fall back to finding an on-disk version
            string fileName = Path.Combine(SettingManager.dbcDir, build, "dbfilesclient", $"{tableName}.db2");

            // if the db2 variant doesn't exist try dbc
            if (File.Exists(fileName))
                return new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            fileName = Path.ChangeExtension(fileName, ".dbc");

            // if the dbc variant doesn't exist throw
            if (!File.Exists(fileName))
                throw new FileNotFoundException($"Unable to find {tableName}");

            return new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
    }
}
