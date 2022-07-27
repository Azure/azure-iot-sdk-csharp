// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Helpers.Templates
{
    public static class FaultInjectionPoolingOverAmqp

    {
        public static async Task TestFaultInjectionPoolAmqpAsync(
            string devicePrefix,
            ITransportSettings TransportSettings,
            string proxyAddress,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            TimeSpan delayInSec,
            TimeSpan durationInSec,
            Func<IotHubDeviceClient, TestDevice, TestDeviceCallbackHandler, Task> initOperation,
            Func<IotHubDeviceClient, TestDevice, TestDeviceCallbackHandler, Task> testOperation,
            Func<List<IotHubDeviceClient>, List<TestDeviceCallbackHandler>, Task> cleanupOperation,
            ConnectionStringAuthScope authScope,
            MsTestLogger logger)
        {
            var transportSettings  = new AmqpTransportSettings(TransportSettings.Protocol)
            {
                ConnectionPoolSettings = new AmqpConnectionPoolSettings()
                {
                    MaxPoolSize = unchecked((uint)poolSize),
                    Pooling = true,
                },
                Proxy = proxyAddress == null ? null : new WebProxy(proxyAddress),
            };

            var testDevices = new List<TestDevice>();
            var deviceClients = new List<IotHubDeviceClient>();
            var testDeviceCallbackHandlers = new List<TestDeviceCallbackHandler>();
            var amqpConnectionStatuses = new List<AmqpConnectionStatusChange>();
            var operations = new List<Task>();

            // Arrange
            // Initialize the test device client instances
            // Set the device client connection status change handler
            logger.Trace($">>> {nameof(FaultInjectionPoolingOverAmqp)} Initializing Device Clients for multiplexing test.");
            for (int i = 0; i < devicesCount; i++)
            {
                TestDevice testDevice = await TestDevice.GetTestDeviceAsync(logger, $"{devicePrefix}_{i}_").ConfigureAwait(false);
                IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings), authScope);

                var amqpConnectionStatusChange = new AmqpConnectionStatusChange(testDevice.Id, logger);
                deviceClient.SetConnectionStatusChangesHandler(amqpConnectionStatusChange.ConnectionStatusChangesHandler);

                var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, logger);

                testDevices.Add(testDevice);
                deviceClients.Add(deviceClient);
                testDeviceCallbackHandlers.Add(testDeviceCallbackHandler);
                amqpConnectionStatuses.Add(amqpConnectionStatusChange);

                operations.Add(initOperation(deviceClient, testDevice, testDeviceCallbackHandler));
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
                    logger.Trace($">>> {nameof(FaultInjectionPoolingOverAmqp)}: Performing baseline operation for device {i}.");
                    operations.Add(testOperation(deviceClients[i], testDevices[i], testDeviceCallbackHandlers[i]));
                }
                await Task.WhenAll(operations).ConfigureAwait(false);
                operations.Clear();

                int countBeforeFaultInjection = amqpConnectionStatuses[0].ConnectionStatusChangeCount;
                // Inject the fault into device 0
                watch.Start();

                logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: {testDevices[0].Id} Requesting fault injection type={faultType} reason={reason}, delay={delayInSec}s, duration={durationInSec}s");
                using Client.Message faultInjectionMessage = FaultInjection.ComposeErrorInjectionProperties(faultType, reason, delayInSec, durationInSec);
                await deviceClients[0].SendEventAsync(faultInjectionMessage).ConfigureAwait(false);

                logger.Trace($"{nameof(FaultInjection)}: Waiting for fault injection to be active: {delayInSec} seconds.");
                await Task.Delay(delayInSec).ConfigureAwait(false);

                // For disconnect type faults, the faulted device should disconnect and all devices should recover.
                if (FaultInjection.FaultShouldDisconnect(faultType))
                {
                    logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirming fault injection has been actived.");
                    // Check that service issued the fault to the faulting device [device 0]
                    bool isFaulted = false;

                    var sw = Stopwatch.StartNew();
                    while (sw.Elapsed < FaultInjection.LatencyTimeBuffer)
                    {
                        if (amqpConnectionStatuses[0].ConnectionStatusChangeCount > countBeforeFaultInjection)
                        {
                            isFaulted = true;
                            break;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    sw.Reset();

                    Assert.IsTrue(isFaulted, $"The device {testDevices[0].Id} did not get faulted with fault type: {faultType}");
                    logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirmed fault injection has been actived.");

                    // Check all devices are back online
                    logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirming all devices back online.");
                    bool notRecovered = true;
                    int j = 0;

                    sw.Start();
                    while (notRecovered && sw.Elapsed < durationInSec.Add(FaultInjection.LatencyTimeBuffer))
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
                    logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirmed all devices back online.");

                    // Perform the test operation for all devices
                    for (int i = 0; i < devicesCount; i++)
                    {
                        logger.Trace($">>> {nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation for device {i}.");
                        operations.Add(testOperation(deviceClients[i], testDevices[i], testDeviceCallbackHandlers[i]));
                    }
                    await Task.WhenAll(operations).ConfigureAwait(false);
                    operations.Clear();
                }
                else
                {
                    logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation while fault injection is being activated.");
                    // Perform the test operation for the faulted device multi times.
                    int counter = 0;
                    var sw = Stopwatch.StartNew();
                    while (sw.Elapsed < FaultInjection.LatencyTimeBuffer)
                    {
                        logger.Trace($">>> {nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation for device 0 - Run {counter++}.");
                        await testOperation(deviceClients[0], testDevices[0], testDeviceCallbackHandlers[0]).ConfigureAwait(false);
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
                            Assert.IsTrue(amqpConnectionStatuses[i].ConnectionStatusChangeCount >= 2, $"The expected connection status change count for {testDevices[i].Id}  should be 2 but was {amqpConnectionStatuses[i].ConnectionStatusChangeCount}");
                        }
                    }
                    Assert.AreEqual(ConnectionStatus.Disabled, amqpConnectionStatuses[i].LastConnectionStatus, $"The expected connection status should be {ConnectionStatus.Disabled} but was {amqpConnectionStatuses[i].LastConnectionStatus}");
                    Assert.AreEqual(ConnectionStatusChangeReason.Client_Close, amqpConnectionStatuses[i].LastConnectionStatusChangeReason, $"The expected connection status change reason should be {ConnectionStatusChangeReason.Client_Close} but was {amqpConnectionStatuses[i].LastConnectionStatusChangeReason}");
                }
            }
            finally
            {
                // Close the service-side components and dispose the device client instances.
                await cleanupOperation(deviceClients, testDeviceCallbackHandlers).ConfigureAwait(false);

                watch.Stop();

                TimeSpan timeToFinishFaultInjection = durationInSec.Subtract(watch.Elapsed);
                if (timeToFinishFaultInjection > TimeSpan.Zero)
                {
                    logger.Trace($"{nameof(FaultInjection)}: Waiting {timeToFinishFaultInjection}ms to ensure that FaultInjection duration passed.");
                    await Task.Delay(timeToFinishFaultInjection).ConfigureAwait(false);
                }
            }
        }
    }
}
