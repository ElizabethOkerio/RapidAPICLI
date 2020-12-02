using RapidApi.Remote.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace RapidApi.Config
{
    class ConfigManager
    {
        public string RootPath
        {
            get
            {
                var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                path = Path.Combine(path, "rapidapi");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public RootConfig GetRootConfig()
        {
            var path = GetRootConfigPath();

            try
            {
                var content = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<RootConfig>(content);
                return config;
            }
            catch
            {
                return new RootConfig();
            }
        }

        public RootConfig SaveRootConfig(RootConfig config)
        {
            var currentConfig = GetRootConfig();

            if (!string.IsNullOrEmpty(config.Tenant))
            {
                currentConfig.Tenant = config.Tenant;
            }

            if (!string.IsNullOrEmpty(config.Subscription))
            {
                currentConfig.Subscription = config.Subscription;
            }

            var path = GetRootConfigPath();
            var content = JsonSerializer.Serialize(currentConfig);
            File.WriteAllText(path, content);

            return currentConfig;
        }

        public void SaveProjectData(RemoteProject project)
        {
            var json = JsonSerializer.Serialize(project);
            var fullPath = GetAppJsonPath(project.AppId);
            File.WriteAllText(fullPath, json);
        }

        public RemoteProject LoadProject(string appName)
        {
            var fullPath = GetAppJsonPath(appName);
            var project = JsonSerializer.Deserialize<RemoteProject>(File.ReadAllText(fullPath));
            return project;
        }

        public void DeleteProjectData(string appName)
        {
            var fullPath = GetAppJsonPath(appName);
            File.Delete(fullPath);
        }

        public string GetRootConfigPath()
        {
            return Path.Combine(RootPath, ".config.json");
        }

        public string GetAppJsonPath(string appName)
        {
            var filename = $"{appName}.json";
            return Path.Combine(RootPath, filename);
        }

        public IEnumerable<RemoteProject> GetSavedProjects()
        {
            var files = Directory.GetFiles(RootPath, "*.json");
            foreach (var file in files)
            {
                var basename = Path.GetFileNameWithoutExtension(file);
                if (basename.StartsWith(".")) continue; // skip .config.json file

                var project = LoadProject(basename);
                yield return project;
            }
        }
    }
}
