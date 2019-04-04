using System.IO;
using PetaJson;

namespace FtdModManager
{
    public class ModPreferences
    {
        public ModManifest manifest;

        [Json]
        public UpdateType updateType = UpdateType.Disabled;
        [Json]
        public bool checkCompatibility = false;
        [Json]
        public string localVersion = "";
        
        public string modName;
        public string basePath;
        public string newTreeUrl;
        public string updateTitle = "";
        public string updateMessage = "";

        public bool isUpdateAvailable = false;
        public bool hasManifest;
        public bool hasGit;
        public bool Managed => hasManifest && !hasGit;

        public const string prefFileName = "user.modpref";
        public const string manifestFileName = "modmanifest.json";

        public ModPreferences() { }

        public ModPreferences(string modName, string basePath)
        {
            this.modName = modName;
            this.basePath = basePath;
            
            string manifestPath = Path.Combine(this.basePath, manifestFileName);
            hasManifest = File.Exists(manifestPath);
            hasGit = Directory.Exists(Path.Combine(this.basePath, ".git"));

            if (!hasManifest || hasGit)
            {
                updateType = UpdateType.Disabled;
            }
            else
            {
                manifest = Json.ParseFile<ModManifest>(manifestPath);

                string modPrefPath = Path.Combine(basePath, prefFileName);
                if (!File.Exists(modPrefPath))
                {
                    updateType = manifest.defaultUpdateType;
                    Save();
                }
                else
                {
                    Json.ParseFileInto(modPrefPath, this);
                }
            }
        }

        public void Save()
        {
            Json.WriteFile(Path.Combine(basePath, prefFileName), this);
        }
    }
}
