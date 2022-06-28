// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Azure.Devices.E2ETests.Provisioning;

#if !NET451

using Azure.Identity;

#endif
using FluentAssertions;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class TestConfiguration
    {
        public static partial class Provisioning
        {
            public static string CertificatePassword => GetValue("DPS_X509_PFX_CERTIFICATE_PASSWORD");

            public static string ConnectionString => GetValue("PROVISIONING_CONNECTION_STRING");

            public static string GetProvisioningHostName()
            {
                var connectionString = new ConnectionStringParser(ConnectionString);
                return connectionString.ProvisioningHostName;
            }

#if !NET451

            public static ClientSecretCredential GetClientSecretCredential()
            {
                return new ClientSecretCredential(
                    GetValue("MSFT_TENANT_ID"),
                    GetValue("E2E_TEST_AAD_APP_CLIENT_ID"),
                    GetValue("E2E_TEST_AAD_APP_CLIENT_SECRET"));
            }

#endif

            public static string GetProvisioningSharedAccessSignature(TimeSpan timeToLive)
            {
                var connectionString = new ConnectionStringParser(ConnectionString);
                return GenerateSasToken(
                    connectionString.ProvisioningHostName,
                    connectionString.SharedAccessKey,
                    timeToLive,
                    connectionString.SharedAccessKeyName);
            }

            public static string GlobalDeviceEndpoint =>
                GetValue("DPS_GLOBALDEVICEENDPOINT", "global.azure-devices-provisioning.net");

            public static string IdScope => GetValue("DPS_IDSCOPE");

            public static string X509GroupEnrollmentName => GetValue("DPS_X509_GROUP_ENROLLMENT_NAME");

            // This certificate is a part of the chain whose root has been verified by the Provisioning service.
            // The certificates used by the group enrollment tests are signed by this intermediate certificate.
            // Chain: Root->Intermediate1->Intermediate2
            // Certificate: Intermediate2->deviceCert
            public static string GetGroupEnrollmentIntermediatePfxCertificateBase64()
            {
                const string intermediateCert = "X509_CHAIN_INTERMEDIATE2_PFX_CERTIFICATE";
                using X509Certificate2 cert = GetBase64EncodedCertificate(intermediateCert, CertificatePassword);
                cert.NotAfter.Should().NotBeBefore(DateTime.Now, $"The X509 cert from {intermediateCert} has expired.");
                return GetValue(intermediateCert);
            }

            public static string ConnectionStringInvalidServiceCertificate => GetValue("PROVISIONING_CONNECTION_STRING_INVALIDCERT", string.Empty);

            public static string GlobalDeviceEndpointInvalidServiceCertificate => GetValue("DPS_GLOBALDEVICEENDPOINT_INVALIDCERT", string.Empty);

            //Note: Due to limitations with using VSTS Hosted agents, there is no guarantee that this hub is actually farther away
            // than the other test iot hub. As such, geolatency allocation policies cannot be tested properly
            public static string FarAwayIotHubHostName => GetValue("FAR_AWAY_IOTHUB_HOSTNAME");

            public static string CustomAllocationPolicyWebhook => GetValue("CUSTOM_ALLOCATION_POLICY_WEBHOOK");

            private static string GenerateSasToken(string resourceUri, string sharedAccessKey, TimeSpan timeToLive, string policyName = default)
            {
                // Calculate expiry value for token
                var epochTime = new DateTime(1970, 1, 1);
                DateTime expiresOn = DateTime.UtcNow.Add(timeToLive);
                TimeSpan secondsFromEpochTime = expiresOn.Subtract(epochTime);
                long seconds = Convert.ToInt64(secondsFromEpochTime.TotalSeconds, CultureInfo.InvariantCulture);
                string expiry = Convert.ToString(seconds, CultureInfo.InvariantCulture);

                string stringToSign = WebUtility.UrlEncode(resourceUri) + "\n" + expiry;

                using var hmac = new HMACSHA256(Convert.FromBase64String(sharedAccessKey));
                string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

                // SharedAccessSignature sr=ENCODED(dh://mydps.azure-devices-provisioning.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]
                string token = string.Format(
                    CultureInfo.InvariantCulture,
                    "SharedAccessSignature sr={0}&sig={1}&se={2}",
                    WebUtility.UrlEncode(resourceUri),
                    WebUtility.UrlEncode(signature),
                    expiry);

                // add policy name only if user chooses to include it
                if (!string.IsNullOrWhiteSpace(policyName))
                {
                    token += "&skn=" + WebUtility.UrlEncode(policyName);
                }

                return token;
            }
        }
    }
}
