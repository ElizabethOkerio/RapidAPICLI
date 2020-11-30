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

        static void Main(string[] args)
        {

            var configManager = new ConfigManager();
            var runner = new CommandRunner(configManager);

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

            app.Command("config", (command) =>
            {
                command.Description = "Sets global configuration settings";
                command.HelpOption("-?|-h|--help");

                var tenantOption = command.Option(
                    "-t|--tenant <TENANT>", "The default azure tenant id to use for remote services",
                    CommandOptionType.SingleValue);

                var subscriptionOption = command.Option(
                    "-i|--subscription <SUBSCRIPTION>", "The default azure subscription to use for remote services",
                    CommandOptionType.SingleValue);

                command.OnExecute(() =>
                {
                    runner.SetConfig(tenant: tenantOption.Value(), subscription: subscriptionOption.Value());
                    return Task.FromResult(0);
                });
            });

            app.Command("run", (command) =>
            {
                //description and help text of the command.
                command.Description = "This command will deploy a project either localy or to the server when called.";
                command.HelpOption("-?|-h|--help");

                var schemaOption = command.Option("-s|--schema <SCHEMA>", "The path to the xml schema file.", CommandOptionType.SingleValue);
                var portOption = command.Option("-p|--port <PORT>", "The port to bind the local server to.", CommandOptionType.SingleValue);
                var seedOption = command.Option("-d|--seed", "Whether to seed the database with random data", CommandOptionType.NoValue);

                command.OnExecute(() =>
                {

                    runner.DeployLocally(new FileInfo(schemaOption.Value()), portOption.Value(), seedOption.HasValue());

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
                var tenantIdOption = command.Option("-t|--tenant <TENANTID>", "The tenant ID to deploy to", CommandOptionType.SingleValue);
                var subscriptionIdOption = command.Option("-i|--subscription <SUBSCRIPTIONID>", "The subscription Id to use.", CommandOptionType.SingleValue);
                var seedOption = command.Option("-d|--seed", "Whether to seed the database with random data", CommandOptionType.NoValue);

                command.OnExecute(async () =>
                {
                    await runner.DeployRemotely(new FileInfo(schemaOption.Value()), appNameOption.Value(), tenantIdOption.Value(), subscriptionIdOption.Value(), seedOption.HasValue());
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
                var tenantIdOption = command.Option("-t|--tenant <TENANTID>", "The tenant ID to deploy to", CommandOptionType.SingleValue);
                var subscriptionIdOption = command.Option("-i|--subscription <SUBSCRIPTIONID>", "The subscription Id to use.", CommandOptionType.SingleValue);
                
                command.OnExecute(async () =>
                {
                    await runner.UpdateRemoteService(new FileInfo(schemaOption.Value()), appNameOption.Value(), tenantIdOption.Value(), subscriptionIdOption.Value());
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
                var tenantOption = command.Option("-t|--tenant <TENANTID>", "The tenant ID to deploy to", CommandOptionType.SingleValue);
                var subscriptionOption = command.Option("-i|--subscription <SUBSCRIPTIONID>", "The subscription Id to use.", CommandOptionType.SingleValue);


                command.OnExecute(async () =>
                {
                    await runner.DeleteRemoteService(appNameOption.Value(), tenantOption.Value(), subscriptionOption.Value());
                    return 0;
                });

            });

            app.Command("list", (command) =>
            {
                command.Description = "Lists remotely deployed services";
                command.HelpOption("-?|-h|--help");

                command.OnExecute(() =>
                {
                    runner.ListRemoteProjects();
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

    }
}
