using System;
using System.Linq;
using System.Text.RegularExpressions;
using BbGit.ConsoleUtils;
using BbGit.Framework;
using BbGit.Git;
using CommandDotNet.Attributes;
using MoreLinq;
using Console = System.Console;

namespace BbGit.ConsoleApp
{
    [ApplicationMetadata(Name = "local", Description = "commands for managing local BitBucket repositories")]
    public class LocalRepoCommand
    {
        [InjectProperty]
        public BbService BbService { get; set; }

        [InjectProperty]
        public GitService GitService { get; set; }

        [ApplicationMetadata(Description = "List local repositories matching the search criteria")]
        public void Repos(
            [Option(ShortName = "p", LongName = "projects", Description = "comma-separated list of project keys to filter by.  use '*' to specify has remote repo config.  (some commands require the config to use)")]
            string projects,
            [Option(ShortName = "T", Description = "display additional info in a table format")]
            bool showTable,
            [Option(ShortName = "P", Description = "show project information. requires BB API call")]
            bool showProjectInfo,
            [Option(ShortName = "r", LongName = "repo", Description = "regex to filter repo name by")]
            string repoRegex,
            [Option(ShortName = "l", Description = "where local branch is checked out")]
            bool isInLocalBranch,
            [Option(ShortName = "b", Description = "with local branches")]
            bool withLocalBranches,
            [Option(ShortName = "w", Description = "with changes: staged or unstaged")]
            bool withLocalChanges,
            [Option(ShortName = "s", Description = "with stashes")]
            bool withLocalStashes,
            [Option(ShortName = "o", Description = "where options:b,w,s are `OR` instead of `AND`")]
            bool orLocalChecks)
        {
            using (var localRepos = GitService.GetLocalRepos())
            {
                var repos = localRepos.AsEnumerable();

                if (repoRegex != null)
                {
                    var regex = new Regex(repoRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    repos = repos.Where(r => regex.IsMatch(r.Name));
                }

                if (isInLocalBranch)
                {
                    repos = repos.Where(r => r.IsInLocalBranch);
                }

                if (withLocalBranches || withLocalChanges || withLocalStashes)
                {
                    if (orLocalChecks)
                    {
                        repos = repos.Where(r =>
                            (!withLocalBranches || r.LocalBranchCount > 0)
                            || (!withLocalChanges || r.LocalChangesCount > 0)
                            || (!withLocalStashes || r.StashCount > 0)
                        );
                    }
                    else
                    {
                        repos = repos.Where(r =>
                            (!withLocalBranches || r.LocalBranchCount > 0)
                            && (!withLocalChanges || r.LocalChangesCount > 0)
                            && (!withLocalStashes || r.StashCount > 0)
                        );
                    }
                }

                if (projects != null)
                {
                    if (projects == "*")
                    {
                        repos = repos.Where(l => l.RemoteRepo != null);
                    }
                    else
                    {
                        var projKeys = projects.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                        repos = repos.Where(l => l.RemoteRepo != null && projKeys.Contains(l.RemoteRepo.ProjectKey));
                    }
                }

                var filteredRepos = repos.ToList();

                if (showTable)
                {
                    var headers = showProjectInfo
                        ? new[] {"repo", "project", "changes", "branches", "stashes", "branch"}
                        : new[] {"repo", "changes", "branches", "stashes", "branch"};

                    filteredRepos
                        .Where(l => projects == null || l.RemoteRepo != null)
                        .Select(l => showProjectInfo
                            ? new[]
                            {
                                l.Name,
                                l.RemoteRepo?.ProjectKey,
                                CountToString(l.LocalChangesCount),
                                CountToString(l.LocalBranchCount),
                                CountToString(l.StashCount),
                                l.IsInLocalBranch ? l.CurrentBranchName : ""
                            }
                            : new[]
                            {
                                l.Name,
                                CountToString(l.LocalChangesCount),
                                CountToString(l.LocalBranchCount),
                                CountToString(l.StashCount),
                                l.IsInLocalBranch ? l.CurrentBranchName : ""
                            })
                        .WriteTable(headers);
                }
                else
                {
                    filteredRepos.ForEach(l => Console.Out.WriteLine(l.Name));
                }
            }
        }

        [ApplicationMetadata(Description = "Clone all repositories matching the search criteria")]
        public void CloneAll(
            [Option(ShortName = "p", LongName = "projects", Description = "comma-separated list of project keys to filter by. overridden when piped input is given.")] string projects,
            [Option] bool dryrun)
        {
            var repositories = BbService.GetRepos(projects, usePipedValuesIfAvailable: true).ToList();

            if (dryrun)
            {
                repositories
                    .OrderBy(r => r.name)
                    .Select(r => $"{r.name} ({r.project.key})")
                    .WriteTable();
            }
            else
            {
                repositories
                    .Select(r => new RemoteRepo(r))
                    .SafelyForEach(r => GitService.CloneRepo(r), summarizeErrors: true);
            }
        }

        [ApplicationMetadata(Description = "Pull all repositories in the parent directory.  Piped input can be used to target specific repositories.")]
        public void PullAll(
            [Option] bool prune,
            [Option] bool dryrun,
            [Option] string branch = "master")
        {
            using (var repositories = GitService.GetLocalRepos(usePipedValuesIfAvailable: true))
            {
                if (dryrun)
                {
                    repositories
                        .OrderBy(r => r.Name)
                        .Select(r => r.Name)
                        .WriteTable();
                }
                else
                {
                    repositories.SafelyForEach(r => GitService.PullLatest(r, prune, branch), summarizeErrors: true);
                }
            }
        }

        private static string CountToString(int count)
        {
            return count == 0 ? "" : count.ToString();
        }
    }
}