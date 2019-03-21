using System.IO;
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
        public string modName;
        [JsonIgnore]
        public string basePath;
        [JsonIgnore]
        public string newTreeUrl;
        [JsonIgnore]
        public string updateTitle = "";
        [JsonIgnore]
        public string updateMessage = "";

        public const string prefFileName = "user.modpref";
        public const string manifestFileName = "modmanifest.json";

        public ModPreferences() { }

        public ModPreferences(string modName, string basePath)
        {
            this.modName = modName;
            this.basePath = basePath;
            
            string manifestPath = Path.Combine(this.basePath, manifestFileName);
            if (!File.Exists(manifestPath) || Directory.Exists(Path.Combine(this.basePath, ".git")))
            {
                updateType = UpdateType.Disabled;
            }
            else
            {
                manifest = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText(manifestPath));

                string modPrefPath = Path.Combine(basePath, prefFileName);
                if (!File.Exists(modPrefPath))
                {
                    updateType = manifest.defaultUpdateType;
                    Save();
                }
                else
                {
                    JsonConvert.PopulateObject(File.ReadAllText(modPrefPath), this);
                }
            }
        }

        public void Save()
        {
            File.WriteAllText(Path.Combine(basePath, prefFileName), JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
