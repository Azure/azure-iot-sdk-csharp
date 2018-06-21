using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace Microsoft.Azure.Devices.Samples
{
    public class ADMSample
    {
        // Either set the IOTHUB_DEVICE_CONN_STRING environment variable or within launchSettings.json:
        private static string connectionString = Environment.GetEnvironmentVariable("IOTHUB_CONN_STRING_CSHARP");

        public async Task RunSampleAsync()
        {
            var registryManager = RegistryManager.CreateFromConnectionString(connectionString);

            int maxCount = 5;

            Console.WriteLine("Create configurations, count: " + maxCount);
            await AddDeviceConfiguration(registryManager, "config001").ConfigureAwait(false);
            await AddDeviceConfiguration(registryManager, "config002").ConfigureAwait(false);
            await AddDeviceConfiguration(registryManager, "config003").ConfigureAwait(false);
            await AddDeviceConfiguration(registryManager, "config004").ConfigureAwait(false);
            await AddDeviceConfiguration(registryManager, "config005").ConfigureAwait(false);

            Console.WriteLine("List existing configurations");
            await GetConfigurations(registryManager, 5).ConfigureAwait(false);

            Console.WriteLine("Remove some connfigurations");
            await DeleteConfiguration(registryManager, "config002").ConfigureAwait(false);
            await DeleteConfiguration(registryManager, "config004").ConfigureAwait(false);

            Console.WriteLine("List existing configurations");
            await GetConfigurations(registryManager, 5).ConfigureAwait(false);

            Console.WriteLine("Remove remaining connfigurations");
            await DeleteConfiguration(registryManager, "config001").ConfigureAwait(false);
            await DeleteConfiguration(registryManager, "config003").ConfigureAwait(false);
            await DeleteConfiguration(registryManager, "config005").ConfigureAwait(false);

            Console.WriteLine("List existing configurations (should be empty)");
            await GetConfigurations(registryManager, 5).ConfigureAwait(false);
        }

        private static async Task AddDeviceConfiguration(RegistryManager registryManager, string configurationId)
        {
            Configuration configuration = new Configuration(configurationId);

            CreateDeviceContent(configuration, configurationId);
            CreateModulesContent(configuration, configurationId);
            CreateMetricsAndTargetCondition(configuration, configurationId);

            await registryManager.AddConfigurationAsync(configuration).ConfigureAwait(false);

            Console.WriteLine("Configuration added, id: " + configurationId);
        }

        private static void CreateDeviceContent(Configuration configuration, string configurationId)
        {
            configuration.Content = new ConfigurationContent();
            configuration.Content.DeviceContent = new Dictionary<string, object>();
            configuration.Content.DeviceContent["properties.desired.deviceContent_key"] = "deviceContent_value-" + configurationId;
        }

        private static void CreateModulesContent(Configuration configuration, string configurationId)
        {
            configuration.Content.ModulesContent = new Dictionary<string, IDictionary<string, object>>();
            IDictionary<string, object> modules_value = new Dictionary<string, object>();
            modules_value["properties.desired.modulesContent_key"] = "modulesContent_value-" + configurationId;
            configuration.Content.ModulesContent["properties.desired.modules_key"] = modules_value;
        }

        private static void CreateMetricsAndTargetCondition(Configuration configuration, string configurationId)
        {
            configuration.Metrics.Queries.Add("waterSettingsPending", "SELECT deviceId FROM devices WHERE properties.reported.chillerWaterSettings.status=\'pending\'");
            configuration.TargetCondition = "properties.reported.chillerProperties.model=\'4000x\'";
            configuration.Priority = 20;
        }

        private static async Task DeleteConfiguration(RegistryManager registryManager, string configurationId)
        {
            await registryManager.RemoveConfigurationAsync(configurationId).ConfigureAwait(false);

            Console.WriteLine("Configuration deleted, id: " + configurationId);
        }

        private static async Task GetConfigurations(RegistryManager registryManager, int count)
        {
            IEnumerable<Configuration> configurations = await registryManager.GetConfigurationsAsync(count).ConfigureAwait(false);

            // Check configuration's metrics for expected conditions
            foreach (var configuration in configurations)
            {
                PrintConfiguration(configuration);
                Thread.Sleep(1000);
            }
            Console.WriteLine("Configurations received");
        }

        private static void PrintConfiguration(Configuration configuration)
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

        private static void PrintContent(string contentType, ConfigurationContent configurationContent)
        {
            Console.WriteLine("Configuration ContentType: " + contentType);

            Console.WriteLine("Configuration Content: " + configurationContent.ModulesContent);
            Console.WriteLine("Configuration Content: " + configurationContent.DeviceContent);
        }

        private static void PrintConfigurationMetrics(ConfigurationMetrics metrics, string title)
        {
            Console.WriteLine(title + " Results:" + metrics.Results);
            Console.WriteLine(title + " Queries:" + metrics.Queries);
        }
    }
}
