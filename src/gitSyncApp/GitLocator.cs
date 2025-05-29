using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace gitSyncApp
{
    public static class GitLocator
    {
        // Returns the full path to the git executable, or null if not found
        public static string? LocateGit()
        {
            // Check appsettings.json possible paths first
            foreach (var customPath in AppSettings.GetGitPossiblePaths())
            {
                if (!string.IsNullOrWhiteSpace(customPath) && Path.IsPathFullyQualified(customPath) && File.Exists(customPath))
                    return customPath;
            }

            var candidates = new List<string>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                candidates.AddRange(new[]
                {
                    // Common Windows install locations
                    @"C:\\Program Files\\Git\\cmd\\git.exe",
                    @"C:\\Program Files (x86)\\Git\\cmd\\git.exe",
                    @"C:\\Git\\cmd\\git.exe"
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                candidates.AddRange(new[]
                {
                    "/usr/local/bin/git",
                    "/usr/bin/git",
                    "/opt/homebrew/bin/git"
                });
            }
            else // Linux or others
            {
                candidates.AddRange(new[]
                {
                    "/usr/bin/git",
                    "/usr/local/bin/git"
                });
            }

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private static string? _cachedDefault;
        public static string Default => _cachedDefault ??= LocateGit() ?? throw new InvalidOperationException("Could not locate the git executable.");
    }
}
