using System;
using System.Threading.Tasks;
using BrilliantSkies.Ui.Special.PopUps;
using BrilliantSkies.Ui.Tips;
using UnityEngine;
using UnityEngine.Networking;

namespace FtdModManager
{
    public class FtdModUpdateInfo : AbstractModUpdateInfo
    {
        public FtdModUpdateInfo(ModManifest modManifest, ModPreferences modPreferences) : base(
            modManifest,
            modPreferences.mod.Header.ComponentId.Name,
            modPreferences.mod.Header.ModDirectoryWithSlash.NormalizedDirPath(),
            modPreferences.updateType,
            modPreferences.localVersion
        )
        { }

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

        public override async Task DownloadToFileAsync(string url, string path)
        {
            var www = UnityWebRequest.Get(url);
            www.downloadHandler = new DownloadHandlerFile(path);
            await www.SendWebRequest();
        }

        public override async Task<string> DownloadStringAsync(string url)
        {
            var www = UnityWebRequest.Get(url);
            await www.SendWebRequest();
            return www.downloadHandler.text;
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
