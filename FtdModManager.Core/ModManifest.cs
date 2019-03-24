using PetaJson;

namespace FtdModManager
{
    [Json]
    public class ModManifest
    {
        public string latestCommitUrl;          // github: https://api.github.com/repos/<user>/<repo>/commits/<branch>
        public string latestReleaseUrl;         // github: https://api.github.com/repos/<user>/<repo>/releases/latest
        public string tagUrlTemplate;           // github: https://api.github.com/repos/<user>/<repo>/git/refs/tags/{0}
        public string recursiveTreeUrlTemplate; // github: https://api.github.com/repos/<user>/<repo>/git/trees/{0}?recursive=1
        public string fileUrlTemplate;          // github: https://raw.githubusercontent.com/<user>/<repo>/{0}/{1}
        
        public UpdateType defaultUpdateType;    // None, LatestCommit, LatestRelease
        public string defaultInstallDir;        // Example: "ExampleMod"

        public ModManifest() { }

        public ModManifest(
            string latestCommitUrl, string latestReleaseUrl,
            string tagUrlTemplate, string recursiveTreeUrlTemplate, string fileUrlTemplate,
            UpdateType defaultUpdateType, string defaultInstallDir)
        {
            this.latestCommitUrl = latestCommitUrl;
            this.latestReleaseUrl = latestReleaseUrl;
            this.tagUrlTemplate = tagUrlTemplate;
            this.recursiveTreeUrlTemplate = recursiveTreeUrlTemplate;
            this.fileUrlTemplate = fileUrlTemplate;
            this.defaultUpdateType = defaultUpdateType;
            this.defaultInstallDir = defaultInstallDir;
        }

        public static ModManifest FromGithubRepo (string user, string repo, string branch,
            UpdateType defaultUpdateType, string defaultInstallDir = null)
        {
            return new ModManifest(
                $"https://api.github.com/repos/{user}/{repo}/commits/{branch}",
                $"https://api.github.com/repos/{user}/{repo}/releases/latest",
                $"https://api.github.com/repos/{user}/{repo}/git/refs/tags/{{0}}",
                $"https://api.github.com/repos/{user}/{repo}/git/trees/{{0}}?recursive=1",
                $"https://raw.githubusercontent.com/{user}/{repo}/{{0}}/{{1}}",
                defaultUpdateType, defaultInstallDir ?? repo);
        }
    }
}
