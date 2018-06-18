using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace ADMSample
{
    class Program
    {
        // Either set the IOTHUB_DEVICE_CONN_STRING environment variable or within launchSettings.json:
        private static string connectionString = Environment.GetEnvironmentVariable("IOTHUB_CONN_STRING_CSHARP");

        // Either set the IOTHUB_PFX_X509_THUMBPRINT and IOTHUB_PFX_X509_THUMBPRINT2 environment variables 
        // or within launchSettings.json:
        private static string primaryThumbprint = Environment.GetEnvironmentVariable("IOTHUB_PFX_X509_THUMBPRINT");
        private static string secondaryThumbprint = Environment.GetEnvironmentVariable("IOTHUB_PFX_X509_THUMBPRINT2");

        static void Main(string[] args)
        {
            var registryManager = RegistryManager.CreateFromConnectionString(connectionString);

            int maxCount = 5;

            Console.WriteLine("Create configurations, count: " + maxCount);
            AddDeviceConfiguration(registryManager, "config001").Wait();
            AddDeviceConfiguration(registryManager, "config002").Wait();
            AddDeviceConfiguration(registryManager, "config003").Wait();
            AddDeviceConfiguration(registryManager, "config004").Wait();
            AddDeviceConfiguration(registryManager, "config005").Wait();

            Console.WriteLine("List existing configurations");
            GetConfigurations(registryManager, 5).Wait();

            Console.WriteLine("Remove some connfigurations");
            DeleteConfiguration(registryManager, "config002").Wait();
            DeleteConfiguration(registryManager, "config004").Wait();

            Console.WriteLine("List existing configurations");
            GetConfigurations(registryManager, 5).Wait();

            Console.WriteLine("Remove remaining connfigurations");
            DeleteConfiguration(registryManager, "config001").Wait();
            DeleteConfiguration(registryManager, "config003").Wait();
            DeleteConfiguration(registryManager, "config005").Wait();

            Console.WriteLine("List existing configurations (should be empty)");
            GetConfigurations(registryManager, 5).Wait();
        }

        private static async Task AddDeviceConfiguration(RegistryManager registryManager, string configurationId)
        {
            Configuration configuration = new Configuration(configurationId);

            configuration.Content = new ConfigurationContent();
            configuration.Content.DeviceContent = new Dictionary<string, object>();
            configuration.Content.DeviceContent["properties.desired.deviceContent_key"] = "deviceContent_value-" + configurationId;

            configuration.Content.ModulesContent = new Dictionary<string, IDictionary<string, object>>();
            IDictionary<string, object> modules_value = new Dictionary<string, object>();
            modules_value["properties.desired.modulesContent_key"] = "modulesContent_value-" + configurationId;
            configuration.Content.ModulesContent["properties.desired.modules_key"] = modules_value;

            await registryManager.AddConfigurationAsync(configuration).ConfigureAwait(false);

            Console.WriteLine("Configuration added, id: " + configurationId);
        }


        private static async Task DeleteConfiguration(RegistryManager registryManager, string configurationId)
        {
            await registryManager.RemoveConfigurationAsync(configurationId).ConfigureAwait(false);

            Console.WriteLine("Configuration deleted, id: " + configurationId);
        }

        private static async Task GetConfigurations(RegistryManager registryManager, int count)
        {
            IEnumerable<Configuration> configurations = await registryManager.GetConfigurationsAsync(count).ConfigureAwait(false);

            foreach (var configuration in configurations)
            {
                PrintConfiguration(configuration);
            }
            Console.WriteLine("Configurations received, count: " + configurations.Count());
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
