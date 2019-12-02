using System;
using System.Collections.Generic;
using System.Linq;
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

        public EdgeDeploymentSample(RegistryManager registryManager)
        {
            _registryManager = registryManager ?? throw new ArgumentNullException(nameof(registryManager));
        }

        public async Task RunSampleAsync()
        {
            try
            {
                var devices = GenerateEdgeDevices(DeviceIdPrefix, NumOfEdgeDevices);
                var conditionPropertyName = "condition-" + Guid.NewGuid().ToString("N");
                var conditionPropertyValue = Guid.NewGuid().ToString();
                var targetCondition = $"tags.{conditionPropertyName}='{conditionPropertyValue}'";

                var edgeDevices = devices.ToList();
                BulkRegistryOperationResult createResult = await CreateEdgeDevices(edgeDevices).ConfigureAwait(false);
                if (createResult.Errors.Length > 0)
                {
                    foreach (var err in createResult.Errors)
                    {
                        Console.WriteLine($"Create failed: {err.DeviceId}-{err.ErrorCode}-{err.ErrorStatus}");
                    }
                }

                foreach (var device in edgeDevices)
                {
                    var twin = await _registryManager.GetTwinAsync(device.Id).ConfigureAwait(false);
                    twin.Tags[conditionPropertyName] = conditionPropertyValue;
                    await _registryManager.UpdateTwinAsync(device.Id, twin, twin.ETag).ConfigureAwait(false);
                }
                
                var baseConfiguration = new Configuration($"{ConfigurationIdPrefix}base-{Guid.NewGuid().ToString()}")
                {
                    Labels = new Dictionary<string, string>
                    {
                        { "App", "Mongo" }
                    },
                    Content = GetBaseConfigurationContent(),
                    Priority = BasePriority,
                    TargetCondition = targetCondition
                };

                var addOnConfiguration = new Configuration($"{ConfigurationIdPrefix}addon-{Guid.NewGuid().ToString()}")
                {
                    Labels = new Dictionary<string, string>
                    {
                        { "AddOn", "Stream Analytics" }
                    },
                    Content = GetAddOnConfigurationContent(),
                    Priority = BasePriority + 1,
                    TargetCondition = targetCondition
                };

                var baseConfigTask = _registryManager.AddConfigurationAsync(baseConfiguration);
                var addOnConfigTask = _registryManager.AddConfigurationAsync(addOnConfiguration);
                Task.WaitAll(baseConfigTask, addOnConfigTask);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static IEnumerable<Device> GenerateEdgeDevices(string deviceIdPrefix, int numToAdd)
        {
            IList<Device> edgeDevices = new List<Device>();
            
            for (var i = 0; i < numToAdd; i++)
            {
                var deviceName = $"{deviceIdPrefix}_{i:D8}-{Guid.NewGuid().ToString()}";
                var device = new Device(deviceName)
                {
                    Capabilities = new DeviceCapabilities
                    {
                        IotEdge = true
                    }
                };
                
                edgeDevices.Add(device);
            }

            return edgeDevices;
        }

        private async Task<BulkRegistryOperationResult> CreateEdgeDevices(IEnumerable<Device> edgeDevices)
        {
            return await _registryManager.AddDevices2Async(edgeDevices);
        }
        
        private static ConfigurationContent GetBaseConfigurationContent()
        {
            return new ConfigurationContent
            {
                ModulesContent = new Dictionary<string, IDictionary<string, object>>
                {
                    ["$edgeAgent"] = new Dictionary<string, object>
                    {
                        ["properties.desired"] = GetEdgeAgentConfiguration()
                    },
                    ["$edgeHub"] = new Dictionary<string, object>
                    {
                        ["properties.desired"] = GetEdgeHubConfiguration()
                    },
                    ["mongoserver"] = new Dictionary<string, object>
                    {
                        ["properties.desired"] = GetTwinConfiguration("mongoserver")
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
                        ["properties.desired.modules.asa"] = GetEdgeAgentAddOnConfiguration()
                    },
                    ["asa"] = new Dictionary<string, object>
                    {
                        ["properties.desired"] = GetTwinConfiguration("asa")
                    },
                    ["$edgeHub"] = new Dictionary<string, object>
                    {
                        ["properties.desired.routes.route1"] = "from /* INTO $upstream"
                    }
                }
            };
        }

        private static object GetEdgeAgentAddOnConfiguration()
        {
            var desiredProperties = new
            {
                version = "1.0",
                type = "docker",
                status = "running",
                restartPolicy = "on-failure",
                settings = new
                {
                    image = "mongo",
                    createOptions = string.Empty
                }
            };
            return desiredProperties;
        }

        private static object GetTwinConfiguration(string moduleName)
        {
            var desiredProperties = new
            {
                name = moduleName
            };
            return desiredProperties;
        }

        private static object GetEdgeHubConfiguration()
        {
            var desiredProperties = new
            {
                schemaVersion = "1.0",
                routes = new Dictionary<string, string>
                {
                    ["route1"] = "from /* INTO $upstream",
                },
                storeAndForwardConfiguration = new
                {
                    timeToLiveSecs = 20
                }
            };
            return desiredProperties;
        }

        private static object GetEdgeAgentConfiguration()
        {
            var desiredProperties = new
            {
                schemaVersion = "1.0",
                runtime = new
                {
                    type = "docker",
                    settings = new
                    {
                        loggingOptions = string.Empty
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
                            createOptions = string.Empty
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
                            createOptions = string.Empty
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
                            createOptions = string.Empty
                        }
                    }
                }
            };
            return desiredProperties;
        }
    }
}