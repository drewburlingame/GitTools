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

        private bool _isDisposing;
        public string FullPath { get; }
        public string Name { get; }
        public bool Exists { get; private set; }
        public bool IsGitDir { get; private set; }

        public Repository? GitRepo { get; private set; }

        public RemoteRepo? RemoteRepo { get; set; }

        public string? CurrentBranchName => GitRepo?.Head.FriendlyName;

        public bool IsAMainBranch => MainBranchNames.Contains(CurrentBranchName);

        public bool IsInLocalBranch => !CurrentBranchName.IsIn(MainBranchNames);

        public int LocalBranchCount => GitRepo?.Branches.Count(b => !b.IsRemote && !b.FriendlyName.IsIn(MainBranchNames)) ?? 0;

        public int RemoteBranchCount => GitRepo?.Branches.Count(b => b.IsRemote) ?? 0;

        public int StashCount => GitRepo?.Stashes.Count() ?? 0;

        public int LocalChangesCount => GitRepo?
            .RetrieveStatus(new StatusOptions
            {
                IncludeUntracked = true,
                RecurseUntrackedDirs = true,
                ExcludeSubmodules = true,
                Show = StatusShowOption.WorkDirOnly
            })
            .Count(s => s.State != FileStatus.Ignored) ?? 0;

        public LocalRepo(string fullPath, RemoteRepo remoteRepo)
        {
            FullPath = fullPath;
            Name = new DirectoryInfo(FullPath).Name;
            RemoteRepo = remoteRepo;
            SetGitRepo();
        }

        /// <summary>private because it doesn't make sense to provide both</summary>
        public LocalRepo(string fullPath, Func<LocalRepo, RemoteRepo?> getRemote)
        {
            FullPath = fullPath;
            Name = new DirectoryInfo(FullPath).Name;
            if (SetGitRepo())
            {
                RemoteRepo = getRemote(this);
            }
        }

        public void Dispose()
        {
            if (_isDisposing)
            {
                return;
            }

            lock (this)
            {
                if (_isDisposing)
                {
                    return;
                }

                _isDisposing = true;
                GitRepo?.Dispose();
            }
        }

        /// <summary>Evaluates if the local repo exists and if so, updates related properties</summary>
        public bool SetGitRepo()
        {
            Exists = Directory.Exists(FullPath);
            if (Exists)
            {
                IsGitDir = IsGitDirectory(FullPath);
                if (IsGitDir)
                {
                    GitRepo ??= new Repository(FullPath);
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
            return Name;
        }
    }
}