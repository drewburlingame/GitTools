﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BbGit.BitBucket;
using BbGit.Framework;
using BbGit.Git;
using CommandDotNet;
using static MoreLinq.Extensions.ForEachExtension;
using Spectre.Console;
using Table = BbGit.Tables.Table;

namespace BbGit.ConsoleApp
{
    [Command("bb", Description = "Query repository info via BitBucket API")]
    public class BitBucketRepoCommand
    {
        private readonly BbService _bbService;
        private readonly GitService _gitService;

        public BitBucketRepoCommand(BbService bbService, GitService gitService)
        {
            _bbService = bbService ?? throw new ArgumentNullException(nameof(bbService));
            _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        }

        public Task<int> Interceptor(InterceptorExecutionDelegate next,
            [Option('i')] bool ignoreCache = false,
            [Option('s')] bool skipCacheRefresh = false,
            [Option('f')] bool forceCacheRefresh = false,
            [Option('w')] bool warnOnCacheRefresh = false)
        {
            _bbService.RefreshCaches(ignoreCache, skipCacheRefresh, forceCacheRefresh, warnOnCacheRefresh);
            return next();
        }

        [Command(
            Description = "List projects from the server",
            ExtendedHelpText =
                "for regex flags, see https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options")]
        public int Proj(IAnsiConsole console, CancellationToken cancellationToken,
            TableFormatModel tableFormatModel,
            [Option('k', Description = "regex to match key. Prefix with $! for NOT matching.")]
            Regex? keyPattern,
            [Option('n', Description = "regex to match name. Prefix with $! for NOT matching.")]
            Regex? namePattern,
            [Option('d', Description = "regex to match description. Prefix with $! for NOT matching.")]
            Regex? descPattern,
            [Option('o', null, Description = "output only keys")]
            bool outputKeys = false)
        {
            var projects = _bbService
                .GetProjects()
                .WhereMatches(p => p.Key, keyPattern)
                .WhereMatches(p => p.Name, namePattern)
                .WhereMatches(p => p.Description, descPattern)
                .OrderBy(p => p.Name)
                .ToCollection();

            if (outputKeys)
            {
                projects
                    .Select(p => p.Key)
                    .ForEach(console.WriteLine);
            }
            else
            {
                var localRepos = _gitService.GetLocalRepos();

                var remoteRepos = _bbService
                    .GetRepos(projects.Select(p => p.Name).ToCollection());

                var repoPairs = localRepos.PairRepos(remoteRepos, mustHaveRemote: true).Values;

                var remoteRepoCounts = repoPairs
                    .Where(p => p.Remote is not null)
                    .GroupBy(p => p.Remote!.ProjectKey)
                    .ToDictionary(
                        g => g.Key,
                        g => new { remote = g.Count(), local = g.Count(r => r.Local is not null) });

                var records = projects
                    .Select(p => new
                    {
                        p.Key,
                        p.Name,
                        p.Description,
                        p.Id,
                        p.Public,
                        p.Type,
                        Repos = remoteRepoCounts.GetValueOrDefault(p.Key)?.remote ?? 0,
                        Cloned = remoteRepoCounts.GetValueOrDefault(p.Key)?.local ?? 0
                    });

                new Table(console, tableFormatModel?.GetTheme(), includeCount: true)
                    .OverrideColumn(records, r => r.Description, c =>
                    {
                        c.WrapText = true;
                    })
                    .Write(records);
            }

            return ExitCodes.Success.Result;
        }

        [Command(Description = "show the repos for the given projects")]
        public void Repo(
            IAnsiConsole console, CancellationToken cancellationToken, 
            TableFormatModel tableFormatModel,
            ProjOrRepoKeys projOrRepoKeys,
            [Option('n', Description = "regex to match name. Prefix with $! for NOT matching.")]
            Regex? namePattern,
            [Option('d', Description = "regex to match description. Prefix with $! for NOT matching.")]
            Regex? descPattern,
            [Option('o', null, Description = "output only names")]
            bool outputNames = false,
            [Option('c', Description = "return only cloned")]
            bool cloned = false,
            [Option('u', Description = "return only uncloned")]
            bool uncloned = false,
            [Option("public", Description = "return only public")]
            bool onlyPublic = false,
            [Option("private", Description = "return only private")]
            bool onlyPrivate = false,
            [Option('x', Description = "exclude repositories marked obsolete")]
            bool excludeObsolete = false)
        {
            if (cloned && uncloned)
            {
                throw new ArgumentException("cannot request both cloned and uncloned");
            }
            if (onlyPublic && onlyPrivate)
            {
                throw new ArgumentException("cannot request both public and private");
            }

            var projKeys = projOrRepoKeys.Projects?.ToHashSet();
            var repoKeys = projOrRepoKeys.Repos?.ToHashSet();

            var projects = _bbService
                .GetProjects()
                .WhereIf(projKeys is not null, p => projKeys!.Contains(p.Key))
                .ToCollection();

            var localRepos = _gitService.GetLocalRepos();

            var remoteRepos = _bbService
                .GetRepos(projects.Select(p => p.Name).ToCollection())
                .WhereMatches(r => r.Name, namePattern)
                .WhereMatches(r => r.Description, descPattern)
                .WhereIf(onlyPublic, r => r.Public)
                .WhereIf(onlyPrivate, r => !r.Public)
                .WhereIf(repoKeys is not null, r => repoKeys!.Contains(r.Name))
                .WhereIf(excludeObsolete, r => !r.Name.Contains("obsolete", StringComparison.OrdinalIgnoreCase));

            bool? isCloned = cloned ? true : uncloned ? false : null;
            var repoPairs = localRepos.PairRepos(remoteRepos, mustHaveRemote: true, isCloned).Values;

            if (outputNames)
            {
                repoPairs
                    .OrderBy(p => p.Remote!.Description)
                    .Select(p => p.Remote!.Name)
                    .ForEach(console.WriteLine);
            }
            else
            {
                var rows = repoPairs
                    .OrderBy(p => p.Remote!.Description)
                    .Select(p => new
                    {
                        p.Remote!.Name,
                        p.Remote.ProjectKey,
                        Cloned = p.Local is not null,
                        p.Remote.Public,
                        p.Remote.StatusMessage
                    });
                new Table(console, tableFormatModel?.GetTheme(), includeCount: true)
                    .Write(rows);
            }
        }

        [Command(Description = "Clone all repositories matching the search criteria")]
        public void Clone(CancellationToken ct,
            [Operand] [Required] string[] repos,
            [Option("ssh", Description = "Set origin to ssh after completed")] 
            bool setSshOrigin = false)
        {
            _bbService.GetRepos()
                .Where(r => repos.Contains(r.Name))
                .UntilCancelled(ct)
                .SafelyForEach(
                    r => _gitService.CloneRepo(r, setSshOrigin), 
                    ct, summarizeErrors: true);
        }

        [Command(
            Description = "executes the command for each repository",
            ExtendedHelpText = RepositoryExecutor.RemoteRepoExtendedHelpText,
            ArgumentSeparatorStrategy = ArgumentSeparatorStrategy.PassThru)]
        public void Exec(CommandContext context,
            IConsole console, CancellationToken cancellationToken,
            [Operand][Required] string[] repos,
            [Option('c', Description = "Use the current directory as the working directly, else the repository directory is used")] 
            bool useCurrentDirectory = false)
        {
            var repositories = _bbService.GetRepos()
                .Where(r => repos.Contains(r.Name));

            new RepositoryExecutor(context, console, cancellationToken, useCurrentDirectory)
                .ExecuteFor(repositories);
        }
    }
}