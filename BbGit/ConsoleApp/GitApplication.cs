using CommandDotNet;

namespace BbGit.ConsoleApp
{
    [Command(Name = "BbGit", Description = "GitTools for BitBucket")]
    public class GitApplication
    {
        [SubCommand]
        public BitBucketRepoCommand BitBucketRepoCommand { get; set; }

        [SubCommand]
        public LocalRepoCommand LocalRepoCommand { get; set; }

        [SubCommand]
        public GlobalConfigCommand GlobalConfigCommand { get; set; }
    }
}