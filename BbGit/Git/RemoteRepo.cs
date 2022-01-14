using System.Linq;
using Bitbucket.Net.Models.Core.Projects;

namespace BbGit.Git
{
    public class RemoteRepo
    {
        private readonly Repository _repository;
        public string ProjectKey => Project.Key;
        public string? HttpUrl => _repository.Links.Clone.FirstOrDefault(c => c.Name == "http")?.Href;
        public string? SshUrl => _repository.Links.Clone.FirstOrDefault(c => c.Name == "ssh")?.Href;

        /// <summary>for serialization</summary>
        public RemoteRepo() : this(new Repository())
        {
        }

        public RemoteRepo(Repository repository)
        {
            _repository = repository;
        }

        #region Repository delegated members

        // It's called Name in the BitBucket UI
        public string Name
        {
            get => _repository.Slug;
            set => _repository.Slug = value;
        }

        // It's called Description in the BitBucket UI
        public string Description
        {
            get => _repository.Name;
            set => _repository.Name = value;
        }

        public ProjectRef Project
        {
            get => _repository.Project;
            set => _repository.Project = value;
        }

        public int Id
        {
            get => _repository.Id;
            set => _repository.Id = value;
        }

        public string ScmId
        {
            get => _repository.ScmId;
            set => _repository.ScmId = value;
        }

        public string State
        {
            get => _repository.State;
            set => _repository.State = value;
        }

        public string StatusMessage
        {
            get => _repository.StatusMessage;
            set => _repository.StatusMessage = value;
        }

        public bool Forkable
        {
            get => _repository.Forkable;
            set => _repository.Forkable = value;
        }

        public bool Public
        {
            get => _repository.Public;
            set => _repository.Public = value;
        }

        public CloneLinks Links
        {
            get => _repository.Links;
            set => _repository.Links = value;
        }

        #endregion

        public override string ToString()
        {
            return $"{this.Name} - {this.Description} ({this.ProjectKey})";
        }
    }
}