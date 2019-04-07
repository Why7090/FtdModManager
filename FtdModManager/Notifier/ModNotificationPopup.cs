using BrilliantSkies.Ui.Consoles;
using BrilliantSkies.Ui.Consoles.Getters;
using BrilliantSkies.Ui.Consoles.Interpretters.Simple;
using BrilliantSkies.Ui.Consoles.Interpretters.Subjective;
using BrilliantSkies.Ui.Special.PopUps.Internal;
using BrilliantSkies.Ui.Tips;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FtdModManager.Notifier
{
#pragma warning disable 4014
    public class ModNotificationPopup : AbstractPopup<PopSimple>
    {
        protected override int Width => 700;
        protected override int Height => 600;

        protected IEnumerable<ModNotifierBlock.Mod> mods;

        public ModNotificationPopup(IEnumerable<ModNotifierBlock.Mod> mods) : base("Missing Mods", new PopSimple())
        {
            this.mods = mods;
        }

        protected override void AddContentToWindow(ConsoleWindow window)
        {
            var seg0 = window.Screen.CreateStandardSegment();
            seg0.AddInterpretter(SubjectiveDisplay<bool>.Quick(false, M.m<bool>(x =>
            {
                if (mods.All(y => y.processed)) _focus.Do();
                return
                "The following mods are required by this construct but are not installed on this instance of FtD.\n" +
                "This popup will automatically close when all mods are either installed or discarded.\n" +
                "You have to restart FtD in order to apply the newly installed mods.";
            }), ""));

            foreach (var mod in mods)
            {
                window.Screen.CreateHeader(mod.name, new ToolTip("Missing mod"))
                    .SetConditionalDisplay(() => !mod.processed);
                var seg1 = window.Screen.CreateStandardSegment();
                seg1.SetConditionalDisplay(() => !mod.processed);

                seg1.AddInterpretter(StringDisplay.Quick(mod.description, "Description of missing mod"));

                var seg2 = window.Screen.CreateStandardHorizontalSegment();
                seg2.SetConditionalDisplay(() => !mod.processed);

                if (!string.IsNullOrWhiteSpace(mod.manifest)
                    && Uri.TryCreate(mod.manifest, UriKind.Absolute, out var _))
                {
                    seg2.AddInterpretter(Button.Quick("Install mod with FtdModManager", new ToolTip(mod.manifest), () =>
                    {
                        mod.processed = true;
                        ModManagerPlugin.Instance.manager.Install(mod.manifest);
                    }));
                }

                if (!string.IsNullOrWhiteSpace(mod.manifest)
                    && Uri.TryCreate(mod.link, UriKind.Absolute, out var _))
                {
                    seg2.AddInterpretter(Button.Quick("Open mod page", new ToolTip(mod.link), () =>
                    {
                        Application.OpenURL(mod.link);
                    }));
                }

                seg2.AddInterpretter(Button.Quick("Discard", new ToolTip("Ignore this alert"), () =>
                {
                    mod.processed = true;
                }));
            }
        }
    }
}
