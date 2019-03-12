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
        public enum UpdateType
        {
            Disabled, LatestCommit, LatestRelease
        }

        [JsonIgnore]
        public ModData modData;

        [JsonConverter(typeof(StringEnumConverter))]
        public UpdateType updateType = UpdateType.Disabled;
        public bool checkCompatibility = false;
        public string currentTreeUrl = "";

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
        public const string dataFileName = "modmanager.json";

        public ModPreferences() { }

        public static ModPreferences FromMod(Modification mod)
        {
            var self = new ModPreferences
            {
                basePath = mod.Header.ModDirectoryWithSlash,
                mod = mod
            };
            
            string modDataPath = Path.Combine(self.basePath, dataFileName);
            if (!File.Exists(modDataPath) || Directory.Exists(Path.Combine(self.basePath, ".git")))
            {
                self.updateType = UpdateType.Disabled;
            }
            else
            {
                self.modData = JsonConvert.DeserializeObject<ModData>(File.ReadAllText(modDataPath));

                string modPrefPath = Path.Combine(self.basePath, prefFileName);
                if (!File.Exists(modPrefPath))
                {
                    self.updateType = self.modData.defaultUpdateType;
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
            ModManager.RemoveTempFilesInDirectory(basePath);
        }

        public void ConfirmUpdate(Task<bool> checkTask)
        {
            if (checkTask.Result)
            {
                ModManager.Log($"Update found for {mod.Header.ComponentId.Name}");
                GuiPopUp.Instance.Add(new PopupConfirmation(
                    $"{mod.Header.ComponentId.Name} : New version available",
                    $"{updateTitle}\n\n{updateMessage}",
                    x =>
                    {
                        if (x) Update().ContinueWith(AlertUpdateCompletion);
                    },
                    "<b>Update now</b>",
                    "Remind me next time"
                ));
            }
        }

        public void AlertUpdateCompletion(Task<string> updateTask)
        {
            GuiPopUp.Instance.Add(new PopupInfo(
                $"{mod.Header.ComponentId.Name} : Successfully updated",
                $"{updateTitle}\n\n{updateMessage}\n\n{updateTask.Result}"
            ));
        }

        public async Task<string> Update()
        {
            if (string.IsNullOrWhiteSpace(newTreeUrl))
            {
                if (!await CheckUpdate()) return "No update available";
            }
            string log = await ModManager.DownloadModAsync(newTreeUrl, basePath, modData.fileUrlTemplate);
            currentTreeUrl = newTreeUrl;
            Save();
            return log;
        }

        public async Task<bool> CheckUpdate()
        {
            switch (updateType)
            {
                case UpdateType.LatestCommit:
                    return await CheckUpdateLatestCommit();
                case UpdateType.LatestRelease:
                    return await CheckUpdateLatestRelease();
                default:
                    return false;
            }
        }

        public async Task<bool> CheckUpdateLatestCommit()
        {
            try
            {
                ModManager.Log($"Checking update for {mod.Header.ComponentId.Name}");
                string data = await ModManager.DownloadStringAsync(modData.latestCommitUrl);
                var commit = JsonConvert.DeserializeObject<GHCommit>(data);

                updateTitle = commit.commit.message;
                updateMessage = "";

                newTreeUrl = string.Format(modData.recursiveTreeUrlTemplate, commit.sha);
                return newTreeUrl != currentTreeUrl;
            }
            catch (Exception e)
            {
                ModManager.LogException(e);
                return false;
            }
        }

        public async Task<bool> CheckUpdateLatestRelease()
        {
            ModManager.Log($"Checking update for {mod.Header.ComponentId.Name}");
            string releaseData = await ModManager.DownloadStringAsync(modData.latestReleaseUrl);
            var release = JsonConvert.DeserializeObject<GHRelease>(releaseData);
            string tagName = release.tag_name;

            string tagData = await ModManager.DownloadStringAsync(string.Format(modData.tagUrlTemplate, tagName));
            var tag = JsonConvert.DeserializeObject<GHTag>(tagData);

            updateTitle = tagName;
            updateMessage = release.body;

            newTreeUrl = string.Format(modData.recursiveTreeUrlTemplate, tag.commit.sha);
            return newTreeUrl != currentTreeUrl;
        }
    }
}
