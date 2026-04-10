// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class Configuration
    {
        public static partial class Stress
        {
            private static string NamePrefix = "IotClientPerf";
            private static Lazy<string> s_hub = new Lazy<string>(() => {
                return (new Configuration.IoTHub.DeviceConnectionStringParser(Configuration.IoTHub.DeviceConnectionString)).IoTHub;
            });

            private static Lazy<string> s_key1 = new Lazy<string>(() => {
                return (new Configuration.IoTHub.DeviceConnectionStringParser(Configuration.IoTHub.DeviceConnectionString)).SharedAccessKey;
            });

            private static Lazy<string> s_key2 = new Lazy<string>(() => {
                return (new Configuration.IoTHub.DeviceConnectionStringParser(Configuration.IoTHub.DeviceConnectionString2)).SharedAccessKey;
            });

            private static Lazy<X509Certificate2> s_cert = new Lazy<X509Certificate2>(() => { return Configuration.IoTHub.GetCertificateWithPrivateKey(); });

            public static string GetDeviceNameById(int id, string authType)
            {
                return $"{NamePrefix}_{authType}_{id}";
            }

            public static string GetConnectionStringById(int id, string authType)
            {
                if (authType != "sas") throw new NotSupportedException($"Cannot use device connection string with authType '{authType}'");
                return $"HostName={Endpoint};DeviceId={GetDeviceNameById(id, authType)};SharedAccessKey={Key1}";
            }

            public static string Endpoint => s_hub.Value;

            public static string Key1 => s_key1.Value;

            public static string Key2 => s_key2.Value;

            public static X509Certificate2 Certificate => s_cert.Value;
        }
    }
}
