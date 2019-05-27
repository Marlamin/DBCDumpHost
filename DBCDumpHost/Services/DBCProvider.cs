using DBCD.Providers;
using System;
using System.IO;
using System.Linq;

namespace DBCDumpHost.Services
{
    public class DBCProvider : IDBCProvider
    {
        public string GetDBCFile(string tableName, string build)
        {
            if (tableName.Contains("."))
                throw new Exception("Invalid DBC name!");

            if (string.IsNullOrEmpty(build))
                throw new Exception("No build given!");

            // Find file
            var files = Directory.EnumerateFiles(Path.Combine(SettingManager.dbcDir, build), $"{tableName}.db*", SearchOption.AllDirectories);
            if (files.Any())
                return files.First();

            throw new FileNotFoundException($"Unable to find {tableName}");
        }

        public Stream StreamForTableName(string tableName)
        {
            return new FileStream(tableName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
    }
}
