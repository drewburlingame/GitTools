using System;
using System.Linq;
using System.Threading;
using Autofac;
using BbGit.BitBucket;
using BbGit.ConsoleApp;
using BbGit.ConsoleUtils;
using BbGit.Framework;
using BbGit.Git;
using CommandDotNet;
using CommandDotNet.IoC.Autofac;
using CommandDotNet.Models;
using SharpBucket.V2;

namespace BbGit
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0 && args.Last() == "DEBUG")
                {
                    // Debugger.Break used to prompt to open in VS.Net, but that isn'w working
                    // Add a breakpoint to Thread.Sleep and then change bool to false to continue debugging.
                    Console.Out.WriteLine("You can now attach to this process");
                    var changeThisToFalse = true;
                    while (changeThisToFalse)
                    {
                        Thread.Sleep(1000);
                    }

                    args = args.Take(args.Length - 1).ToArray();
                }


                var containerBuilder = new ContainerBuilder();

                var configs = AppConfigs.Load();
                var config = configs.Default;

                containerBuilder.RegisterInstance(PipedInput.GetPipedInput());
                containerBuilder.RegisterInstance(configs);
                containerBuilder.RegisterInstance(config);
                containerBuilder.RegisterInstance(new DirectoryResolver());
                containerBuilder.RegisterType<BbService>();
                containerBuilder.RegisterType<GitService>();

                RegisterBbApi(containerBuilder, config);

                var container = containerBuilder.Build();

                var appSettings = new AppSettings {Case = Case.KebabCase};
                var appRunner = new AppRunner<GitApplication>(appSettings).UseAutofac(container);
                return appRunner.Run(args);
            }
            catch (Exception e)
            {
                e.Print();
                return 1;
            }
        }

        private static void RegisterBbApi(ContainerBuilder containerBuilder, AppConfig appConfig)
        {
            var bb = new SharpBucketV2();
            bb.BasicAuthentication(appConfig.Username, appConfig.AppPassword);
            containerBuilder.RegisterInstance(bb);
        }
    }
}