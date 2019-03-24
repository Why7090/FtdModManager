using System;
using System.IO;
using System.Threading.Tasks;

namespace FtdModManager.Standalone
{
    public class StandaloneModUpdateInfo : AbstractModUpdateInfo
    {
        public string root;

        public StandaloneModUpdateInfo(string root) : base()
        {
            this.root = root;
        }

        public StandaloneModUpdateInfo(string root, ModManifest modManifest, string modName, string basePath, UpdateType updateType, string localVersion)
            : base(modManifest, modName, basePath, updateType, localVersion)
        {
            this.root = root;
        }

        public StandaloneModUpdateInfo(string root, string basePath) : base(basePath)
        {
            this.root = root;
        }
        
        public override Task<string> DownloadStringAsync(string url)
        {
            return Helper.DownloadStringAsync(url);
        }

        public override Task DownloadToFileAsync(string url, string path)
        {
            return Helper.DownloadToFileAsync(url, path);
        }

        public override string GetModAbsolutePath(string modDir)
        {
            Directory.SetCurrentDirectory(root);
            return Path.GetFullPath(modDir).NormalizedDirPath();
        }

        public override void Log(string message)
        {
            Helper.Log(message);
        }

        public override void LogException(Exception e)
        {
            Helper.LogException(e);
        }
    }
}
