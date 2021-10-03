using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BbGit.Framework;
using SharpConfig;

namespace BbGit
{
    public class AppConfigs
    {
        public AppConfig Default { get; set; }
        public Dictionary<string, AppConfig> ConfigsByName { get; set; } = new();

        public static string InitConfigFile()
        {
            return ConfigFolder
                .UserFolder()
                .SaveConfig("config", GetSampleConfig());
        }

        public static AppConfigs Load()
        {
            var appConfigs = new AppConfigs();

            var configString = ConfigFolder.UserFolder()
                .GetConfig("config");
            if (configString == null)
            {
                return appConfigs;
            }
            var configuration = Configuration.LoadFromString(configString);

            foreach (var config in configuration)
            {
                var appConfig = config.ToObject<AppConfig>();

                var configName = config.Name;
                var isDefault = false;
                var nameSegments = configName.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                if (nameSegments.Length > 1)
                {
                    configName = nameSegments[0];
                    isDefault = nameSegments.Contains("default", StringComparer.OrdinalIgnoreCase);
                }

                appConfigs.ConfigsByName[configName] = appConfig;
                if (isDefault)
                {
                    appConfigs.Default = appConfig;
                }
            }

            return appConfigs;
        }

        private static string GetSampleConfig()
        {
            using var stream = typeof(AppConfigs).Assembly.GetManifestResourceStream("BbGit.config.example");
            using var reader = new StreamReader(stream!);
            return reader.ReadToEnd();
        }
    }
}