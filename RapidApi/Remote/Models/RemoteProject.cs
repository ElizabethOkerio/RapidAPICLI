using System;
using System.Collections.Generic;
using System.Text;

namespace RapidApi.Remote.Models
{
    public class RemoteProject
    {
        public string AppId { get; set; }
        
        public string AppServicePlanId { get; set; }
        public string SubScriptionId { get; set; }
        public string Region { get; set; }
        public string ResourceGroup { get; set; }

        public string StorageAccountName { get; set; }
        public string StorageAccountKey { get; set; }
        public string AzureFileShare { get; set; }

        public bool SeedData { get; set; }

        public string StorageConnectionString
        {
            get => $"DefaultEndpointsProtocol=https;AccountName={StorageAccountName};AccountKey={StorageAccountKey}";
        }

        public string AppUrl { get => $"http://{AppId}.{Region}.azurecontainer.io/odata"; }
        public string ContainerUrl { get => AppUrl; }

        
    }
}
