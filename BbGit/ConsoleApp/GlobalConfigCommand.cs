using System;
using CommandDotNet;

namespace BbGit.ConsoleApp
{
    [Command("config-global", Description = "Global configurations for BbGit. e.g. AWS account info")]
    public class GlobalConfigCommand
    {
        [Command(Description = "Initializes BbGit configuration. " +
                                           "Creates ~/.bbgit folder with a sample config. " +
                                           "note: config is stored in your user directory.")]
        public void InitApp()
        {
            var configFilePath = AppConfigs.InitConfigFile();

            Console.Out.WriteLine("init completed.  you must now edit the config file with your personal information");
            Console.Out.WriteLine("");
            Console.Out.WriteLine($"config: {configFilePath}");
        }
    }
}