﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BbGit.BitBucket;
using BbGit.ConsoleUtils;
using BbGit.Framework;
using BbGit.Git;
using BbGit.Tables;
using CommandDotNet;
using CommandDotNet.Rendering;
using Console = Colorful.Console;

namespace BbGit.ConsoleApp
{
    [Command(
        Name = "local",
        Description = "manage local BitBucket repositories")]
    public class LocalRepoCommand
    {
        private readonly BbService bbService;
        private readonly GitService gitService;

        public LocalRepoCommand(BbService bbService, GitService gitService)
        {
            this.bbService = bbService ?? throw new ArgumentNullException(nameof(bbService));
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        }

        [Command(Description = "List local repositories matching the search criteria")]
        public void Repos(
            IConsole console,
            [Option(
                ShortName = "p",
                LongName = "projects",
                Description = "comma-separated list of project keys to filter by.  " +
                              "use '*' to specify has remote repo config.  " +
                              "(some commands require the config to use)")]
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
            bool orLocalChecks,
            [Option(
                ShortName = "i",
                LongName = "includeIgnored",
                Description = "includes projects ignored in configuration")]
            bool includeIgnored,
            [Option(
                ShortName = "I",
                LongName = "onlyIgnored",
                Description = "lists only projects ignored in configuration")]
            bool onlyIgnored)
        {
            using (var localRepos = this.gitService.GetLocalRepos())
            {
                console.WriteLine($"found {localRepos.Count} repos");

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
                            !withLocalBranches || r.LocalBranchCount > 0 || !withLocalChanges ||
                            r.LocalChangesCount > 0 || !withLocalStashes || r.StashCount > 0
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
                        var projKeys = projects.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                            .ToHashSet();
                        repos = repos.Where(l => l.RemoteRepo != null && projKeys.Contains(l.RemoteRepo.ProjectKey));
                    }
                }

                var filteredRepos = repos.ToList();

                if (showTable)
                {
                    filteredRepos.Where(l => projects == null || l.RemoteRepo != null);

                    if (showProjectInfo)
                    {
                        var records = filteredRepos.Select(l => new
                        {
                            repo = l.Name,
                            project = l.RemoteRepo?.ProjectKey,
                            changes = CountToString(l.LocalChangesCount),
                            branches = CountToString(l.LocalBranchCount),
                            stashes = CountToString(l.StashCount),
                            branch = l.IsInLocalBranch ? l.CurrentBranchName : ""
                        });
                        new Table(console, ((TableFormatModel)null)?.GetTheme(), includeCount: true)
                            .Write(records);
                    }
                    else
                    {
                        var records = filteredRepos.Select(l => new
                        {
                            repo = l.Name,
                            changes = CountToString(l.LocalChangesCount),
                            branches = CountToString(l.LocalBranchCount),
                            stashes = CountToString(l.StashCount),
                            branch = l.IsInLocalBranch ? l.CurrentBranchName : ""
                        });
                        new Table(console, ((TableFormatModel)null)?.GetTheme(), includeCount: true)
                            .Write(records);
                    }
                }
                else
                {
                    filteredRepos.ForEach(l => Console.Out.WriteLine(l.Name));
                }
            }
        }

        [Command(Description = "Clone all repositories matching the search criteria")]
        public async void CloneAll(
            IConsole console,
            OnlyReposOperandList onlyRepos,
            [Option(
                ShortName = "p",
                LongName = "projects",
                Description = "comma-separated list of project keys to filter by. " +
                              "overridden when piped input is given.")]
            string projects,
            [Option] bool dryrun)
        {
            var repositories = (await this.bbService.GetRepos(projects, onlyRepos: onlyRepos.RepoNames)).ToList();

            if (dryrun)
            {
                IEnumerable<string> records = repositories
                    .OrderBy(r => r.Slug)
                    .Select(r => $"{r.Slug} ({r.Project.Key})");
                new Table(console, ((TableFormatModel)null)?.GetTheme(), includeCount: true)
                    .Write(records);
            }
            else
            {
                repositories
                    .Select(r => new RemoteRepo(r))
                    .SafelyForEach(r => this.gitService.CloneRepo(r), summarizeErrors: true);
            }
        }

        [Command(Description = "Pull all repositories in the parent directory.  " +
                                           "Piped input can be used to target specific repositories.")]
        public void PullAll(
            IConsole console,
            OnlyReposOperandList onlyRepos,
            [Option] bool prune,
            [Option] bool dryrun,
            [Option] string branch = "master")
        {
            using (var repositories = this.gitService.GetLocalRepos(onlyRepos: onlyRepos.RepoNames))
            {
                if (dryrun)
                {
                    IEnumerable<string> records = repositories
                        .OrderBy(r => r.Name)
                        .Select(r => r.Name);
                    new Table(console, ((TableFormatModel)null)?.GetTheme(), includeCount: true)
                        .Write(records);
                }
                else
                {
                    repositories.SafelyForEach(r => this.gitService.PullLatest(r, prune, branch),
                        summarizeErrors: true);
                }
            }
        }

        private static string CountToString(int count)
        {
            return count == 0 ? "" : count.ToString();
        }
    }
}