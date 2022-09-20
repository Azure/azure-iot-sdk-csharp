// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
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
            IotHubClientTransportSettings TransportSettings,
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
            var transportSettings  = new IotHubClientAmqpSettings(TransportSettings.Protocol)
            {
                ConnectionPoolSettings = new AmqpConnectionPoolSettings()
                {
                    MaxPoolSize = unchecked((uint)poolSize),
                    Pooling = true,
                },
                Proxy = proxyAddress == null ? null : new WebProxy(proxyAddress),
            };

            var testDevices = new List<TestDevice>(devicesCount);
            var deviceClients = new List<IotHubDeviceClient>(devicesCount);
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
                IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings), authScope);

                var amqpConnectionStatusesChange = new AmqpConnectionStatusChange(testDevice.Id, logger);
                deviceClient.SetConnectionStatusChangeHandler(amqpConnectionStatusesChange.ConnectionStatusChangeHandler);

                var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, logger);

                testDevices.Add(testDevice);
                deviceClients.Add(deviceClient);
                testDeviceCallbackHandlers.Add(testDeviceCallbackHandler);
                amqpConnectionStatuses.Add(amqpConnectionStatusesChange);

                operations.Add(initOperation(deviceClient, testDevice, testDeviceCallbackHandler));
            }
            await Task.WhenAll(operations).ConfigureAwait(false);
            operations.Clear();

            var faultInjectionDuration = new Stopwatch();

            try
            {
                // Act-Assert

                // Perform the test operation and verify the operation is successful.
                for (int i = 0; i < devicesCount; i++)
                {
                    logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: Performing baseline operation for device {i}.");
                    operations.Add(testOperation(deviceClients[i], testDevices[i], testDeviceCallbackHandlers[i]));
                }
                await Task.WhenAll(operations).ConfigureAwait(false);
                operations.Clear();

                int countBeforeFaultInjection = amqpConnectionStatuses.First().ConnectionStatusChangeCount;

                logger.Trace($"{nameof(FaultInjectionPoolingOverAmqp)}: {testDevices.First().Id} Requesting fault injection type={faultType} reason={reason}, delay={delayInSec}s, duration={durationInSec}s");
                using Client.Message faultInjectionMessage = FaultInjection.ComposeErrorInjectionProperties(faultType, reason, delayInSec, durationInSec);
                faultInjectionDuration.Start();
                await deviceClients.First().SendEventAsync(faultInjectionMessage).ConfigureAwait(false);

                logger.Trace($"{nameof(FaultInjection)}: Waiting for fault injection to be active: {delayInSec} seconds.");
                await Task.Delay(delayInSec).ConfigureAwait(false);

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
                    while (connectionChangeWaitDuration.Elapsed < durationInSec.Add(FaultInjection.LatencyTimeBuffer))
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

                // Verify the connection status change checks.
                // For all of the devices - last connection status should be "Disabled", with reason "ClientClose"
                for (int i = 0; i < devicesCount; i++)
                {
                    // For the faulted device [device 0] - verify the connection status change count.
                    if (i == 0)
                    {
                        if (FaultInjection.FaultShouldDisconnect(faultType))
                        {
                            // 4 is the minimum notification count: connect, fault, reconnect, disable.
                            amqpConnectionStatuses[i].ConnectionStatusChangeCount.Should().BeGreaterOrEqualTo(
                                4,
                                $"The expected connection status change count for {testDevices[i].Id} should equals or greater than 4 but was {amqpConnectionStatuses[i].ConnectionStatusChangeCount}");
                        }
                        else
                        {
                            // 2 is the minimum notification count: connect, disable.
                            amqpConnectionStatuses[i].ConnectionStatusChangeCount.Should().BeGreaterOrEqualTo(
                                2,
                                $"The expected connection status change count for {testDevices[i].Id}  should be 2 but was {amqpConnectionStatuses[i].ConnectionStatusChangeCount}");
                        }
                    }
                    amqpConnectionStatuses[i].LastConnectionStatus.Should().Be(
                        ConnectionStatus.Disabled,
                        $"With reason {amqpConnectionStatuses[i].LastConnectionStatusChangeReason}");
                    amqpConnectionStatuses[i].LastConnectionStatusChangeReason.Should().Be(ConnectionStatusChangeReason.Client_Close);
                }
            }
            finally
            {
                await cleanupOperation(deviceClients, testDeviceCallbackHandlers).ConfigureAwait(false);

                testDeviceCallbackHandlers.ForEach(x => x.Dispose());
                deviceClients.ForEach(x => x.Dispose());
                await Task.WhenAll(testDevices.Select(x => x.RemoveDeviceAsync())).ConfigureAwait(false);

                faultInjectionDuration.Stop();

                // Make sure we use up the remaining time that fault injection was requested for, as to not impact other tests.
                TimeSpan timeToFinishFaultInjection = durationInSec.Subtract(faultInjectionDuration.Elapsed);
                if (timeToFinishFaultInjection > TimeSpan.Zero)
                {
                    logger.Trace($"{nameof(FaultInjection)}: Waiting {timeToFinishFaultInjection} to ensure that FaultInjection duration passed.");
                    await Task.Delay(timeToFinishFaultInjection).ConfigureAwait(false);
                }
            }
        }
    }
}
