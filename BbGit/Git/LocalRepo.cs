using System;
using System.IO;
using System.Linq;
using BbGit.Framework;
using LibGit2Sharp;

namespace BbGit.Git
{
    public class LocalRepo : IDisposable
    {
        private readonly FolderConfig config;
        private LazyLoadProxy<RemoteRepo> remoteRepo;

        private bool isDisposing;
        public string FullPath { get; }
        public string Name { get; }
        public bool Exists { get; private set; }
        public bool IsGitDir { get; private set; }
        public Repository GitRepo { get; private set; }
        
        public RemoteRepo RemoteRepo
        {
            get => remoteRepo;
            set => remoteRepo = value;
        }

        public string CurrentBranchName => GitRepo.Head.FriendlyName;

        public bool IsInLocalBranch => CurrentBranchName != "master";

        public int LocalBranchCount => GitRepo.Branches.Count(b => !b.IsRemote && b.FriendlyName != "master");

        public int RemoteBranchCount => GitRepo.Branches.Count(b => b.IsRemote);

        public int StashCount => GitRepo.Stashes.Count();

        public int LocalChangesCount => GitRepo
            .RetrieveStatus(new StatusOptions {IncludeUntracked = true, RecurseUntrackedDirs = true, ExcludeSubmodules = true, Show = StatusShowOption.WorkDirOnly})
            .Count(s => s.State != FileStatus.Ignored);
        
        public LocalRepo(Repository gitRepo)
            : this(gitRepo.Info.WorkingDirectory, gitRepo)
        {
        }

        public LocalRepo(string fullPath)
            : this(fullPath, null)
        {
        }

        /// <summary>private because it doesn't make sense to provide both</summary>
        private LocalRepo(string fullPath, Repository gitRepo = null)
        {
            FullPath = fullPath;
            GitRepo = gitRepo;
            Exists = Directory.Exists(FullPath);
            Name = new DirectoryInfo(FullPath).Name;
            config = new FolderConfig(FullPath);
            EvaluateIfExists();

            this.remoteRepo = new LazyLoadProxy<RemoteRepo>(() => config.GetJsonConfig<RemoteRepo>($"{Name}-remote"));
        }

        public LocalRepo(LocalRepo localRepo)
        {
            FullPath = localRepo.FullPath;
            this.remoteRepo = localRepo.remoteRepo;
            Exists = localRepo.Exists;
            IsGitDir = localRepo.IsGitDir;
            GitRepo = localRepo.GitRepo;
            Name = localRepo.Name;
            config = localRepo.config;
        }

        public void SaveConfigs()
        {
            config.SaveJsonConfig($"{Name}-remote", RemoteRepo);
        }

        public void ClearConfigs()
        {
            config.ClearAll();
        }

        /// <summary>Evaluates if the local repo exists and if so, updates related properties</summary>
        public void EvaluateIfExists()
        {
            Exists = Directory.Exists(FullPath);
            if (Exists)
            {
                IsGitDir = IsGitDirectory(FullPath);
                if (IsGitDir)
                {
                    GitRepo = GitRepo ?? new Repository(FullPath);
                }
            }
        }

        public void Dispose()
        {
            if (isDisposing)
            {
                return;
            }

            lock (this)
            {
                if (isDisposing || GitRepo == null)
                {
                    return;
                }
                isDisposing = true;

                GitRepo.Dispose();
                GitRepo = null;
            }
        }

        private static bool IsGitDirectory(string directoryPath)
        {
            try
            {
                return Directory.GetDirectories(directoryPath, ".git", SearchOption.TopDirectoryOnly).Any();
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