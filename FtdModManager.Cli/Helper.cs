using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FtdModManager.Cli
{
    public static class Helper
    {
        public static string userAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36";

        private static readonly HttpClient client = new HttpClient();

        static Helper()
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }

        public static Task<string> DownloadStringAsync(string url)
        {
            return client.GetStringAsync(url);
        }

        public static async Task DownloadToFileAsync(string url, string path)
        {
            using (var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            using (var response = await client.GetStreamAsync(url))
            {
                await response.CopyToAsync(file);
            }
        }

        public static void Log<T>(T message)
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
