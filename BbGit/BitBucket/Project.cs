using System.Linq;
using Bitbucket.Net.Models.Core.Projects;
using bbProj=Bitbucket.Net.Models.Core.Projects.Project;

namespace BbGit.BitBucket
{
    public class Project
    {
        private readonly bbProj bbProj;

        public Project()
        {
            this.bbProj = new bbProj();
        }

        public Project(bbProj bbProj)
        {
            this.bbProj = bbProj;
        }

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

        public string LinkToSelf => bbProj.Links.Self.First().Href;
    }
}