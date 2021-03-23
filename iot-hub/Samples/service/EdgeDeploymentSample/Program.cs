using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        // The IoT Hub connection string. This is available under the "Shared access policies" in the Azure portal.

        // For this sample either:
        // - pass this value as a command-prompt argument
        // - set the IOTHUB_CONNECTION_STRING environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable

        private static string s_connectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
        
        public static async Task<int> Main(string[] args)
        {
            if (string.IsNullOrEmpty(s_connectionString) && args.Length > 0)
            {
                s_connectionString = args[0];
            }

            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(s_connectionString);

            var sample = new EdgeDeploymentSample(registryManager);
            await sample.RunSampleAsync().ConfigureAwait(false);
            
            Console.WriteLine("Done.");
            return 0;
        }
    }
}