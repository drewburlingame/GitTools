using System;
using System.Collections.Generic;
using System.Linq;
using BbGit.BitBucket;
using BbGit.Framework;
using BbGit.Git;
using CommandDotNet.Attributes;
using MoreLinq;
using SharpBucket.V2.Pocos;

namespace BbGit.ConsoleApp
{
    [ApplicationMetadata(Name = "config-repo", Description = "Repo configurations in the .bbgit folder")]
    public class RepoConfigCommand
    {
        [InjectProperty]
        public BbService BbService { get; set; }

        [InjectProperty]
        public GitService GitService { get; set; }


        [ApplicationMetadata(
            Description = "Set's configs for managing repositories located in the working directory. " +
                          "Piped options are not filtered by ignore regex's. " +
                          "To clear a string config, set it to \" \"")]
        public void Set(
            [Option(LongName = "ilr", Description ="regex to ignore local repos when listing or performing bulk operations.")] 
            string ignoreLocalReposRegex,
            [Option(LongName = "irr", Description ="regex to ignore remote repos when listing or performing bulk operations.")]
            string ignoredRemoteReposRegex,
            [Option(LongName = "irp", Description ="regex to ignore remote projects when listing or performing bulk operations.")]
            string ignoredRemoteProjectsRegex)
        {
            if (ignoreLocalReposRegex != null)
            {
                var config = GitService.GetLocalReposConfig();
                config.IgnoredReposRegex = ignoreLocalReposRegex;
                GitService.SaveLocalReposConfig(config);
            }

            if (ignoredRemoteReposRegex != null || ignoredRemoteProjectsRegex != null)
            {
                var config = BbService.GetRemoteReposConfig();
                if (ignoredRemoteReposRegex != null)
                {
                    config.IgnoredReposRegex = ignoredRemoteReposRegex;
                }
                if (ignoredRemoteProjectsRegex != null)
                {
                    config.IgnoredProjectsRegex = ignoredRemoteProjectsRegex;
                }
                BbService.SaveRemoteReposConfig(config);
            }
        }

        public void List()
        {
            var localConfig = GitService.GetLocalReposConfig();
            Console.Out.WriteLine("");
            Console.Out.WriteLine($"ignore local repos: --ilr    = {localConfig.IgnoredReposRegex}");
            var remoteConfig = BbService.GetRemoteReposConfig();
            Console.Out.WriteLine($"ignore remote repos: --irr   = {remoteConfig.IgnoredReposRegex}");
            Console.Out.WriteLine($"ignore remote projects --irp = {remoteConfig.IgnoredProjectsRegex}");
            Console.Out.WriteLine("");
        }
        
        [ApplicationMetadata(
            Description = "updates the BbGit configurations in the local repos", 
            ExtendedHelpText = "use this when repositories were not cloned by BbGit or when using a new version of BbGit that uses updated configs.")]
        public void LocalConfigsUpdate()
        {
            var remoteRepos = BbService.GetRepos(usePipedValuesIfAvailable: true).ToList();

            using (var localRepos = GitService.GetLocalRepos(usePipedValuesIfAvailable: true))
            {
                var joinedRepos = LeftJoinRepos(localRepos, remoteRepos).Where(j => j.remote != null);

                joinedRepos.SafelyForEach(
                    j => GitService.UpdateRepoConfigs(new LocalRepo(j.local) { RemoteRepo = new RemoteRepo(j.remote) }),
                    summarizeErrors: true);
            }
        }

        [ApplicationMetadata(Description = "removes the BbGit configurations and .bbgit folder from the local repos")]
        public void LocalConfigsClear()
        {
            using (var localRepos = GitService.GetLocalRepos(usePipedValuesIfAvailable: true))
            {
                localRepos.SafelyForEach(
                    j => GitService.ClearRepoConfigs(new LocalRepo(j)),
                    summarizeErrors: true);
            }
        }

        private static List<(LocalRepo local, Repository remote)> LeftJoinRepos(IEnumerable<LocalRepo> localRepos, IEnumerable<Repository> remoteRepos)
        {
            return localRepos
                .LeftJoin(
                    remoteRepos,
                    lr => lr.Name,
                    rr => rr.name,
                    lr => (lr, null),
                    (lr, rr) => (lr, rr))
                .ToList();
        }
    }
}