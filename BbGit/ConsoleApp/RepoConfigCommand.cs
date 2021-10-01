using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BbGit.BitBucket;
using BbGit.Framework;
using BbGit.Git;
using Bitbucket.Net.Models.Core.Projects;
using CommandDotNet;
using MoreLinq;

namespace BbGit.ConsoleApp
{
    [Command(Name = "config-repo",
        Description = "Configurations for repos within the same parent folder.",
        ExtendedHelpText =
            "Creates {currentfolder}/.bbgit where currentFolder contains the repos.\n" +
            "\n" +
            "Terminology\n" +
            "  repos config:  configs for the set of repos stored in the parent folder\n" +
            "  local repos:   a repo cloned to this machine\n" +
            "  local configs: configs for a local repo\n" +
            "  remote repos:  repos in bitbucket")]
    public class RepoConfigCommand
    {
        private readonly BbService bbService;
        private readonly GitService gitService;

        public RepoConfigCommand(BbService bbService, GitService gitService)
        {
            this.bbService = bbService ?? throw new ArgumentNullException(nameof(bbService));
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        }

        [Command(
            Description = "Set's configs for managing repositories located in the working directory. \n" +
                          "To clear a string config, set it to \" \"")]
        public void Set(
            [Option(
                LongName = "ilr",
                Description = "regex to ignore local repos when listing or performing bulk operations.")]
            string ignoreLocalReposRegex,
            [Option(
                LongName = "irr",
                Description = "regex to ignore remote repos when listing or performing bulk operations.")]
            string ignoredRemoteReposRegex,
            [Option(
                LongName = "irp",
                Description = "regex to ignore remote projects when listing or performing bulk operations.")]
            string ignoredRemoteProjectsRegex)
        {
            if (ignoreLocalReposRegex != null)
            {
                var config = this.gitService.GetLocalReposConfig();
                config.IgnoredReposRegex = ignoreLocalReposRegex;
                this.gitService.SaveLocalReposConfig(config);
            }

            if (ignoredRemoteReposRegex != null || ignoredRemoteProjectsRegex != null)
            {
                var config = this.bbService.GetRemoteReposConfig();
                if (ignoredRemoteReposRegex != null)
                {
                    config.IgnoredReposRegex = ignoredRemoteReposRegex;
                }

                if (ignoredRemoteProjectsRegex != null)
                {
                    config.IgnoredProjectsRegex = ignoredRemoteProjectsRegex;
                }

                this.bbService.SaveRemoteReposConfig(config);
            }
        }

        [Command(Description = "prints the location of the repos config file")]
        public void Where(
            [Option(
                ShortName = "o",
                Description = "opens the directory in windows explorer")]
            bool open)
        {
            var path = this.gitService.GetLocalReposConfigPath();

            if (!Directory.Exists(path))
            {
                Console.Out.WriteLine($"repos config not set: {path}");
                return;
            }

            Console.Out.WriteLine(path);
            if (open && !path.IsNullOrEmpty())
            {
                Process.Start(path);
            }
        }

        public void List()
        {
            var localConfig = this.gitService.GetLocalReposConfig();
            Console.Out.WriteLine("");
            Console.Out.WriteLine($"ignore local repos: --ilr    = {localConfig.IgnoredReposRegex}");
            var remoteConfig = this.bbService.GetRemoteReposConfig();
            Console.Out.WriteLine($"ignore remote repos: --irr   = {remoteConfig.IgnoredReposRegex}");
            Console.Out.WriteLine($"ignore remote projects --irp = {remoteConfig.IgnoredProjectsRegex}");
            Console.Out.WriteLine("");
        }

        [Command(
            Description = "updates the BbGit configurations in the local repos",
            ExtendedHelpText = "use this when repositories were not cloned by BbGit " +
                               "or when using a new version of BbGit that uses updated configs.")]
        public async void LocalConfigsUpdate(OnlyReposOperandList onlyRepos)
        {
            var remoteRepos = (await this.bbService.GetRepos(onlyRepos: onlyRepos.RepoNames)).ToList();

            using (var localRepos = this.gitService.GetLocalRepos(onlyRepos: onlyRepos.RepoNames))
            {
                var joinedRepos = LeftJoinRepos(localRepos, remoteRepos).Where(j => j.remote != null);

                joinedRepos.SafelyForEach(
                    j => j.local.Name,
                    j => this.gitService.UpdateRepoConfigs(new LocalRepo(j.local)
                    {
                        RemoteRepo = new RemoteRepo(j.remote)
                    }),
                    summarizeErrors: true);
            }
        }

        [Command(Description = "removes the BbGit configurations and .bbgit folder from the local repos")]
        public void LocalConfigsClear(OnlyReposOperandList onlyRepos)
        {
            using (var localRepos = this.gitService.GetLocalRepos(onlyRepos.RepoNames))
            {
                localRepos.SafelyForEach(
                    j => this.gitService.ClearRepoConfigs(new LocalRepo(j)),
                    summarizeErrors: true);
            }
        }

        private static List<(LocalRepo local, Repository remote)> LeftJoinRepos(
            IEnumerable<LocalRepo> localRepos,
            IEnumerable<Repository> remoteRepos)
        {
            return localRepos
                .LeftJoin(
                    remoteRepos,
                    lr => lr.Name,
                    rr => rr.Name,
                    lr => (lr, null),
                    (lr, rr) => (lr, rr))
                .ToList();
        }
    }
}