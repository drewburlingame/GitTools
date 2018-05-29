using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BbGit.ConsoleUtils;
using BbGit.Framework;
using BbGit.Git;
using CommandDotNet.Attributes;
using MoreLinq;
using SharpBucket.V2.Pocos;

namespace BbGit.ConsoleApp
{
    [ApplicationMetadata(Name = "bb", Description = "commands for querying repository info via BitBucket API")]
    public class BitBucketRepoCommand
    {
        [InjectProperty]
        public BbService BbService { get; set; }

        [InjectProperty]
        public GitService GitService { get; set; }

        [ApplicationMetadata(Description = "Lists projects that contain repositories")]
        public void Projects(
            [Option(ShortName = "u", LongName = "uncloned", Description = "filters out repositories that have already been cloned.")]
            bool uncloned)
        {
            var localRepoNames = GitService.GetLocalRepoNames().ToHashSet();

            var projects = new Dictionary<string, Tuple<string, List<Repository>>>();

            foreach (var repo in BbService
                .GetRepos(usePipedValuesIfAvailable: true)
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
                .WriteTable(new[] {"key", "name", "repo count"});
        }

        [ApplicationMetadata(Description = "List BitBucket repositories matching the search criteria")]
        public void Repos(
            [Option(ShortName = "p", LongName = "projects", Description = "comma-separated list of project keys to filter by")]
            string projects,
            [Option(ShortName = "T", Description = "display additional info in a table format")]
            bool showTable,
            [Option(ShortName = "P", Description = "show project information")]
            bool showProjectInfo,
            [Option(ShortName = "r", LongName = "repo", Description = "regex to filter repo name by")]
            string repoRegex,
            [Option(ShortName = "u", LongName = "uncloned", Description = "filters out repositories that have already been cloned.")]
            bool uncloned
            )
        {
            var localRepoNames = GitService.GetLocalRepoNames().ToHashSet();
            var bbRepos = BbService.GetRepos(projects);

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
                    bbRepos.OrderBy(r => r.name).Select(r => new[] { r.name, r.project.key }).WriteTable();
                }
                else
                {
                    bbRepos.OrderBy(r => r.name).Select(r => $"{r.name}").WriteTable();
                }
            }
            else
            {
                bbRepos.ForEach(r => Console.Out.WriteLine((string) r.name));
            }
        }
    }
}