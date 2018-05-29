using System;
using CommandDotNet.Attributes;

namespace BbGit.ConsoleApp
{
    [ApplicationMetadata(Name ="app-config", Description = "commands to configure BbGit")]
    public class AppConfigCommand
    {
        [ApplicationMetadata(Description = "Initializes BbGit configuration.  Creates ~/.bbgit folder with a sample config")]
        public void InitApp()
        {
            var configFilePath = AppConfigs.InitConfigFile();

            Console.Out.WriteLine("init completed.  you must now edit the config file with your personal information");
            Console.Out.WriteLine("");
            Console.Out.WriteLine($"config: {configFilePath}");
        }
    }
}