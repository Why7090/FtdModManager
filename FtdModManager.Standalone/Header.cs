using System;

namespace FtdModManager.Standalone
{
    public class Header
    {
        public string Authors;
        public Guid Version;
        public string Description;
        public ComponentId ComponentId;
    }

    public class ComponentId
    {
        public Guid Guid;
        public string Name;
    }
}
