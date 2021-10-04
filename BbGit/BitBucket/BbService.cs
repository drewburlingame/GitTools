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
using CommandDotNet.Rendering;

namespace BbGit.BitBucket
{
    public class BbService
    {
        private readonly IConsole console;
        private readonly BitbucketClient bbServerClient;
        private bool ignoreCache;

        public BbService(IConsole console, BitbucketClient bbServerClient)
        {
            this.console = console;
            this.bbServerClient = bbServerClient;
        }

        public void RefreshCaches(
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