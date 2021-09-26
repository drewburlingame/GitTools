using System.Linq;
using Bitbucket.Net.Models.Core.Projects;

namespace BbGit.Git
{
    public class RemoteRepo
    {
        public string Name { get; set; }
        public string ProjectKey { get; set; }
        public string ProjectName { get; set; }
        public string HttpsUrl { get; set; }
        public string SshUrl { get; set; }

        /// <summary>for serialization</summary>
        public RemoteRepo()
        {
        }

        public RemoteRepo(Repository repository)
        {
            this.Name = repository.Slug;
            this.ProjectKey = repository.Project.Key;
            this.ProjectName = repository.Project.Key;
            this.HttpsUrl = repository.Links.Clone.FirstOrDefault(c => c.Name == "https")?.Href;
            this.SshUrl = repository.Links.Clone.FirstOrDefault(c => c.Name == "ssh")?.Href;
        }
    }
}