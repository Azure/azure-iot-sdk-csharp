// Copyright (c) Microsoft. All rights reserved.
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
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            Func<IotHubDeviceClient, TestDevice, TestDeviceCallbackHandler, Task> initOperation,
            Func<IotHubDeviceClient, TestDevice, TestDeviceCallbackHandler, Task> testOperation,
            Func<Task> cleanupOperation,
            ConnectionStringAuthScope authScope,
            bool ignoreConnectionState,
            MsTestLogger logger)
        {
            transportSettings.ConnectionPoolSettings = new AmqpConnectionPoolSettings
            {
                MaxPoolSize = unchecked((uint)poolSize),
                Pooling = true,
            };

            int totalRuns = 0;
            int successfulRuns = 0;
            int currentSuccessRate = 0;
            bool reRunTest = false;

            var testDevices = new List<TestDevice>(devicesCount);
            var deviceClients = new List<IotHubDeviceClient>(devicesCount);
            var testDeviceCallbackHandlers = new List<TestDeviceCallbackHandler>(devicesCount);
            var amqpConnectionStates = new List<AmqpConnectionStateChange>(devicesCount);
            var operations = new List<Task>(devicesCount);

            do
            {
                totalRuns++;

                // Arrange

                logger.Trace($">>> {nameof(PoolingOverAmqp)} Initializing device clients for multiplexing test - Test run {totalRuns}");
                for (int i = 0; i < devicesCount; i++)
                {
                    // Initialize the test device client instances
                    TestDevice testDevice = await TestDevice.GetTestDeviceAsync(logger, $"{devicePrefix}_{i}_").ConfigureAwait(false);
                    IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings), authScope: authScope);

                    // Set the device client connection state change handler
                    var amqpConnectionStateChange = new AmqpConnectionStateChange(logger);
                    deviceClient.SetConnectionStateChangeHandler(amqpConnectionStateChange.ConnectionStateChangeHandler);

                    var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, logger);

                    testDevices.Add(testDevice);
                    deviceClients.Add(deviceClient);
                    testDeviceCallbackHandlers.Add(testDeviceCallbackHandler);
                    amqpConnectionStates.Add(amqpConnectionStateChange);

                    if (initOperation != null)
                    {
                        await initOperation(deviceClient, testDevice, testDeviceCallbackHandler).ConfigureAwait(false);
                    }
                }

                try
                {
                    for (int i = 0; i < devicesCount; i++)
                    {
                        operations.Add(testOperation(deviceClients[i], testDevices[i], testDeviceCallbackHandlers[i]));
                    }
                    await Task.WhenAll(operations).ConfigureAwait(false);
                    operations.Clear();

                    // Close the device client instances and verify the connection state change checks
                    bool deviceConnectionStateAsExpected = true;
                    for (int i = 0; i < devicesCount; i++)
                    {
                        await deviceClients[i].CloseAsync().ConfigureAwait(false);

                        if (!ignoreConnectionState)
                        {
                            // The connection state change count should be 2: connect (open) and disabled (close)
                            if (amqpConnectionStates[i].ConnectionStateChangeHandlerCount != 2)
                            {
                                deviceConnectionStateAsExpected = false;
                            }

                            // The connection state should be "Disabled", with connection state change reason "ClientClose"
                            Assert.AreEqual(
                                ConnectionState.Disabled,
                                deviceClients[i].LastConnectionState,
                                $"The actual connection state is = {deviceClients[i].LastConnectionState}");
                            Assert.AreEqual(
                                ConnectionStateChangeReason.ClientClose,
                                deviceClients[i].LastConnectionStateChangeReason,
                                $"The actual connection state change reason is = {deviceClients[i].LastConnectionStateChangeReason}");
                        }
                    }
                    if (deviceConnectionStateAsExpected)
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

                    deviceClients.ForEach(deviceClient => deviceClient.Dispose());
                    testDeviceCallbackHandlers.ForEach(testDeviceCallbackHandler => testDeviceCallbackHandler.Dispose());

                    // Clean up the local lists
                    testDevices.Clear();
                    deviceClients.Clear();
                    amqpConnectionStates.Clear();
                }
            } while (reRunTest && totalRuns < MaxTestRunCount);

            Assert.IsFalse(reRunTest, $"Device client instances got disconnected in {totalRuns - successfulRuns} runs out of {totalRuns}; current testSuccessRate = {currentSuccessRate}%.");
        }

        private class AmqpConnectionStateChange
        {
            private readonly MsTestLogger _logger;

            public AmqpConnectionStateChange(MsTestLogger logger)
            {
                ConnectionStateChangeHandlerCount = 0;
                _logger = logger;
            }

            public void ConnectionStateChangeHandler(ConnectionState state, ConnectionStateChangeReason reason)
            {
                ConnectionStateChangeHandlerCount++;
                _logger.Trace($"{nameof(PoolingOverAmqp)}.{nameof(ConnectionStateChangeHandler)}: state={state} stateChangeReason={reason} count={ConnectionStateChangeHandlerCount}");
            }

            public int ConnectionStateChangeHandlerCount { get; set; }
        }
    }
}
