using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FtdModManager
{
    public class ModData
    {
        public string latestCommitUrl;          // github: https://api.github.com/repos/<user>/<repo>/commits/<branch>
        public string latestReleaseUrl;         // github: https://api.github.com/repos/<user>/<repo>/releases/latest
        public string tagUrlTemplate;           // github: https://api.github.com/repos/<user>/<repo>/git/refs/tags/{0}
        public string recursiveTreeUrlTemplate; // github: https://api.github.com/repos/<user>/<repo>/git/trees/{0}?recursive=1
        public string fileUrlTemplate;          // github: https://raw.githubusercontent.com/<user>/<repo>/<branch>/{0}

        [JsonConverter(typeof(StringEnumConverter))]
        public ModPreferences.UpdateType defaultUpdateType;

        public ModData() { }

        public ModData(
            string latestCommitUrl, string latestReleaseUrl,
            string tagUrlTemplate, string recursiveTreeUrlTemplate, string fileUrlTemplate,
            ModPreferences.UpdateType defaultUpdateType)
        {
            this.latestCommitUrl = latestCommitUrl;
            this.latestReleaseUrl = latestReleaseUrl;
            this.tagUrlTemplate = tagUrlTemplate;
            this.recursiveTreeUrlTemplate = recursiveTreeUrlTemplate;
            this.fileUrlTemplate = fileUrlTemplate;
            this.defaultUpdateType = defaultUpdateType;
        }

        public static ModData FromGithubRepo (string user, string repo, string branch, ModPreferences.UpdateType defaultUpdateType)
        {
            return new ModData(
                $"https://api.github.com/repos/{user}/{repo}/commits/{branch}",
                $"https://api.github.com/repos/{user}/{repo}/releases/latest",
                $"https://api.github.com/repos/{user}/{repo}/git/refs/tags/{{0}}",
                $"https://api.github.com/repos/{user}/{repo}/git/trees/{{0}}?recursive=1",
                $"https://raw.githubusercontent.com/{user}/{repo}/{branch}/{{0}}",
                defaultUpdateType);
        }
    }
}
