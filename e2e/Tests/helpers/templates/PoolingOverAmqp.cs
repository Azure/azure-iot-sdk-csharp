// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.E2ETests.Helpers.Templates
{
    internal static class PoolingOverAmqp
    {
        public const int SingleConnection_DevicesCount = 2;
        public const int SingleConnection_PoolSize = 1;
        public const int MultipleConnections_DevicesCount = 4;
        public const int MultipleConnections_PoolSize = 2;

        internal static async Task TestPoolAmqpAsync(
            string devicePrefix,
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            Func<TestDevice, TestDeviceCallbackHandler, Task> initOperation,
            Func<TestDevice, TestDeviceCallbackHandler, Task> testOperation,
            Func<Task> cleanupOperation,
            ConnectionStringAuthScope authScope)
        {
            transportSettings.ConnectionPoolSettings = new AmqpConnectionPoolSettings
            {
                MaxPoolSize = unchecked((uint)poolSize),
                UsePooling = true,
            };

            var testDevices = new List<TestDevice>(devicesCount);
            var testDeviceCallbackHandlers = new List<TestDeviceCallbackHandler>(devicesCount);
            var amqpConnectionStatuses = new List<AmqpConnectionStatusChange>(devicesCount);
            var operations = new List<Task>(devicesCount);

            try
            {
                // Arrange
                // Initialize the test device client instances
                // Set the device client connection status change handler
                VerboseTestLogger.WriteLine($"{nameof(PoolingOverAmqp)} Initializing device clients for multiplexing test");
                for (int deviceCreateIndex = 0; deviceCreateIndex < devicesCount; deviceCreateIndex++)
                {
                    // Initialize the test device client instances
                    TestDevice testDevice = await TestDevice.GetTestDeviceAsync($"{devicePrefix}_{deviceCreateIndex}_").ConfigureAwait(false);
                    IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings), authScope: authScope);

                    // Set the device client connection status change handler
                    var amqpConnectionStatusChange = new AmqpConnectionStatusChange();
                    deviceClient.ConnectionStatusChangeCallback = amqpConnectionStatusChange.ConnectionStatusChangeHandler;

                    await deviceClient.OpenAsync().ConfigureAwait(false);
                    var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);

                    testDevices.Add(testDevice);
                    testDeviceCallbackHandlers.Add(testDeviceCallbackHandler);
                    amqpConnectionStatuses.Add(amqpConnectionStatusChange);

                    if (initOperation != null)
                    {
                        await initOperation(testDevice, testDeviceCallbackHandler).ConfigureAwait(false);
                    }
                }

                for (int deviceInitIndex = 0; deviceInitIndex < devicesCount; deviceInitIndex++)
                {
                    operations.Add(testOperation(testDevices[deviceInitIndex], testDeviceCallbackHandlers[deviceInitIndex]));
                }
                await Task.WhenAll(operations).ConfigureAwait(false);
                operations.Clear();
            }
            finally
            {
                // Close the service-side components and dispose the device client instances.
                if (cleanupOperation != null)
                {
                    try
                    {
                        await cleanupOperation().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        VerboseTestLogger.WriteLine($"Failed to run clean up due to {ex}");
                    }
                }

                testDeviceCallbackHandlers.ForEach(testDeviceCallbackHandler => { try { testDeviceCallbackHandler.Dispose(); } catch { } });
                await Task.WhenAll(testDevices.Select(x => x.DisposeAsync().AsTask())).ConfigureAwait(false);
            }
        }

        private class AmqpConnectionStatusChange
        {
            public AmqpConnectionStatusChange()
            {
                ConnectionStatusChangeHandlerCount = 0;
            }

            public void ConnectionStatusChangeHandler(ConnectionStatusInfo connectionStatusInfo)
            {
                ConnectionStatusChangeHandlerCount++;
                VerboseTestLogger.WriteLine(
                    $"{nameof(PoolingOverAmqp)}.{nameof(ConnectionStatusChangeHandler)}: status={connectionStatusInfo.Status} statusChangeReason={connectionStatusInfo.ChangeReason} count={ConnectionStatusChangeHandlerCount}");
            }

            public int ConnectionStatusChangeHandlerCount { get; set; }
        }
    }
}
