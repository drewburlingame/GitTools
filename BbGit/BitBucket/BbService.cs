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

namespace BbGit.BitBucket
{
    public class BbService
    {
        private const string RemoteReposConfigFileName = "remoteRepos";
        private readonly AppConfig appConfig;
        private readonly BitbucketClient bbServerClient;
        private readonly DirectoryResolver directoryResolver;

        private RemoteReposConfig remoteReposConfig;

        private string CurrentDirectory => this.directoryResolver.CurrentDirectory;

        public BbService(
            BitbucketClient bbServerClient,
            AppConfig appConfig,
            DirectoryResolver directoryResolver)
        {
            this.bbServerClient = bbServerClient;
            this.appConfig = appConfig;
            this.directoryResolver = directoryResolver;
        }

        public IEnumerable<RemoteProj> GetProjects()
        {
            return GetProjectsAsync().Result;
        }

        public async Task<IEnumerable<RemoteProj>> GetProjectsAsync()
        {
            return (await this.bbServerClient.GetProjectsAsync())
                .Select(p => new RemoteProj(p));
        }

        public IEnumerable<RemoteRepo> GetRepos(string projectName = null)
        {
            return GetReposAsync().Result;
        }

        public async Task<IEnumerable<RemoteRepo>> GetReposAsync(string projectName = null)
        {
            var privateRepos = await this.bbServerClient.GetRepositoriesAsync(projectName: projectName);
            var publicRepos = await this.bbServerClient.GetRepositoriesAsync(projectName: projectName, isPublic: true);
            return privateRepos.Concat(publicRepos).Select(p => new RemoteRepo(p));
        }

        public IEnumerable<RemoteRepo> GetRepos(ICollection<string> projectNames)
        {
            return GetReposAsync(projectNames).Result;
        }

        public async Task<IEnumerable<RemoteRepo>> GetReposAsync(ICollection<string> projectNames)
        {
            return await projectNames.SelectManyAsync(GetReposAsync);
        }

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
                new FolderConfig(this.CurrentDirectory)
                    .GetJsonConfig<RemoteReposConfig>(RemoteReposConfigFileName);
        }

        public void SaveRemoteReposConfig(RemoteReposConfig config)
        {
            new FolderConfig(this.CurrentDirectory).SaveJsonConfig(RemoteReposConfigFileName, config);
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