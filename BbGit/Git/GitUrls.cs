using System;
using BbGit.Framework;
using LibGit2Sharp;

namespace BbGit.Git
{
    public static class GitUrls
    {
        public static void SetOriginToSsh(LocalRepo localRepo)
        {
            localRepo.GitRepo.Network.Remotes.Update("origin", updater =>
            {
                updater.Url = localRepo.RemoteRepo.SshUrl;
                updater.PushUrl = localRepo.RemoteRepo.SshUrl;
            });
        }

        public static IDisposable ToggleHttpsUrl(LocalRepo localRepo)
        {
            // LibGit2Sharp does not support ssh at this time
            // so we'll need to switch to https for any operations with a remote

            var remote = localRepo.GitRepo.Network.Remotes["origin"];
            if (remote == null)
            {
                throw new Exception($"origin not specified for {localRepo.Name}.  add an origin or reclone this repo.");
            }

            if (remote.Url.StartsWith("https") && remote.PushUrl.StartsWith("https"))
            {
                return new DisposableAction(null);
            }
            
            var httpsUrl = localRepo.RemoteRepo?.HttpsUrl;
            if (httpsUrl == null)
            {
                throw new Exception($"remote repo not found for {localRepo.Name}");
            }

            SetOriginUrl(localRepo.GitRepo, httpsUrl, httpsUrl);
            return new DisposableAction(() => SetOriginUrl(localRepo.GitRepo, remote.Url, remote.PushUrl));
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