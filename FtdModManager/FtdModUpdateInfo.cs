﻿using System;
using System.IO;
using System.Threading.Tasks;
using BrilliantSkies.Core.Constants;
using BrilliantSkies.Ui.Special.PopUps;
using BrilliantSkies.Ui.Tips;
using UnityEngine;
using UnityEngine.Networking;

namespace FtdModManager
{
    public class FtdModUpdateInfo : AbstractModUpdateInfo
    {
        public FtdModUpdateInfo() : base() { }

        public FtdModUpdateInfo(ModManifest modManifest, ModPreferences modPreferences) : base(
            modManifest,
            modPreferences.modName,
            modPreferences.basePath,
            modPreferences.updateType,
            modPreferences.localVersion
        )
        { }

        public void ConfirmUpdate(Task task = null)
        {
            if (isUpdateAvailable)
            {
                Log($"Mod Update Available : {modName}");
                try
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
                catch (Exception e)
                {
                    LogException(e);
                }
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

        public override string GetModAbsolutePath(string modDir)
        {
            return Path.Combine(Get.PerminentPaths.RootModDir().ToString(), modDir).NormalizedDirPath();
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
