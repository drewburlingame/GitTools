using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BbGit.Framework;

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
            return folderConfig.AddConfig("config", sampleConfig);
        }

        public static AppConfigs Load()
        {
            var folderConfig = GetFolderConfig();
            var lines = folderConfig.GetConfig("config").Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            var configs = new AppConfigs();
            AppConfig appConfig = null;

            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                if (line.StartsWith("["))
                {
                    appConfig = new AppConfig();
                    var headerSegments = line.Substring(1, line.Length - 2)
                        .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    configs.ConfigsByName[headerSegments[0]] = appConfig;

                    if (headerSegments.Length > 1 &&
                        headerSegments[0].Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        configs.Default = appConfig;
                    }

                    if (configs.Default == null)
                    {
                        configs.Default = appConfig;
                    }
                }

                var segments = line.Split(" = ".ToCharArray());
                switch (segments[0].ToLower())
                {
                    case "authtype":
                        appConfig.AuthType = segments[3].ToEnum<AppConfig.AuthTypes>();
                        break;
                    case "username":
                        appConfig.Username = segments[3];
                        break;
                    case "apppassword":
                        appConfig.AppPassword = segments[3];
                        break;
                    case "defaultaccount":
                        appConfig.DefaultAccount = segments[3];
                        break;
                    case "setorigintossh":
                        appConfig.SetOriginToSsh = bool.Parse(segments[3]);
                        break;
                }
            }

            return configs;
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