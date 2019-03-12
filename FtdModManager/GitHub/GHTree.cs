using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FtdModManager.GitHub
{
    public class GHTree
    {
        public string sha;
        public string url;
        public Item[] tree;

        public class Item
        {
            public string path;
            [JsonConverter(typeof(StringEnumConverter))]
            public ItemType type;
            public string sha;
            public int size;

            [JsonIgnore]
            public string localPath;
        }

        public enum ItemType
        {
            blob, tree
        }
    }
}
