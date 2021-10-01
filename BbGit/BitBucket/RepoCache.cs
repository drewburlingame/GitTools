using System;
using System.Collections.Generic;
using System.Linq;
using BbGit.Framework;
using Bitbucket.Net.Models.Core.Projects;

namespace BbGit.BitBucket
{
    public class RepoCache
    {
        public DateTime CachedOn { get; set; }
        public Dictionary<string, List<Repository>> ReposByProjectKey { get; set; }

        public IEnumerable<Repository> Repos => ReposByProjectKey.Values.SelectMany(r => r);

        private static RepoCache _instance;
        
        public static RepoCache Get()
        {
            return _instance ??= ConfigFolder
                .CurrentDirectory()
                .GetJsonConfigOrDefault<RepoCache>("repo_cache");
        }

        public void Save()
        {
            ConfigFolder.CurrentDirectory().SaveJsonConfig("repo_cache", this);
        }
    }
}