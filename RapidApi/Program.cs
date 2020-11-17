using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Default;
using LibGit2Sharp;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using RapidAPI.Models;
using Ductus.FluentDocker;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Services.Extensions;
using Ductus.FluentDocker.Services;
using System.Diagnostics;
using RapidApi.Remote;

namespace RapidApi
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("-------------");
                Console.WriteLine("Odata CLI Tool to bootstrap an Odata service");
                Console.WriteLine("-------------");
                Console.WriteLine();
                Console.WriteLine("OPTIONS");
                Console.WriteLine("--schema <Path to xml schema file>");
                //Console.WriteLine("--subscriptionId <The Azure Subscription Id>");
                Console.WriteLine("--app <The name of the app service to create.>");
                Console.WriteLine("--remote");
                return;
            }
            var rootCommand = new RootCommand("Odata CLI Tool to bootstrap an Odata service")
            {
                new Option(new string[] {"--csdl", "--metadata", "--schema"}, "The path to the xml schema file.")
                {
                    Argument = new Argument<FileInfo>()
                },
                //new Option(new string[] { "--subscriptionId", "--id" }, "The Azure Subscription Id.")
                //{
                //    Argument = new Argument<string>()
                //},
                new Option(new string[] { "--appServiceName", "--app" }, "The name of the App Service to create if deploying to azure.")
                {
                    Argument = new Argument<string>()
                },
                new Option(new string[] { "--remote" }, "Whether to deploy the mock service remotely to azure.")
                {
                    Argument = new Argument<bool>()
                }
                //new Option(new string[] { "--deploymenttype", "--dtype" }, "Do you want to deploy locally or to azure.")
                //{
                //    Argument = new Argument<string>()
                //},
            };

            //  rootCommand.Handler = CommandHandler.Create<FileInfo, string, string>(BootstrapAsync);
            // await rootCommand.InvokeAsync(args);


            rootCommand.Handler = CommandHandler.Create<FileInfo, string, bool>(RunCommand);
            await rootCommand.InvokeAsync(args);

            //BootstrapApiFromDockerImage();

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
