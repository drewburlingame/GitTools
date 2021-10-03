using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BbGit.Framework;
using BbGit.Git;
using Bitbucket.Net;
using Bitbucket.Net.Models.Core.Projects;
using CommandDotNet;
using CommandDotNet.Rendering;

namespace BbGit.BitBucket
{
    public class BbService
    {
        private const string RemoteReposConfigFileName = "remoteRepos";
        private readonly AppConfig appConfig;
        private readonly BitbucketClient bbServerClient;
        private RemoteReposConfig remoteReposConfig;
        public bool ignoreCache;

        public BbService(
            BitbucketClient bbServerClient,
            AppConfig appConfig)
        {
            this.bbServerClient = bbServerClient;
            this.appConfig = appConfig;
        }

        public void RefreshCaches(IConsole console,
            bool ignoreCache, bool skipCacheRefresh, bool forceCacheRefresh, bool warnOnCacheRefresh)
        {
            this.ignoreCache = ignoreCache;

            if (!skipCacheRefresh && !ignoreCache)
            {
                // create if not exist because we'll definitely be populating them
                var projectCache = ProjectCache.Get();
                var repoCache = RepoCache.Get();

                // TODO: setting for refreshInterval
                var refreshInterval = TimeSpan.FromDays(1);

                if (forceCacheRefresh || projectCache.CachedOn.Add(refreshInterval) < DateTime.Now)
                {
                    if (warnOnCacheRefresh)
                    {
                        console.WriteLine("Refreshing project cache from BitBucket");
                    }

                    var sw = Stopwatch.StartNew();
                    projectCache.CachedOn = DateTime.Now;
                    projectCache.ProjectsByKey = GetProjectsRawAsync().Result
                        .ToDictionary(p => p.Key);
                    projectCache.Save();

                    if (warnOnCacheRefresh)
                    {
                        console.WriteLine($"project cache refreshed: {sw.Elapsed}");
                    }
                }

                if (forceCacheRefresh || repoCache.CachedOn.Add(refreshInterval) < DateTime.Now)
                {
                    if (warnOnCacheRefresh)
                    {
                        console.WriteLine("Refreshing repository cache from BitBucket");
                    }

                    var sw = Stopwatch.StartNew();
                    repoCache.CachedOn = DateTime.Now;
                    repoCache.ReposByProjectKey = GetReposRawAsync().Result
                        .GroupBy(r => r.Project.Key)
                        .ToDictionary(g => g.Key, g => g.ToList());
                    repoCache.Save();

                    if (warnOnCacheRefresh)
                    {
                        console.WriteLine($"repository cache refreshed: {sw.Elapsed}");
                    }
                }
            }

        }

        public IEnumerable<RemoteProj> GetProjects()
        {
            return GetProjectsAsync().Result;
        }
        
        public async Task<IEnumerable<RemoteProj>> GetProjectsAsync()
        {
            var projects = ignoreCache 
                ? await GetProjectsRawAsync() 
                : ProjectCache.Get().ProjectsByKey.Values;

            return projects.Select(p => new RemoteProj(p));
        }

        public IEnumerable<RemoteRepo> GetRepos(ICollection<string> projectNames)
        {
            return GetReposAsync(projectNames).Result;
        }

        public async Task<IEnumerable<RemoteRepo>> GetReposAsync(ICollection<string> projectNames)
        {
            return await projectNames.SelectManyAsync(GetReposAsync);
        }


        public IEnumerable<RemoteRepo> GetRepos()
        {
            return GetReposAsync((string)null).Result;
        }

        public IEnumerable<RemoteRepo> GetRepos(string projectName)
        {
            return GetReposAsync(projectName).Result;
        }

        public async Task<IEnumerable<RemoteRepo>> GetReposAsync(string projectName = null)
        {
            IEnumerable<Repository> repositories;

            if (ignoreCache)
            {
                repositories = await GetReposRawAsync(projectName);
            }
            else
            {
                if (projectName is null)
                {
                    repositories = RepoCache.Get().GetRepos();
                }
                else
                {
                    var projectKey = ProjectCache.Get().ProjectsByKey.Values.First(p => p.Name == projectName).Key;
                    repositories = RepoCache.Get().ReposByProjectKey[projectKey];
                }
            }

            return repositories.Select(p => new RemoteRepo(p));
        }

