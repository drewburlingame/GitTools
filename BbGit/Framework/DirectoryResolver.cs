using System;

namespace BbGit.Framework
{
    /// <summary>
    ///     Allows tests to target a folder and for debugging from VS.NET
    /// </summary>
    public class DirectoryResolver
    {
        public string CurrentDirectory { get; private set; } = Environment.CurrentDirectory;

        public void SetCurrentDirectory(string currentDirectory)
        {
            CurrentDirectory = currentDirectory ?? Environment.CurrentDirectory;
        }
    }
}