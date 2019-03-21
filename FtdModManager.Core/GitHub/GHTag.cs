using Newtonsoft.Json;

namespace FtdModManager.GitHub
{
    public class GHTag
    {
        [JsonProperty("object")]
        public GHObjectWithLink commit;
    }
}
