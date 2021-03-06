﻿using System;
using System.IO;
using Jil;

namespace BbGit.Framework
{
    public class FolderConfig
    {
        private readonly string path;
        
        public string BbGitPath { get; }

        public FolderConfig(string path)
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

        public T GetJsonConfig<T>(string filename) where T : new()
        {
            filename = SetJsonExtension(filename);
            var json = this.GetConfig(filename);
            return string.IsNullOrWhiteSpace(json)
                ? new T()
                : JSON.Deserialize<T>(json);
        }

        public void ClearAll()
        {
            Directory.Delete(this.BbGitPath, true);
        }

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