﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

                int countBeforeFaultInjection = amqpConnectionStatuses[0].ConnectionStatusChangeCount;
                // Inject the fault into device 0
                watch.Start();

                s_log.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: {testDevices[0].Id} Requesting fault injection type={faultType} reason={reason}, delay={delayInSec}s, duration={durationInSec}s");
                var faultInjectionMessage = FaultInjection.ComposeErrorInjectionProperties(faultType, reason, delayInSec, durationInSec);
                await deviceClients[0].SendEventAsync(faultInjectionMessage).ConfigureAwait(false);

                s_log.WriteLine($"{nameof(FaultInjection)}: Waiting for fault injection to be active: {delayInSec} seconds.");
                await Task.Delay(TimeSpan.FromSeconds(delayInSec)).ConfigureAwait(false);

                // For disconnect type faults, the faulted device should disconnect and all devices should recover.
                if (FaultInjection.FaultShouldDisconnect(faultType))
                {
                    s_log.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirming fault injection has been actived.");
                    // Check that service issued the fault to the faulting device [device 0]
                    bool isFaulted = false;
                    for (int i = 0; i < FaultInjection.LatencyTimeBufferInSec; i++)
                    {
                        if (amqpConnectionStatuses[0].ConnectionStatusChangeCount > countBeforeFaultInjection)
                        {
                            isFaulted = true;
                            break;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }

                    Assert.IsTrue(isFaulted, $"The device {testDevices[0].Id} did not get faulted with fault type: {faultType}");
                    s_log.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirmed fault injection has been actived.");

                    // Check all devices are back online
                    s_log.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirming all devices back online.");
                    bool notRecovered = true;
                    int j = 0;
                    for (int i = 0; notRecovered && i < durationInSec + FaultInjection.LatencyTimeBufferInSec; i++)
                    {
                        notRecovered = false;
                        for (j = 0; j < devicesCount; j++)
                        {
                            if (amqpConnectionStatuses[j].LastConnectionStatus != ConnectionStatus.Connected)
                            {
                                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                                notRecovered = true;
                                break;
                            }
                        }
                    }

                    if (notRecovered)
                    {
                        Assert.Fail($"{testDevices[j].Id} did not reconnect.");
                    }
                    s_log.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirmed all devices back online.");

                    // Perform the test operation for all devices
                    for (int i = 0; i < devicesCount; i++)
                    {
                        s_log.WriteLine($">>> {nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation for device {i}.");
                        operations.Add(testOperation(deviceClients[i], testDevices[i]));
                    }
                    await Task.WhenAll(operations).ConfigureAwait(false);
                    operations.Clear();
                }
                else
                {
                    s_log.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation while fault injection is being activated.");
                    // Perform the test operation for the faulted device multi times.
                    for (int i = 0; i < FaultInjection.LatencyTimeBufferInSec; i++)
                    {
                        s_log.WriteLine($">>> {nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation for device 0 - Run {i}.");
                        await testOperation(deviceClients[0], testDevices[0]).ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                }

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
                            Assert.IsTrue(amqpConnectionStatuses[i].ConnectionStatusChangeCount >= 4, $"The expected connection status change count for {testDevices[i].Id} should equals or greater than 4 but was {amqpConnectionStatuses[i].ConnectionStatusChangeCount}");
                        }
                        else
                        {
                            // 2 is the minimum notification count: connect, disable.
                            Assert.IsTrue(amqpConnectionStatuses[i].ConnectionStatusChangeCount == 2, $"The expected connection status change count for {testDevices[i].Id}  should be 2 but was {amqpConnectionStatuses[i].ConnectionStatusChangeCount}");
                        }
                    }
                    Assert.AreEqual(ConnectionStatus.Disabled, amqpConnectionStatuses[i].LastConnectionStatus, $"The expected connection status should be {ConnectionStatus.Disabled} but was {amqpConnectionStatuses[i].LastConnectionStatus}");
                    Assert.AreEqual(ConnectionStatusChangeReason.Client_Close, amqpConnectionStatuses[i].LastConnectionStatusChangeReason, $"The expected connection status change reason should be {ConnectionStatusChangeReason.Client_Close} but was {amqpConnectionStatuses[i].LastConnectionStatusChangeReason}");
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

        public class AmqpConnectionStatusChange
        {
            private string _deviceId;

            public AmqpConnectionStatusChange(string deviceId)
            {
                LastConnectionStatus = null;
                LastConnectionStatusChangeReason = null;
                ConnectionStatusChangeCount = 0;
                _deviceId = deviceId;
            }

            public void ConnectionStatusChangesHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
            {
                ConnectionStatusChangeCount++;
                LastConnectionStatus = status;
                LastConnectionStatusChangeReason = reason;
                s_log.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}.{nameof(ConnectionStatusChangesHandler)}: {_deviceId}: status={status} statusChangeReason={reason} count={ConnectionStatusChangeCount}");
            }

            public int ConnectionStatusChangeCount { get; set; }

            public ConnectionStatus? LastConnectionStatus { get; set; }

            public ConnectionStatusChangeReason? LastConnectionStatusChangeReason { get; set; }
        }
    }
}
