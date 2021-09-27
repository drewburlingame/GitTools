using System.Diagnostics;
using System.Threading.Tasks;
using CommandDotNet;
using CommandDotNet.Directives;
using CommandDotNet.Execution;

namespace BbGit.ConsoleApp
{
    internal static class TimeDirective
    {
        internal static AppRunner UseTimerDirective(this AppRunner appRunner)
        {
            return appRunner.Configure(c =>
            {
                c.UseMiddleware(TimerDirective, MiddlewareSteps.DebugDirective + 1);
            });
        }

        private static Task<int> TimerDirective(CommandContext context, ExecutionDelegate next)
        {
            if (context.Original.Tokens.TryGetDirective("timer", out _))
            {
                var sw = Stopwatch.StartNew();
                var result = next(context);
                sw.Stop();
                context.Console.WriteLine();
                context.Console.WriteLine($"Timer: {sw.Elapsed}");
                return result;
            }

            return next(context);
        }
    }
}