﻿using System.Collections.Generic;
using System.Linq;
using BbGit.Framework;
using BbGit.Git;
using MoreLinq;

namespace BbGit.ConsoleApp
{
    public static class RepoExtensions
    {
        public static IDictionary<string, RepoPair> PairRepos(
            this IEnumerable<LocalRepo> locals, IEnumerable<RemoteRepo> remotes, 
            bool mustHaveRemote = false, bool? isCloned = null)
        {
            var pairs = new Dictionary<string, RepoPair>();
            
            foreach (var local in locals)
            {
                pairs.GetValueOrAdd(local.Name).Local = local;
            }

            foreach (var remote in remotes)
            {
                pairs.GetValueOrAdd(remote.Name).Remote = remote;
            }

            if (mustHaveRemote)
            {
                pairs.Values
                    .Where(p => p.Remote is null)
                    .ForEach(p => pairs.Remove(p.Local!.Name));
            }

            if (isCloned.HasValue)
            {
                if (isCloned.Value)
                {
                    pairs.Values
                        .Where(p => p.Local is null)
                        .ForEach(p => pairs.Remove(p.Remote!.Name));
                }
                else
                {
                    pairs.Values
                        .Where(p => p.Local is not null && p.Remote is not null)
                        .ForEach(p => pairs.Remove(p.Remote!.Name));
                }
            }

            return pairs;
        }
    }
}