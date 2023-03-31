// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
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
            IotHubClientTransportSettings transportSettings,
            string proxyAddress,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            TimeSpan faultDelay,
            TimeSpan faultDuration,
            Func<TestDevice, TestDeviceCallbackHandler, CancellationToken, Task> initOperation,
            Func<TestDevice, TestDeviceCallbackHandler, CancellationToken, Task> testOperation,
            Func<List<TestDevice>, List<TestDeviceCallbackHandler>, CancellationToken, Task> cleanupOperation,
            CancellationToken ct)
        {
            var amqpTransportSettings = new IotHubClientAmqpSettings(transportSettings.Protocol)
            {
                ConnectionPoolSettings = new AmqpConnectionPoolSettings
                {
                    MaxPoolSize = unchecked((uint)poolSize),
                    UsePooling = true,
                },
                Proxy = proxyAddress == null ? null : new WebProxy(proxyAddress),
            };

            var testDevices = new List<TestDevice>(devicesCount);
            var testDeviceCallbackHandlers = new List<TestDeviceCallbackHandler>(devicesCount);
            var amqpConnectionStatuses = new List<AmqpConnectionStatusChange>();
            var operations = new List<Task>(devicesCount);

            // Arrange

            // Initialize the test device client instances.
            // Set the device client connection status change handler.
            VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)} Initializing device clients for multiplexing test.");
            for (int i = 0; i < devicesCount; i++)
            {
                TestDevice testDevice = await TestDevice.GetTestDeviceAsync($"{devicePrefix}_{i}_", ct: ct).ConfigureAwait(false);
                IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(amqpTransportSettings));

                var amqpConnectionStatusesChange = new AmqpConnectionStatusChange(testDevice.Id);
                deviceClient.ConnectionStatusChangeCallback = amqpConnectionStatusesChange.ConnectionStatusChangeHandler;
                await deviceClient.OpenAsync(ct).ConfigureAwait(false);

                var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);

                testDevices.Add(testDevice);
                testDeviceCallbackHandlers.Add(testDeviceCallbackHandler);
                amqpConnectionStatuses.Add(amqpConnectionStatusesChange);

                operations.Add(initOperation(testDevice, testDeviceCallbackHandler, ct));
            }
            await Task.WhenAll(operations).ConfigureAwait(false);
            operations.Clear();

            var faultInjectionDuration = new Stopwatch();

            try
            {
                // Act-Assert

                int countBeforeFaultInjection = amqpConnectionStatuses.First().ConnectionStatusChangeCount;

                // Inject the fault into device 0
                VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: {testDevices.First().Id} Requesting fault injection type={faultType} reason={reason}, delay={faultDelay}, duration={faultDuration}");
                faultInjectionDuration.Start();
                TelemetryMessage faultInjectionMessage = FaultInjection.ComposeErrorInjectionProperties(faultType, reason, faultDelay, faultDuration);
                await testDevices.First().DeviceClient.SendTelemetryAsync(faultInjectionMessage, ct).ConfigureAwait(false);

                VerboseTestLogger.WriteLine($"{nameof(FaultInjection)}: Waiting for fault injection to be active: {faultDelay} seconds.");
                await Task.Delay(faultDelay, ct).ConfigureAwait(false);

                // For disconnect type faults, the faulted device should disconnect and all devices should recover.
                if (FaultInjection.FaultShouldDisconnect(faultType))
                {
                    VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirming fault injection has been actived.");
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

                        await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
                    }
                    connectionChangeWaitDuration.Reset();

                    isFaulted.Should().BeTrue($"The device {testDevices.First().Id} did not get faulted with fault type: {faultType}");
                    VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirmed fault injection has been actived.");

                    // Check all devices are back online
                    VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirming all devices back online.");

                    connectionChangeWaitDuration.Start();
                    bool isRecovered = false;
                    while (connectionChangeWaitDuration.Elapsed < faultDuration.Add(FaultInjection.LatencyTimeBuffer))
                    {
                        isRecovered = testDevices.All(x => x.DeviceClient.ConnectionStatusInfo.Status == ConnectionStatus.Connected);
                        if (isRecovered)
                        {
                            break;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    }

                    if (!isRecovered)
                    {
                        var unconnectedDevices = new List<string>();
                        for (int i = 0; i < devicesCount; ++i)
                        {
                            if (testDevices[i].DeviceClient.ConnectionStatusInfo.Status != ConnectionStatus.Connected)
                            {
                                unconnectedDevices.Add(testDevices[i].Id);
                            }
                        }
                        Assert.Fail($"Some devices did not reconnect: {string.Join(", ", unconnectedDevices)}");
                    }

                    VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Confirmed all devices back online.");

                    // Perform the test operation for all devices
                    for (int i = 0; i < devicesCount; i++)
                    {
                        VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation for device {i}.");
                        operations.Add(testOperation(testDevices[i], testDeviceCallbackHandlers[i], ct));
                    }
                    await Task.WhenAll(operations).ConfigureAwait(false);
                    operations.Clear();
                }
                else
                {
                    VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation while fault injection is being activated.");
                    // Perform the test operation for the faulted device multiple times.
                    int counter = 0;
                    var runOperationUnderFaultInjectionDuration = Stopwatch.StartNew();
                    while (runOperationUnderFaultInjectionDuration.Elapsed < FaultInjection.LatencyTimeBuffer)
                    {
                        VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolingOverAmqp)}: Performing test operation for device 0 - Run {counter++}.");
                        await testOperation(testDevices[0], testDeviceCallbackHandlers[0], ct).ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await cleanupOperation(testDevices, testDeviceCallbackHandlers, ct).ConfigureAwait(false);

                testDeviceCallbackHandlers.ForEach(x => x.Dispose());
                await Task.WhenAll(testDevices.Select(x => x.DisposeAsync().AsTask())).ConfigureAwait(false);

                if (!FaultInjection.FaultShouldDisconnect(faultType))
                {
                    faultInjectionDuration.Stop();

                    TimeSpan timeToFinishFaultInjection = faultDuration.Subtract(faultInjectionDuration.Elapsed);
                    if (timeToFinishFaultInjection > TimeSpan.Zero)
                    {
                        VerboseTestLogger.WriteLine($"{nameof(FaultInjection)}: Waiting {timeToFinishFaultInjection} to ensure that FaultInjection duration passed.");
                        await Task.Delay(timeToFinishFaultInjection, ct).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
