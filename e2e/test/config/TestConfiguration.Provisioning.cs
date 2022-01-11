// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Azure.Identity;

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

            public static ClientSecretCredential GetClientSecretCredential()
            {
                return new ClientSecretCredential(
                    GetValue("MSFT_TENANT_ID"),
                    GetValue("PROVISIONING_CLIENT_ID"),
                    GetValue("PROVISIONING_CLIENT_SECRET"));
            }

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

            // To generate use Powershell: [System.Convert]::ToBase64String( (Get-Content .\certificate.pfx -Encoding Byte) )
            public static X509Certificate2 GetIndividualEnrollmentCertificate()
                => GetBase64EncodedCertificate("DPS_INDIVIDUALX509_PFX_CERTIFICATE", CertificatePassword);

            public static X509Certificate2 GetGroupEnrollmentCertificate()
                => GetBase64EncodedCertificate("DPS_GROUPX509_PFX_CERTIFICATE", CertificatePassword);

            public static X509Certificate2Collection GetGroupEnrollmentChain()
                => GetBase64EncodedCertificateCollection("DPS_GROUPX509_CERTIFICATE_CHAIN");

            public static string ConnectionStringInvalidServiceCertificate => GetValue("PROVISIONING_CONNECTION_STRING_INVALIDCERT", string.Empty);

            public static string GlobalDeviceEndpointInvalidServiceCertificate => GetValue("DPS_GLOBALDEVICEENDPOINT_INVALIDCERT", string.Empty);

            //Note: Due to limitations with using VSTS Hosted agents, there is no guarantee that this hub is actually farther away
            // than the other test iot hub. As such, geolatency allocation policies cannot be tested properly
            public static string FarAwayIotHubHostName => GetValue("FAR_AWAY_IOTHUB_HOSTNAME");

            public static string CustomAllocationPolicyWebhook => GetValue("CUSTOM_ALLOCATION_POLICY_WEBHOOK");

            private static string GenerateSasToken(string resourceUri, string sharedAccessKey, TimeSpan timeToLive, string policyName = default)
            {
                var epochTime = new DateTime(1970, 1, 1);
                DateTime expiresOn = DateTime.UtcNow.Add(timeToLive);
                TimeSpan secondsFromEpochTime = expiresOn.Subtract(epochTime);
                long seconds = Convert.ToInt64(secondsFromEpochTime.TotalSeconds, CultureInfo.InvariantCulture);
                string expiry = Convert.ToString(seconds, CultureInfo.InvariantCulture);

                string stringToSign = WebUtility.UrlEncode(resourceUri) + "\n" + expiry;

                using var hmac = new HMACSHA256(Convert.FromBase64String(sharedAccessKey));
                string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

                // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]
                string token = string.Format(
                    CultureInfo.InvariantCulture,
                    "SharedAccessSignature sr={0}&sig={1}&se={2}",
                    WebUtility.UrlEncode(resourceUri),
                    WebUtility.UrlEncode(signature),
                    expiry);

                if (!string.IsNullOrWhiteSpace(policyName))
                {
                    token += "&skn=" + policyName;
                }

                return token;
            }

            public class ConnectionStringParser
            {
                public ConnectionStringParser(string connectionString)
                {
                    string[] parts = connectionString.Split(';');
                    foreach (string part in parts)
                    {
                        string[] tv = part.Split('=');

                        switch (tv[0].ToUpperInvariant())
                        {
                            case "HOSTNAME":
                                ProvisioningHostName = part.Substring("HOSTNAME=".Length);
                                break;

                            case "SHAREDACCESSKEY":
                                SharedAccessKey = part.Substring("SHAREDACCESSKEY=".Length);
                                break;

                            case "DEVICEID":
                                DeviceID = part.Substring("DEVICEID=".Length);
                                break;

                            case "SHAREDACCESSKEYNAME":
                                SharedAccessKeyName = part.Substring("SHAREDACCESSKEYNAME=".Length);
                                break;

                            default:
                                throw new NotSupportedException("Unrecognized tag found in test ConnectionString.");
                        }
                    }
                }

                public string ProvisioningHostName { get; private set; }

                public string DeviceID { get; private set; }

                public string SharedAccessKey { get; private set; }

                public string SharedAccessKeyName { get; private set; }
            }
        }
    }
}
