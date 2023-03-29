// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;

// If you see intermittent failures on devices that are created by this file, check to see if you have multiple suites
// running at the same time because one test run could be accidentally destroying devices created by a different test run.

namespace Microsoft.Azure.Devices.E2ETests.Helpers.Templates
{
    internal static class FaultInjection
    {
        public static readonly TimeSpan DefaultFaultDelay = TimeSpan.FromSeconds(1); // Time in seconds after service initiates the fault.
        public static readonly TimeSpan DefaultFaultDuration = TimeSpan.FromSeconds(5); // Duration in seconds
        public static readonly TimeSpan LatencyTimeBuffer = TimeSpan.FromSeconds(10); // Buffer time waiting fault occurs or connection recover

        public static readonly TimeSpan WaitForDisconnectDuration = TimeSpan.FromTicks(DefaultFaultDelay.Ticks * 3);
        public static readonly TimeSpan WaitForReconnectDuration = TimeSpan.FromTicks(DefaultFaultDuration.Ticks * 2);
        public static readonly TimeSpan ShortRetryDuration = TimeSpan.FromTicks(DefaultFaultDuration.Ticks / 2);

        public static readonly TimeSpan RecoveryTime = TimeSpan.FromMinutes(5);

        public static TelemetryMessage ComposeErrorInjectionProperties(
            string faultType,
            string reason,
            TimeSpan faultDelay,
            TimeSpan faultDuration)
        {
            return new TelemetryMessage(Guid.NewGuid().ToString())
            {
                Properties =
                {
                    ["AzIoTHub_FaultOperationType"] = faultType,
                    ["AzIoTHub_FaultOperationCloseReason"] = reason,
                    ["AzIoTHub_FaultOperationDelayInSecs"] = faultDelay.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                    ["AzIoTHub_FaultOperationDurationInSecs"] = faultDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture)
                }
            };
        }

        internal static bool FaultShouldDisconnect(string faultType)
        {
            return faultType != FaultInjectionConstants.FaultType_Auth
                && faultType != FaultInjectionConstants.FaultType_Throttle
                && faultType != FaultInjectionConstants.FaultType_QuotaExceeded;
        }

