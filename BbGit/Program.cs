using System;
using Autofac;
using BbGit.BitBucket;
using BbGit.ConsoleApp;
using BbGit.ConsoleUtils;
using BbGit.Framework;
using BbGit.Git;
using CommandDotNet;
using CommandDotNet.Diagnostics;
using CommandDotNet.IoC.Autofac;
using CommandDotNet.NameCasing;
using SharpBucket.V2;

namespace BbGit
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var configs = AppConfigs.Load();

                var appRunner = new AppRunner<GitApplication>()
                    .UseDefaultMiddleware(excludePrompting: true)
                    .UseNameCasing(Case.KebabCase)
                    .UseErrorHandler((ctx, ex) =>
                    {
                        ctx.Console.Error.WriteLine(ctx.ToString());
                        var exMsg = ex.Print(includeProperties: true, includeData: true, includeStackTrace: true);
                        ctx.Console.Error.WriteLine(exMsg);
                        return ExitCodes.Error.Result;
                    })
                    .RegisterContainer(configs);

                return appRunner.Run(args);
            }
            catch (Exception e)
            {
                e.Print();
                return 1;
            }
        }

        private static AppRunner RegisterContainer(this AppRunner appRunner, AppConfigs configs)
        {
            var config = configs.Default ?? new AppConfig();

            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterType<BitBucketRepoCommand>();
            containerBuilder.RegisterType<GlobalConfigCommand>();
            containerBuilder.RegisterType<LocalRepoCommand>();
            containerBuilder.RegisterType<RepoConfigCommand>();

            containerBuilder.RegisterInstance(PipedInput.GetPipedInput());
            containerBuilder.RegisterInstance(configs);
            containerBuilder.RegisterInstance(config);
            containerBuilder.RegisterInstance(new DirectoryResolver());
            containerBuilder.RegisterType<BbService>();
            containerBuilder.RegisterType<GitService>();

            containerBuilder.RegisterBbApi(config);

            var container = containerBuilder.Build();

            return appRunner.UseAutofac(container);
        }

        private static void RegisterBbApi(this ContainerBuilder containerBuilder, AppConfig appConfig)
        {
            var bb = new SharpBucketV2();
            switch (appConfig.AuthType)
            {
                case AppConfig.AuthTypes.Basic:
                    bb.BasicAuthentication(appConfig.Username, appConfig.AppPassword);
                    break;
                case AppConfig.AuthTypes.OAuth:
                    bb.OAuth1TwoLeggedAuthentication(appConfig.Username, appConfig.AppPassword);
                    break;
            }
            containerBuilder.RegisterInstance(bb);
        }
    }
}