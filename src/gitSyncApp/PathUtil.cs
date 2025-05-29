using System;
using System.IO;

namespace gitSyncApp
{
    public static class PathUtil
    {
        public static string GetRepoNameFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be null or whitespace.", nameof(path));
            if (!Path.IsPathFullyQualified(path))
                throw new ArgumentException($"Path must be fully qualified: '{path}'", nameof(path));
            return Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
    }
}
