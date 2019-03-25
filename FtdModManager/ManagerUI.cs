using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using BrilliantSkies.Modding.Types;
using BrilliantSkies.Ui.Consoles;
using BrilliantSkies.Ui.Consoles.Getters;
using BrilliantSkies.Ui.Consoles.Interpretters;
using BrilliantSkies.Ui.Consoles.Interpretters.Simple;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective.Buttons;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective.Texts;
using BrilliantSkies.Ui.Layouts.DropDowns;
using BrilliantSkies.Ui.Tips;
using UnityEngine;

namespace FtdModManager
{
    public class ManagerUI : ConsoleUi<Manager>
    {
        public ModPreferences selected;
        public string manifestUri = "";
        public string modDir = "";
        public bool preparingInstall = false;
        public bool isInstalling = false;

        public ManagerUI(Manager manager) : base(manager) { }

        protected override ConsoleWindow BuildInterface(string suggestedName = "")
        {
            var window1 = NewWindow("Mod List", WindowSizing.GetLhs());
            window1.DisplayTextPrompt = false;
            var seg1 = window1.Screen.CreateStandardSegment();
            
            foreach (var mod in _focus.mods)
            {
                var btn = seg1.AddInterpretter(SubjectiveButton<ModPreferences>.Quick(mod, mod.modName, new ToolTip(mod.basePath), x =>
                {
                    selected = x;
                }));
                btn.Color = M.m<ModPreferences>(x => selected == x ? Color.green : Color.white);
            }


            var window2 = NewWindow("Mod Options", WindowSizing.GetRhs());
            window2.DisplayTextPrompt = false;
            var seg2 = window2.Screen.CreateStandardSegment();

            seg2.AddInterpretter(SubjectiveDisplay<Manager>.Quick(_focus, M.m<Manager>(
                x => $"This mod is {(selected.Managed ? "" : "<b>not</b> ")}managed by FtdModManager."
                ), "Information about this mod"))
                .SetConditionalDisplayFunction(() => selected != null);

            var items = Enum.GetNames(typeof(UpdateType)).Select(x => new DropDownMenuAltItem<UpdateType> { Name = x, ToolTip = x });
            var menu = new DropDownMenuAlt<UpdateType>();
            menu.SetItems(items.ToArray());

            seg2.AddInterpretter(new DropDown<Manager, UpdateType>(_focus, menu,
                (manager, x) => x == selected.updateType,
                (manager, x) => _focus.SetUpdateType(selected, x)))
                .SetConditionalDisplayFunction(() => selected != null);

            seg2.AddInterpretter(SubjectiveButton<Manager>.Quick(_focus, "Check update", new ToolTip("Check update"), x =>
                {
                    x.CheckUpdate(selected);
                }))
                .SetConditionalDisplayFunction(() => selected != null);

            seg2.AddInterpretter(SubjectiveButton<Manager>.Quick(_focus, "Uninstall", new ToolTip("Uninstall mod"), x =>
                {
                    _focus.DetectMods();
                    TriggerRebuild();
                }))
                .SetConditionalDisplayFunction(() => selected != null);

            seg2.AddInterpretter(new Blank());

            seg2.AddInterpretter(SubjectiveButton<Manager>.Quick(_focus, "Install new mod", new ToolTip("Install new mod"),
                x => preparingInstall = true))
                .SetConditionalDisplayFunction(() => !preparingInstall);

            seg2.AddInterpretter(TextInput<Manager>.Quick(_focus, M.m<Manager>(x => manifestUri), "Installation URI",
                new ToolTip("Paste the URI of the modmanifest.json here"), (manager, x) => manifestUri = x))
                .SetConditionalDisplayFunction(() => preparingInstall);

            seg2.AddInterpretter(TextInput<Manager>.Quick(_focus, M.m<Manager>(x => modDir), "Installation path (Optional)",
                new ToolTip("The installation directory of the new mod. Leave empty to use default value"), (manager, x) => modDir = x))
                .SetConditionalDisplayFunction(() => preparingInstall);

            seg2.AddInterpretter(SubjectiveButton<Manager>.Quick(_focus, "Install", new ToolTip("Install new mod!"), x =>
                {
                    _focus.Install(manifestUri, modDir).ContinueWith(y =>
                    {
                        isInstalling = false;
                        preparingInstall = false;
                        manifestUri = "";
                        modDir = "";
                        _focus.DetectMods();
                        TriggerRebuild();
                    });
                }))
                .SetConditionalDisplayFunction(() => preparingInstall && !isInstalling);

            seg2.AddInterpretter(SubjectiveButton<Manager>.Quick(_focus, "Cancel", new ToolTip("Cancel mod installation"),
                x => preparingInstall = false))
                .SetConditionalDisplayFunction(() => preparingInstall && !isInstalling);

            seg2.AddInterpretter(SubjectiveButton<Manager>.Quick(_focus, "Restart FtD", new ToolTip("Reload mods"),
                x => _focus.RestartGame()))
                .SetConditionalDisplayFunction(() => !isInstalling);

            //window.Screen.CreateSpace();
            return window1;
        }
    }
}
