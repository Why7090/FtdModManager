using BrilliantSkies.Core.Timing;
using BrilliantSkies.Modding;
using BrilliantSkies.PlayerProfiles;
using BrilliantSkies.Ui.Displayer;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FtdModManager
{
    public class ModManagerPlugin : GamePlugin_PostLoad
    {
        public static ModManagerPlugin Instance { get; private set; }

        public string name => "FtdModManager";

        public Version version => new Version("0.3.1");

        public readonly List<ModPreferences> mods = new List<ModPreferences>();

        public Manager manager;

        public bool AfterAllPluginsLoaded()
        {
            manager = new Manager();
            manager.DetectMods();
            manager.CheckUpdate();

            GameEvents.UpdateEvent += Helper.CreateKeyPressEvent(ts =>
            {
                new ManagerUI(manager).ActivateGuiToggle(GuiActivateType.Stack, GuiActivateToggleType.Type);
            }, new KeyDef(KeyCode.M, KeyMod.Ctrl));

            return true;
        }

        public void OnLoad()
        {
            Instance = this;
        }
        public void OnSave()
        {

        }
    }
}
