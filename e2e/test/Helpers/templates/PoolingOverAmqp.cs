﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Helpers.Templates
{
    public static class PoolingOverAmqp
    {
        public const int SingleConnection_DevicesCount = 2;
        public const int SingleConnection_PoolSize = 1;
        public const int MultipleConnections_DevicesCount = 4;
        public const int MultipleConnections_PoolSize = 2;
        public const int MaxTestRunCount = 5;
        public const int TestSuccessRate = 80; // 4 out of 5 (80%) test runs should pass (even after accounting for network instability issues).

        public static async Task TestPoolAmqpAsync(
            string devicePrefix,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, TestDevice, TestDeviceCallbackHandler, Task> initOperation,
            Func<DeviceClient, TestDevice, TestDeviceCallbackHandler, Task> testOperation,
            Func<Task> cleanupOperation,
            ConnectionStringAuthScope authScope,
            bool ignoreConnectionStatus,
            MsTestLogger logger)
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
            IList<TestDeviceCallbackHandler> testDeviceCallbackHandlers = new List<TestDeviceCallbackHandler>();
            IList<AmqpConnectionStatusChange> amqpConnectionStatuses = new List<AmqpConnectionStatusChange>();
            IList<Task> operations = new List<Task>();

            do
            {
                totalRuns++;

                // Arrange
                // Initialize the test device client instances
                // Set the device client connection status change handler
                logger.Trace($">>> {nameof(PoolingOverAmqp)} Initializing Device Clients for multiplexing test - Test run {totalRuns}");
                for (int i = 0; i < devicesCount; i++)
                {
                    TestDevice testDevice = await TestDevice.GetTestDeviceAsync(logger, $"{devicePrefix}_{i}_").ConfigureAwait(false);
                    DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings, authScope);

                    var amqpConnectionStatusChange = new AmqpConnectionStatusChange(logger);
                    deviceClient.SetConnectionStatusChangesHandler(amqpConnectionStatusChange.ConnectionStatusChangesHandler);

                    var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, logger);

                    testDevices.Add(testDevice);
                    deviceClients.Add(deviceClient);
                    testDeviceCallbackHandlers.Add(testDeviceCallbackHandler);
                    amqpConnectionStatuses.Add(amqpConnectionStatusChange);

                    if (initOperation != null)
                    {
                        operations.Add(initOperation(deviceClient, testDevice, testDeviceCallbackHandler));
                    }
                }

                await Task.WhenAll(operations).ConfigureAwait(false);
                operations.Clear();

                try
                {
                    for (int i = 0; i < devicesCount; i++)
                    {
                        operations.Add(testOperation(deviceClients[i], testDevices[i], testDeviceCallbackHandlers[i]));
                    }
                    await Task.WhenAll(operations).ConfigureAwait(false);
                    operations.Clear();

                    // Close the device client instances and verify the connection status change checks
                    bool deviceConnectionStatusAsExpected = true;
                    for (int i = 0; i < devicesCount; i++)
                    {
                        await deviceClients[i].CloseAsync().ConfigureAwait(false);

                        if (!ignoreConnectionStatus)
                        {
                            // The connection status change count should be 2: connect (open) and disabled (close)
                            if (amqpConnectionStatuses[i].ConnectionStatusChangesHandlerCount != 2)
                            {
                                deviceConnectionStatusAsExpected = false;
                            }

                            // The connection status should be "Disabled", with connection status change reason "Client_close"
                            Assert.AreEqual(
                                ConnectionStatus.Disabled,
                                amqpConnectionStatuses[i].LastConnectionStatus,
                                $"The actual connection status is = {amqpConnectionStatuses[i].LastConnectionStatus}");
                            Assert.AreEqual(
                                ConnectionStatusChangeReason.Client_Close,
                                amqpConnectionStatuses[i].LastConnectionStatusChangeReason,
                                $"The actual connection status change reason is = {amqpConnectionStatuses[i].LastConnectionStatusChangeReason}");
                        }
                    }
                    if (deviceConnectionStatusAsExpected)
                    {
                        successfulRuns++;
                    }

                    currentSuccessRate = (int)((double)successfulRuns / totalRuns * 100);
                    reRunTest = currentSuccessRate < TestSuccessRate;
                }
                finally
                {
                    // Close the service-side components and dispose the device client instances.
                    if (cleanupOperation != null)
                    {
                        await cleanupOperation().ConfigureAwait(false);
                    }

                    foreach (DeviceClient deviceClient in deviceClients)
                    {
                        deviceClient.Dispose();
                    }

                    // Clean up the local lists
                    testDevices.Clear();
                    deviceClients.Clear();
                    amqpConnectionStatuses.Clear();
                }
            } while (reRunTest && totalRuns < MaxTestRunCount);

            Assert.IsFalse(reRunTest, $"Device client instances got disconnected in {totalRuns - successfulRuns} runs out of {totalRuns}; current testSuccessRate = {currentSuccessRate}%.");
        }

        private class AmqpConnectionStatusChange
        {
            private readonly MsTestLogger _logger;

            public AmqpConnectionStatusChange(MsTestLogger logger)
            {
                LastConnectionStatus = null;
                LastConnectionStatusChangeReason = null;
                ConnectionStatusChangesHandlerCount = 0;
                _logger = logger;
            }

            public void ConnectionStatusChangesHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
            {
                ConnectionStatusChangesHandlerCount++;
                LastConnectionStatus = status;
                LastConnectionStatusChangeReason = reason;
                _logger.Trace($"{nameof(PoolingOverAmqp)}.{nameof(ConnectionStatusChangesHandler)}: status={status} statusChangeReason={reason} count={ConnectionStatusChangesHandlerCount}");
            }

            public int ConnectionStatusChangesHandlerCount { get; set; }

            public ConnectionStatus? LastConnectionStatus { get; set; }

            public ConnectionStatusChangeReason? LastConnectionStatusChangeReason { get; set; }
        }
    }
}
