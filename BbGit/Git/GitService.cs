using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using BbGit.BitBucket;
using BbGit.ConsoleUtils;
using BbGit.Framework;
using Colorful;
using CommandDotNet;
using CommandDotNet.Rendering;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Pastel;
using Console = Colorful.Console;

namespace BbGit.Git
{
    public class GitService
    {
        private readonly BbService bbService;
        private readonly CredentialsHandler credentialsProvider;

        private readonly string currentDirectory = Directory.GetCurrentDirectory();

        public GitService(BbService bbService, CredentialsHandler credentialsProvider)
        {
            this.bbService = bbService;
            this.credentialsProvider = credentialsProvider;
        }

        public void CloneRepo(IConsole console, RemoteRepo remoteRepo, bool setOriginToSsh)
        {
            var localRepo =
                new LocalRepo(remoteRepo, Path.Combine(this.currentDirectory, remoteRepo.Name));

            var defaultColor = Colors.DefaultColor;
            if (localRepo.Exists)
            {
                console.WriteLine($"{remoteRepo.Name.ColorRepo()} already exists".ColorDefault());
                return;
            }

            console.WriteLine($"cloning  {remoteRepo.Name.ColorRepo()}");
            console.WriteLine($"  from  {remoteRepo.HttpsUrl.ColorPath()}");
            console.WriteLine($"  to  {localRepo.FullPath.ColorPath()}");

            Repository.Clone(
                remoteRepo.HttpsUrl,
                localRepo.FullPath,
                new CloneOptions
                {
                    CredentialsProvider = this.credentialsProvider
                });

            localRepo.EvaluateIfExists();
            if (setOriginToSsh)
            {
                GitUrls.SetOriginToSsh(localRepo);
            }

            localRepo.EvaluateIfExists();
        }

        public void PullLatest(LocalRepo localRepo, bool prune, string branchName = "master")
        {
            var repoColor = Colors.RepoColor;
            var branchColor = Colors.BranchColor;
            if (!localRepo.GitRepo.Head.FriendlyName.Equals(branchName))
            {
                Console.WriteLineFormatted(
                    "Skipping pull for {0}. Expected branch {1} but was {2}",
                    new Formatter(localRepo.Name, repoColor),
                    new Formatter(branchName, branchColor),
                    new Formatter(localRepo.GitRepo.Head.FriendlyName, branchColor),
                    Color.Red);

                return;
            }

            // TODO: git stash > co target-branch > pull > co orig-branch > git stash pop

            Console.WriteLineFormatted(
                "pulling " + (prune ? "and pruning " : null) + "branch {0} for {1}",
                new Formatter(branchName, branchColor),
                new Formatter(localRepo.Name, repoColor),
                Colors.DefaultColor);

            using (GitUrls.ToggleHttpsUrl(localRepo))
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
                    Console.WriteLine(compressedObjects);
                }

                if (totals != null)
                {
                    Console.WriteLine(totals);
                }
            }
        }

        public IEnumerable<string> GetLocalRepoNames(ICollection<string> onlyRepos = null)
        {
            return this.GetLocalDirectoryPaths()
                .Where(p => onlyRepos?.Contains(p.name) ?? true)
                .Select(p => p.path)
                .ToList();
        }

        public DisposableCollection<LocalRepo> GetLocalRepos(ICollection<string> onlyRepos = null)
        {
            var paths = GetLocalDirectoryPaths();

            var remoteRepos = 
                this.bbService
                .GetRepos((string)null)
                .ToList();

            return paths
                .Where(p => onlyRepos?.Contains(p.name) ?? true)
                .Select(p => new LocalRepo(remoteRepos.FirstOrDefault(r => r.Name == p.name), p.path))
                .Where(r => r.IsGitDir)
                .OrderBy(r => r.Name)
                .ToDisposableCollection();

            // I haven't figured out why, but Libgit2Object.Dispose throws an 
            //   AccessViolationException - Attempted to read or write protected memory
            // It's only seen when the '-o' option is specified
            // That option only affects how a list is filtered, so I fear debugging will not be easy.
            // Disposing the collection seems to fix it.
        }

        private IEnumerable<(string name, string path)> GetLocalDirectoryPaths()
        {
            try
            {
                return Directory
                    .GetDirectories(this.currentDirectory)
                    .Select(d => (Path.GetDirectoryName(d), d));
            }
            catch (Exception e)
            {
                throw new Exception($"{e.Message} {new {currentDirectory = this.currentDirectory}}", e);
            }
        }
    }
}