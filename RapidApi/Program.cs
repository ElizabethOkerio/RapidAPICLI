using System;
using System.IO;
using System.Threading.Tasks;
using RapidApi.Remote;
using RapidApi.Local;
using Microsoft.Extensions.CommandLineUtils;
using System.Reflection;
using RapidApi.Remote.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Text.Json;

namespace RapidApi
{
    class Program
    {
        static string TenantId = "TENANT ID";
        static string ClientId = "CLIENT ID";
        static string ClientSecret = "CLIENT SECRET";

        static void Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.Name = "RapidApi";
            app.Description = "Rapid API CLI application.";

            // Set the arguments to display the description and help text
            app.HelpOption("-?|-h|--help");

            // This is a helper/shortcut method to display version info - it is creating a regular Option, with some defaults.
            // The default help text is "Show version Information"
            app.VersionOption("-v|--version", () => {
                return string.Format("Version {0}", Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            });

            app.Command("run", (command) =>
            {
                //description and help text of the command.
                command.Description = "This command will deploy a project either localy or to the server when called.";
                command.HelpOption("-?|-h|--help");

                var schemaOption = command.Option("-s|--schema <SCHEMA>", "The path to the xml schema file.", CommandOptionType.SingleValue);
                var portOption = command.Option("-p|--port <PORT>", "The port to bind the local server to.", CommandOptionType.SingleValue);

                command.OnExecute(() =>
                {

                    DeployLocally(new FileInfo(schemaOption.Value()), portOption.Value());

                    return Task.FromResult(0); //return 0 on a successful execution
                });

            });

            // This is the deploy project command.
            app.Command("deploy", (command) =>
            {
                //description and help text of the command.
                command.Description = "This command will deploy a project either localy or to the server when called.";
                command.HelpOption("-?|-h|--help");

                var schemaOption = command.Option("-s|--schema <SCHEMA>", "The path to the xml schema file.", CommandOptionType.SingleValue);
                var appNameOption = command.Option("-a|--app <APPSERVICENAME>", "The name of the app service to create.", CommandOptionType.SingleValue);

                command.OnExecute(async () =>
                {

                   await DeployRemotely(new FileInfo(schemaOption.Value()), appNameOption.Value());
        
                    return 0; //return 0 on a successful execution
                });

            });

            // This is the update project command.
            app.Command("update", (command) =>
            {
                //description and help text of the command.
                command.Description = "This is the update command.";
                command.ExtendedHelpText = "This is the extended help text for simple-command.";
                command.HelpOption("-?|-h|--help");

                var schemaOption = command.Option("-s|--schema <SCHEMA>", "The path to the xml schema file.", CommandOptionType.SingleValue);
                var appNameOption = command.Option("-a|--app <APPSERVICENAME>", "The name of the app service to create.", CommandOptionType.SingleValue);

                command.OnExecute(async () =>
                {
                    await UpdateRemoteService(new FileInfo(schemaOption.Value()), appNameOption.Value());
                    return 0; //return 0 on a successful execution
                });

            });

            // This is the delete project command.
            app.Command("delete", (command) =>
            {
                //description and help text of the command.
                command.Description = "Deletes a remote service";
                command.ExtendedHelpText = "This is the extended help text for simple-command.";
                command.HelpOption("-?|-h|--help");

                var appNameOption = command.Option("-a|--app <appName>", "The name of the app to delete.", CommandOptionType.SingleValue);

                command.OnExecute(async () =>
                {
                    
                    await DeleteRemoteService(appNameOption.Value());
                    return 0;
                });

            });

            try
            {
                // This begins the actual execution of the application
                app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                // You'll always want to catch this exception, otherwise it will generate a messy and confusing error for the end user.
                // the message will usually be something like:
                // "Unrecognized command or argument '<invalid-command>'"
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to execute application: {0}", ex.Message);
            }
        }

        static async Task DeployRemotely(FileInfo csdl, string appServiceName)
        {
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

            Console.WriteLine($"Schema Path: {csdl.FullName}");
            Console.WriteLine($"App service name: {appServiceName}");

            string AppId = appServiceName;
            //string SubscriptionId = "e8a5d058-e1b5-48f4-b1ff-b3bc830fb899";

            var remoteManager = new RemoteServiceManager(TenantId, ClientId, ClientSecret);
            try
            {
                Console.WriteLine("Deploying resources, please wait...");
                var deployment = await remoteManager.Create(AppId, csdl.FullName);
                var project = deployment.Project;

                SaveProjectData(project);
                Console.WriteLine($"App created successfully. Your app URL is: {project.AppUrl}");

                
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
           
        }

        static async Task UpdateRemoteService(FileInfo csdl, string appServiceName)
        {
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
                var project = LoadProject(appServiceName);
                Console.WriteLine("Updating app, please wait...");
                var remoteManager = new RemoteServiceManager(TenantId, ClientId, ClientSecret);
                await remoteManager.UpdateSchema(project, csdl.FullName);
                Console.WriteLine($"Update complete. App url is {project.AppUrl}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }


            
        }

        static async Task DeleteRemoteService(string appName)
        {
            if (appName == null)
            {
                Console.Error.WriteLine("Please specify the app to delete");
                Environment.Exit(1);
            }

            var project = new RemoteProject();
            project.AppId = appName;
            project.Region = Region.USCentral.Name;
            project.ResourceGroup = $"rg{appName}";

            
            var remoteManager = new RemoteServiceManager(TenantId, ClientId, ClientSecret);

            try
            {
                Console.WriteLine($"Deleting {appName} and related resources...");
                await remoteManager.Delete(project);
                Console.WriteLine($"The app {appName} and its related resources have been delete.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

        }

        static void DeployLocally(FileInfo csdl, string portString = null)
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

            using (var serverRunner = new LocalRunner(csdl.FullName, port))
            {
                serverRunner.BeforeRestart = (path, port) => Console.WriteLine($"Changes detected to {csdl.FullName}, restarting server...");
                serverRunner.AfterRestart = (path, port) => Console.WriteLine($"Server running on http://localhost:{port}/odata");
                serverRunner.Start();
                Console.WriteLine($"Server running on http://localhost:{port}/odata");
                Console.ReadKey();
            }

        }

        static string GetAppDataFolder()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            path = Path.Combine(path, "rapidapi");
            Directory.CreateDirectory(path);
            return path;
        }

        static void SaveProjectData(RemoteProject project)
        {
            var json = JsonSerializer.Serialize(project);
            var dir = GetAppDataFolder();
            var filename = $"{project.AppId}.json";
            var fullPath = Path.Combine(dir, filename);
            File.WriteAllText(fullPath, json);
        }

        static RemoteProject LoadProject(string appName)
        {
            var filename = $"{appName}.json";
            var fullPath = Path.Combine(GetAppDataFolder(), filename);
            var project = JsonSerializer.Deserialize<RemoteProject>(File.ReadAllText(fullPath));
            return project;
        }

    }
}
