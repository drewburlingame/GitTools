using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BbGit.BitBucket;
using BbGit.Framework;
using BbGit.Git;
using BbGit.Tables;
using CommandDotNet;
using CommandDotNet.Rendering;
using static MoreLinq.Extensions.ForEachExtension;

namespace BbGit.ConsoleApp
{
    [Command(Name = "bb", Description = "Query repository info via BitBucket API")]
    public class BitBucketRepoCommand
    {
        private readonly BbService bbService;
        private readonly GitService gitService;

        public BitBucketRepoCommand(BbService bbService, GitService gitService)
        {
            this.bbService = bbService ?? throw new ArgumentNullException(nameof(bbService));
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        }

        public Task<int> Interceptor(InterceptorExecutionDelegate next,
            [Option(ShortName = "i")] bool ignoreCache,
            [Option(ShortName = "s")] bool skipCacheRefresh,
            [Option(ShortName = "f")] bool forceCacheRefresh,
            [Option(ShortName = "w")] bool warnOnCacheRefresh)
        {
            bbService.RefreshCaches(ignoreCache, skipCacheRefresh, forceCacheRefresh, warnOnCacheRefresh);
            return next();
        }

        [Command(
            Description = "List projects from the server",
            ExtendedHelpText =
                "for regex flags, see https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options")]
        public int Proj(IConsole console, CancellationToken cancellationToken,
            TableFormatModel tableFormatModel,
            [Option(ShortName = "k", Description = "regex to match key")]
            string keyPattern = null,
            [Option(ShortName = "n", Description = "regex to match name")]
            string namePattern = null,
            [Option(ShortName = "d", Description = "regex to match description")]
            string descPattern = null,
            [Option(Description = "output only keys")]
            bool keys = false)
        {
            var projects = this.bbService
                .GetProjects()
                .WhereMatches(p => p.Key, keyPattern)
                .WhereMatches(p => p.Name, namePattern)
                .WhereMatches(p => p.Description, descPattern)
                .OrderBy(p => p.Name)
                .ToCollection();

            if (keys)
            {
                projects
                    .Select(p => p.Key)
                    .ForEach(console.WriteLine);
            }
            else
            {
                var localRepos = this.gitService.GetLocalRepos();

                var remoteRepos = this.bbService
                    .GetRepos(projects.Select(p => p.Name).ToCollection());

                var repoPairs = localRepos.PairRepos(remoteRepos, mustHaveRemote: true).Values;

                var remoteRepoCounts = repoPairs
                    .GroupBy(p => p.Remote.ProjectKey)
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

                new Table(console, tableFormatModel?.GetTheme(), includeCount: true, cancellationToken)
                    .OverrideColumn(records, r => r.Key, c => c.WrapText = false)
                    .OverrideColumn(records, r => r.Name, c => c.WrapText = false)
                    .OverrideColumn(records, r => r.Type, c => c.WrapText = false)
                    .OverrideColumn(records, r => r.Description, c =>
                    {
                        c.WrapText = true;
                        c.MaxWidth = 70;
                    })
                    .Write(records);
            }

            return ExitCodes.Success.Result;
        }

        [Command(Description = "show the repos for the given projects")]
        public void Repo(
            IConsole console, CancellationToken cancellationToken, 
            TableFormatModel tableFormatModel,
            ProjOrRepoKeys projOrRepoKeys,
            [Option(ShortName = "n", Description = "regex to match name")]
            string namePattern,
            [Option(ShortName = "d", Description = "regex to match description")]
            string descPattern,
            [Option(Description = "output only keys")]
            bool keys,
            [Option(ShortName = "c", Description = "return only cloned")]
            bool cloned,
            [Option(ShortName = "u", Description = "return only uncloned")]
            bool uncloned,
            [Option(LongName = "public", Description = "return only public")]
            bool onlyPublic,
            [Option(LongName = "private", Description = "return only private")]
            bool onlyPrivate,
            [Option(ShortName = "x", Description = "exclude repositories marked obsolete")]
            bool excludeObsolete)
        {
            if (cloned && uncloned)
            {
                throw new ArgumentException("cannot request both cloned and uncloned");
            }
            if (onlyPublic && onlyPrivate)
            {
                throw new ArgumentException("cannot request both public and private");
            }

            var projKeys = projOrRepoKeys.GetProjKeysOrNull()?.ToHashSet();
            var repoKeys = projOrRepoKeys.GetRepoKeysOrNull()?.ToHashSet();

            var projects = this.bbService
                .GetProjects()
                .WhereIf(projKeys is not null, p => projKeys!.Contains(p.Key))
                .ToCollection();

            var localRepos = this.gitService.GetLocalRepos();

            var remoteRepos = this.bbService
                .GetRepos(projects.Select(p => p.Name).ToCollection())
                .WhereMatches(r => r.Name, namePattern)
                .WhereMatches(r => r.Description, descPattern)
                .WhereIf(onlyPublic, r => r.Public)
                .WhereIf(onlyPrivate, r => !r.Public)
                .WhereIf(repoKeys is not null, r => repoKeys!.Contains(r.Name))
                .WhereIf(excludeObsolete, r => !r.Name.Contains("obsolete", StringComparison.OrdinalIgnoreCase));

            bool? isCloned = cloned ? true : uncloned ? false : null;
            var repoPairs = localRepos.PairRepos(remoteRepos, mustHaveRemote: true, isCloned).Values;

            if (keys)
            {
                repoPairs
                    .OrderBy(p => p.Remote.Description)
                    .Select(p => p.Remote.Name)
                    .ForEach(console.WriteLine);
            }
            else
            {
                var rows = repoPairs
                    .OrderBy(p => p.Remote.Description)
                    .Select(p => new
                    {
                        p.Remote.Name,
                        p.Remote.ProjectKey,
                        Cloned = p.Local is not null,
                        p.Remote.Public,
                        p.Remote.StatusMessage
                    });
                new Table(console, tableFormatModel?.GetTheme(), includeCount: true, cancellationToken)
                    .OverrideColumn(rows, c => c.ProjectKey, c => c.WrapText = false)
                    .OverrideColumn(rows, c => c.Name, c => c.WrapText = false)
                    .Write(rows);
            }
        }

        [Command(Description = "Clone all repositories matching the search criteria")]
        public void Clone(
            IConsole console, CancellationToken cancellationToken,
            [Operand] [Required] string[] repos,
            [Option(LongName = "ssh", Description = "Set origin to ssh after completed")] 
            bool setSshOrigin = false)
        {
            this.bbService.GetRepos()
                .Where(r => repos.Contains(r.Name))
                .SafelyForEach(
                    r => this.gitService.CloneRepo(r, setSshOrigin), 
                    cancellationToken,
                    summarizeErrors: true);
        }
    }
}