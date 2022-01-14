using CommandDotNet;

namespace BbGit.ConsoleApp
{
    [Command("BbGit", Description = "GitTools for BitBucket")]
    public class GitApplication
    {
        [Subcommand]
        public BitBucketRepoCommand BitBucketRepoCommand { get; set; }

        [Subcommand]
        public LocalRepoCommand LocalRepoCommand { get; set; }

        [Subcommand]
        public GlobalConfigCommand GlobalConfigCommand { get; set; }
    }
}