// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Threading.Tasks;

namespace TlsProtocolTests
{
    internal class Program
    {
        /// <summary>
        /// Validates that the registry manager, and all the device client transports will succeed or fail given the OS settings configured.
        /// </summary>
        /// <remarks>
        /// To observe messages from the hub, consider using the az cli:
        ///   az iot hub monitor-events -n "your hub name"
        /// </remarks>
        static async Task Main(string[] args)
        {
            string hubConnectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
            string deviceConnectionString = Environment.GetEnvironmentVariable("DEVICE_CONN_STRING");

            await IotServiceTests.RunTest(hubConnectionString).ConfigureAwait(false);
            Console.WriteLine();

#if !NET451
            string scopeId = Environment.GetEnvironmentVariable("DPS_IDSCOPE");
            string deviceSas = Environment.GetEnvironmentVariable("DEVICE_SAS");
            string deviceId = Environment.GetEnvironmentVariable("DEVICE_ID");
            string dpsEndpoint = Environment.GetEnvironmentVariable("DPS_GLOBALDEVICEENDPOINT")
                ?? "global.azure-devices-provisioning.net";
            await DpsClientTests.RunTest(scopeId, deviceSas, deviceId, dpsEndpoint).ConfigureAwait(false);
            Console.WriteLine();
#endif

            await DeviceClientTests.RunTest(deviceConnectionString).ConfigureAwait(false);

            Console.WriteLine("Press any key to quit.");
            Console.ReadKey();
        }
    }
}
