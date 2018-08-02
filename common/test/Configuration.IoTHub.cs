// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class Configuration
    {
        public static partial class IoTHub
        {
            public static string ConnectionString => GetValue("IOTHUB_CONN_STRING_CSHARP");

            public static string EventHubString => GetValue("IOTHUB_EVENTHUB_CONN_STRING_CSHARP", string.Empty);

            public static string EventHubCompatibleName => GetValue("IOTHUB_EVENTHUB_COMPATIBLE_NAME", string.Empty);

            public static string EventHubConsumerGroup => GetValue("IOTHUB_EVENTHUB_CONSUMER_GROUP", "$Default");

            public static X509Certificate2 GetCertificateWithPrivateKey() 
                => GetBase64EncodedCertificate("IOTHUB_X509_PFX_CERTIFICATE", defaultValue:string.Empty);

            public static string ConnectionStringInvalidServiceCertificate => GetValue("IOTHUB_CONN_STRING_INVALIDCERT", string.Empty);

            public static string DeviceConnectionStringInvalidServiceCertificate => GetValue("IOTHUB_DEVICE_CONN_STRING_INVALIDCERT", string.Empty);

            public static string DeviceConnectionString => GetValue("IOTHUB_DEVICE_CONN_STRING");

            public class DeviceConnectionStringParser
            {
                public DeviceConnectionStringParser(string connectionString)
                {
                    string[] parts = connectionString.Split(';');
                    foreach (string part in parts)
                    {
                        string[] tv = part.Split('=');
    
                        switch(tv[0].ToUpperInvariant())
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
