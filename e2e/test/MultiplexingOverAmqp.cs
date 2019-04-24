// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
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

        public static async Task TestMultiplexingOperationAsync(
            string devicePrefix,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, TestDevice, Task> initOperation,
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
            IList<AmqpConnectionStatusChange> amqpConnectionStatuses = new List<AmqpConnectionStatusChange>();

            // Arrange
            // Initialize the test device client instances
            // Set the device client connection status change handler
            s_log.WriteLine($">>> {nameof(MultiplexingOverAmqp)} Initializing Device Clients for multiplexing test.");
            for (int i = 0; i < devicesCount; i++)
            {
                TestDevice testDevice = await TestDevice.GetTestDeviceAsync($"{devicePrefix}_{i}_").ConfigureAwait(false);
                DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings, authScope);

                var amqpConnectionStatusChange = new AmqpConnectionStatusChange();
                deviceClient.SetConnectionStatusChangesHandler(amqpConnectionStatusChange.ConnectionStatusChangesHandler);

                testDevices.Add(testDevice);
                deviceClients.Add(deviceClient);
                amqpConnectionStatuses.Add(amqpConnectionStatusChange);

                await initOperation(deviceClient, testDevice).ConfigureAwait(false);
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
                for (int i = 0; i < devicesCount; i++)
                {
                    await deviceClients[i].CloseAsync().ConfigureAwait(false);

                    // The connection status change count should be 2: connect (open) and disabled (close)
                    // The connection status should be "Disabled", with connection status change reason "Client_close"
                    Assert.IsTrue(amqpConnectionStatuses[i].ConnectionStatusChangesHandlerCount == 2, $"The actual connection status change count is = {amqpConnectionStatuses[i].ConnectionStatusChangesHandlerCount}");
                    Assert.AreEqual(ConnectionStatus.Disabled, amqpConnectionStatuses[i].LastConnectionStatus, $"The actual connection status is = {amqpConnectionStatuses[i].LastConnectionStatus}");
                    Assert.AreEqual(ConnectionStatusChangeReason.Client_Close, amqpConnectionStatuses[i].LastConnectionStatusChangeReason, $"The actual connection status change reason is = {amqpConnectionStatuses[i].LastConnectionStatusChangeReason}");
                }

            }
            finally
            {
                // Close the service-side components and dispose the device client instances.
                await cleanupOperation(deviceClients).ConfigureAwait(false);
            }
        }

        private class AmqpConnectionStatusChange
        {
            private int _connectionStatusChangesHandlerCount;

            public AmqpConnectionStatusChange()
            {
                LastConnectionStatus = null;
                LastConnectionStatusChangeReason = null;
                _connectionStatusChangesHandlerCount = 0;
            }

            public void ConnectionStatusChangesHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
            {
                _connectionStatusChangesHandlerCount++;
                LastConnectionStatus = status;
                LastConnectionStatusChangeReason = reason;
                s_log.WriteLine($"{nameof(MultiplexingOverAmqp)}.{nameof(ConnectionStatusChangesHandler)}: status={status} statusChangeReason={reason} count={_connectionStatusChangesHandlerCount}");
            }

            public int ConnectionStatusChangesHandlerCount
            {
                get => _connectionStatusChangesHandlerCount;
                set => _connectionStatusChangesHandlerCount = value;
            }

            public ConnectionStatus? LastConnectionStatus { get; set; }

            public ConnectionStatusChangeReason? LastConnectionStatusChangeReason { get; set; }
        }
    }
}