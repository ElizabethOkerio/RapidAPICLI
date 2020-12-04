using Microsoft.Extensions.CommandLineUtils;
using RapidApi.Cli.Common;
using RapidApi.Common.Models;
using RapidApi.Config;
using RapidApi.Local;
using RapidApi.Remote;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RapidApi
{
    class CommandRunner
    {
        readonly IUserConfigManager configManager;
        readonly IImageCredentialsProvider imageProvider;
        readonly CommandLineApplication app;

        public CommandRunner(CommandLineApplication app, IUserConfigManager configManager, IImageCredentialsProvider imageProvider)
        {
            this.app = app;
            this.configManager = configManager;
            this.imageProvider = imageProvider;
        }

        public async Task DeployRemotely(FileInfo csdl, string appId, string tenantId, string subscriptionId, bool seedData)
        {

            var config = configManager.GetRootConfig();

            if (appId == null)
            {
                throw new Exception("Please specify unique app name for remote deployment");
            }

            if (csdl == null)
            {
                throw new Exception("Please specify the schema file for your project");
            }

            tenantId ??= config.Tenant;
            subscriptionId ??= config.Subscription;

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new Exception("Please provide value for tenant");
            }

            app.Out.WriteLine($"Schema Path: {csdl.FullName}");
            app.Out.WriteLine($"App service name: {appId}");
            app.Out.WriteLine($"Tenant Id: {tenantId}");
            app.Out.WriteLine($"Subscription Id: {subscriptionId}");

            var args = new ProjectRunArgs()
            {
                SeedData = seedData
            };

            var image = await imageProvider.GetCredentials();
            var remoteManager = new RemoteServiceManager(tenantId, subscriptionId, image);
            var deployment = await remoteManager.Create(appId, csdl.FullName, args);
            var project = deployment.Project;
            configManager.SaveProjectData(project);
            app.Out.WriteLine($"App created successfully. Your app URL is: {project.AppUrl}");
        }

        public async Task UpdateRemoteService(FileInfo csdl, string appId, string tenantId, string subscriptionId)
        {
            var config = configManager.GetRootConfig();

            if (appId == null)
            {
                throw new Exception("Please specify the app to update");
            }

            var project = configManager.LoadProject(appId);

            csdl ??= new FileInfo(project.LocalSchemaPath);
            if (csdl == null)
            {
                throw new Exception("Please specify the schema file for your project");
            }

            

            tenantId ??= project.TenantId ?? config.Tenant;
            subscriptionId ??= project.SubScriptionId ?? config.Subscription;

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new Exception("Please provide value for tenant");
            }

            app.Out.WriteLine($"Schema path: {csdl.FullName}");
            app.Out.WriteLine("Updating app, please wait...");
            var image = await imageProvider.GetCredentials();
            var remoteManager = new RemoteServiceManager(tenantId, subscriptionId, image);
            await remoteManager.UpdateSchema(project, csdl.FullName);
            configManager.SaveProjectData(project);
            app.Out.WriteLine($"Update complete. App url is {project.AppUrl}");

        }

        public async Task DeleteRemoteService(string appName, string tenantId, string subscriptionId)
        {
            if (appName == null)
            {
                throw new Exception("Please specify the app to delete");
            }

            var config = configManager.GetRootConfig();

            var project = configManager.LoadProject(appName);

            tenantId ??= project.TenantId ?? config.Tenant;
            subscriptionId ??= project.SubScriptionId ?? config.Subscription;

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new Exception("Please provide value for tenant");
            }


            var image = await imageProvider.GetCredentials();
            var remoteManager = new RemoteServiceManager(tenantId, subscriptionId, image);

            app.Out.WriteLine($"Deleting {appName} and related resources...");
            await remoteManager.Delete(project);
            configManager.DeleteProjectData(appName);
            app.Out.WriteLine($"The app {appName} and its related resources have been delete.");

        }

        public async Task DeployLocally(FileInfo csdl, string portString, bool seedData)
        {
            if (csdl != null)
            {
                app.Out.WriteLine($"Schema Path: {csdl.FullName}");
            }

            Random r = new Random();
            int port = r.Next(4000, 9000);
            if (!string.IsNullOrEmpty(portString))
            {
                port = int.Parse(portString);
            }

            string Schema = File.ReadAllText(csdl.FullName);

            var args = new ProjectRunArgs()
            {
                SeedData = seedData
            };

            var image = await imageProvider.GetCredentials();

            app.Out.WriteLine("Updating core service...");
            var updater = new ServiceUpdater(image);
            updater.UpdateService();

            using (var serverRunner = new LocalRunner(csdl.FullName, port, args, image))
            {
                Console.CancelKeyPress += (sender, args) =>
                {
                    // dispose server if terminated with Ctrl+C
                    serverRunner.Stop();
                    Environment.Exit(0);
                };

                serverRunner.OnError = e => app.Error.WriteLine(e.Message);
                serverRunner.OnSchemaChange = () => app.Out.WriteLine($"Changes detected to {csdl.FullName}...");
                serverRunner.BeforeRestart = (path, port) => app.Out.WriteLine("Restarting server...");
                serverRunner.AfterRestart = (path, port) => app.Out.WriteLine($"Server running on http://localhost:{port}/odata");
                serverRunner.OnTerminate = () => app.Out.WriteLine("Terminating server...");
                serverRunner.Start();

                app.Out.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }

        }

        public void SetConfig(string tenant, string subscription)
        {
            var config = new RootUserConfig() { Subscription = subscription, Tenant = tenant };
            var updatedConfig = configManager.SaveRootConfig(config);

            app.Out.WriteLine("Config settings");
            app.Out.WriteLine("Tenant: {0}", updatedConfig.Tenant);
            app.Out.WriteLine("Subscription: {0}", updatedConfig.Subscription);
        }

        public void ListRemoteProjects()
        {
            var projects = configManager.GetSavedProjects();

            foreach (var project in projects)
            {
                app.Out.WriteLine("App name: {0}", project.AppId);
                app.Out.WriteLine("URL: {0}", project.AppUrl);
                app.Out.WriteLine("Subscription: {0}", project.SubScriptionId);
                app.Out.WriteLine("Tenant: {0}", project.TenantId);
                app.Out.WriteLine("Schema path: {0}", project.LocalSchemaPath);
                app.Out.WriteLine();
            }
        }
    }
}
