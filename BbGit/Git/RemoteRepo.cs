using System.Linq;
using SharpBucket.V2.Pocos;

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
            this.Name = repository.name;
            this.ProjectKey = repository.project.key;
            this.ProjectName = repository.project.name;
            this.HttpsUrl = repository.links.clone.FirstOrDefault(c => c.name == "https")?.href;
            this.SshUrl = repository.links.clone.FirstOrDefault(c => c.name == "ssh")?.href;
        }
    }
}