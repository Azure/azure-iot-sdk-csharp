// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
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

            /// <summary>
            /// Gets the import export BLOB URI.
            /// </summary>
            public static string ImportExportBlobUri => GetValue("IOTHUB_IMPORTEXPORT_BLOB_URI");

            /// <summary>
            /// Gets the connected devices percentage expected by the runner after the test ended.
            /// </summary>
            public static long? ConnectedDevicesPercentage => ParseNullable(GetValue("IOTHUB_PERF_CONNECTED_PERCENTAGE", ""));

            /// <summary>
            /// Gets the connected devices percentage expected by the runner after the test ended.
            /// </summary>
            public static long? TcpConnectionsPercentage => ParseNullable(GetValue("IOTHUB_PERF_TCP_PERCENTAGE", ""));

            /// <summary>
            /// Gets the requests per second minimum average after the test ended.
            /// </summary>
            public static long? RequestsPerSecondMinAvg => ParseNullable(GetValue("IOTHUB_PERF_RPS_MIN_AVG", ""));

            /// <summary>
            /// Gets the requests per second minimum standard deviation after the test ended.
            /// </summary>
            public static long? RequestsPerSecondMaxStd => ParseNullable(GetValue("IOTHUB_PERF_RPS_MAX_STD", ""));

            /// <summary>
            /// Gets the requests per second minimum standard deviation after the test ended.
            /// </summary>
            public static long? GCMemoryBytes => ParseNullable(GetValue("IOTHUB_PERF_GC_MEM_BYTES_MAX", ""));

            /// <summary>
            /// Success rate defined as operations completed / (completed + failed + cancelled).
            /// </summary>
            public static long? SuccessRate => ParseNullable(GetValue("IOTHUB_PERF_SUCCESS_RATE_PERCENTAGE", ""));

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

            private static long? ParseNullable(string s)
            {
                if (long.TryParse(s, out long l)) return l;
                return null;
            }
        }
    }
}
