// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static class FaultInjectionPoolingOverAmqp
    {
        private static TestLogging s_log = TestLogging.GetInstance();

        public static async Task TestFaultInjectionPoolAmqpAsync(
            string devicePrefix,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            int delayInSec,
            int durationInSec,
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

            IList<TestDevice> testDevices = new List<TestDevice>();
            IList<DeviceClient> deviceClients = new List<DeviceClient>();
            IList<AmqpConnectionStatusChange> amqpConnectionStatuses = new List<AmqpConnectionStatusChange>();
            IList<Task> operations = new List<Task>();

            // Arrange
            // Initialize the test device client instances
            // Set the device client connection status change handler
            s_log.WriteLine($">>> {nameof(FaultInjectionPoolingOverAmqp)} Initializing Device Clients for multiplexing test.");
            for (int i = 0; i < devicesCount; i++)
            {
                TestDevice testDevice = await TestDevice.GetTestDeviceAsync($"{devicePrefix}_{i}_").ConfigureAwait(false);
                DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings, authScope);

                var amqpConnectionStatusChange = new AmqpConnectionStatusChange(testDevice.Id);
                deviceClient.SetConnectionStatusChangesHandler(amqpConnectionStatusChange.ConnectionStatusChangesHandler);

                testDevices.Add(testDevice);
                deviceClients.Add(deviceClient);
                amqpConnectionStatuses.Add(amqpConnectionStatusChange);

                operations.Add(initOperation(deviceClient, testDevice));
            }
            await Task.WhenAll(operations).ConfigureAwait(false);
            operations.Clear();

            var watch = new Stopwatch();

            try
            {
                // Act-Assert
                // Perform the test operation and verify the operation is successful

                // Perform baseline test operation
                for (int i = 0; i < devicesCount; i++)
                {
                    s_log.WriteLine($">>> {nameof(FaultInjectionPoolingOverAmqp)}: Performing baseline operation for device {i}.");
                    operations.Add(testOperation(deviceClients[i], testDevices[i]));
                }
                await Task.WhenAll(operations).ConfigureAwait(false);
                operations.Clear();

                // Inject the fault into device 0
                watch.Start();

                s_log.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Device {0} Requesting fault injection type={faultType} reason={reason}, delay={delayInSec}s, duration={durationInSec}s");
                var faultInjectionMessage = FaultInjection.ComposeErrorInjectionProperties(faultType, reason, delayInSec, durationInSec);
                await deviceClients[0].SendEventAsync(faultInjectionMessage).ConfigureAwait(false);

                int delay = FaultInjection.WaitForReconnectMilliseconds - (int)watch.ElapsedMilliseconds;
                if (delay < 0) delay = 0;
                s_log.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Waiting for fault injection to be active and device to be connected: {delay}ms");
                await Task.Delay(delay).ConfigureAwait(false);

                // Perform the test operation for all devices
                for (int i = 0; i < devicesCount; i++)
                {
                    s_log.WriteLine($">>> {nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation for device {i}.");
                    operations.Add(testOperation(deviceClients[i], testDevices[i]));
                }
                await Task.WhenAll(operations).ConfigureAwait(false);
                operations.Clear();

                // Close the device client instances
                for (int i = 0; i < devicesCount; i++)
                {
                    operations.Add(deviceClients[i].CloseAsync());
                }
                await Task.WhenAll(operations).ConfigureAwait(false);
                operations.Clear();

                // Verify the connection status change checks.
                // For all of the devices - last connection status should be "Disabled", with reason "Client_close"
                for (int i = 0; i < devicesCount; i++)
                {
                    // For the faulted device [device 0] - verify the connection status change count.
                    if (i == 0)
                    {
                        if (FaultInjection.FaultShouldDisconnect(faultType))
                        {
                            // 4 is the minimum notification count: connect, fault, reconnect, disable.
                            Assert.IsTrue(amqpConnectionStatuses[i].ConnectionStatusChangesHandlerCount >= 4, $"The actual connection status change count for faulted device[0] is = {amqpConnectionStatuses[i].ConnectionStatusChangesHandlerCount}");
                        }
                        else
                        {
                            // 2 is the minimum notification count: connect, disable.
                            Assert.IsTrue(amqpConnectionStatuses[i].ConnectionStatusChangesHandlerCount == 2, $"The actual connection status change count for for faulted device[0] is = {amqpConnectionStatuses[i].ConnectionStatusChangesHandlerCount}");
                        }
                    }
                    Assert.AreEqual(ConnectionStatus.Disabled, amqpConnectionStatuses[i].LastConnectionStatus);
                    Assert.AreEqual(ConnectionStatusChangeReason.Client_Close, amqpConnectionStatuses[i].LastConnectionStatusChangeReason);
                }
            }
            finally
            {
                // Close the service-side components and dispose the device client instances.
                await cleanupOperation(deviceClients).ConfigureAwait(false);

                watch.Stop();

                int timeToFinishFaultInjection = durationInSec * 1000 - (int)watch.ElapsedMilliseconds;
                if (timeToFinishFaultInjection > 0)
                {
                    s_log.WriteLine($"{nameof(FaultInjection)}: Waiting {timeToFinishFaultInjection}ms to ensure that FaultInjection duration passed.");
                    await Task.Delay(timeToFinishFaultInjection).ConfigureAwait(false);
                }
            }
        }

        private class AmqpConnectionStatusChange
        {
            private string _deviceId;

            public AmqpConnectionStatusChange(string deviceId)
            {
                LastConnectionStatus = null;
                LastConnectionStatusChangeReason = null;
                ConnectionStatusChangesHandlerCount = 0;
                _deviceId = deviceId;
            }

            public void ConnectionStatusChangesHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
            {
                ConnectionStatusChangesHandlerCount++;
                LastConnectionStatus = status;
                LastConnectionStatusChangeReason = reason;
                s_log.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}.{nameof(ConnectionStatusChangesHandler)}: {_deviceId}: status={status} statusChangeReason={reason} count={ConnectionStatusChangesHandlerCount}");
            }

            public int ConnectionStatusChangesHandlerCount { get; set; }

            public ConnectionStatus? LastConnectionStatus { get; set; }

            public ConnectionStatusChangeReason? LastConnectionStatusChangeReason { get; set; }
        }
    }
}