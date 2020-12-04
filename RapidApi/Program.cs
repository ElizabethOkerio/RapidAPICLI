using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using System.Reflection;
using RapidApi.Config;
using RapidApi.Cli.Common;
using RapidApi.Cli.Config;

namespace RapidApi.Cli
{
    class Program
    {
        const int EXIT_SUCCESS = 0;
        const int EXIT_ERROR = 1;
        const string SETTINGS_FILE = "appsettings.json";

        static int Main(string[] args)
        {
            AppSettings settings;
            try
            {
                settings = new AppSettings(SETTINGS_FILE);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error reading settings: {ex.Message}");
                return EXIT_ERROR;
            }

            var version = Assembly.
                GetEntryAssembly().
                GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var app = new CommandLineApplication();
            var configManager = new UserConfigManager(version);

            var imageCredsProvider = new KeyVaultImageCredentialsProvider(
                settings.KeyVault.TenantId,
                settings.KeyVault.VaultUrl)
            {
                OnAuthenticating = () => app.Out.WriteLine("Signing into your Microsoft Corp account...")
            };

            var runner = new CommandRunner(app, configManager, imageCredsProvider);

            app.Name = "RapidApi";
            app.Description = "Rapid API CLI application.";

            // Set the arguments to display the description and help text
            app.HelpOption("-?|-h|--help");

            // This is a helper/shortcut method to display version info - it is creating a regular Option, with some defaults.
            // The default help text is "Show version Information"
            app.VersionOption("-v|--version", () => {
                return string.Format(
                    "Version {0}",
                    Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            });

            app.Command("config", (command) =>
            {
                command.Description = "Sets global configuration settings";
                command.HelpOption("-?|-h|--help");

                var tenantOption = command.Option(
                    "-t|--tenant <TENANT>",
                    "The default Azure tenant id to use for remote services",
                    CommandOptionType.SingleValue);

                var subscriptionOption = command.Option(
                    "-i|--subscription <SUBSCRIPTION>",
                    "The default Azure subscription to use for remote services",
                    CommandOptionType.SingleValue);

                command.OnExecute(() => HandleCommand(app,
                    () => runner.SetConfig(tenant: tenantOption.Value(), subscription: subscriptionOption.Value())));
            });

            app.Command("run", (command) =>
            {
                command.Description = "Launches a mock service locally based on specified schema.";
                command.HelpOption("-?|-h|--help");

                var schemaOption = command.Option(
                    "-s|--schema <SCHEMA>",
                    "The path to the xml schema file.",
                    CommandOptionType.SingleValue);
                var portOption = command.Option(
                    "-p|--port <PORT>",
                    "The port to bind the local server to.",
                    CommandOptionType.SingleValue);
                var seedOption = command.Option(
                    "-d|--seed",
                    "Whether to seed the database with random data",
                    CommandOptionType.NoValue);

                command.OnExecute(() => HandleCommand(app,
                    () => runner.DeployLocally(
                        new FileInfo(schemaOption.Value()),
                        portOption.Value(),
                        seedOption.HasValue())));
            });

            app.Command("deploy", (command) =>
            {
                command.Description = "Creates and deploys a new service remotely to Azure based on provided schema.";
                command.HelpOption("-?|-h|--help");

                var schemaOption = command.Option(
                    "-s|--schema <SCHEMA>",
                    "The path to the xml schema file.",
                    CommandOptionType.SingleValue);
                var appNameOption = command.Option(
                    "-a|--app <APPNAME>",
                    "The unique name of the app to create.",
                    CommandOptionType.SingleValue);
                var tenantIdOption = command.Option(
                    "-t|--tenant <TENANT>",
                    "The Azure tenant ID to deploy to",
                    CommandOptionType.SingleValue);
                var subscriptionIdOption = command.Option(
                    "-i|--subscription <SUBSCRIPTION>",
                    "The Azure subscription Id to use.",
                    CommandOptionType.SingleValue);
                var seedOption = command.Option(
                    "-d|--seed",
                    "Whether to seed the database with random data",
                    CommandOptionType.NoValue);

                command.OnExecute(() => HandleCommand(app,
                    () => runner.DeployRemotely(
                            new FileInfo(schemaOption.Value()),
                            appNameOption.Value(),
                            tenantIdOption.Value(),
                            subscriptionIdOption.Value(),
                            seedOption.HasValue())));
            });

            app.Command("update", (command) =>
            {
                command.Description = "Updates a remote service with a new schema.";
                command.ExtendedHelpText = "This is the extended help text for simple-command.";
                command.HelpOption("-?|-h|--help");

                var schemaOption = command.Option(
                    "-s|--schema <SCHEMA>",
                    "The path to the xml schema file.",
                    CommandOptionType.SingleValue);
                var appNameOption = command.Option(
                    "-a|--app <APPNAME>",
                    "The name of the app to update.",
                    CommandOptionType.SingleValue);
                var tenantIdOption = command.Option(
                    "-t|--tenant <TENANT>",
                    "The Azure tenant ID to deploy to",
                    CommandOptionType.SingleValue);
                var subscriptionIdOption = command.Option(
                    "-i|--subscription <SUBSCRIPTION>",
                    "The Azure subscription Id to use.",
                    CommandOptionType.SingleValue);

                command.OnExecute(() => HandleCommand(app,
                    () => runner.UpdateRemoteService(
                        schemaOption.Value(),
                        appNameOption.Value(),
                        tenantIdOption.Value(),
                        subscriptionIdOption.Value())));
            });

            app.Command("delete", (command) =>
            {
                command.Description = "Deletes a remote service";
                command.ExtendedHelpText = "This is the extended help text for simple-command.";
                command.HelpOption("-?|-h|--help");

                var appNameOption = command.Option(
                    "-a|--app <APP_NAME>",
                    "The name of the app to delete.",
                    CommandOptionType.SingleValue);
                var tenantOption = command.Option(
                    "-t|--tenant <TENANTID>",
                    "The Azure tenant ID the app is deployed to",
                    CommandOptionType.SingleValue);
                var subscriptionOption = command.Option(
                    "-i|--subscription <SUBSCRIPTIONID>",
                    "The Azure subscription Id to use.",
                    CommandOptionType.SingleValue);


                command.OnExecute(() => HandleCommand(app,
                    () => runner.DeleteRemoteService(appNameOption.Value(), tenantOption.Value(), subscriptionOption.Value())));
            });

            app.Command("list", (command) =>
            {
                command.Description = "Lists remotely deployed services";
                command.HelpOption("-?|-h|--help");

                command.OnExecute(() => HandleCommand(app,
                    () => runner.ListRemoteProjects()));
            });

            try
            {
                // This begins the actual execution of the application
                return app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                // You'll always want to catch this exception, otherwise it will generate a messy and confusing error for the end user.
                // the message will usually be something like:
                // "Unrecognized command or argument '<invalid-command>'"
                app.Error.WriteLine(ex.Message);
                return EXIT_ERROR;
            }
            catch (Exception ex)
            {
                app.Error.WriteLine("Unable to execute application: {0}", ex.Message);
                return EXIT_ERROR;
            }
        }

        static async Task<int> HandleCommand(CommandLineApplication app, Func<Task> handler)
        {
            try
            {
                await handler();
                return EXIT_SUCCESS;
            }
            catch (Exception ex)
            {
                app.Error.WriteLine(ex.Message);
                return EXIT_ERROR;
            }
        }

        static int HandleCommand(CommandLineApplication app, Action handler)
        {
            try
            {
                handler();
                return EXIT_SUCCESS;
            }
            catch (Exception ex)
            {
                app.Error.WriteLine(ex.Message);
                return EXIT_ERROR;
            }
        }
    }
}
