using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BrilliantSkies.Ui.Special.PopUps;
using BrilliantSkies.Ui.Tips;
using FtdModManager.GitHub;
using GitSharp;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace FtdModManager
{
    public class ModUpdateInfo
    {
        public bool isUpdateAvailable = false;

        public string updateTitle;
        public string updateMessage;
        public string updateVersion;
        public List<Exception> exceptions = new List<Exception>();

        public string[] removedFiles;
        public string[] removedDirectories;
        public string[] newDirectories;

        public GHTree.Item[] changedFiles;
        public GHTree.Item[] newFiles;
        public long downloadSize;

        public readonly ModData modData;
        public readonly ModPreferences pref;
        public readonly string modName;
        public readonly string basePath;

        public const string tempExtension = ".modmanager_temp";
        // should return "connected"
        public const string connectionCheckUrl = "https://gist.githubusercontent.com/Why7090/9f67ee70a3bba136785fe4d2bece6363/raw/check.txt";

        private string treeUrl;

        public ModUpdateInfo(ModData modData, ModPreferences modPreferences)
        {
            this.modData = modData;
            pref = modPreferences;
            modName = modPreferences.mod.Header.ComponentId.Name;
            basePath = modPreferences.mod.Header.ModDirectoryWithSlash.NormalizedDirPath();
        }

        public async Task CheckAndPrepareUpdate()
        {
            if (!await CheckInternetConnection())
            {
                Log("No Internet connection!");
                isUpdateAvailable = false;
                return;
            }
            try
            {
                await CheckUpdate();

                if (!isUpdateAvailable)
                    return;

                Log($"Update found for {modName} : {updateVersion}");

                await PrepareUpdate();

                if (!isUpdateAvailable)
                    return;
            }
            catch (Exception e)
            {
                LogException(e);
                Log($"Update detection failed for {modName}");
                isUpdateAvailable = false;
                return;
            }
        }

        public async Task PrepareUpdate()
        {
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
                    .Where(x => !rules.IgnoreFile(basePath, x)).Select(StringExtensions.NormalizedFilePath).ToArray();

                folders = Directory.EnumerateDirectories(basePath, "*", SearchOption.AllDirectories)
                    .Where(x => !rules.IgnoreDir(basePath, x)).Select(StringExtensions.NormalizedDirPath).ToArray();
            }

            Log($"Downloading tree JSON: {treeUrl}");
            string data;
            try
            {
                data = await DownloadStringAsync(treeUrl);
            }
            catch (Exception e)
            {
                isUpdateAvailable = false;
                Log("Error while downloading tree!");
                LogException(e);
                exceptions.Add(e);
                return;
            }
            Log("Downloaded tree JSON");

            var tree = JsonConvert.DeserializeObject<GHTree>(data).tree;
            foreach (var item in tree)
            {
                item.localPath = Path.Combine(basePath, item.path).NormalizedPath(item.type == GHTree.ItemType.blob);
            }

            string[] remoteDirectories = tree.Where(x => x.type == GHTree.ItemType.tree).Select(x => x.localPath).ToArray();
            var remoteFiles = tree.Where(x => x.type == GHTree.ItemType.blob).ToArray();
            var filesToDownload = remoteFiles.Where(x => !CompareSHA(x.localPath, x.sha)).ToArray();

            changedFiles = IntersectBlobsWithFiles(filesToDownload, files).ToArray();
            newFiles = filesToDownload.Except(changedFiles).ToArray();
            removedFiles = files.Except(remoteFiles.Select(x => x.localPath), new SamePath()).ToArray();

            removedDirectories = folders.Except(remoteDirectories, new SamePath()).Distinct(new SubPath()).ToArray();
            newDirectories = remoteDirectories.Except(folders, new SamePath()).ToArray();

            downloadSize = filesToDownload.Sum(x => (long)x.size);
        }

        public async Task ApplyUpdate()
        {
            foreach (string file in removedFiles.Concat(changedFiles.Select(x => x.localPath)))
            {
                try
                {
                    try
                    {
                        File.Delete(file);
                        Log($"Deleted file: {file}");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        File.Move(file, file + tempExtension);
                        Log($"Renamed locked file: {file}");
                    }
                }
                catch (Exception e)
                {
                    LogException(e);
                    exceptions.Add(e);
                }
            }

            foreach (string dir in newDirectories)
            {
                Directory.CreateDirectory(dir);
                Log($"Created directory {dir}");
            }

            var tasks = changedFiles.Concat(newFiles).Select(
                x => DownloadToFileAsync(string.Format(modData.fileUrlTemplate, updateVersion, x.path), x.localPath));

            Log($"Downloading files");
            await Task.WhenAll(tasks);
            var errors = tasks.Where(x => x.IsFaulted).Select(x => x.Exception);
            bool completedWithError = false;
            foreach (var e in errors)
            {
                LogException(e);
                completedWithError = true;
            }
            Log(completedWithError ? "Error while downloading files!" : "Downloaded all files successfully");
            
            foreach (string dir in removedDirectories)
            {
                if (!Directory.Exists(dir)) continue;
                try
                {
                    Directory.Delete(dir, true);
                    Log($"Deleted directory: {dir}");
                }
                catch (Exception e)
                {
                    LogException(e);
                    exceptions.Add(e);
                }
            }
        }

        public void ConfirmUpdate(Task task = null)
        {
            Log("Mod Update Available");
            if (isUpdateAvailable)
            {
                GuiPopUp.Instance.Add(
                    new PopupMultiButton(
                        GetConfirmationTitle(),
                        GetConfirmationMessage(),
                        false
                    )
                    .AddButton("<b>Update now</b>", x => ApplyUpdate().ContinueWith(AlertUpdateCompletion))
                    .AddButton("Remind me next time", toolTip: new ToolTip("Cancel update"))
                    .AddButton("Copy to clipboard", x => GUIUtility.systemCopyBuffer = x.message, false)
                );
            }
        }

        public void AlertUpdateCompletion(Task task = null)
        {
            GuiPopUp.Instance.Add(
                new PopupMultiButton(
                    GetFinishTitle(),
                    GetFinishMessage(),
                    false
                )
                .AddButton("Continue")
                .AddButton("Copy to clipboard", x => GUIUtility.systemCopyBuffer = x.message, false)
            );
        }

        public string GetConfirmationTitle()
        {
            if (!isUpdateAvailable)
                return $"No update available for {modName}";

            return $"Update found for : {modName}";
        }

        public string GetConfirmationMessage()
        {
            if (!isUpdateAvailable)
                return "";

            var str = new StringBuilder();
            str.AppendLine($"<b>New version</b> : \n{updateVersion}");
            str.AppendLine();

            str.AppendLine($"<b>Update title</b> : \n{updateTitle}");
            str.AppendLine();

            if (!string.IsNullOrEmpty(updateMessage))
                str.AppendLine($"<b>Update description</b> : \n{updateMessage}");

            str.AppendLine();
            str.AppendLine();

            str.AppendLine($"<b>Total size of data to download</b> : \n{GetBytesReadable(downloadSize)}");
            str.AppendLine();

            str.AppendLine($"<b>Mod installation path</b> : \n{basePath}");
            str.AppendLine();
            str.AppendLine();

            if (newFiles.Length > 0)
            {
                str.AppendLine("<b>New files</b> :");
                foreach (var file in newFiles)
                    str.AppendLine(file.localPath.PathRelativeTo(basePath));
            }

            str.AppendLine();

            if (changedFiles.Length > 0)
            {
                str.AppendLine("<b>Changed files</b> :");
                foreach (var file in changedFiles)
                    str.AppendLine(file.localPath.PathRelativeTo(basePath));
            }

            str.AppendLine();

            if (removedFiles.Length > 0)
            {
                str.AppendLine("<b>Removed files</b> :");
                foreach (string path in removedFiles)
                    str.AppendLine(path.PathRelativeTo(basePath));
            }

            str.AppendLine();

            if (newDirectories.Length > 0)
            {
                str.AppendLine("<b>New directories</b> :");
                foreach (string path in newDirectories)
                    str.AppendLine(path.PathRelativeTo(basePath));
            }

            str.AppendLine();

            if (removedDirectories.Length > 0)
            {
                str.AppendLine("<b>Removed directories</b> :");
                foreach (string path in removedDirectories)
                    str.AppendLine(path.PathRelativeTo(basePath));
            }
            
            return str.ToString();
        }

        public string GetFinishTitle()
        {
            if (exceptions.Count > 0)
                return $"Update completed with error : {modName}";

            return $"Update successful : {modName}";
        }

        public string GetFinishMessage()
        {
            if (exceptions.Count > 0)
                return string.Join("\n\n", "Exceptions :\n", exceptions.Select(x => x.ToString()));

            return "";
        }

        public async Task CheckUpdate()
        {
            try
            {
                switch (pref.updateType)
                {
                    case ModPreferences.UpdateType.LatestCommit:
                        await CheckUpdateLatestCommit();
                        break;
                    case ModPreferences.UpdateType.LatestRelease:
                        await CheckUpdateLatestRelease();
                        break;
                    default:
                        isUpdateAvailable = false;
                        break;
                }
            }
            catch (Exception e)
            {
                ModManager.LogException(e);
                isUpdateAvailable = false;
            }
        }

        public async Task CheckUpdateLatestCommit()
        {
            ModManager.Log($"Checking update for {modName}");
            string data = await ModManager.DownloadStringAsync(modData.latestCommitUrl);
            var commit = JsonConvert.DeserializeObject<GHCommit>(data);

            updateTitle = commit.commit.message;
            updateMessage = "";

            updateVersion = commit.sha;
            isUpdateAvailable = updateVersion != pref.localVersion;
            treeUrl = string.Format(modData.recursiveTreeUrlTemplate, commit.sha);
        }

        public async Task CheckUpdateLatestRelease()
        {
            ModManager.Log($"Checking update for {modName}");
            string releaseData = await ModManager.DownloadStringAsync(modData.latestReleaseUrl);
            var release = JsonConvert.DeserializeObject<GHRelease>(releaseData);
            string tagName = release.tag_name;

            string tagData = await ModManager.DownloadStringAsync(string.Format(modData.tagUrlTemplate, tagName));
            var tag = JsonConvert.DeserializeObject<GHTag>(tagData);

            updateTitle = tagName;
            updateMessage = release.body;

            updateVersion = tag.commit.sha;
            isUpdateAvailable = updateVersion != pref.localVersion;
            treeUrl = string.Format(modData.recursiveTreeUrlTemplate, tag.commit.sha);
        }
        

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

        public static async Task<bool> CheckInternetConnection()
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            string check;
            try
            {
                check = await DownloadStringAsync(connectionCheckUrl);
            }
            catch (Exception e)
            {
                LogException(e);
                check = "";
            }
            stopWatch.Stop();
            Log($"Time took to download check.txt : {stopWatch.ElapsedMilliseconds} ms");
            return check == "connected";
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

        public static void LogException(Exception e)
        {
#if (DEBUG)
            Console.WriteLine(e.ToString());
#else
            Debug.LogException(e);
#endif
        }

        /// <summary>
        /// Returns the human-readable file size for an arbitrary, 64-bit file size.<para/>
        /// The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB".
        /// </summary>
        public static string GetBytesReadable(long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.### ") + suffix;
        }


        internal static IEnumerable<GHTree.Item> IntersectBlobsWithFiles(IEnumerable<GHTree.Item> items, IEnumerable<string> files)
        {
            var comparer = new SamePath();
            return items.Where(x => files.Contains(x.localPath, comparer));
        }
    }
}
