using BbGit.Git;

namespace BbGit.ConsoleApp
{
    public class RepoPair
    {
        public LocalRepo? Local { get; set; }
        public RemoteRepo? Remote { get; set; }
    }
}