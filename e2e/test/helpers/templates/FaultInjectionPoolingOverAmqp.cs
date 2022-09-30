// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Helpers.Templates
{
    internal static class FaultInjectionPoolingOverAmqp
    {
        public static async Task TestFaultInjectionPoolAmqpAsync(
            string devicePrefix,
            Client.TransportType transport,
            string proxyAddress,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            TimeSpan faultDelay,
            TimeSpan faultDuration,
            Func<DeviceClient, TestDevice, TestDeviceCallbackHandler, Task> initOperation,
            Func<DeviceClient, TestDevice, TestDeviceCallbackHandler, Task> testOperation,
            Func<List<DeviceClient>, List<TestDeviceCallbackHandler>, Task> cleanupOperation,
            ConnectionStringAuthScope authScope,
            MsTestLogger logger)
        {
            var transportSettings = new ITransportSettings[]
            {
                new AmqpTransportSettings(transport)
                {
                    AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings
                    {
                        MaxPoolSize = unchecked((uint)poolSize),
                        Pooling = true,
                    },
                    Proxy = proxyAddress == null ? null : new WebProxy(proxyAddress),
                }
            };

            var testDevices = new List<TestDevice>(devicesCount);
            var deviceClients = new List<DeviceClient>(devicesCount);
            var testDeviceCallbackHandlers = new List<TestDeviceCallbackHandler>(devicesCount);
            var amqpConnectionStatuses = new List<AmqpConnectionStatusChange>(devicesCount);
            var operations = new List<Task>(devicesCount);

            // Arrange

            // Initialize the test device client instances.
            // Set the device client connection status change handler.
            logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)} Initializing device clients for multiplexing test.");
            for (int i = 0; i < devicesCount; i++)
            {
                TestDevice testDevice = await TestDevice.GetTestDeviceAsync(logger, $"{devicePrefix}_{i}_").ConfigureAwait(false);
                DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings, authScope);

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

            var faultInjectionDuration = new Stopwatch();

            try
            {
                // Act-Assert

                int countBeforeFaultInjection = amqpConnectionStatuses.First().ConnectionStatusChangeCount;

                // Inject the fault into device 0
                logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: {testDevices.First().Id} Requesting fault injection type={faultType} reason={reason}, delay={faultDelay}s, duration={faultDuration}s");
                faultInjectionDuration.Start();
                using Client.Message faultInjectionMessage = FaultInjection.ComposeErrorInjectionProperties(faultType, reason, faultDelay, faultDuration);
                await deviceClients.First().SendEventAsync(faultInjectionMessage).ConfigureAwait(false);

                logger.Trace($"{nameof(FaultInjection)}: Waiting for fault injection to be active: {faultDelay} seconds.");
                await Task.Delay(faultDelay).ConfigureAwait(false);

                // For disconnect type faults, the faulted device should disconnect and all devices should recover.
                if (FaultInjection.FaultShouldDisconnect(faultType))
                {
                    logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirming fault injection has been actived.");
                    // Check that service issued the fault to the faulting device [device 0]
                    bool isFaulted = false;

                    var connectionChangeWaitDuration = Stopwatch.StartNew();
                    while (connectionChangeWaitDuration.Elapsed < FaultInjection.LatencyTimeBuffer)
                    {
                        if (amqpConnectionStatuses.First().ConnectionStatusChangeCount > countBeforeFaultInjection)
                        {
                            isFaulted = true;
                            break;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    connectionChangeWaitDuration.Reset();

                    isFaulted.Should().BeTrue($"The device {testDevices.First().Id} did not get faulted with fault type: {faultType}");
                    logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirmed fault injection has been actived.");

                    // Check all devices are back online
                    logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirming all devices back online.");

                    connectionChangeWaitDuration.Start();
                    bool isRecovered = false;
                    while (connectionChangeWaitDuration.Elapsed < faultDuration.Add(FaultInjection.LatencyTimeBuffer))
                    {
                        isRecovered = amqpConnectionStatuses.All(x => x.LastConnectionStatus == ConnectionStatus.Connected);
                        if (isRecovered)
                        {
                            break;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }

                    if (!isRecovered)
                    {
                        var unconnectedDevices = new List<string>();
                        for (int i = 0; i < devicesCount; ++i)
                        {
                            if (amqpConnectionStatuses[i].LastConnectionStatus != ConnectionStatus.Connected)
                            {
                                unconnectedDevices.Add(testDevices[i].Id);
                            }
                        }
                        Assert.Fail($"Some devices did not reconnect: {string.Join(", ", unconnectedDevices)}");
                    }

                    logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirmed all devices back online.");

                    // Perform the test operation for all devices
                    for (int i = 0; i < devicesCount; i++)
                    {
                        logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation for device {i}.");
                        operations.Add(testOperation(deviceClients[i], testDevices[i], testDeviceCallbackHandlers[i]));
                    }
                    await Task.WhenAll(operations).ConfigureAwait(false);
                    operations.Clear();
                }
                else
                {
                    logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation while fault injection is being activated.");
                    // Perform the test operation for the faulted device multiple times.
                    int counter = 0;
                    var runOperationUnderFaultInjectionDuration = Stopwatch.StartNew();
                    while (runOperationUnderFaultInjectionDuration.Elapsed < FaultInjection.LatencyTimeBuffer)
                    {
                        logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation for device 0 - Run {counter++}.");
                        await testOperation(deviceClients[0], testDevices[0], testDeviceCallbackHandlers[0]).ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                }

                // Close all the device clients.
                await Task.WhenAll(deviceClients.Select(x => x.CloseAsync())).ConfigureAwait(false);
            }
            finally
            {
                await cleanupOperation(deviceClients, testDeviceCallbackHandlers).ConfigureAwait(false);

                testDeviceCallbackHandlers.ForEach(x => x.Dispose());
                deviceClients.ForEach(x => x.Dispose());
                await Task.WhenAll(testDevices.Select(x => x.RemoveDeviceAsync())).ConfigureAwait(false);

                if (!FaultInjection.FaultShouldDisconnect(faultType))
                {
                    faultInjectionDuration.Stop();

                    TimeSpan timeToFinishFaultInjection = faultDuration.Subtract(faultInjectionDuration.Elapsed);
                    if (timeToFinishFaultInjection > TimeSpan.Zero)
                    {
                        logger.Trace($"{nameof(FaultInjection)}: Waiting {timeToFinishFaultInjection} to ensure that FaultInjection duration passed.");
                        await Task.Delay(timeToFinishFaultInjection).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
