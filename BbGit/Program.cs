using System;
using System.Threading.Tasks;
using Autofac;
using BbGit.BitBucket;
using BbGit.ConsoleApp;
using BbGit.ConsoleUtils;
using BbGit.Git;
using Bitbucket.Net;
using CommandDotNet;
using CommandDotNet.DataAnnotations;
using CommandDotNet.Diagnostics;
using CommandDotNet.Execution;
using CommandDotNet.IoC.Autofac;
using CommandDotNet.NameCasing;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Spectre.Console;
using MiddlewareSteps = CommandDotNet.Execution.MiddlewareSteps;

namespace BbGit
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Debugger.AttachIfDebugDirective(args);

            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) 
                => ((Exception)eventArgs.ExceptionObject).Print();

            try
            {
                var configs = AppConfigs.Load();

                var appRunner = new AppRunner<GitApplication>()
                    .UseDefaultMiddleware()
                    .GiveCancellationTokenToFlurl()
                    .Configure(c => c.UseParameterResolver(_ => AnsiConsole.Console))
                    .UseNameCasing(Case.KebabCase)
                    .UseDataAnnotationValidations(showHelpOnError: true)
                    .UseTimerDirective()
                    .UseDefaultsFromAppSetting(configs.Settings, true)
                    .UseErrorHandler((ctx, ex) =>
                    {
                        var errorWriter = (ctx?.Console.Error ?? Console.Error);
                        ex.Print(errorWriter.WriteLine,
                            includeProperties: true,
                            includeData: true,
                            includeStackTrace: false);

                        // use CommandLogger if it has not already logged for this CommandContext
                        if (ctx is not null && !CommandLogger.HasLoggedFor(ctx))
                        {
                            CommandLogger.Log(ctx,
                                writer: errorWriter.WriteLine,
                                includeSystemInfo: true,
                                includeAppConfig: false
                            );

                            errorWriter.WriteLine();
                        }

                        // print help for the target command or root command
                        // if the exception occurred before a command could be parsed
                        ctx?.PrintHelp();

                        return ExitCodes.Error.Result;
                    })
                    .UseCommandLogger()
                    .RegisterContainer(configs);

                return appRunner.Run(args);
            }
            catch (OperationCanceledException)
            {
                return 1;
            }
            catch (Exception e)
            {
                e.Print();
                return 1;
            }
        }

        private class CommandContextHolder
        {
            public CommandContext Context { get; set; } = null!;
        }

        private static AppRunner RegisterContainer(this AppRunner appRunner, AppConfigs configs)
        {
            var config = configs.Default ?? new AppConfig();

            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterInstance(configs);
            containerBuilder.RegisterInstance(config);

            containerBuilder.RegisterType<BitBucketRepoCommand>().InstancePerLifetimeScope();
            containerBuilder.RegisterType<GlobalConfigCommand>().InstancePerLifetimeScope();
            containerBuilder.RegisterType<LocalRepoCommand>().InstancePerLifetimeScope();

            containerBuilder.RegisterType<BbService>().InstancePerLifetimeScope();
            containerBuilder.RegisterType<GitService>().InstancePerLifetimeScope();

            containerBuilder.RegisterServerBbApi(config);

            containerBuilder.RegisterType<CommandContextHolder>().InstancePerLifetimeScope();
            containerBuilder.Register(c => c.Resolve<CommandContextHolder>().Context)
                .As<CommandContext>()
                .InstancePerLifetimeScope();
            containerBuilder
                .Register(c => c.Resolve<CommandContext>().Console)
                .As<IConsole>()
                .InstancePerLifetimeScope();

            appRunner.Configure(r => r.UseMiddleware(
                SetCommandContextForDependencyResolver, 
                MiddlewareSteps.DependencyResolver.BeginScope + 1));

            return appRunner.UseAutofac(containerBuilder.Build());
        }

        private static Task<int> SetCommandContextForDependencyResolver(CommandContext context, ExecutionDelegate next)
        {
            var holder = (CommandContextHolder?)context!.DependencyResolver!.Resolve(typeof(CommandContextHolder));
            if (holder is not null)
            {
                holder.Context = context;
            }
            return next(context);
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