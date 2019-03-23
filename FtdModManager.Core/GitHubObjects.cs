using PetaJson;

namespace FtdModManager
{
    [Json]
    public class GHCommit
    {
        public string sha;
        public Commit commit;
        
        [Json]
        public class Commit
        {
            public string message;
            public GHObjectWithLink tree;
        }
    }

    [Json]
    public class GHTree
    {
        public string sha;
        public string url;
        public GHTreeItem[] tree;
    }

    [Json]
    public class GHTreeItem
    {
        public string path;
        public GHTreeItemType type;
        public string sha;
        public int size;

        [JsonExclude]
        public string localPath;
    }

    [Json]
    public class GHTag
    {
        [Json("object")]
        public GHObjectWithLink commit;
    }

    [Json]
    public class GHRelease
    {
        public string tag_name;
        public string name;
        public bool draft;
        public string body;
    }

    [Json]
    public class GHObjectWithLink
    {
        public string sha;
        public string url;
    }

    public enum GHTreeItemType
    {
        blob, tree
    }
}
