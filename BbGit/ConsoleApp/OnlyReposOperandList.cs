using System.Collections.Generic;
using CommandDotNet;

namespace BbGit.ConsoleApp
{
    public class OnlyReposOperandList : IArgumentModel
    {

        [Operand(
            Name = "only-repos",
            Description = "If provided, include only these repos")]
        public List<string> RepoNames { get; set; }
    }
}