// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// If you see intermittent failures on devices that are created by this file, check to see if you have multiple suites
// running at the same time because one test run could be accidentally destroying devices created by a different test run.

namespace Microsoft.Azure.Devices.E2ETests.Helpers.Templates
{
    public static class FaultInjection
    {
        public const string FaultType_Tcp = "KillTcp";
        public const string FaultType_AmqpConn = "KillAmqpConnection";
        public const string FaultType_AmqpSess = "KillAmqpSession";
        public const string FaultType_AmqpCBSReq = "KillAmqpCBSLinkReq";
        public const string FaultType_AmqpCBSResp = "KillAmqpCBSLinkResp";
        public const string FaultType_AmqpD2C = "KillAmqpD2CLink";
        public const string FaultType_AmqpC2D = "KillAmqpC2DLink";
        public const string FaultType_AmqpTwinReq = "KillAmqpTwinLinkReq";
        public const string FaultType_AmqpTwinResp = "KillAmqpTwinLinkResp";
        public const string FaultType_AmqpMethodReq = "KillAmqpMethodReqLink";
        public const string FaultType_AmqpMethodResp = "KillAmqpMethodRespLink";
        public const string FaultType_Throttle = "InvokeThrottling";
        public const string FaultType_QuotaExceeded = "InvokeMaxMessageQuota";
        public const string FaultType_Auth = "InvokeAuthError";
        public const string FaultType_GracefulShutdownAmqp = "ShutDownAmqp";
        public const string FaultType_GracefulShutdownMqtt = "ShutDownMqtt";

        public const string FaultCloseReason_Boom = "boom";
        public const string FaultCloseReason_Bye = "byebye";

        public static readonly TimeSpan DefaultFaultDelay = TimeSpan.FromSeconds(5); // Time in seconds after service initiates the fault.
        public static readonly TimeSpan DefaultFaultDuration = TimeSpan.FromSeconds(5); // Duration in seconds
        public static readonly TimeSpan LatencyTimeBuffer = TimeSpan.FromSeconds(10); // Buffer time waiting fault occurs or connection recover

        public static readonly TimeSpan WaitForDisconnectDuration = TimeSpan.FromTicks(DefaultFaultDelay.Ticks * 3);
        public static readonly TimeSpan WaitForReconnectDuration = TimeSpan.FromTicks(DefaultFaultDuration.Ticks * 2);
        public static readonly TimeSpan ShortRetryDuration = TimeSpan.FromTicks(DefaultFaultDuration.Ticks / 2);

        public static readonly TimeSpan RecoveryTime = TimeSpan.FromMinutes(5);

        public static Client.Message ComposeErrorInjectionProperties(string faultType, string reason, TimeSpan delayInSecs, TimeSpan durationInSecs)
        {
            string dataBuffer = Guid.NewGuid().ToString();

            return new Client.Message(Encoding.UTF8.GetBytes(dataBuffer))
            {
                Properties =
                {
                    ["AzIoTHub_FaultOperationType"] = faultType,
                    ["AzIoTHub_FaultOperationCloseReason"] = reason,
                    ["AzIoTHub_FaultOperationDelayInSecs"] = delayInSecs.Seconds.ToString(CultureInfo.InvariantCulture),
                    ["AzIoTHub_FaultOperationDurationInSecs"] = durationInSecs.Seconds.ToString(CultureInfo.InvariantCulture)
                }
            };
        }

        public static bool FaultShouldDisconnect(string faultType)
        {
            return faultType != FaultType_Auth
                && faultType != FaultType_Throttle
                && faultType != FaultType_QuotaExceeded;
        }

