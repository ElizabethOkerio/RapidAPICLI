using Azure.Storage.Files.Shares;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using RapidApi.Remote.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RapidApi.Remote
{
    public class RemoteServiceManager
    {
        private string _tenantId;
        private IAzure azure;
        private string registryServer;
        private string registryUsername;
        private string registryPassword;

        private static readonly string RemoteCsdlFileName = "Project.csdl";
        private static readonly string RemoteCsdlFileDir = "schema";
        

        public RemoteServiceManager(string tenantId, string subscriptionId)
        {
            _tenantId = tenantId;
            var token = GetToken(tenantId).Result;
            var tokenCredentials = new TokenCredentials(token);

            if (subscriptionId == null)
            {
                azure = Microsoft.Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(new AzureCredentials(
                        tokenCredentials,
                        tokenCredentials,
                        tenantId,
                        AzureEnvironment.AzureGlobalCloud))
                    .WithDefaultSubscription();
            }
            else 
            {
                azure = Microsoft.Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(new AzureCredentials(
                        tokenCredentials,
                        tokenCredentials,
                        tenantId,
                        AzureEnvironment.AzureGlobalCloud))
                    .WithSubscription(subscriptionId);
            }


            registryServer = "rapidapiregistry.azurecr.io";
            registryUsername = "rapidapiregistry";
            registryPassword = "3RSdU=zGg=AIvjesICqISXdBbMiwYigk";
        }

        public async Task<string> GetToken(string tenantId)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var token = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com", tenantId);
            return token;
        }

        /// <summary>
        /// Creates a remote service based on the specified schema and deploys it on Azure
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public async Task<RemoteDeployment> Create(string appId, string schemaPath, ProjectRunArgs projectRunArgs)
        {
            var project = new RemoteProject();
            project.AppId = appId;
            project.SubScriptionId = azure.SubscriptionId;
            project.TenantId = _tenantId;
            project.SeedData = projectRunArgs.SeedData;

            var deployment = new RemoteDeployment();
            deployment.Project = project;
            deployment.StartedAt = DateTimeOffset.Now;
            deployment.DeploymentName = $"dep{appId}";

            var rgName = $"rg{appId}";
            var storageAccountName = $"st{appId}";
            var shareName = $"share{appId}";

            project.ResourceGroup = rgName;
            project.StorageAccountName = storageAccountName;
            project.AzureFileShare = shareName;


            var region = Region.USCentral;

            project.Region = region.Name;

            await azure.ResourceGroups.Define(rgName).WithRegion(region).CreateAsync();

            // create storage account
            var storage = await azure.StorageAccounts.Define(storageAccountName).WithRegion(region)
                .WithExistingResourceGroup(rgName)
                .WithAccessFromAllNetworks()
                .CreateAsync();
            var stKey = storage.GetKeys().First().Value;
            project.StorageAccountKey = stKey;

            var storageConnString = $"DefaultEndpointsProtocol=https;AccountName={storage.Name};AccountKey={stKey}";
            var shareClient = new ShareClient(storageConnString, shareName);
            await shareClient.CreateAsync();

            // upload CSDL
            await UploadSchema(shareClient, schemaPath, RemoteCsdlFileDir, RemoteCsdlFileName);

            var template = TemplateHelper.CreateDeploymentTemplate(project, registryServer, registryUsername, registryPassword);
            var templateJson = JsonSerializer.Serialize(template);
            await azure.Deployments.Define(deployment.DeploymentName)
                    .WithExistingResourceGroup(rgName)
                    .WithTemplate(templateJson)
                    .WithParameters("{}")
                    .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                    .CreateAsync();

            deployment.FinishedAt = DateTimeOffset.Now;

            return deployment;
        }

        /// <summary>
        /// Updates the remote project's schema and restarts the service
        /// </summary>
        /// <param name="project"></param>
        /// <param name="schemaPath"></param>
        /// <returns></returns>
        public async Task UpdateSchema(RemoteProject project, string schemaPath)
        {
            var shareClient = new ShareClient(project.StorageConnectionString, project.AzureFileShare);
            await UploadSchema(shareClient, schemaPath, RemoteCsdlFileDir, RemoteCsdlFileName);
            await azure.ContainerGroups.GetByResourceGroup(project.ResourceGroup, project.AppId).RestartAsync();
        }

        /// <summary>
        /// Deletes the remote project and all of its related resources on Azure
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public async Task Delete(RemoteProject project)
        {
            await azure.ResourceGroups.DeleteByNameAsync(project.ResourceGroup);
        }

        private async Task UploadSchema(ShareClient client, string schemaPath, string targetDir, string targetFile)
        {
            var schemaStream = File.OpenRead(schemaPath);

            var directoryClient = client.GetDirectoryClient(targetDir);
            await directoryClient.CreateIfNotExistsAsync();
            var fileClient = directoryClient.GetFileClient(targetFile);
            await fileClient.CreateAsync(schemaStream.Length);
            await fileClient.UploadAsync(schemaStream);
        }
    }
}
