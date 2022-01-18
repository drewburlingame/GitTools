using CommandDotNet;

namespace BbGit.ConsoleApp
{
    public class DryRunArgs : IArgumentModel
    {
        [Option("dryrun")] public bool IsDryRun { get; set; } = false;
    }
}