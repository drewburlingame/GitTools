using System;
using System.IO;
using Jil;

namespace BbGit.Git
{
    public class RepoConfig
    {
        private readonly LocalRepo localRepo;
        private readonly string path;

        public RepoConfig(LocalRepo localRepo)
        {
            this.localRepo = localRepo;
            this.path = Path.Combine(localRepo.FullPath, ".bbgit");
        }

        public void AddConfig(string filename, string contents)
        {
            EnsureDirectoryExists();
            File.WriteAllText(BuildFilePath(filename), contents);
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

        public void AddJsonConfig(string filename, object value)
        {
            EnsureDirectoryExists();
            var filePath = BuildFilePath(SetJsonExtension(filename));
            var json = JSON.Serialize(value, Options.PrettyPrintExcludeNullsIncludeInheritedUtc);
            File.WriteAllText(filePath, json);
        }

        public T GetJsonConfig<T>(string filename)
        {
            EnsureDirectoryExists();
            var filePath = BuildFilePath(SetJsonExtension(filename));
            if (!File.Exists(filePath))
            {
                return default(T);
            }
            var json = File.ReadAllText(filePath);
            return JSON.Deserialize<T>(json);
        }

        public void ClearAll()
        {
            Directory.Delete(this.path, true);
        }

        private void EnsureDirectoryExists()
        {
            if (!this.localRepo.Exists)
            {
                throw new InvalidOperationException($"local repo does not exist: {this.localRepo.FullPath}");
            }
            if (!Directory.Exists(this.path))
            {
                var directory = Directory.CreateDirectory(this.path);
                directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
        }

        private string BuildFilePath(string filename)
        {
            return Path.Combine(this.path, filename);
        }

        private static string SetJsonExtension(string filename)
        {
            return filename.EndsWith(".json")
                ? filename
                : $"{filename}.json";
        }
    }
}