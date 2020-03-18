using Microsoft.Azure.Devices;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TlsProtocolTests
{
    internal static class IotServiceTests
    {
        public static async Task RunTest(string hubCs)
        {
            Console.WriteLine("Running IoT service tests.");

            int successes = 0;
            int failures = 0;

            var regResult = await TestRegistryManager(hubCs).ConfigureAwait(false);
            if (regResult.Succeeded)
            {
                successes++;
            }
            else
            {
                failures++;
            }

            if (await TestServiceClient(hubCs, regResult.DeviceId).ConfigureAwait(false))
            {
                successes++;
            }
            else
            {
                failures++;
            }

            Console.WriteLine($"IoT service tests finished with {successes} successes and {failures} failures.");
        }

        private static async Task<RegManResult> TestRegistryManager(string hubCs)
        {
            try
            {
                Console.WriteLine($"RegistryManager connect and attempt query for devices.");
                var registryManager = RegistryManager.CreateFromConnectionString(hubCs);

                await registryManager.OpenAsync().ConfigureAwait(false);

                var query = registryManager.CreateQuery("select *");
                if (query.HasMoreResults)
                {
                    var twins = await query.GetNextAsTwinAsync().ConfigureAwait(false);
                    string firstDevice = twins.FirstOrDefault()?.DeviceId;
                    Console.WriteLine($"\tRegistryManager successfully found a device: {firstDevice}.\n");
                    return new RegManResult(true, firstDevice);
                }
            }
            catch (Exception ex)
            {
                // Print all the relevant reasons for failing, without printing out the entire exception information
                var reason = new StringBuilder();

                Exception next = ex;
                do
                {
                    reason.AppendFormat($" - {next.GetType()}: {next.Message}\n");
                    next = next.InnerException;
                }
                while (next != null);
                Console.WriteLine($"\tFailed due to:\n{reason}");
                return new RegManResult(false);
            }

            return new RegManResult(true);
        }

        private static async Task<bool> TestServiceClient(string hubCs, string deviceId)
        {
            try
            {
                Console.WriteLine($"\tServiceManager connect and attempt purged message queue for {deviceId}.");
                var serviceClient = ServiceClient.CreateFromConnectionString(hubCs);
                await serviceClient.OpenAsync().ConfigureAwait(false);

                var purgeResult = await serviceClient.PurgeMessageQueueAsync(deviceId).ConfigureAwait(false);
                Console.WriteLine($"\tServiceManager successfully purged message queue for {deviceId}.\n");
            }
            catch (Exception ex)
            {
                // Print all the relevant reasons for failing, without printing out the entire exception information
                var reason = new StringBuilder();

                Exception next = ex;
                do
                {
                    reason.AppendFormat($" - {next.GetType()}: {next.Message}\n");
                    next = next.InnerException;
                }
                while (next != null);
                Console.WriteLine($"Failed due to:\n{reason}");
                return false;
            }

            return true;
        }

        private struct RegManResult
        {
            public bool Succeeded;
            public string DeviceId;

            public RegManResult(bool succeeded, string deviceId = "")
            {
                Succeeded = succeeded;
                DeviceId = deviceId;
            }
        }
    }
}
