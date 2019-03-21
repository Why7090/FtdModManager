using System;
using System.Net;
using System.Runtime.InteropServices;
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
        
        public override async Task<string> DownloadStringAsync(string url)
        {
            try
            {
                return await Helper.DownloadStringAsync(url);
            }
            catch (Exception e)
            {
                Log(url);
                LogException(e);
                return "";
            }
        }

        public override async Task DownloadToFileAsync(string url, string path)
        {
            try
            {
                await Helper.DownloadToFileAsync(url, path);
            }
            catch (Exception e)
            {
                Log(url);
                LogException(e);
                return;
            }
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
