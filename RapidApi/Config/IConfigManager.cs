using RapidApi.Remote.Models;
using System.Collections.Generic;

namespace RapidApi.Config
{
    interface IConfigManager
    {
        void DeleteProjectData(string appName);
        RootConfig GetRootConfig();
        IEnumerable<RemoteProject> GetSavedProjects();
        RemoteProject LoadProject(string appName);
        void SaveProjectData(RemoteProject project);
        RootConfig SaveRootConfig(RootConfig config);
    }
}