using System;
using BbGit.Framework;
using LibGit2Sharp;

namespace BbGit.Git
{
    public static class GitUrls
    {
        public static Remote GetOrigin(this LocalRepo localRepo)
        {
            return localRepo.GitRepo.Network.Remotes["origin"];
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
            
            var httpsUrl = localRepo.RemoteRepo?.HttpsUrl;
            if (httpsUrl == null)
            {
                throw new Exception($"remote repo not found for {localRepo.Name}");
            }

            var originalUrl = remote.Url;
            var originalPushUrl = remote.PushUrl;

            SetOriginUrl(localRepo.GitRepo, httpsUrl, httpsUrl);

            return new DisposableAction(() => SetOriginUrl(localRepo.GitRepo, originalUrl, originalPushUrl));
        }

        public static void SetOriginToSsh(this LocalRepo localRepo)
        {
            SetOriginUrl(localRepo.GitRepo, localRepo.RemoteRepo.SshUrl, localRepo.RemoteRepo.SshUrl);
        }

        private static void SetOriginUrl(Repository repo, string url, string pushUrl)
        {
            repo.Network.Remotes.Update("origin", updater =>
            {
                updater.Url = url;
                updater.PushUrl = pushUrl;
            });
        }
    }
}