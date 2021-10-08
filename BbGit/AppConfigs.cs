using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        public NameValueCollection Settings { get; set; } = new();

        public static string InitConfigFile()
        {
            using var stream = typeof(AppConfigs).Assembly.GetManifestResourceStream("BbGit.config.example");
            using var reader = new StreamReader(stream!);
            var sampleConfig = reader.ReadToEnd();
            return ConfigFolder
                .UserProfile()
                .SaveConfig("config", sampleConfig);
        }

        public static AppConfigs Load()
        {
            var appConfigs = new AppConfigs();
            Load(ConfigFolder.UserProfile(), appConfigs);
            Load(ConfigFolder.CurrentDirectory(), appConfigs);
            return appConfigs;
        }

        private static void Load(ConfigFolder configFolder, AppConfigs appConfigs)
        {
            var configString = configFolder.GetConfig("config");

            if (configString == null)
            {
                return;
            }

            var configuration = Configuration.LoadFromString(configString);

            foreach (var configSection in configuration.Where(c => c.Name != "$SharpConfigDefaultSection"))
            {
                if (configSection.Name.Equals("settings", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var setting in configSection)
                    {
                        appConfigs.Settings[setting.Name] = setting.StringValue;
                    }
                }
                else
                {
                    var config = appConfigs.ConfigsByName.GetValueOrAdd(ParseName(configSection, out var isDefault));
                    configSection.SetValuesTo(config);
                    if (isDefault)
                    {
                        // checking every time ensures last default from last file is used
                        appConfigs.Default = config;
                    }
                }
            }
        }

        private static string ParseName(Section config, out bool isDefault)
        {
            var configName = config.Name;
            isDefault = false;
            var nameSegments = configName.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (nameSegments.Length > 1)
            {
                configName = nameSegments[0];
                isDefault = nameSegments.Contains("default", StringComparer.OrdinalIgnoreCase);
            }

            return configName;
        }
    }
}