using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace CMS.Utility
{
    /// <summary>
    /// Static class used for logging data. For logging to work, TRACE needs to be defined.
    /// </summary>
    public static class Logger
    {
        private static readonly string _file = $"AI_Log/log_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture)}";

        static Logger()
        {
            new FileInfo(_file).Directory.Create();
        }

        [Conditional("TRACE")]
        public static void Log(string msg)
        {
            /*using (var logFile = new StreamWriter(_file, true))
            {
                logFile.WriteLine(msg);
            }*/
        }
    }
}
