// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class Configuration
    {
        public static partial class IoTHub
        {
            public static string ConnectionString => GetValue("IOTHUB_CONN_STRING_CSHARP");

            public static X509Certificate2 GetCertificateWithPrivateKey()
            {
                const string hubPfxCert = "IOTHUB_X509_PFX_CERTIFICATE";
                var cert = GetBase64EncodedCertificate(hubPfxCert, defaultValue: string.Empty);
                Assert.IsTrue(cert.NotAfter > DateTime.UtcNow, $"The X509 cert from {hubPfxCert} has expired.");
                return cert;
            }

            public static string ConnectionStringInvalidServiceCertificate => GetValue("IOTHUB_CONN_STRING_INVALIDCERT", string.Empty);

            public static string DeviceConnectionStringInvalidServiceCertificate => GetValue("IOTHUB_DEVICE_CONN_STRING_INVALIDCERT", string.Empty);

            public static string ProxyServerAddress => GetValue("IOTHUB_PROXY_SERVER_ADDRESS");

            /// <summary>
            /// A proxy server that should not exist (on local host)
            /// </summary>
            public const string InvalidProxyServerAddress = "127.0.0.1:1234";

            public class DeviceConnectionStringParser
            {
                public DeviceConnectionStringParser(string connectionString)
                {
                    string[] parts = connectionString.Split(';');
                    foreach (string part in parts)
                    {
                        string[] tv = part.Split('=');

                        switch (tv[0].ToUpperInvariant())
                        {
                            case "HOSTNAME":
                                IoTHub = part.Substring("HOSTNAME=".Length);
                                break;

                            case "SHAREDACCESSKEY":
                                SharedAccessKey = part.Substring("SHAREDACCESSKEY=".Length);
                                break;

                            case "DEVICEID":
                                DeviceID = part.Substring("DEVICEID=".Length);
                                break;

                            default:
                                throw new NotSupportedException("Unrecognized tag found in test ConnectionString.");
                        }
                    }
                }

                public string IoTHub
                {
                    get;
                    private set;
                }

                public string DeviceID
                {
                    get;
                    private set;
                }

                public string SharedAccessKey
                {
                    get;
                    private set;
                }
            }
        }
    }
}
