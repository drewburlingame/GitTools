using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BbGit.Framework;
using BbGit.Git;
using BbGit.Tables;
using CliWrap;
using CommandDotNet;
using CommandDotNet.Prompts;
using CommandDotNet.Rendering;
using static MoreLinq.Extensions.ForEachExtension;
using Command = CliWrap.Command;

namespace BbGit.ConsoleApp
{
    [Command(
        Name = "local",
        Description = "manage local BitBucket repositories")]
    public class LocalRepoCommand
    {
        private readonly GitService gitService;

        public LocalRepoCommand(GitService gitService)
        {
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        }

        [Command(Description = "List local repositories matching the search criteria")]
        public void Repo(
            IConsole console, CancellationToken cancellationToken,
            TableFormatModel tableFormatModel,
            ProjOrRepoKeys projOrRepoKeys,
            [Option(ShortName = "n", Description = "regex to match name")]
            string namePattern,
            [Option(Description = "output only repo names")]
            bool names,
            [Option(Description = "output only project keys")]
            bool projKeys,
            [Option(LongName = "branch", Description = "return only with the branch name")]
            string branchPattern,
            [Option(ShortName = "l", Description = "where local branch is checked out")]
            bool isInLocalBranch,
            [Option(ShortName = "b", Description = "with local branches")]
            bool withLocalBranches,
            [Option(ShortName = "c", Description = "with changes: staged or unstaged")]
            bool withLocalChanges,
            [Option(ShortName = "s", Description = "with stashes")]
            bool withLocalStashes,
            [Option(ShortName = "o", Description = "treat options:b,w,s as `OR` instead of `AND`")]
            bool orLocalChecks,
            [Option(ShortName = "d", Description = "has no remote repo")]
            bool noRemote,
            [Option(Description = "inverts the filter")]
            bool not)
        {
            using var localRepos = this.gitService.GetLocalRepos(projOrRepoKeys);

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

            if (names || Console.IsOutputRedirected)
            {
                repos
                    .Select(r => r.Name)
                    .ForEach(console.WriteLine);
            }
            else if (projKeys)
            {
                repos
                    .Select(r => r.RemoteRepo?.ProjectKey)
                    .Where(k => k is not null)
                    .Distinct()
                    .OrderBy(k => k)
                    .ForEach(console.WriteLine);
            }
            else
            {
                PrintReposTable(console, cancellationToken, repos, tableFormatModel.GetTheme());
            }
        }

        [Command(Description = "Pull all repositories in the parent directory.  " +
                               "Piped input can be used to target specific repositories.")]
        public void Pull(
            IConsole console, CancellationToken cancellationToken,
            ProjOrRepoKeys projOrRepoKeys,
            [Option] bool prune,
            [Option] bool dryrun,
            [Option] string branch)
        {
            using var repositories = this.gitService.GetLocalRepos(projOrRepoKeys);

            if (dryrun)
            {
                console.WriteLine();
                console.WriteLine("dryrun: the following repositories would be pulled");
                var records = repositories.OrderBy(r => r.Name);
                PrintReposTable(console, cancellationToken, records, TableTheme.ColumnLines);
            }
            else
            {
                repositories.SafelyForEach(
                    r => this.gitService.PullLatest(r, prune, branch),
                    cancellationToken,
                    summarizeErrors: true);
            }
        }

        [Command]
        public void SetOriginToHttp(IConsole console, CancellationToken cancellationToken, ProjOrRepoKeys projOrRepoKeys)
        {
            using var repositories = this.gitService.GetLocalRepos(projOrRepoKeys);
            repositories
                .Where(r => r.RemoteRepo is not null)
                .SafelyForEach(r => r.SetOriginToHttp(),
                cancellationToken,
                summarizeErrors: true);
        }

        [Command]
        public void SetOriginToSsh(IConsole console, CancellationToken cancellationToken, ProjOrRepoKeys projOrRepoKeys)
        {
            using var repositories = this.gitService.GetLocalRepos(projOrRepoKeys);
            repositories
                .Where(r => r.RemoteRepo is not null)
                .SafelyForEach(r => r.SetOriginToSsh(),
                cancellationToken,
                summarizeErrors: true);
        }

        [Command(
            Description = "executes the command for each repository",
            ArgumentSeparatorStrategy = ArgumentSeparatorStrategy.PassThru)]
        public void Exec(CommandContext context,
            IConsole console, CancellationToken cancellationToken,
            ProjOrRepoKeys projOrRepoKeys)
        {
            var separatedArguments = context.ParseResult!.SeparatedArguments;
            var program = separatedArguments.First();
            var arguments = separatedArguments.Skip(1).ToCsv(" ");

            using var repositories = this.gitService.GetLocalRepos(projOrRepoKeys);
            repositories.SafelyForEach(
                r =>
                {
                    Cli.Wrap(program)
                        .WithArguments(arguments)
                        .WithWorkingDirectory(r.FullPath)
                        .WithStandardOutputPipe(PipeTarget.ToDelegate(console.WriteLine))
                        .WithStandardErrorPipe(PipeTarget.ToDelegate(console.Error.WriteLine))
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteAsync(cancellationToken).Task.Wait(cancellationToken);
                },
                cancellationToken,
                summarizeErrors: true);
        }

        [Command]
        public void Delete(
            IConsole console, CancellationToken cancellationToken,
            IPrompter prompter,
            ProjOrRepoKeys projOrRepoKeys,
            [Option] bool dryrun)
        {
            using var repositories = this.gitService.GetLocalRepos(projOrRepoKeys);

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

            if (dryrun)
            {
                console.WriteLine();
                console.WriteLine("dryrun: the following repositories would be deleted");
                var records = repositories.OrderBy(r => r.Name);
                PrintReposTable(console, cancellationToken, records, TableTheme.ColumnLines);
            }
            else
            {
                repositories.SafelyForEach(
                    r =>
                    {
                        var warning = BuildWarning(r).ToCsv();
                        if (!warning.IsNullOrEmpty())
                        {
                            var answer = prompter.PromptForValue(
                                $"{r.Name} has {warning}. Would you still like to delete? y or n",
                                out var isCancellationRequested);

                            if (!answer.ToLower().IsIn("y","yes"))
                            {
                                console.WriteLine($"skipping {r.Name}");
                                return;
                            }
                        }

                        try
                        {
                            r.GitRepo.Dispose();
                            console.WriteLine($"Deleting {r.FullPath}");
                            Directory.Delete(r.FullPath, true);
                        }
                        catch (UnauthorizedAccessException uae)
                        {
                            r.FullPath.SetFileAttributes(FileAttributes.Normal);
                            Directory.Delete(r.FullPath, true);
                        }
                    },
                    cancellationToken,
                    summarizeErrors: true);
            }
        }

        private static void PrintReposTable(IConsole console, CancellationToken cancellationToken, IEnumerable<LocalRepo> repos, TableTheme tableTheme = null)
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
            new Table(console, tableTheme, includeCount: true, cancellationToken)
                {
                    HideZeros = true
                }
                .Write(records);
        }
    }
}