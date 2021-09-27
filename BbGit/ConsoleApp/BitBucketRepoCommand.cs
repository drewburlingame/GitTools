using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BbGit.BitBucket;
using BbGit.ConsoleUtils;
using BbGit.Framework;
using BbGit.Git;
using Bitbucket.Net.Models.Core.Projects;
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
        public void Projects(IConsole console,
            TableFormatModel tableFormatModel,
            [Option(ShortName = "n", Description = "regex to match name")]
            string namePattern = null,
            [Option(ShortName = "d", Description = "regex to match description")]
            string descPattern = null,
            [Option(ShortName = "r", LongName = "repos", Description = "include the count repositories")]
            bool includeRepoCounts = false)
        {
            var nameRegex = namePattern.IsNullOrEmpty() ? null : new Regex(namePattern, RegexOptions.Compiled);
            var descRegex = descPattern.IsNullOrEmpty() ? null : new Regex(descPattern, RegexOptions.Compiled);

            var projects = this.bbService
                .GetProjects()
                .Where(p => nameRegex is null || nameRegex.IsMatch(p.Name))
                .Where(p => descRegex is null || (p.Description is not null && descRegex.IsMatch(p.Description)))
                .OrderBy(p => p.Key)
                .ToCollection();

            if (includeRepoCounts)
            {
                if (tableFormatModel.Table == TableFormatModel.TableFormat.k)
                {
                    console.Error.WriteLine("Repository counts will not be shown when only keys will be output");
                    console.Error.WriteLine("Do not include repository counts or do not set the table format to 'k'");
                }
                
                var localRepos = this.gitService.GetLocalRepos()
                    .Select(r => r.Name)
                    .ToHashSet();
                
                var remoteRepoCounts = this.bbService
                    .GetRepos(projects.Select(p => p.Name).ToCollection())
                    .Select(r => new {r.ProjectKey, r.Name, HasLocal=localRepos.Contains(r.Slug)})
                    .GroupBy(p => p.ProjectKey)
                    .ToDictionary(
                        g => g.Key, 
                        g => new {remote= g.Count(), local=g.Count(r => r.HasLocal)});

                console.WriteTable(tableFormatModel,
                    projects
                        .Select(p => new
                        {
                            p.Key, p.Name, p.Description, p.Id, p.Public, p.Type,
                            RemoteRepos = remoteRepoCounts[p.Key].remote, 
                            LocalRepos = remoteRepoCounts[p.Key].local
                        }),
                    null);
            }
            else
            {
                console.WriteTable(tableFormatModel, 
                    projects.Select(p => new { p.Key, p.Name, p.Description, p.Id, p.Public, p.Type }), 
                    p => p.Key);
            }
        }

        [Command(
            Name = "projects-old", 
            Description = "Lists projects based on repos from the server")]
        public void Projects(
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

            var projects = new Dictionary<string, Tuple<string, List<Repository>>>();

            var repos = this.bbService.GetRepos(includeIgnored: includeIgnored, onlyIgnored: onlyIgnored).Result;
            foreach (var repo in repos
                .Where(r => !uncloned || !localRepoNames.Contains(r.Slug)))
            {
                var value = projects.GetValueOrAdd(repo.Project.Key,
                    r => new Tuple<string, List<Repository>>(repo.Project.Key, new List<Repository>()));
                value.Item2.Add(repo);
            }

            projects
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new[]
                {
                    kvp.Key,
                    kvp.Value.Item1,
                    kvp.Value.Item2.Count.ToString()
                })
                .WriteTable(new[] { "key", "name", "repo count" });
        }

        [Command(Description = "List BitBucket repositories matching the search criteria")]
        public async void Repos(
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
            bool onlyIgnored
        )
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
                    bbRepos.OrderBy(r => r.Slug).Select(r => new[] {r.Slug, r.Project.Key}).WriteTable();
                }
                else
                {
                    bbRepos.OrderBy(r => r.Slug).Select(r => $"{r.Slug}").WriteTable();
                }
            }
            else
            {
                bbRepos.ForEach(r => Console.Out.WriteLine(r.Slug));
            }
        }
    }
}