using System;
using System.IO;

namespace DBCDTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var dbcProvider = new DBCProvider();
            var dbdProvider = new DBDProvider();
            if(args.Length > 0)
            {
                foreach (var file in Directory.GetFiles(Path.Combine(SettingManager.dbcDir, args[0], "dbfilesclient"), "*.*", SearchOption.AllDirectories))
                {
                    var db = Path.GetFileNameWithoutExtension(file);
                    try
                    {
                        Console.WriteLine();
                        Console.WriteLine(file.Replace(SettingManager.dbcDir + "\\", ""));
                        DBCD.DBCD storage = new DBCD.DBCD(dbcProvider, dbdProvider);
                        storage.Load(db, args[0]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + " " + e.StackTrace);
                    }
                }
            }
            else
            {
                foreach (var dir in Directory.GetDirectories(SettingManager.dbcDir))
                {
                    var build = dir.Replace(SettingManager.dbcDir + "\\", "");
                    foreach (var file in Directory.GetFiles(Path.Combine(dir, "dbfilesclient"), "*.*", SearchOption.AllDirectories))
                    {
                        var db = Path.GetFileNameWithoutExtension(file);
                        try
                        {
                            Console.WriteLine();
                            Console.WriteLine(file.Replace(SettingManager.dbcDir + "\\", ""));
                            DBCD.DBCD storage = new DBCD.DBCD(dbcProvider, dbdProvider);
                            storage.Load(db, build);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message + " " + e.StackTrace);
                        }
                    }
                }
            }
        }
    }
}
