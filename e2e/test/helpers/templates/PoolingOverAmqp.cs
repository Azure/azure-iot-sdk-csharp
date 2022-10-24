// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.E2ETests.Helpers.Templates
{
    public static class PoolingOverAmqp
    {
        public const int SingleConnection_DevicesCount = 2;
        public const int SingleConnection_PoolSize = 1;
        public const int MultipleConnections_DevicesCount = 4;
        public const int MultipleConnections_PoolSize = 2;

        public static async Task TestPoolAmqpAsync(
            string devicePrefix,
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            Func<IotHubDeviceClient, TestDevice, TestDeviceCallbackHandler, Task> initOperation,
            Func<IotHubDeviceClient, TestDevice, TestDeviceCallbackHandler, Task> testOperation,
            Func<Task> cleanupOperation,
            ConnectionStringAuthScope authScope,
            bool ignoreConnectionStatus)
        {
            transportSettings.ConnectionPoolSettings = new AmqpConnectionPoolSettings
            {
                MaxPoolSize = unchecked((uint)poolSize),
                UsePooling = true,
            };

            var testDevices = new List<TestDevice>(devicesCount);
            var deviceClients = new List<IotHubDeviceClient>(devicesCount);
            var testDeviceCallbackHandlers = new List<TestDeviceCallbackHandler>(devicesCount);
            var amqpConnectionStatuses = new List<AmqpConnectionStatusChange>(devicesCount);
            var operations = new List<Task>(devicesCount);

            // Arrange
            // Initialize the test device client instances
            // Set the device client connection status change handler
            VerboseTestLogger.WriteLine($"{nameof(PoolingOverAmqp)} Initializing device clients for multiplexing test");
            for (int i = 0; i < devicesCount; i++)
            {
                // Initialize the test device client instances
                TestDevice testDevice = await TestDevice.GetTestDeviceAsync($"{devicePrefix}_{i}_").ConfigureAwait(false);
                IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings), authScope: authScope);

                // Set the device client connection status change handler
                var amqpConnectionStatusChange = new AmqpConnectionStatusChange();
                deviceClient.ConnectionStatusChangeCallback = amqpConnectionStatusChange.ConnectionStatusChangeHandler;

                var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice);

                testDevices.Add(testDevice);
                deviceClients.Add(deviceClient);
                testDeviceCallbackHandlers.Add(testDeviceCallbackHandler);
                amqpConnectionStatuses.Add(amqpConnectionStatusChange);

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
                amqpConnectionStatuses.Clear();
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
                VerboseTestLogger.WriteLine($"{nameof(PoolingOverAmqp)}.{nameof(ConnectionStatusChangeHandler)}: status={connectionStatusInfo.Status} statusChangeReason={connectionStatusInfo.ChangeReason} count={ConnectionStatusChangeHandlerCount}");
            }

            public int ConnectionStatusChangeHandlerCount { get; set; }
        }
    }
}
