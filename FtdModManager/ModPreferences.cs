using System;
using System.IO;
using System.Threading.Tasks;
using BrilliantSkies.Modding;
using BrilliantSkies.Ui.Special.PopUps;
using FtdModManager.GitHub;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FtdModManager
{
    public class ModPreferences
    {
        [JsonIgnore]
        public ModManifest manifest;

        [JsonConverter(typeof(StringEnumConverter))]
        public UpdateType updateType = UpdateType.Disabled;
        public bool checkCompatibility = false;
        public string localVersion = "";

        [JsonIgnore]
        public Modification mod;
        [JsonIgnore]
        public string newTreeUrl;
        [JsonIgnore]
        public string basePath;
        [JsonIgnore]
        public string updateTitle = "";
        [JsonIgnore]
        public string updateMessage = "";

        public const string prefFileName = "user.modpref";
        public const string manifestFileName = "modmanifest.json";

        public ModPreferences() { }

        public static ModPreferences FromMod(Modification mod)
        {
            var self = new ModPreferences
            {
                basePath = mod.Header.ModDirectoryWithSlash,
                mod = mod
            };
            
            string manifestPath = Path.Combine(self.basePath, manifestFileName);
            if (!File.Exists(manifestPath) || Directory.Exists(Path.Combine(self.basePath, ".git")))
            {
                self.updateType = UpdateType.Disabled;
            }
            else
            {
                self.manifest = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText(manifestPath));

                string modPrefPath = Path.Combine(self.basePath, prefFileName);
                if (!File.Exists(modPrefPath))
                {
                    self.updateType = self.manifest.defaultUpdateType;
                    self.Save();
                }
                else
                {
                    JsonConvert.PopulateObject(File.ReadAllText(modPrefPath), self);
                }
            }

            return self;
        }

        public void Save()
        {
            File.WriteAllText(Path.Combine(basePath, prefFileName), JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public void RemoveTempFiles()
        {
            Helper.RemoveTempFilesInDirectory(basePath);
        }
    }
}
