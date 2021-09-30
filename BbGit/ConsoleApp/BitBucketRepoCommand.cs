using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
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

        [Command(
            Description = "List projects from the server",
            ExtendedHelpText =
                "for regex flags, see https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options")]
        public int Proj(IConsole console,
            TableFormatModel tableFormatModel,
            [Option(ShortName = "k", Description = "regex to match key")]
            string keyPattern = null,
            [Option(ShortName = "n", Description = "regex to match name")]
            string namePattern = null,
            [Option(ShortName = "d", Description = "regex to match description")]
            string descPattern = null,
            [Option(ShortName = "r", LongName = "repos", Description = "include the count repositories")]
            bool includeRepoCounts = false)
        {
            var keyRegex = keyPattern.IsNullOrEmpty() ? null : new Regex(keyPattern, RegexOptions.Compiled);
            var nameRegex = namePattern.IsNullOrEmpty() ? null : new Regex(namePattern, RegexOptions.Compiled);
            var descRegex = descPattern.IsNullOrEmpty() ? null : new Regex(descPattern, RegexOptions.Compiled);

            var projects = this.bbService
                .GetProjects()
                .Where(p => keyRegex is null || keyRegex.IsMatch(p.Key))
                .Where(p => nameRegex is null || nameRegex.IsMatch(p.Name))
                .Where(p => descRegex is null || (p.Description is not null && descRegex.IsMatch(p.Description)))
                .OrderBy(p => p.Name)
                .ToCollection();

            if (includeRepoCounts)
            {
                if (tableFormatModel.Table == TableFormatModel.TableFormat.k)
                {
                    console.Error.WriteLine("Repository counts will not be shown when only keys will be output");
                    console.Error.WriteLine("Do not include repository counts or do not set the table format to 'k'");
                    return ExitCodes.ValidationError.Result;
                }

                var localRepos = this.gitService.GetLocalRepos();

                var remoteRepos = this.bbService
                    .GetRepos(projects.Select(p => p.Name).ToCollection());

                var repoPairs = localRepos.PairRepos(remoteRepos, mustHaveRemote: true).Values;

                var remoteRepoCounts = repoPairs
                    .GroupBy(p => p.Remote.ProjectKey)
                    .ToDictionary(
                        g => g.Key, 
                        g => new {remote=g.Count(), local=g.Count(r => r.Local is not null)});

                var records = projects
                    .Select(p => new
                    {
                        p.Key, p.Name, p.Description, p.Id, p.Public, p.Type,
                        Repos = DictionaryExtensions.GetValueOrDefault(remoteRepoCounts, p.Key)?.remote ?? 0,
                        Cloned = DictionaryExtensions.GetValueOrDefault(remoteRepoCounts, p.Key)?.local ?? 0
                    });

                new Table(console, tableFormatModel?.GetTheme(), includeCount: true)
                    .Write(records);
            }
            else
            {
                if (tableFormatModel?.Table == TableFormatModel.TableFormat.k)
                {
                    projects
                        .Select(p => p.Key)
                        .ForEach(console.WriteLine);
                }
                else
                {
                    new Table(console, tableFormatModel?.GetTheme(), includeCount: true)
                        .Write(projects.Select(p => 
                            new { p.Key, p.Name, p.Description, p.Public, p.Type }));
                }
            }

            return ExitCodes.Success.Result;
        }

        [Command(Description = "show the repos for the given projects")]
        public void Repo(
            IConsole console, 
            TableFormatModel tableFormatModel,
            [Operand(Description = "the projects containing the repos")]
            List<string> projectKeys,
            [Option(ShortName = "n", Description = "regex to match name")]
            string namePattern = null,
            [Option(ShortName = "s", Description = "regex to match slug")]
            string slugPattern = null)
        {
            var projects = this.bbService
                .GetProjects()
                .Where(p => projectKeys.Contains(p.Key))
                .ToCollection();

            var localRepos = this.gitService.GetLocalRepos();

            var nameRegex = namePattern.IsNullOrEmpty() ? null : new Regex(namePattern, RegexOptions.Compiled);
            var slugRegex = slugPattern.IsNullOrEmpty() ? null : new Regex(slugPattern, RegexOptions.Compiled);

            var remoteRepos = this.bbService
                .GetRepos(projects.Select(p => p.Name).ToCollection())
                .Where(p => nameRegex is null || nameRegex.IsMatch(p.Name))
                .Where(p => slugRegex is null || slugRegex.IsMatch(p.Slug));

            var repoPairs = localRepos.PairRepos(remoteRepos, mustHaveRemote: true).Values;

            if (tableFormatModel?.Table == TableFormatModel.TableFormat.k)
            {
                repoPairs
                    .OrderBy(p => p.Remote.Name)
                    .Select(p => p.Remote.Slug)
                    .ForEach(console.WriteLine);
            }
            else
            {
                var rows = repoPairs
                    .OrderBy(p => p.Remote.Name)
                    .Select(p => new
                    {
                        p.Remote.Slug,
                        p.Remote.ProjectKey,
                        Cloned = p.Local is not null,
                        p.Remote.Public,
                        p.Remote.StatusMessage
                    });
                new Table(console, tableFormatModel?.GetTheme(), includeCount: true)
                    .Write(rows);
            }
        }

        [Command(
            Name = "repos2",
            Description = "List BitBucket repositories matching the search criteria")]
        public async void Repos(IConsole console,
            [Option(
                ShortName = "p",
                LongName = "projects",
                Description = "comma-separated list of project keys to filter by")]
            string projects,
            [Option(
                ShortName = "T",
                Description = "display additional info in a table format")]
            bool showTable,
            [Option(
                ShortName = "P",
                Description = "show project information")]
            bool showProjectInfo,
            [Option(
                ShortName = "r",
                LongName = "repo",
                Description = "regex to filter repo name by")]
            string repoRegex,
            [Option(
                ShortName = "u",
                LongName = "uncloned",
                Description = "filters out repositories that have already been cloned.")]
            bool uncloned,
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
            var localRepoNames = this.gitService.GetLocalRepoNames(includeIgnored, onlyIgnored).ToHashSet();
            var bbRepos = await this.bbService.GetRepos(projects, includeIgnored: includeIgnored, onlyIgnored: onlyIgnored);

            if (repoRegex != null)
            {
                var regex = new Regex(repoRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                bbRepos = bbRepos.Where(r => regex.IsMatch(r.Slug));
            }

            if (uncloned)
            {
                bbRepos = bbRepos.Where(r => !localRepoNames.Contains(r.Slug));
            }

            if (showTable)
            {
                if (showProjectInfo)
                {
                    IEnumerable<string[]> records = bbRepos
                        .OrderBy(r => r.Slug)
                        .Select(r => new[] {r.Slug, r.Project.Key});
                    new Table(console, includeCount: true).Write(records);
                }
                else
                {
                    IEnumerable<string> records = bbRepos
                        .OrderBy(r => r.Slug)
                        .Select(r => r.Slug);
                    new Table(console, includeCount: true).Write(records);
                }
            }
            else
            {
                bbRepos.ForEach(r => Console.Out.WriteLine(r.Slug));
            }
        }
    }
}