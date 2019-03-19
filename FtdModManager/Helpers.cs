using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BrilliantSkies.Modding;
using FtdModManager.GitHub;
using GitSharp;
using Newtonsoft.Json;

#if !DEBUG
using UnityEngine;
using UnityEngine.Networking;
#endif

namespace FtdModManager
{
    public static class Helpers
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
    }
}
