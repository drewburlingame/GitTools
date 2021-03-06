﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BbGit.Framework;
using MoreLinq;
using SharpBucket.V2;
using SharpBucket.V2.Pocos;

namespace BbGit.BitBucket
{
    public class BbService
    {
        private const string RemoteReposConfigFileName = "remoteRepos";
        private readonly AppConfig appConfig;
        private readonly SharpBucketV2 bbApi;
        private readonly DirectoryResolver directoryResolver;
        private readonly PipedInput pipedInput;

        private RemoteReposConfig remoteReposConfig;

        private string CurrentDirectory => this.directoryResolver.CurrentDirectory;

        public BbService(
            SharpBucketV2 bbApi,
            AppConfig appConfig,
            PipedInput pipedInput,
            DirectoryResolver directoryResolver)
        {
            this.bbApi = bbApi;
            this.appConfig = appConfig;
            this.pipedInput = pipedInput;
            this.directoryResolver = directoryResolver;
        }

        public IEnumerable<Repository> GetRepos(
            string projects = null,
            bool usePipedValuesIfAvailable = false,
            bool includeIgnored = false,
            bool onlyIgnored = false)
        {
            var repositories = this.bbApi.RepositoriesEndPoint()
                .ListRepositories(this.appConfig.DefaultAccount)
                .Where(repo => this.Include(includeIgnored, onlyIgnored, repo))
                .OrderBy(r => r.name)
                .AsEnumerable();

            if (projects != null)
            {
                repositories = repositories.Where(r =>
                    Regex.IsMatch(r.project.key, $"^{projects}$".Replace(",", "$|^"), RegexOptions.IgnoreCase));
            }

            if (usePipedValuesIfAvailable && this.pipedInput.HasValues)
            {
                var set = this.pipedInput.Values.ToHashSet();
                repositories = repositories.Where(r => set.Contains(r.name));
            }

            return repositories.ToList();
        }

        public RemoteReposConfig GetRemoteReposConfig()
        {
            return this.remoteReposConfig
                   ?? (this.remoteReposConfig =
                       new FolderConfig(this.CurrentDirectory).GetJsonConfig<RemoteReposConfig>(
                           RemoteReposConfigFileName));
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

            var repoIsIgnored = hasIgnoreRepoRegex && Regex.IsMatch(repo.name, ignoreRepoRegex);
            var projIsIgnored = hasIgnoreProjRegex && Regex.IsMatch(repo.project.key, ignoreProjRegex);

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

            return Regex.IsMatch(repo.name, ignoredReposRegex);
        }

        private bool ProjectIsIgnored(Repository repo)
        {
            var ignoredProjectsRegex = this.GetRemoteReposConfig().IgnoredProjectsRegex;
            if (string.IsNullOrWhiteSpace(ignoredProjectsRegex))
            {
                return true;
            }

            return Regex.IsMatch(repo.project.key, ignoredProjectsRegex);
        }
    }
}