        private async Task<IEnumerable<Project>> GetProjectsRawAsync()
        {
            return (await this.bbServerClient.GetProjectsAsync());
        }

        private Task<IEnumerable<Repository>> GetReposRawAsync(string projectName = null)
        {
            return this.bbServerClient.GetRepositoriesAsync(projectName: projectName);
        }

        [Obsolete]
        public async Task<IEnumerable<Repository>> GetRepos(
            string projectNamePattern = null,
            ICollection<string> onlyRepos = null,
            bool includeIgnored = false,
            bool onlyIgnored = false)
        {
            var repos = await this.bbServerClient.GetRepositoriesAsync();

            var repositories = repos
                .Where(repo => this.Include(includeIgnored, onlyIgnored, repo))
                .OrderBy(r => r.Slug)
                .AsEnumerable();

            if (projectNamePattern != null)
            {
                repositories = repositories.Where(r =>
                    Regex.IsMatch(r.Project.Key, $"^{projectNamePattern}$".Replace(",", "$|^"), RegexOptions.IgnoreCase));
            }

            if (onlyRepos?.Any() ?? false)
            {
                var repoNameSet = onlyRepos.ToHashSet();
                repositories = repositories.Where(r => repoNameSet.Contains(r.Slug));
            }

            return repositories.ToList();
        }

        public RemoteReposConfig GetRemoteReposConfig()
        {
            return this.remoteReposConfig ??=
                ConfigFolder.CurrentDirectory()
                    .GetJsonConfigOrDefault<RemoteReposConfig>(RemoteReposConfigFileName);
        }

        public void SaveRemoteReposConfig(RemoteReposConfig config)
        {
            ConfigFolder.CurrentDirectory().SaveJsonConfig(RemoteReposConfigFileName, config);
        }

        private bool Include(bool includeIgnored, bool onlyIgnored, Repository repo)
        {
            if (includeIgnored && onlyIgnored)
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(includeIgnored)} && {nameof(onlyIgnored)} are mutually exclusive.  Only one of them can be true");
            }

            var reposConfig = this.GetRemoteReposConfig();
            var ignoreRepoRegex = reposConfig.IgnoredReposRegex;
            var ignoreProjRegex = reposConfig.IgnoredProjectsRegex;

            var hasIgnoreRepoRegex = !string.IsNullOrWhiteSpace(ignoreRepoRegex);
            var hasIgnoreProjRegex = !string.IsNullOrWhiteSpace(ignoreProjRegex);

            if (!hasIgnoreRepoRegex && !hasIgnoreProjRegex)
            {
                return true;
            }

            if (includeIgnored) // include all
            {
                return true;
            }

            var repoIsIgnored = hasIgnoreRepoRegex && Regex.IsMatch(repo.Slug, ignoreRepoRegex);
            var projIsIgnored = hasIgnoreProjRegex && Regex.IsMatch(repo.Project.Key, ignoreProjRegex);

            return onlyIgnored
                ? repoIsIgnored || projIsIgnored
                : !repoIsIgnored && !projIsIgnored;
        }

        private bool RepoIsIgnored(Repository repo)
        {
            var ignoredReposRegex = this.GetRemoteReposConfig().IgnoredReposRegex;
            if (string.IsNullOrWhiteSpace(ignoredReposRegex))
            {
                return true;
            }

            return Regex.IsMatch(repo.Slug, ignoredReposRegex);
        }

        private bool ProjectIsIgnored(Repository repo)
        {
            var ignoredProjectsRegex = this.GetRemoteReposConfig().IgnoredProjectsRegex;
            if (string.IsNullOrWhiteSpace(ignoredProjectsRegex))
            {
                return true;
            }

            return Regex.IsMatch(repo.Project.Key, ignoredProjectsRegex);
        }
    }
}