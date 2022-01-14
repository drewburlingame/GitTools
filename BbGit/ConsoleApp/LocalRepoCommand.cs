using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BbGit.Framework;
using BbGit.Git;
using CommandDotNet;
using static MoreLinq.Extensions.ForEachExtension;
using Spectre.Console;
using Table=BbGit.Tables.Table;

namespace BbGit.ConsoleApp
{
    [Command("local", Description = "manage local BitBucket repositories")]
    public class LocalRepoCommand
    {
        private readonly GitService _gitService;
        
        public LocalRepoCommand(GitService gitService)
        {
            _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        }

        [Command(Description = "List local repositories matching the search criteria")]
        public void Repo(
            IAnsiConsole console, CancellationToken cancellationToken,
            TableFormatModel tableFormatModel,
            ProjOrRepoKeys projOrRepoKeys,
            [Option('n', Description = "regex to match name")]
            string? namePattern,
            [Option("branch", Description = "return only with the branch name")]
            string? branchPattern,
            [Option('o', null, Description = "output only repo names")]
            bool outputNames = false,
            [Option("opk", Description = "output only project keys")]
            bool outputProjectKeys = false,
            [Option('l', Description = "where local branch is checked out")]
            bool isInLocalBranch = false,
            [Option('b', Description = "with local branches")]
            bool withLocalBranches = false,
            [Option('c', Description = "with changes: staged or unstaged")]
            bool withLocalChanges = false,
            [Option('s', Description = "with stashes")]
            bool withLocalStashes = false,
            [Option('r', Description = "treat options:b,w,s as `OR` instead of `AND`")]
            bool orLocalChecks = false,
            [Option('d', Description = "has no remote repo")]
            bool noRemote = false,
            [Option(Description = "inverts the filter")]
            bool not = false)
        {
            using var localRepos = _gitService.GetLocalRepos(projOrRepoKeys);

            var repos = localRepos.AsEnumerable();
            
            repos = repos.WhereMatches(r => r.Name, namePattern)
                .WhereMatches(r => r.CurrentBranchName, branchPattern)
                .WhereIf(noRemote, r => r.RemoteRepo is null)
                .WhereIf(isInLocalBranch, r => r.IsInLocalBranch);

            if (withLocalBranches || withLocalChanges || withLocalStashes)
            {
                repos = orLocalChecks
                    ? repos.Where(r =>
                        (!withLocalBranches || r.LocalBranchCount > 0)
                        || (!withLocalChanges || r.LocalChangesCount > 0)
                        || (!withLocalStashes || r.StashCount > 0)
                    )
                    : repos.Where(r =>
                        (!withLocalBranches || r.LocalBranchCount > 0)
                        && (!withLocalChanges || r.LocalChangesCount > 0)
                        && (!withLocalStashes || r.StashCount > 0)
                    );
            }

            if (not)
            {
                var reposToHide = repos.Select(r => r.Name).ToHashSet();
                repos = localRepos.Where(r => !reposToHide.Contains(r.Name));
            }

            if (outputNames || Console.IsOutputRedirected)
            {
                repos
                    .Select(r => r.Name)
                    .ForEach(console.WriteLine);
            }
            else if (outputProjectKeys)
            {
                repos
                    .Select(r => r.RemoteRepo?.ProjectKey)
                    .Where(k => k is not null)
                    .Distinct()
                    .OrderBy(k => k)
                    .ForEach(console.WriteLine!);
            }
            else
            {
                PrintReposTable(console, repos, tableFormatModel.GetTheme());
            }
        }

        [Command(Description = "Pull all repositories in the parent directory.  " +
                               "Piped input can be used to target specific repositories.")]
        public void Pull(
            IAnsiConsole console, CancellationToken cancellationToken,
            ProjOrRepoKeys projOrRepoKeys,
            DryRunArgs dryRunArgs,
            [Option] string? branch,
            [Option] bool prune = false)
        {
            using var repositories = _gitService.GetLocalRepos(projOrRepoKeys);

            if (dryRunArgs.IsDryRun)
            {
                console.WriteLine();
                console.WriteLine("dryrun: the following repositories would be pulled");
                var records = repositories.OrderBy(r => r.Name);
                PrintReposTable(AnsiConsole.Console, records);
            }
            else
            {
                repositories.SafelyForEach(
                    r => _gitService.PullLatest(r, prune, branch),
                    cancellationToken,
                    summarizeErrors: true);
            }
        }

        [Command(
            Description = "executes the command for each repository",
            ExtendedHelpText = RepositoryExecutor.LocalRepoExtendedHelpText,
            ArgumentSeparatorStrategy = ArgumentSeparatorStrategy.PassThru)]
        public void Exec(CommandContext context,
            IConsole console, CancellationToken cancellationToken,
            ProjOrRepoKeys projOrRepoKeys,
            [Option('c',
                Description = "Use the current directory as the working directly, else the repository directory is used")]
            bool useCurrentDirectory)
        {
            using var repositories = _gitService.GetLocalRepos(projOrRepoKeys);
            new RepositoryExecutor(context, console, cancellationToken, useCurrentDirectory).ExecuteFor(repositories);
        }

        [Command]
        public void Delete(
            IAnsiConsole console, CancellationToken cancellationToken,
            ProjOrRepoKeys projOrRepoKeys,
            DryRunArgs dryRunArgs)
        {
            using var repositories = _gitService.GetLocalRepos(projOrRepoKeys);

            IEnumerable<string> BuildWarning(LocalRepo r)
            {
                if (r.LocalChangesCount > 0)
                {
                    yield return $"{r.LocalChangesCount} local changes";
                }
                if (r.LocalBranchCount > 0)
                {
                    yield return $"{r.LocalBranchCount} local branches";
                }
                if (r.StashCount > 0)
                {
                    yield return $"{r.StashCount} local stashes";
                }
            }

            if (dryRunArgs.IsDryRun)
            {
                console.WriteLine();
                console.WriteLine("dryrun: the following repositories would be deleted");
                var records = repositories.OrderBy(r => r.Name);
                PrintReposTable(console, records);
            }
            else
            {
                repositories.SafelyForEach(
                    r =>
                    {
                        var warning = BuildWarning(r).ToCsv();
                        if (!warning.IsNullOrEmpty())
                        {
                            if (!console.Confirm($"{r.Name} has {warning}. Would you still like to delete?"))
                            {
                                console.WriteLine($"skipping {r.Name}");
                                return;
                            }
                        }

                        try
                        {
                            r.Dispose();
                            console.WriteLine($"Deleting {r.FullPath}");
                            Directory.Delete(r.FullPath, true);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            r.FullPath.SetFileAttributes(FileAttributes.Normal);
                            Directory.Delete(r.FullPath, true);
                        }
                    },
                    cancellationToken,
                    summarizeErrors: true);
            }
        }

        private static void PrintReposTable(IAnsiConsole console, IEnumerable<LocalRepo> repos, TableBorder? tableBorder = null)
        {
            var records = repos.Select(l => new
            {
                repo = l.Name,
                project = l.RemoteRepo?.ProjectKey,
                changes = l.LocalChangesCount,
                branches = l.LocalBranchCount,
                stashes = l.StashCount,
                branch = l.CurrentBranchName + (l.IsInLocalBranch ? " * " : "")
            });
            new Table(console, tableBorder, includeCount: true)
                {
                    HideZeros = true
                }
                .Write(records);
        }
    }
}