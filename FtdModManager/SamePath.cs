using System;
using System.Collections.Generic;
using System.IO;

namespace FtdModManager
{
    public class SamePath : EqualityComparer<string>
    {
        public override bool Equals(string a, string b)
        {
            return Path.GetFullPath(a).Equals(Path.GetFullPath(b), StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode(string a)
        {
            return Path.GetFullPath(a).GetHashCode();
        }
    }

}
