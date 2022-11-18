// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// This sample demonstrates automatic device management using device configurations.
    /// For module configurations, refer to: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Samples/service/EdgeDeploymentSample
    /// </summary>
    public class AutomaticDeviceManagementSample
    {
        private readonly IotHubServiceClient _hubServiceClient;

        public AutomaticDeviceManagementSample(IotHubServiceClient hubServiceClient)
        {
            _hubServiceClient = hubServiceClient ?? throw new ArgumentNullException(nameof(hubServiceClient));
        }

        public async Task RunSampleAsync()
        {
            Console.WriteLine("Create configurations");

            // save unique config names to be used for deletion
            const int configsToAdd = 5;
            var configs = new List<string>(configsToAdd);

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    configs.Add($"config00{i}_{Guid.NewGuid()}");
                    await AddDeviceConfigurationAsync(configs[i]);
                }

                Console.WriteLine("List existing configurations");
                await GetConfigurationsAsync(5);

                Console.WriteLine("Remove some configurations");
                await DeleteConfigurationAsync(configs[3]);
                await DeleteConfigurationAsync(configs[1]);

                Console.WriteLine("List existing configurations");
                await GetConfigurationsAsync(5);
            }
            finally
            {
                Console.WriteLine("Remove remaining connfigurations");
                await DeleteConfigurationAsync(configs[0]);
                await DeleteConfigurationAsync(configs[2]);
                await DeleteConfigurationAsync(configs[4]);

                Console.WriteLine("List existing configurations (should be empty)");
                await GetConfigurationsAsync(5);
            }
        }

        private async Task AddDeviceConfigurationAsync(string configurationId)
        {
            var configuration = new Configuration(configurationId);

            CreateDeviceContent(configuration, configurationId);
            CreateMetricsAndTargetCondition(configuration);

            await _hubServiceClient.Configurations.CreateAsync(configuration);

            Console.WriteLine($"Configuration added, id: {configurationId}");
        }

        private static void CreateDeviceContent(Configuration configuration, string configurationId)
        {
            configuration.Content = new ConfigurationContent
            {
                DeviceContent = new Dictionary<string, object>()
            };
            configuration.Content.DeviceContent["properties.desired.deviceContent_key"] = "deviceContent_value-" + configurationId;
        }

        private static void CreateMetricsAndTargetCondition(Configuration configuration)
        {
            configuration.Metrics.Queries.Add("waterSettingsPending", "SELECT deviceId FROM devices WHERE properties.reported.chillerWaterSettings.status=\'pending\'");
            configuration.TargetCondition = "properties.reported.chillerProperties.model=\'4000x\'";
            configuration.Priority = 20;
        }

        private async Task DeleteConfigurationAsync(string configurationId)
        {
            await _hubServiceClient.Configurations.DeleteAsync(configurationId);
            Console.WriteLine($"Configuration Id {configurationId} deleted");
        }

        private async Task GetConfigurationsAsync(int count)
        {
            IEnumerable<Configuration> configurations = await _hubServiceClient.Configurations.GetAsync(count);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            // Check configuration's metrics for expected conditions
            foreach (Configuration configuration in configurations)
            {
                string configurationString = JsonSerializer.Serialize(configuration, options);
                Console.WriteLine(configurationString);
                await Task.Delay(1000);
            }

            Console.WriteLine("Configurations received.");
        }
    }
}
