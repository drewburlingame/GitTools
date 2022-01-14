using System;
using BbGit.Framework;
using LibGit2Sharp;

namespace BbGit.Git
{
    public static class GitUrls
    {
        public static Remote GetOrigin(this LocalRepo localRepo)
        {
            return localRepo.GitRepo!.Network.Remotes["origin"];
        }

        public static IDisposable ToggleHttpsUrl(this LocalRepo localRepo)
        {
            // LibGit2Sharp does not support ssh at this time
            // so we'll need to switch to https for any operations with a remote

            var remote = GetOrigin(localRepo);
            if (remote == null)
            {
                throw new Exception($"origin not specified for {localRepo.Name}.  add an origin or reclone this repo.");
            }

            if (remote.Url.StartsWith("http") && remote.PushUrl.StartsWith("http"))
            {
                return new DisposableAction(null);
            }

            if (localRepo.RemoteRepo is null)
            {
                throw new Exception($"remote repo not found for {localRepo}");
            }

            var httpsUrl = localRepo.RemoteRepo.HttpUrl;
            if (httpsUrl == null)
            {
                throw new Exception($"http url is null for {localRepo}");
            }

            var originalUrl = remote.Url;
            var originalPushUrl = remote.PushUrl;

            SetOriginUrl(localRepo, httpsUrl, httpsUrl);

            return new DisposableAction(() => SetOriginUrl(localRepo, originalUrl, originalPushUrl));
        }

        public static void SetOriginToHttp(this LocalRepo localRepo)
        {
            if (localRepo.RemoteRepo is null)
            {
                throw new Exception($"remote repo not found for {localRepo}");
            }

            var url = localRepo.RemoteRepo.HttpUrl;
            if (url is null)
            {
                throw new Exception($"http url is null for {localRepo}");
            }

            SetOriginUrl(localRepo, url, url);
        }

        public static void SetOriginToSsh(this LocalRepo localRepo)
        {
            if (localRepo.RemoteRepo is null)
            {
                throw new Exception($"remote repo not found for {localRepo}");
            }

            var url = localRepo.RemoteRepo.SshUrl;
            if (url is null)
            {
                throw new Exception($"ssh url is null for {localRepo}");
            }

            SetOriginUrl(localRepo, url, url);
        }

        private static void SetOriginUrl(LocalRepo localRepo, string? url, string? pushUrl)
        {
            if (localRepo.GitRepo is null)
            {
                throw new Exception($"{localRepo} is not a git repo");
            }

            localRepo.GitRepo.Network.Remotes.Update("origin", updater =>
            {
                updater.Url = url;
                updater.PushUrl = pushUrl;
            });
        }
    }
}