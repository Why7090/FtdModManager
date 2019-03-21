using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FtdModManager.Standalone
{
    public static class Helper
    {
        public static string userAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36";

        public static Task<string> DownloadStringAsync(string url)
        {
            using (var www = new WebClient())
            {
                www.Headers.Add(HttpRequestHeader.UserAgent, userAgent);
                return www.DownloadStringTaskAsync(url);
            }
        }

        public static Task DownloadToFileAsync(string url, string path)
        {
            using (var www = new WebClient())
            {
                www.Headers.Add(HttpRequestHeader.UserAgent, userAgent);
                return www.DownloadFileTaskAsync(url, path);
            }
        }

        public static void Log(string message)
        {
            Console.WriteLine(message);
        }

        public static void LogException(Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        public static void LogSeparator()
        {
            Log("=============================================");
        }
    }
}
