namespace FtdModManager.GitHub
{
    public class GHCommit
    {
        public string sha;
        public Commit commit;
        
        public class Commit
        {
            public string message;
            public GHObjectWithLink tree;
        }
    }
}
