using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BrilliantSkies.Modding;
using BrilliantSkies.Modding.Managing;
using UnityEngine;

namespace FtdModManager
{
    public class ModManagerPlugin : GamePlugin_PostLoad
    {
        public string name => "FtdModManager";

        public Version version => new Version("0.1.0");

        public readonly List<ModPreferences> mods = new List<ModPreferences>();

        public bool AfterAllPluginsLoaded()
        {
            foreach (var mod in ConfigurationManager.Instance.Modifications)
            {
                if (mod.Header.Core)
                    continue;

                var pref = ModPreferences.FromMod(mod);
                mods.Add(pref);
                pref.RemoveTempFiles();
                pref.CheckUpdate().ContinueWith(pref.ConfirmUpdate);
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
