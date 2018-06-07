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
        public Dictionary<string, AppConfig> ConfigsByName { get; set; } = new Dictionary<string, AppConfig>();

        public static string InitConfigFile()
        {
            var folderConfig = GetFolderConfig();
            var sampleConfig = GetSampleConfig();
            return folderConfig.SaveConfig("config", sampleConfig);
        }

        public static AppConfigs Load()
        {
            AppConfigs appConfigs = new AppConfigs();

            var folderConfig = GetFolderConfig();
            var configuration = Configuration.LoadFromString(folderConfig.GetConfig("config"));

            foreach (var config in configuration)
            {
                var appConfig = config.ToObject<AppConfig>();

                var configName = config.Name;
                bool isDefault = false;
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

        private static FolderConfig GetFolderConfig()
        {
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new FolderConfig(userFolder);
        }

        private static string GetSampleConfig()
        {
            string result;
            using (Stream stream = typeof(AppConfigs).Assembly.GetManifestResourceStream("BbGit.config.example"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
            }

            return result;
        }
    }
}