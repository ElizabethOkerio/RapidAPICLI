using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RapidApi.Remote.Models
{
    // see: https://docs.microsoft.com/en-us/azure/container-instances/container-instances-volume-azure-files
    // see: https://docs.microsoft.com/en-us/azure/templates/Microsoft.ContainerInstance/2019-12-01/containerGroups
    public class ContainerDeploymentTemplate
    {
        [JsonPropertyName("$schema")]
        public string Schema { get; set; }

        [JsonPropertyName("contentVersion")]
        public string ContentVersion { get; set; }

        [JsonPropertyName("variables")]
        public VariablesModel Variables { get; set; }
        [JsonPropertyName("resources")]
        public List<ResourceModel> Resources { get; set; }



        public class ResourceModel
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("type")]
            public string Type { get; set; }
            [JsonPropertyName("apiVersion")]
            public string ApiVersion { get; set; }
            [JsonPropertyName("location")]
            public string Location { get; set; }
            [JsonPropertyName("properties")]
            public PropertiesModel Properties { get; set; }
        }

        public class VariablesModel
        {
            [JsonPropertyName("containerName")]
            public string ContainerName { get; set; }

            [JsonPropertyName("containerImage")]
            public string ContainerImage { get; set; }
        }

        public class PropertiesModel
        {
            [JsonPropertyName("containers")]
            public IList<ContainerModel> Containers { get; set; }
            [JsonPropertyName("osType")]
            public string OsType { get; set; }
            [JsonPropertyName("ipAddress")]
            public IpAddressModel IpAddress { get; set; }
            [JsonPropertyName("volumes")]
            public IList<VolumeModel> Volumes { get; set; }
            [JsonPropertyName("imageRegistryCredentials")]
            public IList<ImageRegistryCredentialsModel> ImageRegistryCredentials { get; set; }
        }

        public class ContainerModel
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("properties")]
            public ContainerPropertiesModel Properties { get; set; }
        }

        public class ContainerPropertiesModel
        {
            [JsonPropertyName("image")]
            public string Image { get; set; }
            [JsonPropertyName("resources")]
            public ContainerResourcesModel Resources { get; set; }
            [JsonPropertyName("ports")]
            public IList<PortModel> Ports { get; set; }
            [JsonPropertyName("volumeMounts")]
            public IList<ContainerVolumeMountModel> VolumeMounts { get; set; }
            [JsonPropertyName("environmentVariables")]
            public IList<ContainerEnvironmentVariableModel> EnvironmentVariables { get; set; }
        }

        public class ContainerResourcesModel
        {
            [JsonPropertyName("requests")]
            public ContainerResourceRequestsModel Requests { get; set; }
        }

        public class ContainerResourceRequestsModel
        {
            [JsonPropertyName("cpu")]
            public int Cpu { get; set; }
            [JsonPropertyName("memoryInGb")]
            public double MemoryInGb { get; set; }
        }

        public class ContainerEnvironmentVariableModel
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("value")]
            public string Value { get; set; }
        }

        public class ContainerPortModel
        {
            [JsonPropertyName("port")]
            public int Port { get; set; }
        }

        public class ContainerVolumeMountModel
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("mountPath")]
            public string MountPath { get; set; }
        }


        public class IpAddressModel
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }
            [JsonPropertyName("dnsNameLabel")]
            public string DnsNameLabel { get; set; }
            [JsonPropertyName("ports")]
            public IList<PortModel> Ports { get; set; }
        }

        public class PortModel
        {
            [JsonPropertyName("protocol")]
            public string Protocol { get; set; }
            [JsonPropertyName("port")]
            public int Port { get; set; }
        }

        public class VolumeModel
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("azureFile")]
            public AzureFileModel AzureFile { get; set; }
        }

        public class AzureFileModel
        {
            [JsonPropertyName("shareName")]
            public string ShareName { get; set; }
            [JsonPropertyName("storageAccountName")]
            public string StorageAccountName { get; set; }
            [JsonPropertyName("storageAccountKey")]
            public string StorageAccountKey { get; set; }
        }

        public class ImageRegistryCredentialsModel
        {
            [JsonPropertyName("server")]
            public string Server { get; set; }
            [JsonPropertyName("username")]
            public string Username { get; set; }
            [JsonPropertyName("password")]
            public string Password { get; set; }
        }

    }


}
