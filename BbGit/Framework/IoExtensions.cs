using System.IO;

namespace BbGit.Framework
{
    public static class IoExtensions
    {
        public static void SetFileAttributes(this string directoryPath, FileAttributes attributes = FileAttributes.Normal)
        {
            foreach (var subDir in Directory.GetDirectories(directoryPath))
            {
                SetFileAttributes(subDir, attributes);
            }
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                new FileInfo(file).Attributes = attributes;
            }
        }
    }
}