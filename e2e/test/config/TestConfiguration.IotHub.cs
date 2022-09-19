// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Globalization;
using System.Text;
using System.Security.Cryptography;

#if !NET451

using Azure.Identity;
using Azure;

#endif

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class TestConfiguration
    {
        public static partial class IotHub
        {
            public static string ConnectionString => GetValue("IOTHUB_CONNECTION_STRING");
            public static string X509ChainDeviceName => GetValue("IOTHUB_X509_CHAIN_DEVICE_NAME");

            public static string GetIotHubHostName()
            {
                var connectionString = new ConnectionStringParser(ConnectionString);
                return connectionString.IotHubHostName;
            }

#if !NET451

            public static ClientSecretCredential GetClientSecretCredential()
            {
                return new ClientSecretCredential(
                    GetValue("MSFT_TENANT_ID"),
                    GetValue("E2E_TEST_AAD_APP_CLIENT_ID"),
                    GetValue("E2E_TEST_AAD_APP_CLIENT_SECRET"));
            }

            public static string GetIotHubSharedAccessSignature(TimeSpan timeToLive)
            {
                var connectionString = new ConnectionStringParser(ConnectionString);
                return GenerateSasToken(
                    connectionString.IotHubHostName,
                    connectionString.SharedAccessKey,
                    timeToLive,
                    connectionString.SharedAccessKeyName);
            }

#endif

            public static string UserAssignedMsiResourceId => GetValue("IOTHUB_USER_ASSIGNED_MSI_RESOURCE_ID");

            public static X509Certificate2 GetCertificateWithPrivateKey()
            {
                const string hubPfxCert = "IOTHUB_X509_DEVICE_PFX_CERTIFICATE";
                X509Certificate2 cert = GetBase64EncodedCertificate(hubPfxCert, defaultValue: string.Empty);
                Assert.IsTrue(cert.NotAfter > DateTime.UtcNow, $"The X509 cert from {hubPfxCert} has expired.");
                return cert;
            }

            public static X509Certificate2 GetChainDeviceCertificateWithPrivateKey()
            {
                const string hubPfxCert = "IOTHUB_X509_CHAIN_DEVICE_PFX_CERTIFICATE";
                X509Certificate2 cert = GetBase64EncodedCertificate(hubPfxCert, defaultValue: string.Empty);
                Assert.IsTrue(cert.NotAfter > DateTime.UtcNow, $"The X509 cert from {hubPfxCert} has expired.");
                return cert;
            }

            public static string ConnectionStringInvalidServiceCertificate => GetValue("IOTHUB_CONN_STRING_INVALIDCERT", string.Empty);

            public static string DeviceConnectionStringInvalidServiceCertificate => GetValue("IOTHUB_DEVICE_CONN_STRING_INVALIDCERT", string.Empty);

            public static string ProxyServerAddress => GetValue("PROXY_SERVER_ADDRESS");

            /// <summary>
            /// A proxy server that should not exist (on local host)
            /// </summary>
            public const string InvalidProxyServerAddress = "127.0.0.1:1234";

#if !NET451

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

#endif

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
                                IotHubHostName = part.Substring("HOSTNAME=".Length);
                                break;

                            case "SHAREDACCESSKEY":
                                SharedAccessKey = part.Substring("SHAREDACCESSKEY=".Length);
                                break;

                            case "DEVICEID":
                                DeviceId = part.Substring("DEVICEID=".Length);
                                break;

                            case "SHAREDACCESSKEYNAME":
                                SharedAccessKeyName = part.Substring("SHAREDACCESSKEYNAME=".Length);
                                break;

                            default:
                                throw new NotSupportedException("Unrecognized tag found in test ConnectionString.");
                        }
                    }
                }

                public string IotHubHostName { get; private set; }

                public string DeviceId { get; private set; }

                public string SharedAccessKey { get; private set; }

                public string SharedAccessKeyName { get; private set; }
            }
        }
    }
}
