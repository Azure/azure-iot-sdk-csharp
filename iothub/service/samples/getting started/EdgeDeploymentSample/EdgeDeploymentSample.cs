using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class EdgeDeploymentSample
    {
        private const string DeviceIdPrefix = "EdgeDeploymentSample_";
        private const string ConfigurationIdPrefix = "edgedeploymentsampleconfiguration-";
        private const int NumOfEdgeDevices = 10;
        private const int BasePriority = 2;
        private readonly IotHubServiceClient _serviceClient;

        private readonly List<Configuration> _configurationsToDelete = new();

        public EdgeDeploymentSample(IotHubServiceClient serviceClient)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
        }

        public async Task RunSampleAsync()
        {
            try
            {
                IEnumerable<Device> devices = GenerateEdgeDevices(DeviceIdPrefix, NumOfEdgeDevices);
                string conditionPropertyName = "condition-" + Guid.NewGuid().ToString("N");
                string conditionPropertyValue = Guid.NewGuid().ToString();
                string targetCondition = $"tags.{conditionPropertyName}='{conditionPropertyValue}'";

                var edgeDevices = devices.ToList();
                BulkRegistryOperationResult createResult = await _serviceClient.Devices.CreateAsync(edgeDevices);
                if (createResult.Errors.Count > 0)
                {
                    foreach (DeviceRegistryOperationError err in createResult.Errors)
                    {
                        Console.WriteLine($"Create failed: {err.DeviceId}-{err.ErrorCode}-{err.ErrorStatus}");
                    }
                }

                foreach (Device device in edgeDevices)
                {
                    Console.WriteLine($"Created edge device {device.Id}");
                    Twin twin = await _serviceClient.Twins.GetAsync(device.Id);
                    Console.WriteLine($"\tTwin is {JsonSerializer.Serialize(twin)}");

                    twin.Tags[conditionPropertyName] = conditionPropertyValue;
                    await _serviceClient.Twins.UpdateAsync(device.Id, twin);
                    Console.WriteLine($"\tUpdated twin to {JsonSerializer.Serialize(twin)}\n");
                }

                var baseConfiguration = new Configuration($"{ConfigurationIdPrefix}base-{Guid.NewGuid()}")
                {
                    Labels = new Dictionary<string, string>
                    {
                        { "App", "Mongo" }
                    },
                    Content = GetBaseConfigurationContent(),
                    Priority = BasePriority,
                    TargetCondition = targetCondition
                };
                Console.WriteLine($"Adding base configuration {JsonSerializer.Serialize(baseConfiguration)}");
                Console.WriteLine();

                var addOnConfiguration = new Configuration($"{ConfigurationIdPrefix}addon-{Guid.NewGuid()}")
                {
                    Labels = new Dictionary<string, string>
                    {
                        { "AddOn", "Stream Analytics" }
                    },
                    Content = GetAddOnConfigurationContent(),
                    Priority = BasePriority + 1,
                    TargetCondition = targetCondition
                };
                Console.WriteLine($"Adding add-on configuration {JsonSerializer.Serialize(addOnConfiguration)}");
                Console.WriteLine();

                Task<Configuration> baseConfigTask = _serviceClient.Configurations.CreateAsync(baseConfiguration);
                Task<Configuration> addOnConfigTask = _serviceClient.Configurations.CreateAsync(addOnConfiguration);
                await Task.WhenAll(baseConfigTask, addOnConfigTask);

                Console.WriteLine($"Cleaning up configuration created...");
                await CleanUpConfigurationsAsync();

                Console.WriteLine("Finished.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task CleanUpConfigurationsAsync()
        {
            IEnumerable<Configuration> configurations = await _serviceClient.Configurations.GetAsync(100);
            {
                foreach (Configuration configuration in configurations)
                {
                    string configurationId = configuration.Id;
                    if (configurationId.StartsWith(ConfigurationIdPrefix))
                    {
                        _configurationsToDelete.Add(new Configuration(configurationId));
                    }
                }
            }

            var removeConfigTasks = new List<Task>();
            _configurationsToDelete.ForEach(
                configuration =>
                {
                    Console.WriteLine($"Remove: {configuration.Id}");
                    removeConfigTasks.Add(_serviceClient.Configurations.DeleteAsync(configuration.Id));
                });

            await Task.WhenAll(removeConfigTasks);
            Console.WriteLine($"-- Total # of configurations deleted: {_configurationsToDelete.Count}");
        }

        private static IEnumerable<Device> GenerateEdgeDevices(string deviceIdPrefix, int numToAdd)
        {
            IList<Device> edgeDevices = new List<Device>();

            for (int i = 0; i < numToAdd; i++)
            {
                string deviceName = $"{deviceIdPrefix}_{i:D8}-{Guid.NewGuid()}";
                var device = new Device(deviceName)
                {
                    Capabilities = new DeviceCapabilities
                    {
                        IsIotEdge = true,
                    }
                };

                edgeDevices.Add(device);
            }

            return edgeDevices;
        }

        private static ConfigurationContent GetBaseConfigurationContent()
        {
            return new ConfigurationContent
            {
                ModulesContent = new Dictionary<string, IDictionary<string, object>>
                {
                    ["$edgeAgent"] = new Dictionary<string, object>
                    {
                        ["properties.desired"] = GetEdgeAgentConfiguration(),
                    },
                    ["$edgeHub"] = new Dictionary<string, object>
                    {
                        ["properties.desired"] = GetEdgeHubConfiguration(),
                    },
                    ["mongoserver"] = new Dictionary<string, object>
                    {
                        ["properties.desired"] = GetTwinConfiguration("mongoserver"),
                    }
                }
            };
        }

        private static ConfigurationContent GetAddOnConfigurationContent()
        {
            return new ConfigurationContent
            {
                ModulesContent = new Dictionary<string, IDictionary<string, object>>
                {
                    ["$edgeAgent"] = new Dictionary<string, object>
                    {
                        ["properties.desired.modules.asa"] = GetEdgeAgentAddOnConfiguration(),
                    },
                    ["asa"] = new Dictionary<string, object>
                    {
                        ["properties.desired"] = GetTwinConfiguration("asa"),
                    },
                    ["$edgeHub"] = new Dictionary<string, object>
                    {
                        ["properties.desired.routes.route1"] = "from /* INTO $upstream",
                    }
                }
            };
        }

        private static object GetEdgeAgentAddOnConfiguration()
        {
            return new
            {
                version = "1.0",
                type = "docker",
                status = "running",
                restartPolicy = "on-failure",
                settings = new
                {
                    image = "mongo",
                    createOptions = string.Empty,
                }
            };
        }

        private static object GetTwinConfiguration(string moduleName)
        {
            return new
            {
                name = moduleName,
            };
        }

        private static object GetEdgeHubConfiguration()
        {
            return new
            {
                schemaVersion = "1.0",
                routes = new Dictionary<string, string>
                {
                    ["route1"] = "from /* INTO $upstream",
                },
                storeAndForwardConfiguration = new
                {
                    timeToLiveSecs = 20,
                }
            };
        }

        private static object GetEdgeAgentConfiguration()
        {
            return new
            {
                schemaVersion = "1.0",
                runtime = new
                {
                    type = "docker",
                    settings = new
                    {
                        loggingOptions = string.Empty,
                    }
                },
                systemModules = new
                {
                    edgeAgent = new
                    {
                        type = "docker",
                        settings = new
                        {
                            image = "edgeAgent",
                            createOptions = string.Empty,
                        }
                    },
                    edgeHub = new
                    {
                        type = "docker",
                        status = "running",
                        restartPolicy = "always",
                        settings = new
                        {
                            image = "edgeHub",
                            createOptions = string.Empty,
                        }
                    }
                },
                modules = new
                {
                    mongoserver = new
                    {
                        version = "1.0",
                        type = "docker",
                        status = "running",
                        restartPolicy = "on-failure",
                        settings = new
                        {
                            image = "mongo",
                            createOptions = string.Empty,
                        }
                    }
                }
            };
        }
    }
}