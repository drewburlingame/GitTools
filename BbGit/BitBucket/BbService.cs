using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BbGit.Framework;
using BbGit.Git;
using Bitbucket.Net;
using Bitbucket.Net.Models.Core.Projects;
using CommandDotNet;

namespace BbGit.BitBucket
{
    public class BbService
    {
        private readonly IConsole console;
        private readonly BitbucketClient bbServerClient;
        private readonly AppConfig appConfig;
        private bool _ignoreCache;

        public BbService(IConsole console, BitbucketClient bbServerClient, AppConfig appConfig)
        {
            this.console = console;
            this.bbServerClient = bbServerClient;
            this.appConfig = appConfig;
        }

        public void RefreshCaches(
            bool ignoreCache, bool skipCacheRefresh, bool forceCacheRefresh, bool warnOnCacheRefresh)
        {
            _ignoreCache = ignoreCache;

            if (!ignoreCache)
            {
                // create if not exist because we'll definitely be populating them
                var projectCache = ProjectCache.Get();
                var repoCache = RepoCache.Get();

                var refreshInterval = TimeSpan.FromDays(appConfig.CacheTTLInDays);

                bool RefreshCache(DateTime cachedOn) => 
                    forceCacheRefresh || !skipCacheRefresh && cachedOn.Add(refreshInterval) < DateTime.Now;

                if (RefreshCache(projectCache.CachedOn))
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

                if (RefreshCache(repoCache.CachedOn))
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
            var projects = _ignoreCache 
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

            if (_ignoreCache)
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
                    repositories = RepoCache.Get().ReposByProjectKey.GetValueOrDefault(projectKey) 
                                   ?? Enumerable.Empty<Repository>().ToList();
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
    }
}