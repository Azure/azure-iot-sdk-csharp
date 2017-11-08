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

            public static string ConsumerGroup => GetValue("IOTHUB_EVENTHUB_CONSUMER_GROUP", "$Default");

            public static X509Certificate2 GetCertificateWithPrivateKey() 
                => GetBase64EncodedCertificate("IOTHUB_X509_PFX_CERTIFICATE");
        }
    }
}