        // Fault timings:
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //  --- device in normal operation --- | FaultRequested | --- <delayInSec> --- | --- Device in fault mode for <durationInSec> --- | --- device in normal operation ---
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static async Task ActivateFaultInjectionAsync(ITransportSettings transportSettings, string faultType, string reason, TimeSpan delay, TimeSpan duration, DeviceClient deviceClient, MsTestLogger logger)
        {
            logger.Trace($"{nameof(ActivateFaultInjectionAsync)}: Requesting fault injection type={faultType} reason={reason}, delay={delay}s, duration={DefaultFaultDuration}s");

            try
            {
                using var cts = new CancellationTokenSource(delay);
                using Client.Message faultInjectionMessage = ComposeErrorInjectionProperties(
                    faultType,
                    reason,
                    delay,
                    duration);

                await deviceClient.SendEventAsync(faultInjectionMessage, cts.Token).ConfigureAwait(false);
            }
            catch (IotHubCommunicationException ex)
            {
                logger.Trace($"{nameof(ActivateFaultInjectionAsync)}: {ex}");

                // For quota injection, the fault is only seen for the original HTTP request.
                if (transportSettings is Client.HttpTransportSettings)
                {
                    throw;
                }
            }
            catch (TimeoutException ex)
            {
                logger.Trace($"{nameof(ActivateFaultInjectionAsync)}: {ex}");

                // For quota injection, the fault is only seen for the original HTTP request.
                if (transportSettings is Client.HttpTransportSettings)
                {
                    throw;
                }
            }
            finally
            {
                logger.Trace($"{nameof(ActivateFaultInjectionAsync)}: Fault injection requested.");
            }
        }

        // Error injection template method.
        public static async Task TestErrorInjectionAsync(
            string devicePrefix,
            TestDeviceType type,
            ITransportSettings transportSettings,
            string proxyAddress,
            string faultType,
            string reason,
            TimeSpan delay,
            TimeSpan duration,
            Func<DeviceClient, TestDevice, Task> initOperation,
            Func<DeviceClient, TestDevice, Task> testOperation,
            Func<Task> cleanupOperation,
            MsTestLogger logger)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(logger, devicePrefix, type).ConfigureAwait(false);

            DeviceClient deviceClient = testDevice.CreateDeviceClient(new ClientOptions(transportSettings));

            ConnectionStatus? lastConnectionStatus = null;
            ConnectionStatusChangeReason? lastConnectionStatusChangeReason = null;
            int connectionStatusChangeCount = 0;

            deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
            {
                connectionStatusChangeCount++;
                lastConnectionStatus = status;
                lastConnectionStatusChangeReason = statusChangeReason;
                logger.Trace($"{nameof(FaultInjection)}.{nameof(ConnectionStatusChangesHandler)}: status={status} statusChangeReason={statusChangeReason} count={connectionStatusChangeCount}");
            });

            var watch = new Stopwatch();

            try
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                if (transportSettings is not Client.HttpTransportSettings)
                {
                    // Normally one connection but in some cases, due to network issues we might have already retried several times to connect.
                    connectionStatusChangeCount.Should().BeGreaterOrEqualTo(1);
                    lastConnectionStatus.Should().Be(ConnectionStatus.Connected);
                    lastConnectionStatusChangeReason.Should().Be(ConnectionStatusChangeReason.Connection_Ok);
                }

                if (initOperation != null)
                {
                    await initOperation(deviceClient, testDevice).ConfigureAwait(false);
                }

                logger.Trace($">>> {nameof(FaultInjection)} Testing baseline");
                await testOperation(deviceClient, testDevice).ConfigureAwait(false);

                int countBeforeFaultInjection = connectionStatusChangeCount;
                watch.Start();
                logger.Trace($">>> {nameof(FaultInjection)} Testing fault handling");
                await ActivateFaultInjectionAsync(transportSettings, faultType, reason, delay, duration, deviceClient, logger).ConfigureAwait(false);
                logger.Trace($"{nameof(FaultInjection)}: Waiting for fault injection to be active: {delay} seconds.");
                await Task.Delay(delay).ConfigureAwait(false);

