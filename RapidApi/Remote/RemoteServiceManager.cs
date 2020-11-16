using Azure.Storage.Files.Shares;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
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
        private string _clientId;
        private string _clientSecret;
        private AzureCredentials _azureCredentials;
        private IAzure azure;
        private string registryServer;
        private string registryUsername;
        private string registryPassword;
        

        public RemoteServiceManager(string tenantId, string clientId, string clientSecret)
        {
            _tenantId = tenantId;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _azureCredentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                _clientId,
                _clientSecret,
                _tenantId,
                AzureEnvironment.AzureGlobalCloud);


            azure = Microsoft.Azure.Management.Fluent.Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(_azureCredentials)
                    .WithDefaultSubscription();

            registryServer = "rapidapiregistry.azurecr.io";
            registryUsername = "rapidapiregistry";
            registryPassword = "lfd34HcYycIg+rttO0D5AeZjZL2=pqZt";
        }

        /// <summary>
        /// Creates a remote service based on the specified schema and deploys it on Azure
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public async Task<RemoteProject> Create(string appId, string schema)
        {
            var project = new RemoteProject();

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

            var storageConnString = $"DefaultEndpointsProtocol=https;AccountName={storage.Name};AccountKey={stKey}";
            var shareClient = new ShareClient(storageConnString, shareName);
            await shareClient.CreateAsync();

            // upload CSDL
            await UploadSchema(shareClient, schema, "schema", "Project.csdl");

            var template = TemplateHelper.CreateDeploymentTemplate(project, registryServer, registryUsername, registryPassword);
            var templateJson = JsonSerializer.Serialize(template);
            await azure.Deployments.Define($"dep{appId}")
                    .WithExistingResourceGroup(rgName)
                    .WithTemplate(templateJson)
                    .WithParameters("{}")
                    .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                    .CreateAsync();

            return project;
        }

        /// <summary>
        /// Updates the remote project's schema and restarts the service
        /// </summary>
        /// <param name="project"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public async Task Update(RemoteProject project, string schema)
        {
            var schemaBytes = Encoding.UTF8.GetBytes(schema);
            var schemaStream = new MemoryStream(schemaBytes);
            var shareClient = new ShareClient(project.StorageConnectionString, project.AzureFileShare);
            var dirClient = shareClient.GetDirectoryClient("schema");
            var fileClient = dirClient.GetFileClient("Project.csdl");
            await fileClient.UploadAsync(schemaStream);

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

        private async Task UploadSchema(ShareClient client, string schema, string targetDir, string targetFile)
        {
            var schemaBytes = Encoding.UTF8.GetBytes(schema);
            var schemaStream = new MemoryStream(schemaBytes);
            var directoryResponse = await client.CreateDirectoryAsync(targetDir);
            var fileResponse = await directoryResponse.Value.CreateFileAsync(targetFile, schemaBytes.Length);
            await fileResponse.Value.UploadAsync(schemaStream);
        }
    }
}
