using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Samples
{
    public class EdgeDeploymentSample
    {
        private const string DeviceIdPrefix = "EdgeDeploymentSample_";
        private const string ConfigurationIdPrefix = "edgedeploymentsampleconfiguration-";
        private const int NumOfEdgeDevices = 10;
        private const int BasePriority = 2;
        private readonly RegistryManager _registryManager;

        private readonly List<Configuration> _configurationsToDelete = new List<Configuration>();

        public EdgeDeploymentSample(RegistryManager registryManager)
        {
            _registryManager = registryManager ?? throw new ArgumentNullException(nameof(registryManager));
        }

        public async Task RunSampleAsync()
        {
            try
            {
                IEnumerable<Device> devices = GenerateEdgeDevices(DeviceIdPrefix, NumOfEdgeDevices);
                var conditionPropertyName = "condition-" + Guid.NewGuid().ToString("N");
                var conditionPropertyValue = Guid.NewGuid().ToString();
                var targetCondition = $"tags.{conditionPropertyName}='{conditionPropertyValue}'";

                var edgeDevices = devices.ToList();
                BulkRegistryOperationResult createResult = await CreateEdgeDevicesAsync(edgeDevices).ConfigureAwait(false);
                if (createResult.Errors.Length > 0)
                {
                    foreach (var err in createResult.Errors)
                    {
                        Console.WriteLine($"Create failed: {err.DeviceId}-{err.ErrorCode}-{err.ErrorStatus}");
                    }
                }

                foreach (var device in edgeDevices)
                {
                    Console.WriteLine($"Created edge device {device.Id}");
                    var twin = await _registryManager.GetTwinAsync(device.Id).ConfigureAwait(false);
                    Console.WriteLine($"\tTwin is {twin.ToJson()}");

                    twin.Tags[conditionPropertyName] = conditionPropertyValue;
                    await _registryManager.UpdateTwinAsync(device.Id, twin, twin.ETag).ConfigureAwait(false);
                    Console.WriteLine($"\tUpdated twin to {twin.ToJson()}");

                    Console.WriteLine();
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

                var baseConfigTask = _registryManager.AddConfigurationAsync(baseConfiguration);
                var addOnConfigTask = _registryManager.AddConfigurationAsync(addOnConfiguration);
                await Task.WhenAll(baseConfigTask, addOnConfigTask).ConfigureAwait(false);

                Console.WriteLine($"Cleaning up configuration created...");
                await CleanUpConfigurations().ConfigureAwait(false);

                Console.WriteLine("Finished.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task CleanUpConfigurations()
        {
            var configurations = await _registryManager.GetConfigurationsAsync(100).ConfigureAwait(false);
            {
                foreach (var configuration in configurations)
                {
                    string configurationId = configuration.Id;
                    if (configurationId.StartsWith(ConfigurationIdPrefix))
                    {
                        _configurationsToDelete.Add(new Configuration(configurationId));
                    }
                }
            }

            var removeConfigTasks = new List<Task>();
            _configurationsToDelete.ForEach(configuration =>
            {
                Console.WriteLine($"Remove: {configuration.Id}");
                removeConfigTasks.Add(_registryManager.RemoveConfigurationAsync(configuration.Id));
            });

            await Task.WhenAll(removeConfigTasks).ConfigureAwait(false);
            Console.WriteLine($"-- Total # of configurations deleted: {_configurationsToDelete.Count}");
        }

        private static IEnumerable<Device> GenerateEdgeDevices(string deviceIdPrefix, int numToAdd)
        {
            IList<Device> edgeDevices = new List<Device>();

            for (var i = 0; i < numToAdd; i++)
            {
                string deviceName = $"{deviceIdPrefix}_{i:D8}-{Guid.NewGuid()}";
                var device = new Device(deviceName)
                {
                    Capabilities = new DeviceCapabilities
                    {
                        IotEdge = true,
                    }
                };

                edgeDevices.Add(device);
            }

            return edgeDevices;
        }

        private Task<BulkRegistryOperationResult> CreateEdgeDevicesAsync(IEnumerable<Device> edgeDevices)
        {
            return _registryManager.AddDevices2Async(edgeDevices);
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