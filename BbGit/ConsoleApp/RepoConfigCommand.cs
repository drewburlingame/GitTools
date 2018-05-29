using System.Collections.Generic;
using System.Linq;
using BbGit.Framework;
using BbGit.Git;
using CommandDotNet.Attributes;
using MoreLinq;
using SharpBucket.V2.Pocos;

namespace BbGit.ConsoleApp
{
    [ApplicationMetadata(Name = "repo-config", Description = "commands to manage local repo BbGit configurations in the .bbgit folder")]
    public class RepoConfigCommand
    {
        [InjectProperty]
        public BbService BbService { get; set; }

        [InjectProperty]
        public GitService GitService { get; set; }

        [ApplicationMetadata(
            Description = "updates the BbGit configurations", 
            ExtendedHelpText = "use this when repositories were not cloned by BbGit or when using a new version of BbGit that uses updated configs.")]
        public void UpdateAllConfigs()
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

        [ApplicationMetadata(Description = "removes the BbGit configurations and .bbgit folder")]
        public void ClearAllConfigs()
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