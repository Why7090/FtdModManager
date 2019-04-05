using PetaJson;
using System;
using System.Collections.Generic;

namespace FtdModManager
{
    public class ModNotifierBlock : BlockWithText
    {
        // Mod Name, modmanifest.json URI, Description
        public List<Tuple<string, Uri, string>> mods;

        public override void BlockStart()
        {
            base.BlockStart();
            if (mods == null)
                mods = new List<Tuple<string, Uri, string>>();
        }

        public override bool StillSavingStringsLikeThis => false;

        public override string GetText()
        {
            return Json.Format(mods, JsonOptions.DontWriteWhitespace);
        }
        public override string SetText(string str)
        {
            mods = Json.Parse<List<Tuple<string, Uri, string>>>(str);
            return "";
        }
    }
}
