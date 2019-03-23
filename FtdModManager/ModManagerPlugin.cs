using System;
using System.Collections.Generic;
using BrilliantSkies.Modding;
using BrilliantSkies.Modding.Managing;

namespace FtdModManager
{
    public class ModManagerPlugin : GamePlugin_PostLoad
    {
        public string name => "FtdModManager";

        public Version version => new Version("0.3.0");

        public readonly List<ModPreferences> mods = new List<ModPreferences>();

        public bool AfterAllPluginsLoaded()
        {
            foreach (var mod in ConfigurationManager.Instance.Modifications)
            {
                if (mod.Header.Core)
                    continue;

                var pref = new ModPreferences(mod.Header.ComponentId.Name, mod.Header.ModDirectoryWithSlash);
                mods.Add(pref);
                Helper.RemoveTempFilesInDirectory(pref.basePath);

                var updateInfo = new FtdModUpdateInfo(pref.manifest, pref);
                updateInfo.CheckAndPrepareUpdate().ContinueWith(updateInfo.ConfirmUpdate);
            }
            return true;
        }

        public void OnLoad()
        {

        }
        public void OnSave()
        {

        }
    }
}
