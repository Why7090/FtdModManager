using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitSharp;
using PetaJson;

namespace FtdModManager
{
    public abstract class AbstractModUpdateInfo
    {
        public bool isUpdateAvailable = false;

        public string updateTitle;
        public string updateMessage;
        public string updateVersion;
        public List<Exception> exceptions = new List<Exception>();

        public string[] removedFiles;
        public string[] removedDirectories;
        public string[] newDirectories;

        public GHTreeItem[] changedFiles;
        public GHTreeItem[] newFiles;
        public long downloadSize;

        public readonly ModManifest manifest;
        public readonly UpdateType updateType;
        public readonly string modName;
        public readonly string basePath;
        public readonly string localVersion;

        // should return "connected"
        public string connectionCheckUrl = "https://gist.githubusercontent.com/Why7090/9f67ee70a3bba136785fe4d2bece6363/raw/check.txt";

        public const string tempExtension = ".modmanager_temp";

        protected string treeUrl;


        protected AbstractModUpdateInfo(ModManifest manifest, string modName, string basePath, UpdateType updateType, string localVersion)
        {
            this.manifest = manifest;
            this.updateType = updateType;
            this.modName = modName;
            this.basePath = modName;
            this.localVersion = localVersion;
        }

        protected AbstractModUpdateInfo(string basePath)
        {
            this.basePath = basePath;
            manifest = Json.ParseFile<ModManifest>(Path.Combine(this.basePath, ModPreferences.manifestFileName));
            modName = new DirectoryInfo(basePath).Name;
            
            var pref = new ModPreferences(modName, this.basePath);
            localVersion = pref.localVersion;
            updateType = pref.updateType;
        }


        #region Virtual Methods

        public virtual async Task CheckAndPrepareUpdate()
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

        public virtual async Task PrepareUpdate()
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

            var tree = Json.Parse<GHTree>(data).tree;
            foreach (var item in tree)
            {
                item.localPath = Path.Combine(basePath, item.path).NormalizedPath(item.type == GHTreeItemType.blob);
            }

            string[] remoteDirectories = tree.Where(x => x.type == GHTreeItemType.tree).Select(x => x.localPath).ToArray();
            var remoteFiles = tree.Where(x => x.type == GHTreeItemType.blob).ToArray();
            var filesToDownload = remoteFiles.Where(x => !CompareSHA(x.localPath, x.sha)).ToArray();

            changedFiles = IntersectBlobsWithFiles(filesToDownload, files).ToArray();
            newFiles = filesToDownload.Except(changedFiles).ToArray();
            removedFiles = files.Except(remoteFiles.Select(x => x.localPath), new SamePath()).ToArray();

            removedDirectories = folders.Except(remoteDirectories, new SamePath()).Distinct(new SubPath()).ToArray();
            newDirectories = remoteDirectories.Except(folders, new SamePath()).ToArray();

            downloadSize = filesToDownload.Sum(x => (long)x.size);

            if (   newDirectories.Length == 0
                && removedDirectories.Length == 0
                && changedFiles.Length == 0
                && newFiles.Length == 0
                && removedFiles.Length == 0)
            {
                isUpdateAvailable = false;
            }
        }

        public virtual async Task ApplyUpdate()
        {
            foreach (string file in removedFiles.Concat(changedFiles.Select(x => x.localPath)))
            {
                try
                {
                    try
                    {
                        File.Delete(file);
                        Log($"Deleted file: {file.PathRelativeTo(basePath)}");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        File.Move(file, file + tempExtension);
                        Log($"Renamed locked file: {file.PathRelativeTo(basePath)}");
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
                Log($"Created directory {dir.PathRelativeTo(basePath)}");
            }

            Log($"Downloading files");
            var tasks = await DownloadFiles(changedFiles.Concat(newFiles));

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
                    Log($"Deleted directory: {dir.PathRelativeTo(basePath)}");
                }
                catch (Exception e)
                {
                    LogException(e);
                    exceptions.Add(e);
                }
            }
        }

        public virtual string GetConfirmationTitle()
        {
            if (!isUpdateAvailable)
                return $"No update available for {modName}";

            return $"Update found for : {modName}";
        }

        public virtual string GetConfirmationMessage()
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

        public virtual string GetFinishTitle()
        {
            if (exceptions.Count > 0)
                return $"Update completed with error : {modName}";

            return $"Update successful : {modName}";
        }

        public virtual string GetFinishMessage()
        {
            if (exceptions.Count > 0)
                return string.Join("\n\n", "Exceptions :\n", exceptions.Select(x => x.ToString()));

            return "";
        }

        public virtual async Task CheckUpdate()
        {
            try
            {
                switch (updateType)
                {
                    case UpdateType.LatestCommit:
                        await CheckUpdateLatestCommit();
                        break;
                    case UpdateType.LatestRelease:
                        await CheckUpdateLatestRelease();
                        break;
                    default:
                        isUpdateAvailable = false;
                        break;
                }
            }
            catch (Exception e)
            {
                LogException(e);
                isUpdateAvailable = false;
            }
        }

        public virtual async Task CheckUpdateLatestCommit()
        {
            Log($"Checking update for {modName}");
            string data = await DownloadStringAsync(manifest.latestCommitUrl);
            var commit = Json.Parse<GHCommit>(data);

            updateTitle = commit.commit.message;
            updateMessage = "";

            updateVersion = commit.sha;
            isUpdateAvailable = updateVersion != localVersion;
            treeUrl = string.Format(manifest.recursiveTreeUrlTemplate, commit.sha);
        }

        public virtual async Task CheckUpdateLatestRelease()
        {
            Log($"Checking update for {modName}");
            string releaseData = await DownloadStringAsync(manifest.latestReleaseUrl);
            var release = Json.Parse<GHRelease>(releaseData);
            string tagName = release.tag_name;

            string tagData = await DownloadStringAsync(string.Format(manifest.tagUrlTemplate, tagName));
            var tag = Json.Parse<GHTag>(tagData);

            updateTitle = tagName;
            updateMessage = release.body;

            updateVersion = tag.commit.sha;
            isUpdateAvailable = updateVersion != localVersion;
            treeUrl = string.Format(manifest.recursiveTreeUrlTemplate, tag.commit.sha);
        }

        public virtual async Task<IEnumerable<Task>> DownloadFiles(IEnumerable<GHTreeItem> files)
        {
            var tasks = new List<Task>();
            var throttler = new SemaphoreSlim(16);

            foreach(var file in files)
            {
                // do an async wait until we can schedule again
                await throttler.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await DownloadToFileAsync(string.Format(manifest.fileUrlTemplate, updateVersion, file.path), file.localPath);
                    }
                    finally
                    {
                        throttler.Release();
                    }
                }));
            }
            await Task.WhenAll(tasks);
            return tasks;
        }

        public virtual async Task<bool> CheckInternetConnection()
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

        #endregion Virtual Methods


        #region Static Methods

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

        public static IEnumerable<GHTreeItem> IntersectBlobsWithFiles(IEnumerable<GHTreeItem> items, IEnumerable<string> files)
        {
            var comparer = new SamePath();
            return items.Where(x => files.Contains(x.localPath, comparer));
        }

        #endregion Static Methods


        #region Abstract Methods

        public abstract Task DownloadToFileAsync(string url, string path);

        public abstract Task<string> DownloadStringAsync(string url);

        public abstract void Log(string message);

        public abstract void LogException(Exception e);

        #endregion Abstract Methods
    }
}
