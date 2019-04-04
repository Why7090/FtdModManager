using BrilliantSkies.Ui.Consoles;
using BrilliantSkies.Ui.Consoles.Getters;
using BrilliantSkies.Ui.Consoles.Interpretters.Simple;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective.Buttons;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective.Texts;
using BrilliantSkies.Ui.Layouts.DropDowns;
using BrilliantSkies.Ui.Tips;
using System;
using System.Linq;
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
                var btn = seg1.AddInterpretter(SubjectiveButton<ModPreferences>.Quick(mod, mod.modName, new ToolTip(mod.basePath, 400), x =>
                {
                    selected = x;
                }));
                btn.Color = M.m<ModPreferences>(x => selected == x ? Color.green : Color.white);
            }


            var window2 = NewWindow("Mod Manager", WindowSizing.GetRhs());
            window2.DisplayTextPrompt = false;


            window2.Screen.CreateHeader("Mod Options", new ToolTip("Options for the selected mod"));
            var seg2 = window2.Screen.CreateStandardSegment();
            seg2.SetConditionalDisplay(() => selected != null);

            seg2.AddInterpretter(SubjectiveDisplay<Manager>.Quick(_focus, M.m<Manager>(
                x =>
                {
                    if (selected.Managed)
                        return "This mod is managed by FtdModManager";
                    else
                        return "This mod is <b>not</b> managed by FtdModManager";
                }
                ), "Information about this mod"));

            var items = Enum.GetNames(typeof(UpdateType)).Select(x => new DropDownMenuAltItem<UpdateType> { Name = x, ToolTip = x });
            var menu = new DropDownMenuAlt<UpdateType>();
            menu.SetItems(items.ToArray());

            seg2.AddInterpretter(new DropDown<Manager, UpdateType>(_focus, menu,
                (manager, x) => x == selected.updateType,
                (manager, x) => _focus.SetUpdateType(selected, x)));

            seg2.AddInterpretter(SubjectiveButton<Manager>.Quick(_focus, "Check update", new ToolTip("Check update"), x =>
                {
                    x.CheckUpdate(selected);
                }));

            seg2.AddInterpretter(SubjectiveButton<Manager>.Quick(_focus, "Uninstall", new ToolTip("Uninstall mod"), x =>
                {
                    _focus.DetectMods();
                    TriggerRebuild();
                }));

            window2.Screen.CreateSpace();

            window2.Screen.CreateHeader("Mod Installation", new ToolTip("Install mod, etc."));
            var seg3 = window2.Screen.CreateStandardSegment();

            seg3.AddInterpretter(SubjectiveButton<Manager>.Quick(_focus, "Install new mod", new ToolTip("Install new mod"),
                x => preparingInstall = true))
                .SetConditionalDisplayFunction(() => !preparingInstall);

            seg3.AddInterpretter(TextInput<Manager>.Quick(_focus, M.m<Manager>(x => manifestUri), "Install URI",
                new ToolTip("Paste the URI of the modmanifest.json here"), (manager, x) => manifestUri = x))
                .SetConditionalDisplayFunction(() => preparingInstall);

            seg3.AddInterpretter(TextInput<Manager>.Quick(_focus, M.m<Manager>(x => modDir), "Install path (Optional)",
                new ToolTip("The installation directory of the new mod. Leave empty to use default value"), (manager, x) => modDir = x))
                .SetConditionalDisplayFunction(() => preparingInstall);

            var seg4 = window2.Screen.CreateStandardHorizontalSegment();

            seg4.AddInterpretter(SubjectiveButton<Manager>.Quick(_focus, "Install", new ToolTip("Install new mod!"), x =>
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

            seg4.AddInterpretter(SubjectiveButton<Manager>.Quick(_focus, "Cancel", new ToolTip("Cancel mod installation"),
                x => preparingInstall = false))
                .SetConditionalDisplayFunction(() => preparingInstall && !isInstalling);

            window2.Screen.CreateHeader("Miscellaneous", new ToolTip("Other useful operations"));
            var seg5 = window2.Screen.CreateStandardSegment();

            (seg5.AddInterpretter(SubjectiveButton<Manager>.Quick(_focus, "Restart FtD", new ToolTip("Restart FtD in order to reload mods"),
                x => _focus.RestartGame()))
                .SetConditionalDisplayFunction(() => !isInstalling) as SubjectiveButton<Manager>)
                .Color = new VSG<Color, Manager>(new Color(255 / 255f, 179 / 255f, 179 / 255f));

            //window.Screen.CreateSpace();
            return window1;
        }
    }
}
