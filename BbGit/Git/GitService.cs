using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BbGit.ConsoleUtils;
using BbGit.Framework;
using Colorful;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Console = Colorful.Console;

namespace BbGit.Git
{
    public class GitService
    {
        private const string LocalReposConfigFileName = "localRepos";

        private readonly AppConfig appConfig;
        private readonly DirectoryResolver directoryResolver;
        private readonly PipedInput pipedInput;
        private LocalReposConfig localReposConfig;

        private string CurrentDirectory => this.directoryResolver.CurrentDirectory;

        private CredentialsHandler CredentialsProvider => (url, fromUrl, types) =>
            new UsernamePasswordCredentials {Username = this.appConfig.Username, Password = this.appConfig.AppPassword};

        public GitService(AppConfig appConfig, DirectoryResolver directoryResolver, PipedInput pipedInput)
        {
            this.appConfig = appConfig;
            this.directoryResolver = directoryResolver;
            this.pipedInput = pipedInput;
        }

        public void CloneRepo(RemoteRepo remoteRepo)
        {
            var localRepo =
                new LocalRepo(Path.Combine(this.CurrentDirectory, remoteRepo.Name)) {RemoteRepo = remoteRepo};

            var defaultColor = Colors.DefaultColor;
            if (localRepo.Exists)
            {
                // TODO: handle scenario where FolderConfig hasn't been created
                Console.WriteLineFormatted(
                    "{0} already exists",
                    new Formatter(remoteRepo.Name, Colors.BranchColor),
                    defaultColor);
                return;
            }

            Console.WriteLineFormatted(
                "cloning  {0}  to  {1}",
                new Formatter(remoteRepo.Name, Colors.RepoColor),
                new Formatter(localRepo.FullPath, Colors.PathColor),
                defaultColor);

            var repoPath = Repository.Clone(
                remoteRepo.HttpsUrl,
                localRepo.FullPath,
                new CloneOptions
                {
                    CredentialsProvider = this.CredentialsProvider
                });

            localRepo.EvaluateIfExists();
            if (this.appConfig.SetOriginToSsh)
            {
                GitUrls.SetOriginToSsh(localRepo);
            }

            localRepo.EvaluateIfExists();
            localRepo.SaveConfigs();
        }

        public void UpdateRepoConfigs(LocalRepo localRepo)
        {
            if (localRepo.Exists)
            {
                Console.WriteLineFormatted(
                    "updating configs for {0}",
                    new Formatter(localRepo.Name, Colors.BranchColor),
                    Colors.DefaultColor);

                if (this.appConfig.SetOriginToSsh)
                {
                    GitUrls.SetOriginToSsh(localRepo);
                }

                localRepo.SaveConfigs();
            }
        }

        public void ClearRepoConfigs(LocalRepo localRepo)
        {
            Console.WriteLineFormatted(
                "clearing configs for {0}",
                new Formatter(localRepo.Name, Colors.BranchColor),
                Colors.DefaultColor);

            localRepo.ClearConfigs();
        }

        public void PullLatest(LocalRepo localRepo, bool prune, string branchName = "master")
        {
            var repo = localRepo.GitRepo;

            var gitDir = new LocalRepo(repo);
            var repoColor = Colors.RepoColor;
            var branchColor = Colors.BranchColor;
            if (!repo.Head.FriendlyName.Equals(branchName))
            {
                Console.WriteLineFormatted(
                    "Skipping pull for {0}. Expected branch {1} but was {2}",
                    new Formatter(gitDir.Name, repoColor),
                    new Formatter(branchName, branchColor),
                    new Formatter(repo.Head.FriendlyName, branchColor),
                    Color.Red);

                return;
            }

            // TODO: git stash > co target-branch > pull > co orig-branch > git stash pop

            Console.WriteLineFormatted(
                "pulling " + (prune ? "and pruning " : null) + "branch {0} for {1}",
                new Formatter(branchName, branchColor),
                new Formatter(gitDir.Name, repoColor),
                Colors.DefaultColor);

            using (GitUrls.ToggleHttpsUrl(localRepo))
            {
                string compressedObjects = null;
                string totals = null;
                Commands.Pull(
                    repo,
                    repo.Config.BuildSignature(new DateTimeOffset(DateTime.Now)),
                    new PullOptions
                    {
                        FetchOptions = new FetchOptions
                        {
                            CredentialsProvider = this.CredentialsProvider,
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

        public IEnumerable<string> GetLocalRepoNames(
            bool includeIgnored = false,
            bool onlyIgnored = false)
        {
            return this.GetLocalDirectoryPaths()
                .Select(p => new DirectoryInfo(p).Name)
                .Where(n => includeIgnored
                            || onlyIgnored && this.RepoIsIgnored(n)
                            || !this.RepoIsIgnored(n))
                .ToList();
        }

        public DisposableColleciton<LocalRepo> GetLocalRepos(
            bool usePipedValuesIfAvailable = false,
            bool includeIgnored = false,
            bool onlyIgnored = false)
        {
            var paths = usePipedValuesIfAvailable && this.pipedInput.HasValues
                ? this.pipedInput.Values.Select(n => Path.Combine(this.CurrentDirectory, n))
                : this.GetLocalDirectoryPaths();

            return paths
                .Select(p => new LocalRepo(p))
                .Where(r => r.IsGitDir
                            && (includeIgnored
                                || onlyIgnored && this.RepoIsIgnored(r.Name)
                                || !this.RepoIsIgnored(r.Name)))
                .OrderBy(r => r.Name)
                .ToDisposableColleciton();

            // I haven't figured out why, but Libgit2Object.Dispose throws an 
            //   AccessViolationException - Attempted to read or write protected memory
            // It's only seen when the '-o' option is specified
            // That option only affects how a list is filtered, so I fear debugging will not be easy.
            // Disposing the collection seems to fix it.
        }

        public string GetLocalReposConfigPath()
        {
            return new FolderConfig(this.CurrentDirectory).BbGitPath;
        }

        public LocalReposConfig GetLocalReposConfig()
        {
            return this.localReposConfig ??= 
                new FolderConfig(this.CurrentDirectory)
                    .GetJsonConfig<LocalReposConfig>(LocalReposConfigFileName);
        }

        public void SaveLocalReposConfig(LocalReposConfig config)
        {
            new FolderConfig(this.CurrentDirectory).SaveJsonConfig(LocalReposConfigFileName, config);
        }

        private IEnumerable<string> GetLocalDirectoryPaths()
        {
            try
            {
                return Directory.GetDirectories(this.CurrentDirectory);
            }
            catch (Exception e)
            {
                throw new Exception($"{e.Message} {new {currentDirectory = this.CurrentDirectory}}", e);
            }
        }

        private bool RepoIsIgnored(string repoName)
        {
            if (string.IsNullOrWhiteSpace(this.GetLocalReposConfig().IgnoredReposRegex))
            {
                return true;
            }

            return Regex.IsMatch(repoName, this.GetLocalReposConfig().IgnoredReposRegex);
        }
    }
}