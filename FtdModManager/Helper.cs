using System;
using System.IO;
using System.Linq;
using BrilliantSkies.Core.Timing;
using BrilliantSkies.Core.Unity;
using BrilliantSkies.PlayerProfiles;
using BrilliantSkies.Ui.TreeSelection;
using UnityEngine;

namespace FtdModManager
{
    public static class Helper
    {
        public const string tempExtension = ".modmanager_temp";

        public static void RemoveTempFilesInDirectory(string dir)
        {
            foreach (string file in Directory.EnumerateFiles(dir, $"*{tempExtension}", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                    Log($"Deleted file: {file}");
                }
                catch (Exception e)
                {
                    LogException(e);
                }
            }
        }

        public static void Log(string message)
        {
            Debug.Log("[ModManager] " + message);
        }

        public static void LogException(Exception e)
        {
            Debug.LogException(e);
        }

        public static void OpenInExplorer(string path)
        {
            path = path.TrimEnd(new[] { '\\', '/' }); // Mac doesn't like trailing slash
            System.Diagnostics.Process.Start(path);
        }

        public static GameEvents.DRegularEvent CreateKeyPressEvent(Action<ITimeStep> keyPressed, KeyDef key)
        {
            return ts =>
            {
                if (Input.GetKeyDown(key.Key)
                    && key.ModifiersHappy
                    && !key.UnneccessaryModifiers(ModifierAllows.CancelWhenUnnecessaryModifiers))
                {
                    keyPressed(ts);
                }
            };
        }
    }
}
