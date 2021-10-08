using System;
using System.IO;
using Jil;

namespace BbGit.Framework
{
    public class ConfigFolder
    {
        private readonly ConfigFolder? parentConfigFolder;
        public bool Exists { get; }
        public string FolderPath { get; }

        public static ConfigFolder UserProfile()
        {
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new ConfigFolder(userFolder);
        }

        public static ConfigFolder CurrentDirectory()
        {
            return new ConfigFolder(Environment.CurrentDirectory);
        }

        public ConfigFolder Subfolder(string name) => new(this, name);

        private ConfigFolder(string root)
        {
            if (!Directory.Exists(root))
            {
                throw new InvalidOperationException($"folder does not exist: {root}");
            }
            
            FolderPath = Path.Combine(root, ".bbgit");
            Exists = Directory.Exists(FolderPath);
        }

        private ConfigFolder(ConfigFolder parentConfigFolder, string name)
        {
            this.parentConfigFolder = parentConfigFolder;

            FolderPath = Path.Combine(parentConfigFolder.FolderPath,name);
            Exists = parentConfigFolder.Exists && Directory.Exists(FolderPath);
        }

        public string SaveConfig(string filename, string contents)
        {
            EnsureDirectoryExists();
            var filePath = this.BuildFilePath(filename);
            File.WriteAllText(filePath, contents);
            return filePath;
        }

        public string GetConfig(string filename)
        {
            var filePath = this.BuildFilePath(filename);
            return Exists && File.Exists(filePath) 
                ? File.ReadAllText(filePath) 
                : null;
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
            if (!Exists) return;
            Directory.Delete(this.FolderPath, true);
        }

        private void EnsureDirectoryExists()
        {
            // extra check in case another ConfigFolder created the directory.
            if (Exists || Directory.Exists(FolderPath)) return;
            parentConfigFolder?.EnsureDirectoryExists();
            var directory = Directory.CreateDirectory(FolderPath);
            directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
        }

        private string BuildFilePath(string filename)
        {
            return Path.Combine(this.FolderPath, filename);
        }

        private static string SetJsonExtension(string filename)
        {
            return filename.EndsWith(".json")
                ? filename
                : $"{filename}.json";
        }
    }
}