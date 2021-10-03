using System;
using Autofac;
using BbGit.BitBucket;
using BbGit.ConsoleApp;
using BbGit.ConsoleUtils;
using BbGit.Git;
using Bitbucket.Net;
using CommandDotNet;
using CommandDotNet.DataAnnotations;
using CommandDotNet.IoC.Autofac;
using CommandDotNet.NameCasing;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace BbGit
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => ((Exception)args.ExceptionObject).Print();

            try
            {
                var configs = AppConfigs.Load();

                var appRunner = new AppRunner<GitApplication>()
                    .UseDefaultMiddleware(excludePrompting: true)
                    .UseNameCasing(Case.KebabCase)
                    .UseDataAnnotationValidations(showHelpOnError:true)
                    .UseTimerDirective()
                    .UseErrorHandler((ctx, ex) =>
                    {
                        ctx.Console.Error.WriteLine(ctx.ToString());
                        ex.Print();
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
            
            containerBuilder.RegisterInstance(configs);
            containerBuilder.RegisterInstance(config);
            containerBuilder.RegisterType<BbService>();
            containerBuilder.RegisterType<GitService>();

            containerBuilder.RegisterServerBbApi(config);

            var container = containerBuilder.Build();

            return appRunner.UseAutofac(container);
        }

        private static void RegisterServerBbApi(this ContainerBuilder containerBuilder, AppConfig config)
        {
            BitbucketClient bbClient;
            switch (config.AuthType)
            {
                case AppConfig.AuthTypes.Basic:
                    bbClient = new BitbucketClient(config.BaseUrl, config.Username, config.AppPassword);
                    break;
                case AppConfig.AuthTypes.OAuth:
                    bbClient = new BitbucketClient(config.BaseUrl, () => config.AppPassword);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            containerBuilder.RegisterInstance<CredentialsHandler>((url, fromUrl, types) =>
                new UsernamePasswordCredentials { Username = config.Username, Password = config.AppPassword });

            containerBuilder.RegisterInstance(bbClient);
        }
    }
}