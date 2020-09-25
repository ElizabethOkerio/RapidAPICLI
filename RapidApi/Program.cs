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

namespace RapidApi
{
    class Program
    {
        /// <summary>
        /// Instance of Cloud
        /// </summary>
        private static readonly string AadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];

        /// <summary>
        /// Tenant
        /// </summary>
        private static readonly string Tenant = ConfigurationManager.AppSettings["ida:Tenant"];

        /// <summary>
        /// ClientID of the application
        /// </summary>
        private static readonly string ClientId = ConfigurationManager.AppSettings["ida:ClientId"];

        /// <summary>
        /// Authority
        /// </summary>
        private static readonly string Authority = string.Format(CultureInfo.InvariantCulture, AadInstance, Tenant);

        /// <summary>
        /// Scope of the TodoList action
        /// </summary>
        private static readonly string RapidApiScope = ConfigurationManager.AppSettings["todo:TodoListScope"];

        /// <summary>
        /// Base address of the todolist Web API
        /// </summary>
        private static readonly string RapidApiBaseAddress = ConfigurationManager.AppSettings["todo:TodoListBaseAddress"];
        private static readonly string[] Scopes = { RapidApiScope };
        private static string RapidApiAddress
        {
            get
            {
                string baseAddress = RapidApiBaseAddress;
                return baseAddress.EndsWith("/") ? RapidApiBaseAddress + "api/todolist"
                                                 : RapidApiBaseAddress + "/api/todolist";
            }
        }


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
                Console.WriteLine("--subscriptionId <The Azure Subscription Id>");
                Console.WriteLine("--app <The name of the app service to create.>");
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
                new Option(new string[] { "--appServiceName", "--app" }, "The name of the App Service to create.")
                {
                    Argument = new Argument<string>()
                },
            };

            rootCommand.Handler = CommandHandler.Create<FileInfo, string, string>(BootstrapAsync);
            await rootCommand.InvokeAsync(args);
        }

        static async Task BootstrapAsync(FileInfo csdl, string subscriptionId, string appServiceName)
        {
            //if (subscriptionId != null)
            //{
            //    Console.WriteLine($"Subscription Id: {subscriptionId}");
            //}

            if (csdl != null)
            {
                Console.WriteLine($"Schema Path: {csdl.FullName}");
            }

            if (appServiceName != null)
            {
                Console.WriteLine($"App service name: {appServiceName}");
            }

            string AppId = appServiceName;
            string Schema = File.ReadAllText(csdl.FullName);
            string SubscriptionId = "e8a5d058-e1b5-48f4-b1ff-b3bc830fb899";

            Console.WriteLine("Creating Project");
            var rapidApi = new Container(new Uri("https://testrapidapiservice.azurewebsites.net/odata/"));
            rapidApi.MergeOption = Microsoft.OData.Client.MergeOption.NoTracking;
            var res = await rapidApi.CreateProject(AppId, SubscriptionId, Schema).GetValueAsync();

            var resExpand = await rapidApi.Deployments.ByKey(res.Id).Expand("Project").GetValueAsync();
            var status = resExpand.Status;
            Console.WriteLine(resExpand.Status.ToString());
            while (resExpand.Status != DeploymentStatus.Complete && resExpand.Status != DeploymentStatus.Failed)
            {
                await Task.Delay(5000);
                resExpand = await rapidApi.Deployments.ByKey(res.Id).Expand("Project").GetValueAsync();
                if (resExpand.Status != status)
                {
                    Console.WriteLine(resExpand.Status.ToString());
                    status = resExpand.Status;
                }            
            }

            if (resExpand.Status == DeploymentStatus.Failed)
            {
                Console.WriteLine(resExpand.FailureReason);
            }
            else 
            {
                var AppUrl = resExpand.Project.AppUrl;
                Console.WriteLine("Your APP URL IS: " + AppUrl);
            }
        }

    }
}
