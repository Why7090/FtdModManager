using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using BrilliantSkies.Core.Timing;
using BrilliantSkies.Core.Unity;
using BrilliantSkies.Modding;
using BrilliantSkies.Modding.Managing;
using BrilliantSkies.PlayerProfiles;
using BrilliantSkies.Ui.Displayer;
using UnityEngine;

namespace FtdModManager
{
    public class ModManagerPlugin : GamePlugin_PostLoad
    {
        public string name => "FtdModManager";

        public Version version => new Version("0.3.0");

        public readonly List<ModPreferences> mods = new List<ModPreferences>();

        public Manager manager;

        public bool AfterAllPluginsLoaded()
        {
            manager = new Manager();
            manager.DetectMods();

            GameEvents.UpdateEvent += Helper.CreateKeyPressEvent(() =>
            {
                new ManagerUI(manager).ActivateGuiToggle(GuiActivateType.Stack, GuiActivateToggleType.Type);
            }, false, new KeyDef(KeyCode.M, KeyMod.Ctrl)).ToDRegularEvent();

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
