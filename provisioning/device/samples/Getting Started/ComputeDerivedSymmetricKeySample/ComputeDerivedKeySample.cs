// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// This sample demonstrates how to derive the symmetric key for a particular device enrollment within an enrollment
    /// group. Best security practices dictate that the enrollment group level symmetric key should never be saved to a
    /// particular device, so this code is deliberately separate from the SymmetricKeySample in this same directory.
    /// Users are advised to run this code to generate the derived symmetric key once, and to save
    /// the derived key to the device. Users are not advised to derive the device symmetric key from the enrollment group
    /// level key within each device as that is unsecure.
    /// </summary>
    internal class ComputeDerivedKeySample
    {
        private readonly Parameters _parameters;

        public ComputeDerivedKeySample(Parameters parameters)
        {
            _parameters = parameters;
        }

        public void RunSample()
        {
            string derivedKey = ComputeDerivedSymmetricKey(_parameters.PrimaryKey, _parameters.RegistrationId);

            Console.WriteLine($"Your derived device key is:'{derivedKey}'");
        }

        /// <summary>
        /// Compute a symmetric key for the provisioned device from the enrollment group symmetric key used in attestation.
        /// </summary>
        /// <param name="enrollmentKey">Enrollment group symmetric key.</param>
        /// <param name="registrationId">The registration Id of the key to create.</param>
        /// <returns>The key for the specified device Id registration in the enrollment group.</returns>
        /// <seealso>
        /// https://docs.microsoft.com/azure/iot-edge/how-to-auto-provision-symmetric-keys#derive-a-device-key
        /// </seealso>
        private static string ComputeDerivedSymmetricKey(string enrollmentKey, string registrationId)
        {
            if (string.IsNullOrWhiteSpace(enrollmentKey))
            {
                return enrollmentKey;
            }

            using var hmac = new HMACSHA256(Convert.FromBase64String(enrollmentKey));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationId)));
        }
    }
}
