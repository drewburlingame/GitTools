using System.Linq;
using Bitbucket.Net.Models.Core.Projects;

namespace BbGit.BitBucket
{
    public class RemoteProj
    {
        private readonly Project _bbProj;

        public RemoteProj()
        {
            _bbProj = new Project();
        }

        public RemoteProj(Project bbProj)
        {
            _bbProj = bbProj;
        }

        public string LinkToSelf => _bbProj.Links.Self.First().Href;

        #region bbProj delegated members

        public string Key
        {
            get => _bbProj.Key;
            set => _bbProj.Key = value;
        }

        public string Name
        {
            get => _bbProj.Name;
            set => _bbProj.Name = value;
        }

        public string Description
        {
            get => _bbProj.Description;
            set => _bbProj.Description = value;
        }

        public int Id
        {
            get => _bbProj.Id;
            set => _bbProj.Id = value;
        }

        public bool Public
        {
            get => _bbProj.Public;
            set => _bbProj.Public = value;
        }

        public string Type
        {
            get => _bbProj.Type;
            set => _bbProj.Type = value;
        }

        public Links Links
        {
            get => _bbProj.Links;
            set => _bbProj.Links = value;
        }

        #endregion
    }
}