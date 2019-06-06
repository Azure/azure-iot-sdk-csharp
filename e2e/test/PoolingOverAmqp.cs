// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static class PoolingOverAmqp
    {
        public const int SingleConnection_DevicesCount = 2;
        public const int SingleConnection_PoolSize = 1;
        public const int MultipleConnections_DevicesCount = 4;
        public const int MultipleConnections_PoolSize = 2;
        public const int maxTestRunCount = 5;
        public const int testSuccessRate = 80; // 4 out of 5 (80%) test runs should pass (even after accounting for network instability issues).

        private static TestLogging s_log = TestLogging.GetInstance();

        public static async Task TestPoolAmqpAsync(
            string devicePrefix,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, TestDevice, Task> initOperation,
            Func<DeviceClient, TestDevice, Task> testOperation,
            Func<IList<DeviceClient>, Task> cleanupOperation,
            ConnectionStringAuthScope authScope)
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

            int totalRuns = 0;
            int successfulRuns = 0;
            int currentSuccessRate = 0;
            bool reRunTest = false;

            IList<TestDevice> testDevices = new List<TestDevice>();
            IList<DeviceClient> deviceClients = new List<DeviceClient>();
            IList<AmqpConnectionStatusChange> amqpConnectionStatuses = new List<AmqpConnectionStatusChange>();
            IList<Task> operations = new List<Task>();

            do
            {
                totalRuns++;

                // Arrange
                // Initialize the test device client instances
                // Set the device client connection status change handler
                s_log.WriteLine($">>> {nameof(PoolingOverAmqp)} Initializing Device Clients for multiplexing test - Test run {totalRuns}");
                for (int i = 0; i < devicesCount; i++)
                {
                    TestDevice testDevice = await TestDevice.GetTestDeviceAsync($"{devicePrefix}_{i}_").ConfigureAwait(false);
                    DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings, authScope);

                    var amqpConnectionStatusChange = new AmqpConnectionStatusChange();
                    deviceClient.SetConnectionStatusChangesHandler(amqpConnectionStatusChange.ConnectionStatusChangesHandler);

                    testDevices.Add(testDevice);
                    deviceClients.Add(deviceClient);
                    amqpConnectionStatuses.Add(amqpConnectionStatusChange);

                    operations.Add(initOperation(deviceClient, testDevice));
                }
                await Task.WhenAll(operations).ConfigureAwait(false);
                operations.Clear();

                try
                {
                    for (int i = 0; i < devicesCount; i++)
                    {
                        operations.Add(testOperation(deviceClients[i], testDevices[i]));
                    }
                    await Task.WhenAll(operations).ConfigureAwait(false);
                    operations.Clear();

                    // Close the device client instances and verify the connection status change checks
                    bool deviceConnectionStatusAsExpected = true;
                    for (int i = 0; i < devicesCount; i++)
                    {
                        await deviceClients[i].CloseAsync().ConfigureAwait(false);

                        // The connection status change count should be 2: connect (open) and disabled (close)
                        if (amqpConnectionStatuses[i].ConnectionStatusChangesHandlerCount != 2)
                        {
                            deviceConnectionStatusAsExpected = false;
                        }

                        // The connection status should be "Disabled", with connection status change reason "Client_close"
                        Assert.AreEqual(ConnectionStatus.Disabled, amqpConnectionStatuses[i].LastConnectionStatus, $"The actual connection status is = {amqpConnectionStatuses[i].LastConnectionStatus}");
                        Assert.AreEqual(ConnectionStatusChangeReason.Client_Close, amqpConnectionStatuses[i].LastConnectionStatusChangeReason, $"The actual connection status change reason is = {amqpConnectionStatuses[i].LastConnectionStatusChangeReason}");
                    }
                    if (deviceConnectionStatusAsExpected) successfulRuns++;
                    currentSuccessRate = (int)((double)successfulRuns / totalRuns * 100);
                    reRunTest = currentSuccessRate < testSuccessRate;
                }
                finally
                {
                    // Close the service-side components and dispose the device client instances.
                    await cleanupOperation(deviceClients).ConfigureAwait(false);

                    // Clean up the local lists
                    testDevices.Clear();
                    deviceClients.Clear();
                    amqpConnectionStatuses.Clear();
                }
            } while (reRunTest && totalRuns < maxTestRunCount);

            Assert.IsFalse(reRunTest, $"Device client instances got disconnected in {totalRuns - successfulRuns} runs out of {totalRuns}; current testSuccessRate = {currentSuccessRate}%.");
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
                s_log.WriteLine($"{nameof(PoolingOverAmqp)}.{nameof(ConnectionStatusChangesHandler)}: status={status} statusChangeReason={reason} count={_connectionStatusChangesHandlerCount}");
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