                // For disconnect type faults, the device should disconnect and recover.
                if (FaultShouldDisconnect(faultType))
                {
                    logger.Trace($"{nameof(FaultInjection)}: Confirming fault injection has been activated.");
                    // Check that service issued the fault to the faulting device
                    bool isFaulted = false;

                    var sw = Stopwatch.StartNew();
                    while (sw.Elapsed < LatencyTimeBuffer)
                    {
                        if (connectionStatusChangeCount > countBeforeFaultInjection)
                        {
                            isFaulted = true;
                            break;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    sw.Reset();

                    Assert.IsTrue(isFaulted, $"The device {testDevice.Id} did not get faulted with fault type: {faultType}");
                    logger.Trace($"{nameof(FaultInjection)}: Confirmed fault injection has been activated.");

                    // Check the device is back online
                    logger.Trace($"{nameof(FaultInjection)}: Confirming device back online.");

                    sw.Start();
                    while (lastConnectionStatus != ConnectionStatus.Connected && sw.Elapsed < duration.Add(LatencyTimeBuffer))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    sw.Reset();

                    Assert.AreEqual(ConnectionStatus.Connected, lastConnectionStatus, $"{testDevice.Id} did not reconnect.");
                    logger.Trace($"{nameof(FaultInjection)}: Confirmed device back online.");

                    // Perform the test operation.
                    logger.Trace($">>> {nameof(FaultInjection)}: Performing test operation for device {testDevice.Id}.");
                    await testOperation(deviceClient, testDevice).ConfigureAwait(false);
                }
                else
                {
                    logger.Trace($"{nameof(FaultInjection)}: Performing test operation while fault injection is being activated.");
                    // Perform the test operation for the faulted device multi times.
                    int counter = 0;
                    var sw = Stopwatch.StartNew();
                    while (sw.Elapsed < LatencyTimeBuffer)
                    {
                        logger.Trace($">>> {nameof(FaultInjection)}: Performing test operation for device - Run {counter++}.");
                        await testOperation(deviceClient, testDevice).ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    sw.Reset();
                }

                await deviceClient.CloseAsync().ConfigureAwait(false);

                if (transportSettings is not Client.HttpTransportSettings)
                {
                    if (FaultShouldDisconnect(faultType))
                    {
                        // 4 is the minimum notification count: connect, fault, reconnect, disable.
                        // There are cases where the retry must be timed out (i.e. very likely for MQTT where otherwise
                        // we would attempt to send the fault injection forever.)
                        connectionStatusChangeCount.Should().BeGreaterOrEqualTo(4, $"Device Id {testDevice.Id}");
                    }
                    else
                    {
                        // 2 is the minimum notification count: connect, disable.
                        // We will monitor the test environment real network stability and switch to >=2 if necessary to
                        // account for real network issues.
                        connectionStatusChangeCount.Should().Be(2, $"Device Id {testDevice.Id}");
                    }
                    lastConnectionStatus.Should().Be(ConnectionStatus.Disabled, $"The connection status change reason was {lastConnectionStatusChangeReason}");
                    lastConnectionStatusChangeReason.Should().Be(ConnectionStatusChangeReason.Client_Close);
                }
            }
            finally
            {
                if (cleanupOperation != null)
                {
                    await cleanupOperation().ConfigureAwait(false);
                }
                logger.Trace($"{nameof(FaultInjection)}: Disposing deviceClient {TestLogger.GetHashCode(deviceClient)}");
                deviceClient.Dispose();

                watch.Stop();

                TimeSpan timeToFinishFaultInjection = duration.Subtract(watch.Elapsed);
                if (timeToFinishFaultInjection > TimeSpan.Zero)
                {
                    logger.Trace($"{nameof(FaultInjection)}: Waiting {timeToFinishFaultInjection}ms to ensure that FaultInjection duration passed.");
                    await Task.Delay(timeToFinishFaultInjection).ConfigureAwait(false);
                }
            }
        }
    }
}
