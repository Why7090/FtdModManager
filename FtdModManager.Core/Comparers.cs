using System;
using System.Collections.Generic;
using System.IO;

namespace FtdModManager
{
    public class SamePath : EqualityComparer<string>
    {
        public override bool Equals(string a, string b)
        {
            return Path.GetFullPath(a).Equals(Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode(string a)
        {
            return Path.GetFullPath(a).GetHashCode();
        }
    }

    public class SubPath : EqualityComparer<string>
    {
        public override bool Equals(string a, string b)
        {
            return a.IsSubPathOf(b) || b.IsSubPathOf(a);
        }

        public override int GetHashCode(string a)
        {
            return Path.GetFullPath(a).GetHashCode();
        }
    }

    // https://stackoverflow.com/a/31941159
    public static class StringExtensions
    {
        /// <summary>
        /// Returns true if <paramref name="path"/> starts with the path <paramref name="baseDirPath"/>.
        /// The comparison is case-insensitive, handles / and \ slashes as folder separators and
        /// only matches if the base dir folder name is matched exactly ("c:\foobar\file.txt" is not a sub path of "c:\foo").
        /// </summary>
        public static bool IsSubPathOf(this string path, string baseDirPath)
        {
            string normalizedPath = path.NormalizedDirPath();

            string normalizedBaseDirPath = baseDirPath.NormalizedDirPath();

            return normalizedPath.StartsWith(normalizedBaseDirPath, StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizedDirPath(this string path)
        {
            return Path.GetFullPath(path.Replace('\\', '/').WithEnding("/"));
        }

        public static string NormalizedFilePath(this string path)
        {
            return Path.GetFullPath(path.Replace('\\', '/'));
        }

        public static string NormalizedPath(this string path, bool isFile)
        {
            return isFile ? path.NormalizedFilePath() : path.NormalizedDirPath();
        }

        public static string PathRelativeTo(this string path, string reference)
        {
            var uri1 = new Uri(path);
            var uri2 = new Uri(reference);
            return uri2.MakeRelativeUri(uri1).ToString();
        }

        /// <summary>
        /// Returns <paramref name="str"/> with the minimal concatenation of <paramref name="ending"/> (starting from end) that
        /// results in satisfying .EndsWith(ending).
        /// </summary>
        /// <example>"hel".WithEnding("llo") returns "hello", which is the result of "hel" + "lo".</example>
        public static string WithEnding(this string str, string ending)
        {
            if (str == null)
                return ending;

            string result = str;

            // Right() is 1-indexed, so include these cases
            // * Append no characters
            // * Append up to N characters, where N is ending length
            for (int i = 0; i <= ending.Length; i++)
            {
                string tmp = result + ending.Right(i);
                if (tmp.EndsWith(ending))
                    return tmp;
            }

            return result;
        }

        /// <summary>Gets the rightmost <paramref name="length" /> characters from a string.</summary>
        /// <param name="value">The string to retrieve the substring from.</param>
        /// <param name="length">The number of characters to retrieve.</param>
        /// <returns>The substring.</returns>
        public static string Right(this string value, int length)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", length, "Length is less than zero");
            }

            return (length < value.Length) ? value.Substring(value.Length - length) : value;
        }
    }
}
