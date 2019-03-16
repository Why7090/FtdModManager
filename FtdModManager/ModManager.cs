using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BrilliantSkies.Modding;
using FtdModManager.GitHub;
using GitSharp;
using Newtonsoft.Json;

#if !DEBUG
using UnityEngine;
using UnityEngine.Networking;
#endif

namespace FtdModManager
{
    public static class ModManager
    {
        public const string tempExtension = ".modmanager_temp";
        public const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.122 Safari/537.36 Vivaldi/2.3.1440.60";

        public static void RemoveTempFilesInDirectory(string dir)
        {
            foreach (string file in Directory.EnumerateFiles(dir, $"*{tempExtension}", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                    Log($"Deleted file: {file}");
                }
                catch (Exception e)
                {
                    LogException(e);
                }
            }
        }

        public static void RemoveTempFilesInMod(PluginMeta meta) =>
            RemoveTempFilesInDirectory(meta.dir);

        public static bool CompareSHA(string path, string hash)
        {
            if (!File.Exists(path))
            {
                return false;
            }
            
            byte[] content = File.ReadAllBytes(path);
            byte[] header = Encoding.ASCII.GetBytes($"blob {content.Length}\0");

            byte[] blob = new byte[header.Length + content.Length];
            Buffer.BlockCopy(header, 0, blob, 0, header.Length);
            Buffer.BlockCopy(content, 0, blob, header.Length, content.Length);

            using (var sha = new SHA1Managed())
            {
                hash = hash.ToLower();
                byte[] fileHash = sha.ComputeHash(blob);
                for (int i = 0; i < sha.Hash.Length; i++)
                {
                    if (fileHash[i].ToString("x2") != hash.Substring(2 * i, 2))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static async Task<string> DownloadModAsync(string recursiveTreeUrl, string basePath, string fileUrlTemplate)
        {
            var log = new ConcurrentQueue<string>();

            string[] files = { };
            string[] folders = { };

            if (Directory.Exists(basePath))
            {
                var rules = new IgnoreRules(new string[] { });
                string gitignore = Path.Combine(basePath, ".gitignore");
                if (File.Exists(gitignore))
                {
                    rules = new IgnoreRules(gitignore);
                }

                files = Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories)
                    .Where(x => !rules.IgnoreFile(basePath, x)).ToArray();

                folders = Directory.EnumerateDirectories(basePath, "*", SearchOption.AllDirectories)
                    .Where(x => !rules.IgnoreDir(basePath, x)).ToArray();
            }

            Log($"Downloading tree JSON: {recursiveTreeUrl}", log);
            string data = await DownloadStringAsync(recursiveTreeUrl);
            Log("Downloaded tree JSON", log);

            var tree = JsonConvert.DeserializeObject<GHTree>(data).tree;
            foreach (var item in tree)
            {
                item.localPath = Path.Combine(basePath, item.path);
            }

            string[] newFolders = tree.Where(x => x.type == GHTree.ItemType.tree).Select(x => x.localPath).ToArray();
            var newFiles = tree.Where(x => x.type == GHTree.ItemType.blob).ToArray();
            

            var filesToUpdate = newFiles.Where(x => !CompareSHA(x.localPath, x.sha));
            var filesToDelete = filesToUpdate.Select(x => x.localPath)
                .Intersect(files, new SamePath())
                .Concat(files.Except(newFiles.Select(x => x.localPath), new SamePath()));
            foreach (string file in filesToDelete)
            {
                try
                {
                    try
                    {
                        File.Delete(file);
                        Log($"Deleted file: {file}", log);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        File.Move(file, file + tempExtension);
                        Log($"Renamed locked file: {file}", log);
                    }
                }
                catch (Exception e)
                {
                    LogException(e, log);
                }
            }

            foreach (string folder in newFolders)
            {
                Directory.CreateDirectory(folder);
                Log($"Created directory {folder}", log);
            }
            
            var tasks = filesToUpdate.Select(x => DownloadToFileAsync(string.Format(fileUrlTemplate, x.path), x.localPath));
            Log($"Downloading files", log);
            await Task.WhenAll(tasks);
            Log("Downloaded all files", log);
            

            var foldersToDelete = folders.Except(newFolders, new SamePath());
            foreach (string folder in foldersToDelete)
            {
                if (!Directory.Exists(folder)) continue;
                try
                {
                    Directory.Delete(folder, true);
                    Log($"Deleted folder: {folder}", log);
                }
                catch (Exception e)
                {
                    LogException(e, log);
                }
            }

            return string.Join("\n", log);
        }

        public static async Task DownloadToFileAsync(string url, string path)
        {
            var www = UnityWebRequest.Get(url);
            www.downloadHandler = new DownloadHandlerFile(path);
            await www.SendWebRequest();
        }

        public static async Task<string> DownloadStringAsync(string url)
        {
            var www = UnityWebRequest.Get(url);
            await www.SendWebRequest();
            return www.downloadHandler.text;
        }

        public static void Log(string message)
        {
#if (DEBUG)
            Console.WriteLine("[ModManager] " + message);
#else
            Debug.Log("[ModManager] " + message);
#endif
        }

        public static void Log(string message, ConcurrentQueue<string> log)
        {
            Log(message);
            log.Enqueue("[ModManager] " + message);
        }

        public static void LogException(Exception e)
        {
#if (DEBUG)
            Console.WriteLine(e.ToString());
#else
            Debug.LogException(e);
#endif
        }

        public static void LogException(Exception e, ConcurrentQueue<string> log)
        {
            LogException(e);
            log.Enqueue("");
            log.Enqueue(log.ToString());
            log.Enqueue("");
        }
    }
}
