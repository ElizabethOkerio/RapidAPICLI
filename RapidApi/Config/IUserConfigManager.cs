using RapidApi.Remote.Models;
using System.Collections.Generic;

namespace RapidApi.Config
{
    interface IUserConfigManager
    {
        void DeleteProjectData(string appName);
        RootUserConfig GetRootConfig();
        IEnumerable<RemoteProject> GetSavedProjects();
        RemoteProject LoadProject(string appName);
        void SaveProjectData(RemoteProject project);
        RootUserConfig SaveRootConfig(RootUserConfig config);
    }
}