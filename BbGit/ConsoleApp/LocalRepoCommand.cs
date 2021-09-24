using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BbGit.BitBucket;
using BbGit.ConsoleUtils;
using BbGit.Framework;
using BbGit.Git;
using CommandDotNet;
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

        [Command(Description = "Clone all repositories matching the search criteria")]
        public void CloneAll(
            [Option(
                ShortName = "p",
                LongName = "projects",
                Description = "comma-separated list of project keys to filter by. " +
                              "overridden when piped input is given.")]
            string projects,
            [Option] bool dryrun)
        {
            var repositories = this.bbService.GetRepos(projects, true).ToList();

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
                    .SafelyForEach(r => this.gitService.CloneRepo(r), summarizeErrors: true);
            }
        }

        [Command(Description = "Pull all repositories in the parent directory.  " +
                                           "Piped input can be used to target specific repositories.")]
        public void PullAll(
            [Option] bool prune,
            [Option] bool dryrun,
            [Option] string branch = "master")
        {
            using (var repositories = this.gitService.GetLocalRepos(true))
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
                    repositories.SafelyForEach(r => this.gitService.PullLatest(r, prune, branch),
                        summarizeErrors: true);
                }
            }
        }

        [Command(Description = "Calls paket restore on all ")]
        public void Paket(
            [Option(ShortName = "i", LongName = "install")]
            bool install,
            [Option(ShortName = "r", LongName = "restore")]
            bool restore,
            [Option(ShortName = "R", LongName = "restore --force")]
            bool restoreForced,
            [Option(ShortName = "c", LongName = "clean")]
            bool cleanPackages,
            [Option(ShortName = "s", LongName = "show-installed-packages")]
            bool showInstalledPackages,
            [Option(ShortName = "b", LongName = "bootstrapper")]
            bool forceBootstapper,
            [Option(ShortName = "d", LongName = "dryrun")]
            bool dryrun)
        {
            // TODO: initialize only: only where packages is empty

            if (dryrun)
            {
                Console.WriteLineFormatted(
                    $"Running paket" +
                    $"{(dryrun ? " --dryrun" : "")}" +
                    $"{(cleanPackages ? " --clean" : "")}" +
                    $"{(install ? " --install" : "")}" +
                    $"{(restore && !install ? " --restore" : "")}" +
                    $"{(showInstalledPackages ? " --showInstalledPackages" : "")}",
                    Colors.DefaultColor);
            }

            using (var repositories = this.gitService.GetLocalRepos(true))
            {
                repositories.SafelyForEach(r =>
                {
                    var paketDir = Path.Combine(r.FullPath, ".paket");
                    if (!Directory.Exists(paketDir))
                    {
                        Console.Out.WriteLine($"skipping. no .paket directory");
                        return;
                    }

                    if (cleanPackages)
                    {
                        Console.Out.WriteLine($"cleaning packages");
                        if (!dryrun)
                        {
                            Directory.Delete(Path.Combine(r.FullPath, "packages"), true);
                        }
                    }

                    if (forceBootstapper || !File.Exists(Path.Combine(paketDir, "paket.exe")))
                    {
                        Console.Out.WriteLine($"running paket.bootstrapper.exe");
                        if (!dryrun)
                        {
                            RunProcessAndWait(
                                new ProcessStartInfo("paket.bootstrapper.exe") {WorkingDirectory = paketDir});
                        }
                    }

                    if (install)
                    {
                        Console.Out.WriteLine($"running paket install");
                        if (!dryrun)
                        {
                            RunProcessAndWait(
                                new ProcessStartInfo("paket.exe", "install") {WorkingDirectory = paketDir},
                                120);
                        }
                    }
                    else if (restore)
                    {
                        Console.Out.WriteLine($"running paket restore");
                        if (!dryrun)
                        {
                            RunProcessAndWait(
                                new ProcessStartInfo("paket.exe", "restore") {WorkingDirectory = paketDir},
                                60);
                        }
                    }
                    else if (restoreForced)
                    {
                        Console.Out.WriteLine($"running paket restore --force");
                        if (!dryrun)
                        {
                            RunProcessAndWait(
                                new ProcessStartInfo("paket.exe", "restore --force") {WorkingDirectory = paketDir},
                                60);
                        }
                    }
                    
                    if (showInstalledPackages)
                    {
                        Console.Out.WriteLine($"running paket show-installed-packages");
                        if (!dryrun)
                        {
                            RunProcessAndWait(
                                new ProcessStartInfo("paket.exe", "show-installed-packages") {WorkingDirectory = paketDir},
                                60);
                        }
                    }
                }, summarizeErrors: true);
            }
        }

        private static void RunProcessAndWait(ProcessStartInfo startInfo, int maxSecondsToWait = 30)
        {
            var processStartInfo = startInfo;
            var process = Process.Start(processStartInfo);
            process?.WaitForExit(1000 * maxSecondsToWait);
        }

        private static string CountToString(int count)
        {
            return count == 0 ? "" : count.ToString();
        }
    }
}