using Azure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.Resources;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Core;
using System;

namespace Tyler.Greer
{
    public class CreateCS2Server
    {
        private readonly ILogger<CreateCS2Server> _logger;

        public CreateCS2Server(ILogger<CreateCS2Server> logger)
        {
            _logger = logger;
        }

        [Function("CreateCS2Server")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic? data = JsonConvert.DeserializeObject(requestBody);
            CS2ServerConfig serverConfig;
            try
            {
                // Use the CS2ServerConfig class to handle configuration
                serverConfig = new CS2ServerConfig(data);
            } catch (ArgumentException e) {
                var res = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await res.WriteAsJsonAsync(new { error = e.Message });
                return res;
            }

            // Generate a unique UUID for the container group name
            string uuid = Guid.NewGuid().ToString();
            string containerGroupName = $"cs2containergroup-{uuid}";
            string resourceGroupName = "tc-cs2-rg";
            string imageName = "joedwards32/cs2";

            // Set Azure File Storage configuration
            string mountPath = "/home/steam/cs2-dedicated";
            string storageAccountName = "tgcs2storage";
            string storageAccountShareName = "cs2fileshare";
            //
            string storageAccountKey = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_KEY") ?? string.Empty;
            if (string.IsNullOrEmpty(storageAccountKey))
            {
                // If the storage account key is not set, return a 500 error
                var res = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await res.WriteAsJsonAsync(new { error = "STORAGE_ACCOUNT_KEY is required" });
                return res;
            }

            ArmClient armClient;
            SubscriptionResource subscription;
            ResourceGroupResource resourceGroup;
            try
            {
                var credentials = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ExcludeVisualStudioCodeCredential = true,
                    ExcludeVisualStudioCredential = true
                });

                armClient = new ArmClient(credentials);
                subscription = await armClient.GetDefaultSubscriptionAsync();
                resourceGroup = await subscription.GetResourceGroups().GetAsync(resourceGroupName);
            } catch (Exception e)
            {
                var res = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await res.WriteAsJsonAsync(new { error = e.Message });
                return res;
            }

            // Define container resource requirements
            var containerResourceRequests = new ContainerResourceRequestsContent(2, 2);
            var containerResourceRequirements = new ContainerResourceRequirements(containerResourceRequests);

            // Define the container using the public constructor
            var container = new ContainerInstanceContainer(name: "cs2-container", image: imageName, resources: containerResourceRequirements);

            // Add environment variables and ports to the container instance
            foreach (var envVar in serverConfig.ToEnvironmentVariables())
            {
                container.EnvironmentVariables.Add(envVar);
            }

            var containerPorts = new List<ContainerPort>
            {
                new(27015) { Protocol = ContainerNetworkProtocol.Udp },
                new(27020) { Protocol = ContainerNetworkProtocol.Udp },
                new(27050) { Protocol = ContainerNetworkProtocol.Tcp }
            };
            foreach (var port in containerPorts)
            {
                container.Ports.Add(port);
            }

            // Define the volume mount for Azure File Storage
            var volumeMount = new ContainerVolumeMount("cs2volume", mountPath);
            container.VolumeMounts.Add(volumeMount);

            // Define the Azure File volume
            var azureFileVolume = new ContainerInstanceAzureFileVolume(storageAccountShareName, storageAccountName)
            {
                StorageAccountKey = storageAccountKey
            };

            // Define the container group IP address
            var containerGroupPorts = new List<ContainerGroupPort>
            {
                new ContainerGroupPort(27015) { Protocol = ContainerGroupNetworkProtocol.Udp },
                new ContainerGroupPort(27020) { Protocol = ContainerGroupNetworkProtocol.Udp },
                new ContainerGroupPort(27050) { Protocol = ContainerGroupNetworkProtocol.Tcp }
            };
            var containerGroupIPAddress = new ContainerGroupIPAddress(containerGroupPorts, ContainerGroupIPAddressType.Public);

            // Define the container group
            var containerGroup = new ContainerGroupData(AzureLocation.EastUS, new List<ContainerInstanceContainer> { container }, ContainerInstanceOperatingSystemType.Linux)
            {
                IPAddress = containerGroupIPAddress,
                RestartPolicy = ContainerGroupRestartPolicy.Never
            };

            // Add the volume to the container group
            containerGroup.Volumes.Add(new ContainerVolume("cs2volume")
            {
                AzureFile = azureFileVolume
            });

            await resourceGroup.GetContainerGroups().CreateOrUpdateAsync(WaitUntil.Completed, containerGroupName, containerGroup);

            // Pull the IP address of the container group
            var containerGroupResponse = await resourceGroup.GetContainerGroups().GetAsync(containerGroupName);
            string resolvedIP = containerGroupResponse.Value.Data.IPAddress.IP.ToString();

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                ServerName = serverConfig.ServerName,
                IP = resolvedIP,
                Ports = new
                {
                    GamePort = serverConfig.Port,
                    RconPort = serverConfig.RconPort,
                    TvPort = serverConfig.TvPort
                },
#if DEBUG
                ContainerGroupName = containerGroupName,
                ResourceGroupName = resourceGroupName
#endif
            });

            return response;
        }
    }
}
