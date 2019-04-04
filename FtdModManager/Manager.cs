using System.Collections.Generic;
using System.Threading.Tasks;
using BrilliantSkies.Core.SteamworksIntegration;
using BrilliantSkies.Modding.Managing;
using BrilliantSkies.PlayerProfiles;
using BrilliantSkies.Ui.Special.PopUps;
using BrilliantSkies.Ui.Tips;

namespace FtdModManager
{
    public class Manager
    {
        public List<ModPreferences> mods = new List<ModPreferences>();

        public void DetectMods()
        {
            mods.Clear();
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
                CheckUpdate(mod);
            }
        }

        public void CheckUpdate(ModPreferences mod)
        {
            var updateInfo = new FtdModUpdateInfo(mod.manifest, mod);
            updateInfo.CheckAndPrepareUpdate().ContinueWith(updateInfo.ConfirmUpdate);
        }

        public void SetUpdateType(ModPreferences mod, UpdateType type)
        {
            if (mod?.Managed == true && mod.updateType != type)
            {
                mod.updateType = type;
                mod.Save();
            }
        }

        public async Task Install(string uri, string name)
        {
            var info = new FtdModUpdateInfo();
            bool success = await info.PrepareNewInstallation(uri, name);
            if (!success)
            {
                GuiPopUp.Instance.Add(
                    new PopupMultiButton(
                        $"Do you want to continue installation of {info.modName}",
                        info.basePath,
                        false
                    )
                    .AddButton("<b>Abort</b>", toolTip: new ToolTip("Cancel installation"))
                    .AddButton("<color=red>Delete this directory permanently</color> and continue installation",
                        x => info.PrepareNewInstallation(uri, name).ContinueWith(y => info.CheckAndPrepareUpdate().ContinueWith(info.ConfirmUpdate)))
                    .AddButton("<b>Open directory in Explorer</b>", x => Helper.OpenInExplorer(info.basePath), false,
                        new ToolTip("Check, backup or delete files manually"))
                );
            }
            else
            {
                await info.CheckAndPrepareUpdate().ContinueWith(info.ConfirmUpdate);
            }
        }

        public void RestartGame()
        {
            ProfileManager.Instance.SaveAll();
            new SteamInterface().__RestartGame();
        }
    }
}
