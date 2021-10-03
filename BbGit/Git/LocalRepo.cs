using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace BbGit.Git
{
    public class LocalRepo : IDisposable
    {
        private bool isDisposing;
        public string FullPath { get; }
        public string Name { get; }
        public bool Exists { get; private set; }
        public bool IsGitDir { get; private set; }
        public Repository GitRepo { get; private set; }

        public string CurrentBranchName => this.GitRepo.Head.FriendlyName;

        public bool IsInLocalBranch => this.CurrentBranchName != "master";

        public int LocalBranchCount => this.GitRepo.Branches.Count(b => !b.IsRemote && b.FriendlyName != "master");

        public int RemoteBranchCount => this.GitRepo.Branches.Count(b => b.IsRemote);

        public int StashCount => this.GitRepo.Stashes.Count();

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

        public LocalRepo(RemoteRepo remoteRepo, Repository gitRepo)
            : this(remoteRepo, gitRepo.Info.WorkingDirectory, gitRepo)
        {
        }

        public LocalRepo(RemoteRepo remoteRepo, string fullPath)
            : this(remoteRepo, fullPath, null)
        {
        }

        /// <summary>private because it doesn't make sense to provide both</summary>
        private LocalRepo(RemoteRepo remoteRepo, string fullPath, Repository gitRepo = null)
        {
            this.RemoteRepo = remoteRepo;
            this.FullPath = fullPath;
            this.GitRepo = gitRepo;
            this.Exists = Directory.Exists(this.FullPath);
            this.Name = new DirectoryInfo(this.FullPath).Name;
            this.EvaluateIfExists();
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
        public void EvaluateIfExists()
        {
            this.Exists = Directory.Exists(this.FullPath);
            if (this.Exists)
            {
                this.IsGitDir = IsGitDirectory(this.FullPath);
                if (this.IsGitDir)
                {
                    this.GitRepo ??= new Repository(this.FullPath);
                }
            }
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