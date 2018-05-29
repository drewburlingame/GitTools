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
        public string SshUrl { get; set;  }

        /// <summary>for serialization</summary>
        public RemoteRepo()
        {

        }

        public RemoteRepo(Repository repository)
        {
            Name = repository.name;
            ProjectKey = repository.project.key;
            ProjectName = repository.project.name;
            HttpsUrl = repository.links.clone.FirstOrDefault(c => c.name == "https")?.href;
            SshUrl = repository.links.clone.FirstOrDefault(c => c.name == "ssh")?.href;
        }
    }
}