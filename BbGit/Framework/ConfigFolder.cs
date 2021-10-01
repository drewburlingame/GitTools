using System;
using System.IO;
using Jil;

namespace BbGit.Framework
{
    public class ConfigFolder
    {
        private readonly string path;
        
        public string BbGitPath { get; }

        public static ConfigFolder UserFolder()
        {
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new ConfigFolder(userFolder);
        }

        public static ConfigFolder CurrentDirectory()
        {
            return new ConfigFolder(Environment.CurrentDirectory);
        }

        public ConfigFolder(string path)
        {
            this.path = path;
            this.BbGitPath = Path.Combine(path, ".bbgit");
        }

        public string SaveConfig(string filename, string contents)
        {
            this.EnsureDirectoryExists();

            var filePath = this.BuildFilePath(filename);
            File.WriteAllText(filePath, contents);
            return filePath;
        }

        public string GetConfig(string filename)
        {
            this.EnsureDirectoryExists();

            var filePath = this.BuildFilePath(filename);
            if (!File.Exists(filePath))
            {
                return null;
            }

            return File.ReadAllText(filePath);
        }

        public string SaveJsonConfig(string filename, object value)
        {
            filename = SetJsonExtension(filename);
            var json = JSON.Serialize(value, Options.PrettyPrintExcludeNullsIncludeInheritedUtc);
            return this.SaveConfig(filename, json);
        }


        public T GetJsonConfigOrDefault<T>(string filename) where T : class, new()
        {
            return GetJsonConfigOrEmpty<T>(filename) ?? new T();
        }

        public T GetJsonConfigOrEmpty<T>(string filename) where T: class
        {
            filename = SetJsonExtension(filename);
            var json = this.GetConfig(filename);
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JSON.Deserialize<T>(json);
        }

        public void ClearAll()
        {
            Directory.Delete(this.BbGitPath, true);
        }

        public void Exists() => Directory.Exists(this.BbGitPath);

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(this.path))
            {
                throw new InvalidOperationException($"folder does not exist: {this.path}");
            }

            if (!Directory.Exists(this.BbGitPath))
            {
                var directory = Directory.CreateDirectory(this.BbGitPath);
                directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
        }

        private string BuildFilePath(string filename)
        {
            return Path.Combine(this.BbGitPath, filename);
        }

        private static string SetJsonExtension(string filename)
        {
            return filename.EndsWith(".json")
                ? filename
                : $"{filename}.json";
        }
    }
}