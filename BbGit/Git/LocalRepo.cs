using System;
using System.IO;
using System.Linq;
using BbGit.Framework;
using LibGit2Sharp;

namespace BbGit.Git
{
    public class LocalRepo : IDisposable
    {
        private static readonly string[] MainBranchNames = {"master", "develop"};

        private bool isDisposing;
        public string FullPath { get; }
        public string Name { get; }
        public bool Exists { get; private set; }
        public bool IsGitDir { get; private set; }
        public Repository GitRepo { get; private set; }

        public string CurrentBranchName => this.GitRepo.Head.FriendlyName;

        public bool IsAMainBranch => MainBranchNames.Contains(CurrentBranchName);

        public bool IsInLocalBranch => !CurrentBranchName.IsIn(MainBranchNames);

        public int LocalBranchCount => GitRepo.Branches.Count(b => !b.IsRemote && !b.FriendlyName.IsIn(MainBranchNames));

        public int RemoteBranchCount => GitRepo.Branches.Count(b => b.IsRemote);

        public int StashCount => GitRepo.Stashes.Count();

        public int LocalChangesCount => this.GitRepo
            .RetrieveStatus(new StatusOptions
            {
                IncludeUntracked = true,
                RecurseUntrackedDirs = true,
                ExcludeSubmodules = true,
                Show = StatusShowOption.WorkDirOnly
            })
            .Count(s => s.State != FileStatus.Ignored);

        public RemoteRepo RemoteRepo { get; set; }

        public LocalRepo(string fullPath, RemoteRepo remoteRepo)
        {
            this.FullPath = fullPath;
            this.Name = new DirectoryInfo(this.FullPath).Name;
            this.RemoteRepo = remoteRepo;
            this.SetGitRepo();
        }

        /// <summary>private because it doesn't make sense to provide both</summary>
        public LocalRepo(string fullPath, Func<LocalRepo, RemoteRepo> getRemote)
        {
            this.FullPath = fullPath;
            this.Name = new DirectoryInfo(this.FullPath).Name;
            if (this.SetGitRepo())
            {
                this.RemoteRepo = getRemote(this);
            }
        }

        public void Dispose()
        {
            if (this.isDisposing)
            {
                return;
            }

            lock (this)
            {
                if (this.isDisposing || this.GitRepo == null)
                {
                    return;
                }

                this.isDisposing = true;

                this.GitRepo.Dispose();
                this.GitRepo = null;
            }
        }

        /// <summary>Evaluates if the local repo exists and if so, updates related properties</summary>
        public bool SetGitRepo()
        {
            this.Exists = Directory.Exists(this.FullPath);
            if (this.Exists)
            {
                this.IsGitDir = IsGitDirectory(this.FullPath);
                if (this.IsGitDir)
                {
                    this.GitRepo ??= new Repository(this.FullPath);
                    return true;
                }
            }

            return false;
        }

        private static bool IsGitDirectory(string directoryPath)
        {
            try
            {
                var directories = Directory.GetDirectories(directoryPath, ".git", SearchOption.TopDirectoryOnly);
                return directories.Any();
            }
            catch (Exception e)
            {
                throw new Exception($"{e.Message} {new {directoryPath}}", e);
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Name;
        }
    }
}