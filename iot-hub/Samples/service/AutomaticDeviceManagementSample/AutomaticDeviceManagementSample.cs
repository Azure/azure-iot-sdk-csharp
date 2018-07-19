// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace Microsoft.Azure.Devices.Samples
{
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
            await AddDeviceConfiguration("config001").ConfigureAwait(false);
            await AddDeviceConfiguration("config002").ConfigureAwait(false);
            await AddDeviceConfiguration("config003").ConfigureAwait(false);
            await AddDeviceConfiguration("config004").ConfigureAwait(false);
            await AddDeviceConfiguration("config005").ConfigureAwait(false);

            Console.WriteLine("List existing configurations");
            await GetConfigurations(5).ConfigureAwait(false);

            Console.WriteLine("Remove some connfigurations");
            await DeleteConfiguration("config004").ConfigureAwait(false);
            await DeleteConfiguration("config002").ConfigureAwait(false);

            Console.WriteLine("List existing configurations");
            await GetConfigurations(5).ConfigureAwait(false);

            Console.WriteLine("Remove remaining connfigurations");
            await DeleteConfiguration("config001").ConfigureAwait(false);
            await DeleteConfiguration("config003").ConfigureAwait(false);
            await DeleteConfiguration("config005").ConfigureAwait(false);

            Console.WriteLine("List existing configurations (should be empty)");
            await GetConfigurations(5).ConfigureAwait(false);
        }

        public async Task AddDeviceConfiguration(string configurationId)
        {
            Configuration configuration = new Configuration(configurationId);

            CreateDeviceContent(configuration, configurationId);
            CreateModulesContent(configuration, configurationId);
            CreateMetricsAndTargetCondition(configuration, configurationId);

            await _registryManager.AddConfigurationAsync(configuration).ConfigureAwait(false);

            Console.WriteLine("Configuration added, id: " + configurationId);
        }

        public void CreateDeviceContent(Configuration configuration, string configurationId)
        {
            configuration.Content = new ConfigurationContent();
            configuration.Content.DeviceContent = new Dictionary<string, object>();
            configuration.Content.DeviceContent["properties.desired.deviceContent_key"] = "deviceContent_value-" + configurationId;
        }

        public void CreateModulesContent(Configuration configuration, string configurationId)
        {
            configuration.Content.ModulesContent = new Dictionary<string, IDictionary<string, object>>();
            IDictionary<string, object> modules_value = new Dictionary<string, object>();
            modules_value["properties.desired.modulesContent_key"] = "modulesContent_value-" + configurationId;
            configuration.Content.ModulesContent["properties.desired.modules_key"] = modules_value;
        }

        public void CreateMetricsAndTargetCondition(Configuration configuration, string configurationId)
        {
            configuration.Metrics.Queries.Add("waterSettingsPending", "SELECT deviceId FROM devices WHERE properties.reported.chillerWaterSettings.status=\'pending\'");
            configuration.TargetCondition = "properties.reported.chillerProperties.model=\'4000x\'";
            configuration.Priority = 20;
        }

        public async Task DeleteConfiguration(string configurationId)
        {
            await _registryManager.RemoveConfigurationAsync(configurationId).ConfigureAwait(false);

            Console.WriteLine("Configuration deleted, id: " + configurationId);
        }

        public async Task GetConfigurations(int count)
        {
            IEnumerable<Configuration> configurations = await _registryManager.GetConfigurationsAsync(count).ConfigureAwait(false);

            // Check configuration's metrics for expected conditions
            foreach (var configuration in configurations)
            {
                PrintConfiguration(configuration);
                Thread.Sleep(1000);
            }

            Console.WriteLine("Configurations received");
        }

        public void PrintConfiguration(Configuration configuration)
        {
            Console.WriteLine("Configuration Id: " + configuration.Id);
            Console.WriteLine("Configuration SchemaVersion: " + configuration.SchemaVersion);

            Console.WriteLine("Configuration Labels: " + configuration.Labels);

            PrintContent(configuration.ContentType, configuration.Content);

            Console.WriteLine("Configuration TargetCondition: " + configuration.TargetCondition);
            Console.WriteLine("Configuration CreatedTimeUtc: " + configuration.CreatedTimeUtc);
            Console.WriteLine("Configuration LastUpdatedTimeUtc: " + configuration.LastUpdatedTimeUtc);

            Console.WriteLine("Configuration Priority: " + configuration.Priority);

            PrintConfigurationMetrics(configuration.SystemMetrics, "SystemMetrics");
            PrintConfigurationMetrics(configuration.Metrics, "Metrics");

            Console.WriteLine("Configuration ETag: " + configuration.ETag);
            Console.WriteLine("------------------------------------------------------------");
        }

        private void PrintContent(string contentType, ConfigurationContent configurationContent)
        {
            Console.WriteLine($"Configuration Content [type = {contentType}]");

            Console.WriteLine("ModuleContent:");
            foreach (string modulesContentKey in configurationContent.ModulesContent.Keys)
            {
                foreach (string key in configurationContent.ModulesContent[modulesContentKey].Keys)
                {
                    Console.WriteLine($"\t\t{key} = {configurationContent.ModulesContent[modulesContentKey][key]}");
                }
            }

            Console.WriteLine("DeviceContent:");
            foreach (string key in configurationContent.DeviceContent.Keys)
            {
                Console.WriteLine($"\t{key} = {configurationContent.DeviceContent[key]}");
            }
        }

        private void PrintConfigurationMetrics(ConfigurationMetrics metrics, string title)
        {
            Console.WriteLine($"{title} Results: ({metrics.Results.Count})");
            foreach (string key in metrics.Results.Keys)
            {
                Console.WriteLine($"\t{key} = {metrics.Results[key]}");
            }

            Console.WriteLine($"{title} Queries: ({metrics.Queries.Count})");
            foreach (string key in metrics.Queries.Keys)
            {
                Console.WriteLine($"\t{key} = {metrics.Queries[key]}");
            }
        }
    }
}
