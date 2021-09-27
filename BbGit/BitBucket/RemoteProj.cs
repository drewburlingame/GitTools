using System.Linq;
using Bitbucket.Net.Models.Core.Projects;

namespace BbGit.BitBucket
{
    public class RemoteProj
    {
        private readonly Project bbProj;

        public RemoteProj()
        {
            this.bbProj = new Project();
        }

        public RemoteProj(Project bbProj)
        {
            this.bbProj = bbProj;
        }

        public string LinkToSelf => bbProj.Links.Self.First().Href;

        #region bbProj delegated members

        public string Key
        {
            get => bbProj.Key;
            set => bbProj.Key = value;
        }

        public string Name
        {
            get => bbProj.Name;
            set => bbProj.Name = value;
        }

        public string Description
        {
            get => bbProj.Description;
            set => bbProj.Description = value;
        }

        public int Id
        {
            get => bbProj.Id;
            set => bbProj.Id = value;
        }

        public bool Public
        {
            get => bbProj.Public;
            set => bbProj.Public = value;
        }

        public string Type
        {
            get => bbProj.Type;
            set => bbProj.Type = value;
        }

        public Links Links
        {
            get => bbProj.Links;
            set => bbProj.Links = value;
        }

        #endregion
    }
}