using System;
using System.IO;
using Jil;

namespace BbGit.Framework
{
    public class FolderConfig
    {
        private string path;
        private readonly string bbGitPath;

        public FolderConfig(string path)
        {
            this.path = path;
            this.bbGitPath = Path.Combine(path, ".bbgit");
        }

        public string AddConfig(string filename, string contents)
        {
            EnsureDirectoryExists();

            var filePath = BuildFilePath(filename);
            File.WriteAllText(filePath, contents);
            return filePath;
        }

        public string GetConfig(string filename)
        {
            EnsureDirectoryExists();

            var filePath = BuildFilePath(filename);
            if (!File.Exists(filePath))
            {
                return null;
            }

            return File.ReadAllText(filePath);
        }

        public string AddJsonConfig(string filename, object value)
        {
            filename = SetJsonExtension(filename);
            var json = JSON.Serialize(value, Options.PrettyPrintExcludeNullsIncludeInheritedUtc);
            return AddConfig(filename, json);
        }

        public T GetJsonConfig<T>(string filename)
        {
            filename = SetJsonExtension(filename);
            var json = GetConfig(filename);
            return JSON.Deserialize<T>(json);
        }

        public void ClearAll()
        {
            Directory.Delete(this.bbGitPath, true);
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(this.path))
            {
                throw new InvalidOperationException($"folder does not exist: {this.path}");
            }
            if (!Directory.Exists(this.bbGitPath))
            {
                var directory = Directory.CreateDirectory(this.bbGitPath);
                directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
        }

        private string BuildFilePath(string filename)
        {
            return Path.Combine(this.bbGitPath, filename);
        }

        private static string SetJsonExtension(string filename)
        {
            return filename.EndsWith(".json")
                ? filename
                : $"{filename}.json";
        }
    }
}