using System;
using System.Collections.Generic;
using BbGit.Framework;
using Bitbucket.Net.Models.Core.Projects;

namespace BbGit.BitBucket
{
    public class ProjectCache
    {
        public DateTime CachedOn { get; set; }
        public Dictionary<string,Project> ProjectsByKey { get; set; }

        private static ProjectCache _instance;
        
        public static ProjectCache Get()
        {
            return _instance ??= ConfigFolder
                .CurrentDirectory()
                .GetJsonConfigOrDefault<ProjectCache>("project_cache");
        }

        public void Save()
        {
            ConfigFolder.CurrentDirectory().SaveJsonConfig("project_cache", this);
        }
    }
}