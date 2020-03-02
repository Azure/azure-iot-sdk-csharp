using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TlsProtocolTests
{
    internal static class DpsClientTests
    {
        private static readonly TransportFallbackType[] _transportFallbackTypes = (TransportFallbackType[])Enum.GetValues(typeof(TransportFallbackType));

        public static async Task RunTest(string scopeId, string sasToken, string deviceId, string dpsEndpoint)
        {
            Console.WriteLine("Starting DPS client tests.");

            int successes = 0;
            int failures = 0;

            var security = new SecurityProviderSymmetricKey(deviceId, sasToken, null);
            var transportHandlers = new List<(ProvisioningTransportHandler, string)>();
            transportHandlers.Add((new ProvisioningTransportHandlerHttp(), "default"));
            foreach (var transportFallbackType in _transportFallbackTypes)
            {
                transportHandlers.Add((new ProvisioningTransportHandlerAmqp(transportFallbackType), transportFallbackType.ToString()));
                transportHandlers.Add((new ProvisioningTransportHandlerMqtt(transportFallbackType), transportFallbackType.ToString()));
            }

            foreach ((ProvisioningTransportHandler, string) transportHandler in transportHandlers)
            {
                try
                {
                    Console.WriteLine($"Registering with {transportHandler.Item1.GetType().Name}/{transportHandler.Item2}");

                    var provClient = ProvisioningDeviceClient.Create(
                        dpsEndpoint,
                        scopeId,
                        security,
                        transportHandler.Item1);
                    DeviceRegistrationResult provResult = await provClient.RegisterAsync().ConfigureAwait(false);
                    if (provResult.Status != ProvisioningRegistrationStatusType.Assigned)
                    {
                        Console.WriteLine($"Failed to connect due to {provResult.ErrorCode}: {provResult.ErrorMessage}.");
                        continue;
                    }

                    Console.WriteLine("Succeeded.\n");
                    successes++;
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
                    Console.WriteLine($"Failed for {transportHandler} due to:\n{reason}");
                    failures++;
                }
            }

            Console.WriteLine($"DPS client tests finished with {successes} successes and {failures} failures.");
        }
    }
}
