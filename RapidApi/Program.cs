using System;
using System.IO;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using RapidApi.Remote;
using Microsoft.Extensions.CommandLineUtils;
using System.Reflection;

namespace RapidApi
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.Name = "RapidApi.exe";
            app.Description = "Rapid API CLI application.";

            // Set the arguments to display the description and help text
            app.HelpOption("-?|-h|--help");

            // This is a helper/shortcut method to display version info - it is creating a regular Option, with some defaults.
            // The default help text is "Show version Information"
            app.VersionOption("-v|--version", () => {
                return string.Format("Version {0}", Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            });

            // This is the deploy project command.
            app.Command("rapidapi-deploy", (command) =>
            {
                //description and help text of the command.
                command.Description = "This command will deploy a project either localy or to the server when called.";
                command.HelpOption("-?|-h|--help");

                var schemaOption = command.Option("-s|--schema <SCHEMA>", "The path to the xml schema file.", CommandOptionType.SingleValue);
                var appNameOption = command.Option("-a|--app <APPSERVICENAME>", "The name of the app service to create.", CommandOptionType.SingleValue);
                var remoteOption = command.Option("-r|--remote <REMOTE>", "Whether to deploy the mock service remotely to azure.", CommandOptionType.NoValue);

                command.OnExecute(async () =>
                {

                   await RunCommand(new FileInfo(schemaOption.Value()),appNameOption.Value(),bool.Parse(remoteOption.Value()));
        
                    return 0; //return 0 on a successful execution
                });

            });

            // This is the update project command.
            app.Command("rapidapi-update", (command) =>
            {
                //description and help text of the command.
                command.Description = "This is the update command.";
                command.ExtendedHelpText = "This is the extended help text for simple-command.";
                command.HelpOption("-?|-h|--help");

                command.OnExecute(async () =>
                {
                    return 0; //return 0 on a successful execution
                });

            });

            // This is the delete project command.
            app.Command("rapidapi-delete", (command) =>
            {
                //description and help text of the command.
                command.Description = "This is the rapidapi delete command";
                command.ExtendedHelpText = "This is the extended help text for simple-command.";
                command.HelpOption("-?|-h|--help");

                command.OnExecute(async () =>
                {
                    return 0; //return 0 on a successful execution
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

        static async Task RunCommand(FileInfo csdl, string appServiceName, bool remote)
        {
            if (remote)
            {
                await DeployRemotely(csdl, appServiceName);
            }
            else
            {
                DeployLocally(csdl);
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

            var tenantId = "";
            var clientId = "";
            var clientSecret = "";

            var remoteManager = new RemoteServiceManager(tenantId, clientId, clientSecret);
            try
            {
                Console.WriteLine("Deploying resources, please wait...");
                var deployment = await remoteManager.Create(AppId, csdl.FullName);
                var project = deployment.Project;

                Console.WriteLine($"App created successfully. Your app URL is: {project.AppUrl}");

                
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
           
        }

        static void DeployLocally(FileInfo csdl)
        {
            if (csdl != null)
            {
                Console.WriteLine($"Schema Path: {csdl.FullName}");
            }

            string Schema = File.ReadAllText(csdl.FullName);

            Random r = new Random();
            int genRand = r.Next(4000, 9000);



            int port = genRand;
            
            using (
                var container = new Builder()
                .UseContainer()
                .UseImage("rapidapiregistry.azurecr.io/rapidapimockserv:latest")
                .WithCredential("rapidapiregistry.azurecr.io", "rapidapiregistry", "lfd34HcYycIg+rttO0D5AeZjZL2=pqZt")
                .ExposePort(port, 80)
                .CopyOnStart(csdl.FullName, "/app/Project.csdl")
                .Build()
                .Start()
                )

            {
                Console.WriteLine("APP URL IS: http://localhost:" + port + "/odata");
                Console.ReadKey();
            };
            
              
        }

    }
}
