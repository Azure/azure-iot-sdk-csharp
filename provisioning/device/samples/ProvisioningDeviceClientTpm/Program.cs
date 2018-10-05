// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Comment this define to switch access to real TPM
//#define _USE_TPMSIMULATOR

using Microsoft.Azure.Devices.Client;

using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
#if _USE_TPMSIMULATOR
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
#else
using Microsoft.Azure.Devices.Provisioning.Security;
#endif
using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Linq;

namespace ProvisioningDeviceClientTpm
{

    public static class Program
    {
        private const string GlobalDeviceEndpoint = "global.azure-devices-provisioning.net";
        private static string s_idScope;

        public static async Task RunSample()
        {
            // DPS registration Id should be unique among enrollments. 
            // Such registration Id could be from TPM or any other unique identity, such as device serial number
            // As an example, we use hostname in this sample as the unique registration Id
            // A valid DPS registration Id contains only lower case alphanumeric letters and '-'
            string RegistrationId = Dns.GetHostName().ToLower().Select(i => (Char.IsLetterOrDigit(i) || (i == '-'))? i.ToString(): "-").ToArray().Aggregate((a,b) => a + b);

#if _USE_TPMSIMULATOR          
            Console.WriteLine("Starting TPM simulator.");
            SecurityProviderTpmSimulator.StartSimulatorProcess();

            // Replace the following type with SecurityProviderTpmHsm() to use a real TPM2.0 device.
            using (var security = new SecurityProviderTpmSimulator(RegistrationId))
#else
            using(var security = new SecurityProviderTpmHsm(RegistrationId))
#endif
            using(var transport = new ProvisioningTransportHandlerHttp())
            // using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
            {

                // Note that the TPM simulator will create an NVChip file containing the simulated TPM state.
                Console.WriteLine("Extracting endorsement key.");
                string base64EK = Convert.ToBase64String(security.GetEndorsementKey());
                string registrationId = security.GetRegistrationID();

                Console.WriteLine(
                    "In your Azure Device Provisioning Service please go to 'Manage enrollments' and select " +
                    "'Individual Enrollments'. Select 'Add' then fill in the following:");

                Console.WriteLine("\tMechanism: TPM");
                Console.WriteLine($"\tRegistration ID: {registrationId}");
                Console.WriteLine($"\tEndorsement key: {base64EK}");
                Console.WriteLine();
                Console.WriteLine("Press ENTER when ready.");
                Console.ReadLine();

                ProvisioningDeviceClient provClient =
                    ProvisioningDeviceClient.Create(GlobalDeviceEndpoint,s_idScope,security,transport);

                Console.Write("ProvisioningClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await provClient.RegisterAsync().ConfigureAwait(false);

                Console.WriteLine($"{result.Status}");
                Console.WriteLine($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

                if(result.Status != ProvisioningRegistrationStatusType.Assigned) return;

                var auth = new DeviceAuthenticationWithTpm(result.DeviceId,security);

                using(DeviceClient iotClient = DeviceClient.Create(result.AssignedHub,auth,TransportType.Http1))
                {
                    Console.WriteLine("DeviceClient OpenAsync.");
                    await iotClient.OpenAsync().ConfigureAwait(false);
                    Console.WriteLine("DeviceClient SendEventAsync.");
                    await iotClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes("TestMessage"))).ConfigureAwait(false);
                    Console.WriteLine("DeviceClient CloseAsync.");
                    await iotClient.CloseAsync().ConfigureAwait(false);
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
