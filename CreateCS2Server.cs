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

            // Retrieve the SRCDS_TOKEN from environment variables
            string? srcdsToken = Environment.GetEnvironmentVariable("SRCDS_TOKEN");
            if (string.IsNullOrEmpty(srcdsToken))
            {
                var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("SRCDS_TOKEN is not configured.");
                return badRequestResponse;
            }

            // Set internal configuration
            string serverName = "CS2_Game_Server"; // Automatically set server name
            string cs2Port = "27015";
            string rconPort = "27050"; // Use a different port for RCON to avoid Azure limitation
            string cs2MaxPlayers = data?.CS2_MAXPLAYERS ?? "10";
            string cs2Cheats = data?.CS2_CHEATS ?? "0";

            string containerGroupName = "cs2containergroup";
            string resourceGroupName = "myResourceGroup";
            //string location = "eastus";
            string imageName = "joedwards32/cs2"; // Assuming the image is public or accessible

            ArmClient armClient = new(new DefaultAzureCredential());
            SubscriptionResource subscription = await armClient.GetDefaultSubscriptionAsync();
            ResourceGroupResource resourceGroup = await subscription.GetResourceGroups().GetAsync(resourceGroupName);

            // Define container resource requirements
            var containerResourceRequests = new ContainerResourceRequestsContent(2, 2);
            var containerResourceRequirements = new ContainerResourceRequirements(containerResourceRequests);

            // Define container environment variables
            var environmentVariables = new List<ContainerEnvironmentVariable>
            {
                new("SRCDS_TOKEN") { Value = srcdsToken },
                new("CS2_SERVERNAME") { Value = serverName },
                new("CS2_CHEATS") { Value = cs2Cheats },
                new("CS2_PORT") { Value = cs2Port },
                new("CS2_RCON_PORT") { Value = rconPort },
                new("CS2_MAXPLAYERS") { Value = cs2MaxPlayers }
            };

            // Define container ports
            var containerPorts = new List<ContainerPort>
            {
                new(27015) { Protocol = ContainerNetworkProtocol.Udp },
                new(27020) { Protocol = ContainerNetworkProtocol.Udp },
                new(27050) { Protocol = ContainerNetworkProtocol.Tcp }
            };

            // Define container group ports
            var containerGroupPorts = new List<ContainerGroupPort>
            {
                new ContainerGroupPort(27015) { Protocol = ContainerGroupNetworkProtocol.Udp },
                new ContainerGroupPort(27020) { Protocol = ContainerGroupNetworkProtocol.Udp },
                new ContainerGroupPort(27050) { Protocol = ContainerGroupNetworkProtocol.Tcp }
            };

            // Define the container using the public constructor
            var container = new ContainerInstanceContainer(name: "cs2-container", image: imageName, resources: containerResourceRequirements);

            // Add environment variables and ports to the container instance
            foreach (var envVar in environmentVariables)
            {
                container.EnvironmentVariables.Add(envVar);
            }

            foreach (var port in containerPorts)
            {
                container.Ports.Add(port);
            }

            // Define the container group IP address
            var containerGroupIPAddress = new ContainerGroupIPAddress(containerGroupPorts, ContainerGroupIPAddressType.Public);

            // Define the container group
            var containerGroup = new ContainerGroupData(AzureLocation.EastUS, new List<ContainerInstanceContainer> { container }, ContainerInstanceOperatingSystemType.Linux)
            {
                IPAddress = containerGroupIPAddress,
                RestartPolicy = ContainerGroupRestartPolicy.Never
            };

            await resourceGroup.GetContainerGroups().CreateOrUpdateAsync(WaitUntil.Completed, containerGroupName, containerGroup);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                ServerName = serverName,
                IP = "To be retrieved", // Placeholder for the IP address retrieval logic
                Ports = new
                {
                    GamePort = cs2Port,
                    RconPort = rconPort,
                    TvPort = "27020"
                }
            });

            return response;
        }
    }
}
