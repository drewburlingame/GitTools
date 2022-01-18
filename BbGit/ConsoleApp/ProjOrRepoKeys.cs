using System.Collections.Generic;
using CommandDotNet;

namespace BbGit.ConsoleApp
{
    public class ProjOrRepoKeys : IArgumentModel
    {
        [Option('p', DescriptionLines = new[]{
            "Keys of projects to use.",
            "  Use $* to populate from piped input.",
            "  Mutually exclusive with --repos."
        })]
        public IEnumerable<string>?  Projects { get; set; }

        [Option('r',
            DescriptionLines =  new []{
            "Keys of repos to use.",
            "  Use $* to populate from piped input.",
            "  Mutually exclusive with --projects."
        })]
        public IEnumerable<string>? Repos { get; set; }
    }
}