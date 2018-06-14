// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Shared;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    public static class Program
    {
        // The Provisioning Hub IDScope.

        // For this sample either:
        // - pass this value as a command-prompt argument
        // - set the DPS_IDSCOPE environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_idScope = Environment.GetEnvironmentVariable("DPS_IDSCOPE");

        private const string RegistrationId = "testtpmregistration1";
        private const string GlobalDeviceEndpoint = "global.azure-devices-provisioning.net";
        
        public static int Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(s_idScope) && (args.Length > 1))
            {
                s_idScope = args[0];
            }

            if (string.IsNullOrWhiteSpace(s_idScope))
            {
                Console.WriteLine("ProvisioningDeviceClientTpm <IDScope>");
                return 1;
            }

            // Remove if a real TPM is being used.
            Console.WriteLine("Starting TPM simulator.");
            SecurityProviderTpmSimulator.StartSimulatorProcess();

            // Replace the following type with SecurityProviderTpmHsm() to use a real TPM2.0 device.
            using (var security = new SecurityProviderTpmSimulator(RegistrationId))

            // Select one of the available transports:
            // To optimize for size, reference only the protocols used by your application.
            using (var transport = new ProvisioningTransportHandlerHttp())
            // using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
            // using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.WebSocketOnly))
            {
                // Note that the TPM simulator will create an NVChip file containing the simulated TPM state.
                Console.WriteLine("Extracting endorsement key.");
                string base64EK = Convert.ToBase64String(security.GetEndorsementKey());

                Console.WriteLine(
                    "In your Azure Device Provisioning Service please go to 'Manage enrollments' and select " +
                    "'Individual Enrollments'. Select 'Add' then fill in the following:");

                Console.WriteLine("\tMechanism: TPM");
                Console.WriteLine($"\tRegistration ID: {RegistrationId}");
                Console.WriteLine($"\tEndorsement key: {base64EK}");
                Console.WriteLine("\tDevice ID: iothubtpmdevice1 (or any other valid DeviceID)");
                Console.WriteLine();
                Console.WriteLine("Press ENTER when ready.");
                Console.ReadLine();

                ProvisioningDeviceClient provClient =
                    ProvisioningDeviceClient.Create(GlobalDeviceEndpoint, s_idScope, security, transport);

                var sample = new ProvisioningDeviceClientSample(provClient, security);
                sample.RunSampleAsync().GetAwaiter().GetResult();
            }

            return 0;
        }
    }
}
