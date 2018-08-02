using System.Collections.Generic;
using MoreLinq;

namespace BbGit.Git
{
    public static class LocalRepoExtensions
    {
        public static void DisposeAll(this IEnumerable<LocalRepo> localRepos)
        {
            // I haven't figured out why, but Libgit2Object.Dispose throws an 
            //   AccessViolationException - Attempted to read or write protected memory
            // It's only seen when the '-o' option is specified
            // That option only affects how a list is filtered, so I fear debugging will not be easy.
            // Disposing here seems to fix it.
            localRepos.ForEach(l => l.Dispose());
        }
    }
}