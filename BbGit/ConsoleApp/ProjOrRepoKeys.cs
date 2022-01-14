using System.Collections.Generic;
using CommandDotNet;

namespace BbGit.ConsoleApp
{
    public class ProjOrRepoKeys : IArgumentModel
    {
        [Option('p', Description = "When specified, the inputs are parsed as project keys instead of repository names")]
        public bool UseProjKeys { get; set; }

        [Operand("inputs", 
            Description = "The repository names or project keys to use in the command. Assumes repository unless -p is specified")]
        public IEnumerable<string>? Inputs { get; set; }
        
        public IEnumerable<string>? GetProjKeysOrNull() => UseProjKeys ? Inputs : null;

        public IEnumerable<string>? GetRepoKeysOrNull() => !UseProjKeys ? Inputs : null;
    }
}