using System.Linq;
using Bitbucket.Net.Models.Core.Projects;

namespace BbGit.Git
{
    public class RemoteRepo
    {
        private readonly Repository repository;
        public string ProjectKey => Project.Key;
        public string HttpsUrl => repository.Links.Clone.FirstOrDefault(c => c.Name == "http")?.Href;
        public string SshUrl => repository.Links.Clone.FirstOrDefault(c => c.Name == "ssh")?.Href;

        /// <summary>for serialization</summary>
        public RemoteRepo() : this(new Repository())
        {
        }

        public RemoteRepo(Repository repository)
        {
            this.repository = repository;
        }

        #region Repository delegated members

        public string Slug
        {
            get => repository.Slug;
            set => repository.Slug = value;
        }

        public string Name
        {
            get => repository.Name;
            set => repository.Name = value;
        }

        public ProjectRef Project
        {
            get => repository.Project;
            set => repository.Project = value;
        }

        public int Id
        {
            get => repository.Id;
            set => repository.Id = value;
        }

        public string ScmId
        {
            get => repository.ScmId;
            set => repository.ScmId = value;
        }

        public string State
        {
            get => repository.State;
            set => repository.State = value;
        }

        public string StatusMessage
        {
            get => repository.StatusMessage;
            set => repository.StatusMessage = value;
        }

        public bool Forkable
        {
            get => repository.Forkable;
            set => repository.Forkable = value;
        }

        public bool Public
        {
            get => repository.Public;
            set => repository.Public = value;
        }

        public CloneLinks Links
        {
            get => repository.Links;
            set => repository.Links = value;
        }

        #endregion

        public override string ToString()
        {
            return $"{this.Slug} - {this.Name} ({this.ProjectKey})";
        }
    }
}