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
            Helpers.RemoveTempFilesInDirectory(basePath);
        }

        public void ConfirmUpdate(Task<bool> checkTask)
        {
            if (checkTask.Result)
            {
                Helpers.Log($"Update found for {mod.Header.ComponentId.Name}");
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
            string log = await Helpers.DownloadModAsync(newTreeUrl, basePath, manifest.fileUrlTemplate);
            localVersion = newTreeUrl;
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
                Helpers.Log($"Checking update for {mod.Header.ComponentId.Name}");
                string data = await Helpers.DownloadStringAsync(manifest.latestCommitUrl);
                var commit = JsonConvert.DeserializeObject<GHCommit>(data);

                updateTitle = commit.commit.message;
                updateMessage = "";

                newTreeUrl = string.Format(manifest.recursiveTreeUrlTemplate, commit.sha);
                return newTreeUrl != localVersion;
            }
            catch (Exception e)
            {
                Helpers.LogException(e);
                return false;
            }
        }

        public async Task<bool> CheckUpdateLatestRelease()
        {
            Helpers.Log($"Checking update for {mod.Header.ComponentId.Name}");
            string releaseData = await Helpers.DownloadStringAsync(manifest.latestReleaseUrl);
            var release = JsonConvert.DeserializeObject<GHRelease>(releaseData);
            string tagName = release.tag_name;

            string tagData = await Helpers.DownloadStringAsync(string.Format(manifest.tagUrlTemplate, tagName));
            var tag = JsonConvert.DeserializeObject<GHTag>(tagData);

            updateTitle = tagName;
            updateMessage = release.body;

            newTreeUrl = string.Format(manifest.recursiveTreeUrlTemplate, tag.commit.sha);
            return newTreeUrl != localVersion;
        }
    }
}
