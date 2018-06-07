using BbGit.Framework;
using CommandDotNet.Attributes;

namespace BbGit.ConsoleApp
{
    [ApplicationMetadata(Name = "BbGit", Description = "GitTools for BitBucket")]
    public class GitApplication
    {

        private readonly string targetDir;
        private DirectoryResolver directoryResolver;

        [SubCommand]
        public BitBucketRepoCommand BitBucketRepoCommand { get; set; }

        [SubCommand]
        public LocalRepoCommand LocalRepoCommand { get; set; }

        [SubCommand]
        public GlobalConfigCommand GlobalConfigCommand { get; set; }

        [SubCommand]
        public RepoConfigCommand RepoConfigCommand { get; set; }

        [InjectProperty]
        public DirectoryResolver DirectoryResolver
        {
            get => directoryResolver;
            set
            {
                directoryResolver = value;
                directoryResolver.SetCurrentDirectory(targetDir);
            }
        }

        public GitApplication(
            [Option(ShortName = "d", LongName = "target-dir", Description = "specify a directory to use instead of the console's current working directory. useful for test automation")]
            string targetDir)
        {
            this.targetDir = targetDir;
        }
    }
}