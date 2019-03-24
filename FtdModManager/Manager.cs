using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BrilliantSkies.Core.Constants;
using BrilliantSkies.Modding.Managing;

namespace FtdModManager
{
    public class Manager
    {
        public List<ModPreferences> mods = new List<ModPreferences>();

        public void DetectMods()
        {
            foreach (var mod in ConfigurationManager.Instance.Modifications)
            {
                if (mod.Header.Core)
                    continue;

                var pref = new ModPreferences(mod.Header.ComponentId.Name, mod.Header.ModDirectoryWithSlash);
                mods.Add(pref);
                Helper.RemoveTempFilesInDirectory(pref.basePath);
            }
        }

        public void CheckUpdate()
        {
            foreach (var mod in mods)
            {
                var updateInfo = new FtdModUpdateInfo(mod.manifest, mod);
                updateInfo.CheckAndPrepareUpdate().ContinueWith(updateInfo.ConfirmUpdate);
            }
        }

        public async Task<bool> Install(string uri, string name)
        {
            var info = new FtdModUpdateInfo();
            return await info.PrepareNewInstallation(uri, name);
        }
    }
}
