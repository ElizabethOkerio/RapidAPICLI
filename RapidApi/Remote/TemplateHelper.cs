using RapidApi.Remote.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RapidApi.Remote
{
    static class TemplateHelper
    {

        public static ContainerDeploymentTemplate CreateDeploymentTemplate(RemoteProject project, string registryServer, string registryUsername, string registryPassword)
        {
            // see: https://docs.microsoft.com/en-us/azure/container-instances/container-instances-volume-azure-files
            var template = new ContainerDeploymentTemplate()
            {
                Schema = "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
                ContentVersion = "1.0.0.0",
                Variables = new ContainerDeploymentTemplate.VariablesModel()
                {
                    ContainerImage = "rapidapiregistry.azurecr.io/rapidapimockserv:latest",
                    ContainerName = project.AppId
                },
                Resources = new List<ContainerDeploymentTemplate.ResourceModel>()
                {
                    new ContainerDeploymentTemplate.ResourceModel()
                    {
                        Name = project.AppId,
                        Type =  "Microsoft.ContainerInstance/containerGroups",
                        ApiVersion = "2019-12-01",
                        Location = "[resourceGroup().location]",
                        Properties = new ContainerDeploymentTemplate.PropertiesModel()
                        {
                            Containers = new List<ContainerDeploymentTemplate.ContainerModel>()
                            {
                                new ContainerDeploymentTemplate.ContainerModel()
                                {
                                    Name = "[variables('containerName')]",
                                    Properties = new ContainerDeploymentTemplate.ContainerPropertiesModel()
                                    {
                                        Image = "[variables('containerImage')]",
                                        Resources = new ContainerDeploymentTemplate.ContainerResourcesModel()
                                        {
                                            Requests = new ContainerDeploymentTemplate.ContainerResourceRequestsModel()
                                            {
                                                Cpu = 1,
                                                MemoryInGb = 1.5
                                            }

                                        },
                                        Ports = new List<ContainerDeploymentTemplate.PortModel>()
                                        {
                                            new ContainerDeploymentTemplate.PortModel()
                                            {
                                                Port = 80,
                                                Protocol = "tcp"
                                            }
                                        },
                                        VolumeMounts = new List<ContainerDeploymentTemplate.ContainerVolumeMountModel>()
                                        {
                                            new ContainerDeploymentTemplate.ContainerVolumeMountModel()
                                            {
                                                Name = $"vol{project.AppId}",
                                                MountPath = "/mnt/data"
                                            }
                                        },
                                        EnvironmentVariables = new List<ContainerDeploymentTemplate.ContainerEnvironmentVariableModel>()
                                        {
                                            new ContainerDeploymentTemplate.ContainerEnvironmentVariableModel()
                                            {
                                                Name = "IS_REMOTE_ENV",
                                                Value = "true"
                                            },
                                            new ContainerDeploymentTemplate.ContainerEnvironmentVariableModel()
                                            {
                                                Name = "SEED_DATA",
                                                Value = project.SeedData ? "true" : "false"
                                            },
                                            new ContainerDeploymentTemplate.ContainerEnvironmentVariableModel()
                                            {
                                                Name = "AZURE_FILE_SHARE_NAME",
                                                Value = project.AzureFileShare
                                            },
                                            new ContainerDeploymentTemplate.ContainerEnvironmentVariableModel()
                                            {
                                                Name = "AZURE_STORAGE_CONNECTION_STRING",
                                                Value = project.StorageConnectionString
                                            },
                                            new ContainerDeploymentTemplate.ContainerEnvironmentVariableModel()
                                            {
                                                Name = "AZURE_FILE_CSDL_DIR",
                                                Value = "schema"
                                            },
                                            new ContainerDeploymentTemplate.ContainerEnvironmentVariableModel()
                                            {
                                                Name = "AZURE_FILE_CSDL_FILE",
                                                Value = "Project.csdl"
                                            }
                                        }
                                    }

                                }
                            },
                            OsType = "Linux",
                            IpAddress = new ContainerDeploymentTemplate.IpAddressModel()
                            {
                                Type = "Public",
                                Ports = new List<ContainerDeploymentTemplate.PortModel>()
                                {
                                    new ContainerDeploymentTemplate.PortModel()
                                    {
                                        Protocol = "tcp",
                                        Port = 80
                                    }
                                },
                                DnsNameLabel = project.AppId
                            },
                            Volumes = new List<ContainerDeploymentTemplate.VolumeModel>()
                            {
                                new ContainerDeploymentTemplate.VolumeModel()
                                {
                                    Name = $"vol{project.AppId}",
                                    AzureFile = new ContainerDeploymentTemplate.AzureFileModel()
                                    {
                                        ShareName = project.AzureFileShare,
                                        StorageAccountKey = project.StorageAccountKey,
                                        StorageAccountName = project.StorageAccountName
                                    }
                                }
                            },
                            ImageRegistryCredentials = new List<ContainerDeploymentTemplate.ImageRegistryCredentialsModel>()
                            {
                                new ContainerDeploymentTemplate.ImageRegistryCredentialsModel()
                                {
                                    Server = registryServer,
                                    Username = registryUsername,
                                    Password = registryPassword
                                }
                            }
                        }
                    }
                }
            };

            return template;
        }
    }
}
