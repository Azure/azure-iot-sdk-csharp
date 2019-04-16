// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Samples;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SymmetricKeySample
{
    class Program
    {
        // The Provisioning Hub IDScope.

        // For this sample either:
        // - pass this value as a command-prompt argument
        // - set the DPS_IDSCOPE environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_idScope = Environment.GetEnvironmentVariable("DPS_IDSCOPE");

        // In your Device Provisioning Service please go to "Manage enrollments" and select "Individual Enrollments".
        // Select "Add individual enrollment" then fill in the following:
        // Mechanism: Symmetric Key
        // Auto-generate keys should be checked
        // DeviceID: iothubSymmetricKeydevice1

        // Symmetric Keys may also be used for enrollment groups.
        // In your Device Provisioning Service please go to "Manage enrollments" and select "Enrollment Groups".
        // Select "Add enrollment group" then fill in the following:
        // Group name: <your  group name>
        // Attestation Type: Symmetric Key
        // Auto-generate keys should be checked
        // You may also change other enrollment group parameters according to your needs

        private const string GlobalDeviceEndpoint = "global.azure-devices-provisioning.net";

        //These are the two keys that belong to your enrollment group. 
        // Leave them blank if you want to try this sample for an individual enrollment instead
        private const string enrollmentGroupPrimaryKey = "";
        private const string enrollmentGroupSecondaryKey = "";

        //registration id for enrollment groups can be chosen arbitrarily and do not require any portal setup. 
        //The chosen value will become the provisioned device's device id.
        //
        //registration id for individual enrollments must be retrieved from the portal and will be unrelated to the provioned
        //device's device id
        //
        //This field is mandatory to provide for this sample
        private static string registrationId = "";

        //These are the two keys that belong to your individual enrollment. 
        // Leave them blank if you want to try this sample for an individual enrollment instead
        private const string individualEnrollmentPrimaryKey = "";
        private const string individualEnrollmentSecondaryKey = "";

        public static int Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(s_idScope) && (args.Length > 0))
            {
                s_idScope = args[0];
            }

            if (string.IsNullOrWhiteSpace(s_idScope))
            {
                Console.WriteLine("ProvisioningDeviceClientSymmetricKey <IDScope>");
                return 1;
            }

            string primaryKey = "";
            string secondaryKey = "";
            if (!String.IsNullOrEmpty(registrationId) && !String.IsNullOrEmpty(enrollmentGroupPrimaryKey) && !String.IsNullOrEmpty(enrollmentGroupSecondaryKey))
            {
                //Group enrollment flow, the primary and secondary keys are derived from the enrollment group keys and from the desired registration id
                primaryKey = ComputeDerivedSymmetricKey(Convert.FromBase64String(enrollmentGroupPrimaryKey), registrationId);
                secondaryKey = ComputeDerivedSymmetricKey(Convert.FromBase64String(enrollmentGroupSecondaryKey), registrationId);
            }
            else if (!String.IsNullOrEmpty(registrationId) && !String.IsNullOrEmpty(individualEnrollmentPrimaryKey) && !String.IsNullOrEmpty(individualEnrollmentSecondaryKey))
            {
                //Individual enrollment flow, the primary and secondary keys are the same as the individual enrollment keys
                primaryKey = individualEnrollmentPrimaryKey;
                secondaryKey = individualEnrollmentSecondaryKey;
            }
            else
            {
                Console.WriteLine("Invalid configuration provided, must provide group enrollment keys or individual enrollment keys");
                return -1;
            }

            using (var security = new SecurityProviderSymmetricKey(registrationId, primaryKey, secondaryKey))

            // Select one of the available transports:
            // To optimize for size, reference only the protocols used by your application.
            using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
            // using (var transport = new ProvisioningTransportHandlerHttp())
            // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
            // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly))
            {
                ProvisioningDeviceClient provClient =
                    ProvisioningDeviceClient.Create(GlobalDeviceEndpoint, s_idScope, security, transport);

                var sample = new ProvisioningDeviceClientSample(provClient, security);
                sample.RunSampleAsync().GetAwaiter().GetResult();
            }
            Console.WriteLine("Enter any key to exit");
            Console.ReadLine();
            return 0;
        }

        /// <summary>
        /// Generate the derived symmetric key for the provisioned device from the enrollment group symmetric key used in attestation
        /// </summary>
        /// <param name="masterKey">Symmetric key enrollment group primary/secondary key value</param>
        /// <param name="registrationId">the registration id to create</param>
        /// <returns>the primary/secondary key for the member of the enrollment group</returns>
        public static string ComputeDerivedSymmetricKey(byte[] masterKey, string registrationId)
        {
            using (var hmac = new HMACSHA256(masterKey))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationId)));
            }
        }
    }
}
