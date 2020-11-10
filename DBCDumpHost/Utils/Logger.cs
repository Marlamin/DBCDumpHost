namespace DBCDumpHost.Utils
{
    using System;
    public static class Logger
    {
        public static void WriteLine(string line)
        {
            Console.WriteLine("[" + DateTime.Now + "] " + line);
        }
    }
}
