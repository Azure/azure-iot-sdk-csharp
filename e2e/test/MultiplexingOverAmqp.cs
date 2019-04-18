// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static class MultiplexingOverAmqp
    {
        public const int MuxWithoutPoolingDevicesCount = 2;
        public const int MuxWithoutPoolingPoolSize = 1; // For enabling multiplexing without pooling, the pool size needs to be set to 1
        public const int MuxWithPoolingDevicesCount = 4;
        public const int MuxWithPoolingPoolSize = 2;

        private static TestLogging s_log = TestLogging.GetInstance();
        private static int connectionStatusChangesHandlerCount = 0;
        private static ConnectionStatus lastConnectionStatus;
        private static ConnectionStatusChangeReason lastConnectionStatusChangeReason;

        private static void ConnectionStatusChangesHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            connectionStatusChangesHandlerCount++;
            lastConnectionStatus = status;
            lastConnectionStatusChangeReason = reason;
            s_log.WriteLine($"{nameof(MultiplexingOverAmqp)}.{nameof(ConnectionStatusChangesHandler)}: status={status} statusChangeReason={reason} count={connectionStatusChangesHandlerCount}");
        }

        public static async Task TestMultiplexingOperationAsync(
            string devicePrefix,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, TestDevice, Task> testOperation,
            Func<IList<DeviceClient>, Task> cleanupOperation,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            var transportSettings = new ITransportSettings[]
            {
                new AmqpTransportSettings(transport)
                {
                    AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                    {
                        MaxPoolSize = unchecked((uint)poolSize),
                        Pooling = true
                    }
                }
            };

            IList<TestDevice> testDevices = new List<TestDevice>();
            IList<DeviceClient> deviceClients = new List<DeviceClient>();

            // Arrange
            // Initialize the test device client instances
            // Set the device client connection status change handler
            s_log.WriteLine($">>> {nameof(MultiplexingOverAmqp)} Initializing Device Clients for multiplexing test.");
            for (int i = 0; i < devicesCount; i++)
            {
                TestDevice testDevice = await TestDevice.GetTestDeviceAsync($"{devicePrefix}_{i}_").ConfigureAwait(false);
                DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings, authScope);
                // WIP: set connection status change handler
                // Commented out - needs to be per client + understand depedence on other 
                // single device test running in parallel at the same time
                //deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler);

                testDevices.Add(testDevice);
                deviceClients.Add(deviceClient);
            }

            try
            {
                // Act-Assert
                // Perform the test operation and verify the operation is successful
                for (int i = 0; i < devicesCount; i++)
                {
                    await testOperation(deviceClients[i], testDevices[i]).ConfigureAwait(false);
                }

                // Close the device client instances and verify the connection status change checks
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    await deviceClient.CloseAsync().ConfigureAwait(false);

                    // WIP: set connection status change handler
                    // The connection status change count should be 2: connect (open) and disabled (close)
                    // The connection status should be "Disabled", with connection status change reason "Client_close"
                }

            }
            finally
            {
                // Close the service-side components and dispose the device client instances.
                await cleanupOperation(deviceClients).ConfigureAwait(false);
            }
        }
    }
}