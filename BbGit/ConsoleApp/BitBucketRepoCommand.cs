using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BbGit.BitBucket;
using BbGit.ConsoleUtils;
using BbGit.Framework;
using BbGit.Git;
using CommandDotNet;
using SharpBucket.V2.Pocos;
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

        [Command(Description = "Lists projects that contain repositories")]
        public void Projects(
            [Operand(
                Description = "If specified, include only projects with these repos.")]
            ICollection<string> onlyRepos,
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
            
            foreach (var repo in this.bbService
                .GetRepos(onlyRepos: onlyRepos, includeIgnored: includeIgnored, onlyIgnored: onlyIgnored)
                .Where(r => !uncloned || !localRepoNames.Contains(r.name)))
            {
                var value = projects.GetValueOrAdd(repo.project.key,
                    r => new Tuple<string, List<Repository>>(repo.project.name, new List<Repository>()));
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
        public void Repos(
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
            var bbRepos = this.bbService.GetRepos(projects, includeIgnored: includeIgnored, onlyIgnored: onlyIgnored);

            if (repoRegex != null)
            {
                var regex = new Regex(repoRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                bbRepos = bbRepos.Where(r => regex.IsMatch(r.name));
            }

            if (uncloned)
            {
                bbRepos = bbRepos.Where(r => !localRepoNames.Contains(r.name));
            }

            if (showTable)
            {
                if (showProjectInfo)
                {
                    bbRepos.OrderBy(r => r.name).Select(r => new[] {r.name, r.project.key}).WriteTable();
                }
                else
                {
                    bbRepos.OrderBy(r => r.name).Select(r => $"{r.name}").WriteTable();
                }
            }
            else
            {
                bbRepos.ForEach(r => Console.Out.WriteLine(r.name));
            }
        }
    }
}