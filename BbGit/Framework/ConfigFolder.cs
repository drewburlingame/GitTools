﻿using System;
using System.IO;
using Jil;

namespace BbGit.Framework
{
    public class ConfigFolder
    {
        public string FolderPath { get; }

        public static ConfigFolder UserFolder()
        {
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new ConfigFolder(userFolder);
        }

        public static ConfigFolder CurrentDirectory()
        {
            return new ConfigFolder(Environment.CurrentDirectory);
        }

        public ConfigFolder(string root)
        {
            if (!Directory.Exists(root))
            {
                throw new InvalidOperationException($"folder does not exist: {root}");
            }
            
            FolderPath = Path.Combine(root, ".bbgit");
            EnsureDirectoryExists(FolderPath);
        }

        public ConfigFolder ChildDirectory(string directoryName)
            => new (Path.Combine(FolderPath, directoryName));

        public string SaveConfig(string filename, string contents)
        {
            var filePath = this.BuildFilePath(filename);
            File.WriteAllText(filePath, contents);
            return filePath;
        }

        public string GetConfig(string filename)
        {
            var filePath = this.BuildFilePath(filename);
            return File.Exists(filePath) 
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
            Directory.Delete(this.FolderPath, true);
        }

        private void EnsureDirectoryExists(string bbGitPath)
        {
            if (!Directory.Exists(bbGitPath))
            {
                var directory = Directory.CreateDirectory(bbGitPath);
                directory.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
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