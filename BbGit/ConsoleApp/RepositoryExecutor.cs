using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BbGit.Framework;
using BbGit.Git;
using CliWrap;
using CommandDotNet;

namespace BbGit.ConsoleApp
{
    internal class RepositoryExecutor
    {
        private readonly IConsole _console;
        private readonly CancellationToken _cancellationToken;
        private readonly bool _useCurrentDirectory;
        private readonly string _program;
        private readonly string _arguments;

        public const string LocalRepoExtendedHelpText =
            "Your command can be templated with the following:\n\n" +
            "  $repo-name: the repo name\n" +
            "  $current-branch: the current branch\n" +
            "  $full-path: the full directory path\n" +
            "  $http-url: the http url for git operations\n" +
            "  $ssh-url: the ssh url for git operations\n" +
            "  $browser-url: the BitBucket UI for this repo\n" +
            "  $proj-key: the project key\n";

        public const string RemoteRepoExtendedHelpText =
            "Your command can be templated with the following:\n\n" +
            "  $repo-name: the repo name\n" +
            "  $http-url: the http url for git operations\n" +
            "  $ssh-url: the ssh url for git operations\n" +
            "  $browser-url: the BitBucket UI for this repo\n" +
            "  $proj-key: the project key\n";

        public RepositoryExecutor(CommandContext context, IConsole console, CancellationToken cancellationToken, bool useCurrentDirectory)
        {
            _console = console;
            _cancellationToken = cancellationToken;
            _useCurrentDirectory = useCurrentDirectory;
            var separatedArguments = context.ParseResult!.SeparatedArguments;
            _program = separatedArguments.First();
            _arguments = separatedArguments.Skip(1).ToCsv(" ");
        }

        public void ExecuteFor(IEnumerable<LocalRepo> repositories)
        {
            string Template(LocalRepo r)
            {
                var args = _arguments;
                var remoteRepo = r.RemoteRepo;
                if (remoteRepo is not null)
                {
                    args = args
                        .Replace("$proj-key", remoteRepo.ProjectKey)
                        .Replace("$http-url", remoteRepo.HttpUrl)
                        .Replace("$ssh-url", remoteRepo.SshUrl)
                        .Replace("$browser-url", remoteRepo.Links.Self.FirstOrDefault()?.Href);
                }

                args = args
                    .Replace("$repo-name", r.Name)
                    .Replace("$current-branch", r.CurrentBranchName)
                    .Replace("$full-path", r.FullPath);

                return args;
            }

            repositories.SafelyForEach(
                r =>
                {
                    var workingDirPath = _useCurrentDirectory ? Directory.GetCurrentDirectory() : r.FullPath;

                    Cli.Wrap(_program)
                        .WithArguments(Template(r))
                        .WithWorkingDirectory(workingDirPath)
                        .WithStandardOutputPipe(PipeTarget.ToDelegate(_console.WriteLine))
                        .WithStandardErrorPipe(PipeTarget.ToDelegate(_console.Error.WriteLine))
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteAsync(_cancellationToken).Task.Wait(_cancellationToken);
                },
                _cancellationToken,
                summarizeErrors: true);
        }

        public void ExecuteFor(IEnumerable<RemoteRepo> repositories)
        {
            string Template(RemoteRepo r)
            {
                var args = _arguments;

                args = args
                    .Replace("$repo-name", r.Name)
                    .Replace("$proj-key", r.ProjectKey)
                    .Replace("$http-url", r.HttpUrl)
                    .Replace("$ssh-url", r.SshUrl)
                    .Replace("$browser-url", r.Links.Self.FirstOrDefault()?.Href);

                return args;
            }

            repositories.SafelyForEach(
                r =>
                {
                    var workingDirPath = _useCurrentDirectory ? Directory.GetCurrentDirectory() : Path.Join(Directory.GetCurrentDirectory(),r.Name);

                    Cli.Wrap(_program)
                        .WithArguments(Template(r))
                        .WithWorkingDirectory(workingDirPath)
                        .WithStandardOutputPipe(PipeTarget.ToDelegate(_console.WriteLine))
                        .WithStandardErrorPipe(PipeTarget.ToDelegate(_console.Error.WriteLine))
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteAsync(_cancellationToken).Task.Wait(_cancellationToken);
                },
                _cancellationToken,
                summarizeErrors: true);
        }
    }
}