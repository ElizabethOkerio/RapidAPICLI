using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using RapidApi.Local;
using RapidApi.Remote;
using RapidApi.Remote.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RapidApi
{
    class CommandRunner
    {
        ConfigManager configManager;

        public CommandRunner(ConfigManager configManager)
        {
            this.configManager = configManager;
        }

        public async Task DeployRemotely(FileInfo csdl, string appServiceName, string tenantId, string subscriptionId, bool seedData)
        {

            var config = configManager.GetRootConfig();

            if (appServiceName == null)
            {
                Console.Error.WriteLine("Please specify unique app name for remote deployment");
                Environment.Exit(1);
            }

            if (csdl == null)
            {
                Console.Error.WriteLine("Please specify the schema file for your project");
                Environment.Exit(1);
            }

            tenantId = tenantId ?? config.Tenant;
            subscriptionId = subscriptionId ?? config.Subscription;

            if (string.IsNullOrEmpty(tenantId))
            {
                Console.Error.WriteLine("Please provide value for tenant");
            }

            Console.WriteLine($"Schema Path: {csdl.FullName}");
            Console.WriteLine($"App service name: {appServiceName}");
            Console.WriteLine($"Tenant Id: {tenantId}");
            Console.WriteLine($"Subscription Id: {subscriptionId}");

            string AppId = appServiceName;
            //string SubscriptionId = "e8a5d058-e1b5-48f4-b1ff-b3bc830fb899";

            var remoteManager = new RemoteServiceManager(tenantId, subscriptionId);
            try
            {
                Console.WriteLine("Deploying resources, please wait...");
                var args = new ProjectRunArgs()
                {
                    SeedData = seedData
                };
                var deployment = await remoteManager.Create(AppId, csdl.FullName, args);
                var project = deployment.Project;

                configManager.SaveProjectData(project);
                Console.WriteLine($"App created successfully. Your app URL is: {project.AppUrl}");


            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

        }

        public async Task UpdateRemoteService(FileInfo csdl, string appServiceName, string tenantId, string subscriptionId)
        {
            var config = configManager.GetRootConfig();

            if (appServiceName == null)
            {
                Console.Error.WriteLine("Please specify the app to update");
                Environment.Exit(1);
            }

            if (csdl == null)
            {
                Console.Error.WriteLine("Please specify the schema file for your project");
                Environment.Exit(1);
            }

            

            try
            {
                var project = configManager.LoadProject(appServiceName);

                tenantId = tenantId ?? project.TenantId ?? config.Tenant;
                subscriptionId = subscriptionId ?? project.SubScriptionId ?? config.Subscription;

                if (string.IsNullOrEmpty(tenantId))
                {
                    throw new Exception("Please provide value for tenant");
                }


                Console.WriteLine("Updating app, please wait...");
                var remoteManager = new RemoteServiceManager(tenantId, subscriptionId);
                await remoteManager.UpdateSchema(project, csdl.FullName);
                Console.WriteLine($"Update complete. App url is {project.AppUrl}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }



        }

        public async Task DeleteRemoteService(string appName, string tenantId, string subscriptionId)
        {
            if (appName == null)
            {
                Console.Error.WriteLine("Please specify the app to delete");
                Environment.Exit(1);
            }

            var config = configManager.GetRootConfig();

            try
            {
                var project = configManager.LoadProject(appName);

                tenantId = tenantId ?? project.TenantId ?? config.Tenant;
                subscriptionId = subscriptionId ?? project.SubScriptionId ?? config.Subscription;

                if (string.IsNullOrEmpty(tenantId))
                {
                    throw new Exception("Please provide value for tenant");
                }

                var remoteManager = new RemoteServiceManager(tenantId, subscriptionId);

                Console.WriteLine($"Deleting {appName} and related resources...");
                await remoteManager.Delete(project);
                configManager.DeleteProjectData(appName);
                Console.WriteLine($"The app {appName} and its related resources have been delete.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

        }

        public void DeployLocally(FileInfo csdl, string portString, bool seedData)
        {
            if (csdl != null)
            {
                Console.WriteLine($"Schema Path: {csdl.FullName}");
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

            var updater = new ServiceUpdater();
            updater.UpdateService();

            using (var serverRunner = new LocalRunner(csdl.FullName, port, args))
            {
                Console.CancelKeyPress += (sender, args) =>
                {
                    // dispose server if terminated with Ctrl+C
                    serverRunner.Stop();
                    Environment.Exit(0);
                };

                serverRunner.BeforeRestart = (path, port) => Console.WriteLine($"Changes detected to {csdl.FullName}, restarting server...");
                serverRunner.AfterRestart = (path, port) => Console.WriteLine($"Server running on http://localhost:{port}/odata");
                serverRunner.Start();
                Console.WriteLine($"Server running on http://localhost:{port}/odata");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }

        }

        public void SetConfig(string tenant, string subscription)
        {
            var config = new RootConfig() { Subscription = subscription, Tenant = tenant };
            var updatedConfig = configManager.SaveRootConfig(config);

            Console.WriteLine("Config settings");
            Console.WriteLine("Tenant: {0}", updatedConfig.Tenant);
            Console.WriteLine("Subscription: {0}", updatedConfig.Subscription);
        }

        public void ListRemoteProjects()
        {
            var projects = configManager.GetSavedProjects();

            foreach (var project in projects)
            {
                Console.WriteLine("App name: {0}", project.AppId);
                Console.WriteLine("URL: {0}", project.AppUrl);
                Console.WriteLine("Subscription: {0}", project.SubScriptionId);
                Console.WriteLine("Tenant: {0}", project.TenantId);
                Console.WriteLine();
            }
        }
    }
}
