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

        [Command(Description = "List projects from the server")]
        public void Projects()
        {
            var projects = this.bbService.GetProjects().Result;
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