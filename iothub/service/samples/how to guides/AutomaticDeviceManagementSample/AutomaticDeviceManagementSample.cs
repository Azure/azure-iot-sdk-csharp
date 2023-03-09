// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// This sample demonstrates automatic device management using device configurations.
    /// For module configurations, refer to: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/main/iot-hub/Samples/service/EdgeDeploymentSample
    /// </summary>
    public class AutomaticDeviceManagementSample
    {
        private readonly RegistryManager _registryManager;

        public AutomaticDeviceManagementSample(RegistryManager registryManager)
        {
            _registryManager = registryManager ?? throw new ArgumentNullException(nameof(registryManager));
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

                Console.WriteLine("============================");
                Console.WriteLine("List existing configurations");
                Console.WriteLine("============================");
                await GetConfigurationsAsync(5);

                Console.WriteLine("==========================");
                Console.WriteLine("Remove some configurations");
                Console.WriteLine("==========================");
                await DeleteConfigurationAsync(configs[3]);
                await DeleteConfigurationAsync(configs[1]);

                Console.WriteLine("=============================");
                Console.WriteLine("List remaining configurations");
                Console.WriteLine("=============================");
                await GetConfigurationsAsync(5);
            }
            finally
            {
                Console.WriteLine("===============================");
                Console.WriteLine("Remove remaining configurations");
                Console.WriteLine("===============================");
                await DeleteConfigurationAsync(configs[0]);
                await DeleteConfigurationAsync(configs[2]);
                await DeleteConfigurationAsync(configs[4]);

                Console.WriteLine("==============================================");
                Console.WriteLine("List existing configurations (should be empty)");
                Console.WriteLine("==============================================");
                await GetConfigurationsAsync(5);
            }
        }

        private async Task AddDeviceConfigurationAsync(string configurationId)
        {
            var configuration = new Configuration(configurationId);

            CreateDeviceContent(configuration, configurationId);
            CreateMetricsAndTargetCondition(configuration);

            await _registryManager.AddConfigurationAsync(configuration);

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
            // Setup cancellation token for timeout handling
            using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var sw = Stopwatch.StartNew();

            // Exception handling for timeout and connection issues
            try
            {
                await _registryManager.RemoveConfigurationAsync(configurationId, cancel.Token);
                Console.WriteLine($"Configuration deleted, id: {configurationId}");
            }
            catch (Exception ex)
            {
                if (ex.InnerException is OperationCanceledException ocex)
                {
                    Console.WriteLine($"{nameof(OperationCanceledException)} thrown after {sw.Elapsed} with message: {ocex.Message}");
                }
                else
                {
                    Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!");
                    Console.WriteLine($"{nameof(DeleteConfigurationAsync)} thrown after {sw.Elapsed} an exception [{ex.GetType().Name}:{ex.Message}] when removing Configuration id [{configurationId}]");
                    Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!");
                }
            }
        }

        private async Task GetConfigurationsAsync(int count)
        {
            IEnumerable<Configuration> configurations = await _registryManager.GetConfigurationsAsync(count);
            int num = 0;
            // Check configuration's metrics for expected conditions
            foreach (Configuration configuration in configurations)
            {
                num++;
                string configurationString = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                Console.WriteLine(configurationString);
                Thread.Sleep(1000);
            }

            Console.WriteLine($"Configurations received: {num}.");
        }
    }
}