        // Fault timings:
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //  --- device in normal operation --- | FaultRequested | --- <delayInSec> --- | --- Device in fault mode for <durationInSec> --- | --- device in normal operation ---
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------
        internal static async Task ActivateFaultInjectionAsync(
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            TimeSpan faultDelay,
            TimeSpan faultDuration,
            IotHubDeviceClient deviceClient)
        {
            VerboseTestLogger.WriteLine($"{nameof(ActivateFaultInjectionAsync)}: Requesting fault injection type={faultType} reason={reason}, delay={faultDelay}, duration={DefaultFaultDuration}");

            try
            {
                using var cts = new CancellationTokenSource(LatencyTimeBuffer);
                TelemetryMessage faultInjectionMessage = ComposeErrorInjectionProperties(
                    faultType,
                    reason,
                    faultDelay,
                    faultDuration);

                await deviceClient.SendTelemetryAsync(faultInjectionMessage, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IotHubClientException hubEx && hubEx.IsTransient)
            {
                VerboseTestLogger.WriteLine($"{nameof(ActivateFaultInjectionAsync)}: {ex}");

                // For quota injection, the fault is only seen for the original HTTP request.
                if (transportSettings is IotHubClientHttpSettings)
                {
                    throw;
                }
            }
            catch (OperationCanceledException ex) when (transportSettings is IotHubClientMqttSettings)
            {
                // For MQTT, FaultInjection will terminate the connection prior to a PUBACK
                // which leads to an infinite loop trying to resend the FaultInjection message, which will eventually throw an OperationCanceledException.
                // We will suppress this exception.
                VerboseTestLogger.WriteLine($"{nameof(ActivateFaultInjectionAsync)}.{nameof(ActivateFaultInjectionAsync)} over MQTT (suppressed): {ex}");
            }
            finally
            {
                VerboseTestLogger.WriteLine($"{nameof(ActivateFaultInjectionAsync)}: Fault injection requested.");
            }
        }

        // Error injection template method.
        internal static async Task TestErrorInjectionAsync(
            string devicePrefix,
            TestDeviceType type,
            IotHubClientTransportSettings transportSettings,
            string proxyAddress,
            string faultType,
            string reason,
            TimeSpan faultDelay,
            TimeSpan faultDuration,
            Func<IotHubDeviceClient, TestDevice, Task> initOperation,
            Func<IotHubDeviceClient, TestDevice, Task> testOperation,
            Func<Task> cleanupOperation)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(devicePrefix, type).ConfigureAwait(false);

            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings));

            int connectionStatusChangeCount = 0;

            void OnConnectionStatusChanged(ConnectionStatusInfo connectionStatusInfo)
            {
                connectionStatusChangeCount++;
                VerboseTestLogger.WriteLine($"{nameof(FaultInjection)}.{nameof(TestErrorInjectionAsync)}: status={connectionStatusInfo.Status} statusChangeReason={connectionStatusInfo.ChangeReason} count={connectionStatusChangeCount}");
            }

            deviceClient.ConnectionStatusChangeCallback = OnConnectionStatusChanged;

            var faultInjectionDuration = new Stopwatch();

            try
            {
                await testDevice.OpenWithRetryAsync().ConfigureAwait(false);

                await initOperation(deviceClient, testDevice).ConfigureAwait(false);

                int countBeforeFaultInjection = connectionStatusChangeCount;
                VerboseTestLogger.WriteLine($"{nameof(FaultInjection)} Testing fault handling");
                faultInjectionDuration.Start();
                await ActivateFaultInjectionAsync(transportSettings, faultType, reason, faultDelay, faultDuration, deviceClient).ConfigureAwait(false);
                VerboseTestLogger.WriteLine($"{nameof(FaultInjection)}: Waiting for fault injection to be active: {faultDelay} seconds.");
                await Task.Delay(faultDelay).ConfigureAwait(false);

                // For disconnect type faults, the device should disconnect and recover.
                if (FaultShouldDisconnect(faultType))
                {
                    VerboseTestLogger.WriteLine($"{nameof(FaultInjection)}: Confirming fault injection has been activated.");
                    // Check that service issued the fault to the faulting device
                    bool isFaulted = false;

                    var connectionChangeWaitDuration = Stopwatch.StartNew();
                    while (connectionChangeWaitDuration.Elapsed < LatencyTimeBuffer)
                    {
                        if (connectionStatusChangeCount > countBeforeFaultInjection)
                        {
                            isFaulted = true;
                            break;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    connectionChangeWaitDuration.Reset();

                    isFaulted.Should().BeTrue($"The device {testDevice.Id} did not get faulted with fault type: {faultType}");
                    VerboseTestLogger.WriteLine($"{nameof(FaultInjection)}: Confirmed fault injection has been activated.");

                    // Check the device is back online
                    VerboseTestLogger.WriteLine($"{nameof(FaultInjection)}: Confirming device back online.");

                    connectionChangeWaitDuration.Start();
                    while (deviceClient.ConnectionStatusInfo.Status != ConnectionStatus.Connected
                        && faultInjectionDuration.Elapsed < faultDuration.Add(LatencyTimeBuffer))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    connectionChangeWaitDuration.Reset();

                    deviceClient.ConnectionStatusInfo.Status.Should().Be(ConnectionStatus.Connected, $"{testDevice.Id} did not reconnect");

                    VerboseTestLogger.WriteLine($"{nameof(FaultInjection)}: Confirmed device back online.");

                    // Perform the test operation.
                    VerboseTestLogger.WriteLine($"{nameof(FaultInjection)}: Performing test operation for device {testDevice.Id}.");
                    await testOperation(deviceClient, testDevice).ConfigureAwait(false);
                }
                else
                {
                    VerboseTestLogger.WriteLine($"{nameof(FaultInjection)}: Performing test operation while fault injection is being activated.");
                    // Perform the test operation for the faulted device multi times.
                    int counter = 0;
                    var sw = Stopwatch.StartNew();
                    while (sw.Elapsed < LatencyTimeBuffer)
                    {
                        VerboseTestLogger.WriteLine($"{nameof(FaultInjection)}: Performing test operation for device - Run {counter++}.");
                        await testOperation(deviceClient, testDevice).ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    sw.Reset();
                }
            }
            finally
            {
                if (cleanupOperation != null)
                {
                    await cleanupOperation().ConfigureAwait(false);
                }

                if (!FaultShouldDisconnect(faultType))
                {
                    faultInjectionDuration.Stop();

                    TimeSpan timeToFinishFaultInjection = faultDuration.Subtract(faultInjectionDuration.Elapsed);
                    if (timeToFinishFaultInjection > TimeSpan.Zero)
                    {
                        VerboseTestLogger.WriteLine($"{nameof(FaultInjection)}: Waiting {timeToFinishFaultInjection} to ensure that FaultInjection duration passed.");
                        await Task.Delay(timeToFinishFaultInjection).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
