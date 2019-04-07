using BrilliantSkies.Ui.Consoles;
using BrilliantSkies.Ui.Consoles.Getters;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective.Buttons;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective.Buttons.Custom;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective.Choices;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective.Texts;
using BrilliantSkies.Ui.Tips;

namespace FtdModManager.Notifier
{
    public class ModNotifierBlockUI : ConsoleUi<ModNotifierBlock>
    {
        public ModNotifierBlockUI(ModNotifierBlock focus) : base(focus) { }

        protected override ConsoleWindow BuildInterface(string suggestedName = "")
        {
            var window = NewWindow("Mod Notifier", WindowSizing.GetCentralNarrowTall());
            var seg0 = window.Screen.CreateStandardSegment();
            seg0.AddInterpretter(SubjectiveButton<ModNotifierBlock>.Quick(_focus, "Refresh mod list", new ToolTip(""), x =>
            {
                _focus.Refresh();
                TriggerRebuild();
            }));

            foreach (var pair in _focus.mods)
            {
                var mod = pair.Value;
                window.Screen.CreateHeader(mod.name.ToString(), new ToolTip(pair.Key.ToString()));

                var seg1 = window.Screen.CreateStandardSegment();
                seg1.AddInterpretter(SubjectiveToggle<ModNotifierBlock.Mod>.Quick(mod,
                    "Is dependency",
                    new ToolTip("Do our construct depend on this mod?"),
                    (x, value) => x.enabled = value,
                    x => x.enabled));

                seg1.AddInterpretter(TextInput<ModNotifierBlock.Mod>.Quick(mod,
                    M.m<ModNotifierBlock.Mod>(x => x.description),
                    "Mod description",
                    new ToolTip("You can include a description of the mod, why do we need this, what will not work if it's not installed, etc"),
                    (x, value) => x.description = value));

                seg1.AddInterpretter(TextInput<ModNotifierBlock.Mod>.Quick(mod,
                    M.m<ModNotifierBlock.Mod>(x => x.manifest),
                    "modmanifest.json URI",
                    new ToolTip("Leave blank if the mod doesn't use FtdModManager"),
                    (x, value) => x.manifest = value));

                seg1.AddInterpretter(TextInput<ModNotifierBlock.Mod>.Quick(mod,
                    M.m<ModNotifierBlock.Mod>(x => x.link),
                    "External link",
                    new ToolTip("An external link for the mod. Leave blank if you don't have one"),
                    (x, value) => x.link = value));
            }
            return window;
        }
    }
}
