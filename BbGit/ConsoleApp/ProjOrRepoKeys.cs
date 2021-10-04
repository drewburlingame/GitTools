using System.Collections.Generic;
using System.Linq;
using CommandDotNet;

namespace BbGit.ConsoleApp
{
    public class ProjOrRepoKeys : IArgumentModel
    {
        [Option(ShortName = "r", Description = "When specified, the keys-to-use arguments are considered repository names instead of project keys")]
        public bool UseRepoKeys { get; set; }

        [Operand(
            Name = "keys-to-use", 
            Description = "the keys to use in the command. The keys are considered project keys unless -r is specified")]
        public IEnumerable<string> KeysToUse { get; set; }

        public IEnumerable<string> GetProjKeysOrEmpty() => UseRepoKeys ? Enumerable.Empty<string>() : KeysToUse;

        public IEnumerable<string> GetProjKeysOrNull() => UseRepoKeys ? null : KeysToUse;

        public IEnumerable<string> GetRepoKeysOrEmpty() => UseRepoKeys ? KeysToUse : Enumerable.Empty<string>();

        public IEnumerable<string> GetRepoKeysOrNull() => UseRepoKeys ? KeysToUse : null;
    }
}