using System;
using System.Threading.Tasks;

namespace FtdModManager.Standalone
{
    public class StandaloneModUpdateInfo : AbstractModUpdateInfo
    {
        public StandaloneModUpdateInfo(ModManifest modManifest, string modName, string basePath, UpdateType updateType, string localVersion)
            : base(modManifest, modName, basePath, updateType, localVersion)
        {
        }

        public StandaloneModUpdateInfo(string basePath) : base(basePath)
        {
        }
        
        public override Task<string> DownloadStringAsync(string url)
        {
            return Helper.DownloadStringAsync(url);
        }

        public override Task DownloadToFileAsync(string url, string path)
        {
            return Helper.DownloadToFileAsync(url, path);
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
