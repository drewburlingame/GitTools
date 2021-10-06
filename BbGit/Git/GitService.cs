using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BbGit.BitBucket;
using BbGit.ConsoleApp;
using BbGit.ConsoleUtils;
using BbGit.Framework;
using CommandDotNet;
using CommandDotNet.Rendering;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace BbGit.Git
{
    public class GitService
    {

        private static readonly Regex CaptureProjectKeyFromHttpUrl = new("[projects|scm]/(?<projectkey>.[A-Za-z0-9])");
        private static readonly Regex CaptureProjectKeyFromSshUrl = new("ssh://.*/(?<projectkey>.[A-Za-z0-9])/");

        private readonly string currentDirectory = Directory.GetCurrentDirectory();

        private readonly IConsole console;
        private readonly BbService bbService;
        private readonly CredentialsHandler credentialsProvider;


        public GitService(IConsole console, BbService bbService, CredentialsHandler credentialsProvider)
        {
            this.console = console;
            this.bbService = bbService;
            this.credentialsProvider = credentialsProvider;
        }

        public void CloneRepo(RemoteRepo remoteRepo, bool setOriginToSsh)
        {
            var localRepo =
                new LocalRepo(Path.Combine(this.currentDirectory, remoteRepo.Name), remoteRepo);
            
            if (localRepo.Exists)
            {
                console.WriteLine($"{remoteRepo.Name.ColorRepo()} already exists".ColorDefault());
                return;
            }

            console.WriteLine($"cloning  {remoteRepo.Name.ColorRepo()}");
            console.WriteLine($"  from  {remoteRepo.HttpUrl.ColorPath()}");
            console.WriteLine($"  to  {localRepo.FullPath.ColorPath()}");

            try
            {
                Repository.Clone(
                    remoteRepo.HttpUrl,
                    localRepo.FullPath,
                    new CloneOptions
                    {
                        CredentialsProvider = this.credentialsProvider
                    });

                // remove ReadOnly
                localRepo.FullPath.SetFileAttributes(FileAttributes.Normal);
            }
            catch (Exception e)
            {
                e.Data.Add("remote", $"name:{remoteRepo.Name} http:{remoteRepo.HttpUrl}");
                e.Data.Add("local", $"name:{localRepo.Name} path:{localRepo.FullPath}");
                throw;
            }

            localRepo.SetGitRepo();
            localRepo.RemoteRepo = remoteRepo;
            if (setOriginToSsh)
            {
                localRepo.SetOriginToSsh();
            }

            localRepo.SetGitRepo();
        }

        public void PullLatest(LocalRepo localRepo, bool prune, string branchName)
        {
            var currentBranchName = localRepo.CurrentBranchName;

            bool canPrune = branchName?.Equals(currentBranchName) ?? localRepo.IsAMainBranch;

            if (!canPrune)
            {
                console.WriteLine($"Skipping pull for {localRepo.Name.ColorRepo()}. " +
                                  $"Expected branch {branchName?.ColorBranch() ?? "master or develop".ColorBranch()} " +
                                  $"but was {currentBranchName.ColorBranch()}".ColorError());

                return;
            }

            // TODO: git stash > co target-branch > pull > co orig-branch > git stash pop

            console.WriteLine($"pulling { (prune ? "and pruning " : null)} branch {currentBranchName.ColorBranch()} for {localRepo.Name.ColorRepo()}".ColorDefault());

            using (localRepo.ToggleHttpsUrl())
            {
                string compressedObjects = null;
                string totals = null;
                Commands.Pull(
                    localRepo.GitRepo,
                    localRepo.GitRepo.Config.BuildSignature(new DateTimeOffset(DateTime.Now)),
                    new PullOptions
                    {
                        FetchOptions = new FetchOptions
                        {
                            CredentialsProvider = this.credentialsProvider,
                            Prune = prune,
                            OnProgress = output =>
                            {
                                if (output != null)
                                {
                                    if (output.EndsWith("done."))
                                    {
                                        compressedObjects = output;
                                    }
                                    else if (output.StartsWith("Total "))
                                    {
                                        totals = output;
                                    }
                                }

                                return true;
                            }
                        }
                    });

                if (compressedObjects != null)
                {
                    console.WriteLine(compressedObjects);
                }

                if (totals != null)
                {
                    console.WriteLine(totals);
                }
            }
        }

        public DisposableCollection<LocalRepo> GetLocalRepos(ProjOrRepoKeys projOrRepoKeys)
            => GetLocalRepos(
                projOrRepoKeys.GetProjKeysOrNull()?.ToCollection(),
                projOrRepoKeys.GetRepoKeysOrNull()?.ToCollection());

        public DisposableCollection<LocalRepo> GetLocalRepos(ICollection<string> onlyProjKeys = null, ICollection<string> onlyRepoNames = null)
        {
            var directories = GetLocalDirectoryPaths();

            Dictionary<string, Dictionary<string, RemoteRepo>> remotesByProj = this.bbService.GetRepos()
                .GroupBy(r => r.ProjectKey)
                .ToDictionary(
                    g => g.Key, 
                    g => g.ToDictionary(r => r.Name));

            RemoteRepo GetRemote(LocalRepo local)
            {
                var originUrl = local.GetOrigin().Url;
                Match match;
                if (originUrl.StartsWith("ssh"))
                    match = CaptureProjectKeyFromSshUrl.Match(originUrl);
                else
                    match = CaptureProjectKeyFromHttpUrl.Match(originUrl);

                if (match.Success)
                {
                    var projectKey = match?.Groups["projectkey"]?.Value.ToUpper();
                    return remotesByProj
                        .GetValueOrDefault(projectKey)?
                        .GetValueOrDefault(local.Name);
                }

                return null;
            }

            return directories
                .WhereIf(onlyRepoNames is not null, d => onlyRepoNames!.Contains(d.Name, StringComparer.OrdinalIgnoreCase))
                .Select(d => new LocalRepo(d.FullName, GetRemote)) 
                .WhereIf(onlyProjKeys is not null, r => r.RemoteRepo is not null && onlyProjKeys!.Contains(r.RemoteRepo.ProjectKey))
                .Where(r => r.IsGitDir)
                .OrderBy(r => r.Name)
                .ToDisposableCollection();

            // I haven't figured out why, but Libgit2Object.Dispose throws an 
            //   AccessViolationException - Attempted to read or write protected memory
            // It's only seen when the '-o' option is specified
            // That option only affects how a list is filtered, so I fear debugging will not be easy.
            // Disposing the collection seems to fix it.
        }

        private IEnumerable<DirectoryInfo> GetLocalDirectoryPaths()
        {
            try
            {
                return Directory
                    .GetDirectories(this.currentDirectory)
                    .Select(d => new DirectoryInfo(d));
            }
            catch (Exception e)
            {
                throw new Exception($"{e.Message} {new {currentDirectory = this.currentDirectory}}", e);
            }
        }
    }
}