using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace gitSyncApp
{
    public static class AppSettings
    {
        private static IConfigurationRoot? _configuration;
        public static IConfigurationRoot Configuration => _configuration ??= BuildConfiguration();

        private static IConfigurationRoot BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
        }

        public static IEnumerable<string> GetGitPossiblePaths()
        {
            return Configuration.GetSection("Git:PossiblePaths").Get<IEnumerable<string>>() ?? Array.Empty<string>();
        }
    }
}