// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ProvisioningDeviceClientTpm
{
    public static class Program
    {
        private static string s_idScope;
        private const string RegistrationId = "testtpmregistration1";

        public static async Task RunSample()
        {
            // Replace the following type with SecurityClientTpm() to use a real TPM2.0 device.
            Console.WriteLine("Starting TPM simulator.");
            SecurityClientTpmSimulator.StartSimulatorProcess();

            using (var security = new SecurityClientTpmSimulator(RegistrationId))
            using (var transport = new ProvisioningTransportHandlerHttp())
            {

                // Note that the TPM simulator will create a NVChip file containing the simulated TPM state.
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

                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(s_idScope, security, transport);

                Console.Write("ProvisioningClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await provClient.RegisterAsync();

                Console.WriteLine($"{result.Status}");
                Console.WriteLine($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

                if (result.Status != ProvisioningRegistrationStatusType.Assigned) return;

                var auth = new DeviceAuthenticationWithTpm(result.DeviceId, security);
                // TODO: Temporary workaround until IoTHub DeviceClient gets Token refresh support.
                await auth.GetTokenAsync(result.AssignedHub);

                using (DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth, TransportType.Mqtt))
                {
                    Console.WriteLine("DeviceClient OpenAsync.");
                    await iotClient.OpenAsync();
                    Console.WriteLine("DeviceClient SendEventAsync.");
                    await iotClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes("TestMessage")));
                    Console.WriteLine("DeviceClient CloseAsync.");
                    await iotClient.CloseAsync();
                }
            }
        }

        public static void Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(s_idScope) && (args.Length < 1))
            {
                Console.WriteLine("ProvisioningDeviceClientTpm <IDScope>");
                return;
            }

            s_idScope = args[0];

            RunSample().GetAwaiter().GetResult();
        }
    }
}
