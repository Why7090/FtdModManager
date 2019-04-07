using BrilliantSkies.Ftd.Constructs.Modules.All.Paths;
using BrilliantSkies.Modding.Containers;
using BrilliantSkies.Modding.Managing;
using BrilliantSkies.Modding.Types;
using BrilliantSkies.Ui.Displayer;
using BrilliantSkies.Ui.Special.PopUps;
using BrilliantSkies.Ui.Tips;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FtdModManager.Notifier
{
    public class ModNotifierBlock : BlockWithText
    {
        public override bool StillSavingStringsLikeThis => true;

        public Dictionary<Guid, Mod> mods;

        public override void BlockStart()
        {
            base.BlockStart();
            if (mods == null)
            {
                mods = new Dictionary<Guid, Mod>();
                Refresh();
            }
        }

        public void Refresh()
        {
            var allBlocks = new HashSet<ItemDefinition>(GetAllBlocks(GetC()).Select(x => x.item));

            foreach (var mod in ConfigurationManager.Instance.Modifications)
            {
                if (mod.Header.Core || mod.Header.ComponentId.Name == ModManagerPlugin.Instance.name)
                    continue;

                var moddedBlocks = mod.Get<ModificationComponentContainerItem>().Components.Intersect(allBlocks);

                if (!mods.TryGetValue(mod.Header.ComponentId.Guid, out var m))
                {
                    m = new Mod
                    {
                        enabled = moddedBlocks.Any()
                    };
                }

                m.name = mod.Header.ComponentId.Name;
                m.description = mod.Header.Description;

                if (moddedBlocks.Any())
                {
                    m.description += "\nBlocks from this mod: "
                            + string.Join(", ", moddedBlocks.Select(x => x.ComponentId.Name));
                }
                else
                {
                    m.description += "\nOur construct doesn't contain any block from this mod";
                }

                mods[mod.Header.ComponentId.Guid] = m;
            }
        }

        public void CheckAndNotify()
        {
            var allMods = ConfigurationManager.Instance.Modifications.Select(x => x.Header.ComponentId.Guid);

            var missingMods = from pair in mods
                              where pair.Value.enabled
                                 && !allMods.Contains(pair.Key)
                              select pair.Value;

            if (missingMods.Any())
            {
                GuiPopUp.Instance.Add(new ModNotificationPopup(missingMods));
            }
        }

        public override string GetText()
        {
            return JsonConvert.SerializeObject(mods);
        }
        public override string SetText(string str)
        {
            try
            {
                mods = JsonConvert.DeserializeObject<Dictionary<Guid, Mod>>(str);
                CheckAndNotify();
                return str;
            }
            catch (Exception ex)
            {
                Helper.LogException(ex);
                return "";
            }
        }

        public override void Secondary(Transform T)
        {
            new ModNotifierBlockUI(this).ActivateGui(GuiActivateType.Stack);
        }

        protected override void AppendToolTip(ProTip tip)
        {
            base.AppendToolTip(tip);
            tip.Add(new ProTipSegment_TitleSubTitle("Mod Notifier", "Alert for missing mods"), Position.Middle);
            tip.InfoOnly = false;
        }

        public override BlockTechInfo GetTechInfo()
        {
            return base.GetTechInfo().AddStatement("Can notify for missing mods").AddStatement("Only works if FtdModManager is installed");
        }

        public IEnumerable<Block> GetAllBlocks(AllConstruct c)
        {
            var iBlocks = c.iBlocks;

            return iBlocks.AliveAndDead.Blocks
                .Concat(iBlocks.SubConstructList.SelectMany(x => GetAllBlocks(x)));
        }

        public override void StateChanged(IBlockStateChange change)
        {
            base.StateChanged(change);
            if (change.InitiatedOrInitiatedInUnrepairedState_OnlyCalledOnce)
                GetConstructableOrSubConstructable().iBlocksWithText.BlocksWithText.Add(this);
            else if (change.IsPerminentlyRemovedOrConstructDestroyed)
                GetConstructableOrSubConstructable().iBlocksWithText.BlocksWithText.Remove(this);
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class Mod
        {
            public bool enabled = false;
            public string name = "";
            public string description = "";
            public string manifest = "";
            public string link = "";
            [JsonIgnore]
            public bool processed = false;
        }
    }
}
