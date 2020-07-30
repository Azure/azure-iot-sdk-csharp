// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
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

        public const int DefaultDelayInSec = 5; // Time in seconds after service initiates the fault.
        public const int DefaultDurationInSec = 5; // Duration in seconds
        public const int LatencyTimeBufferInSec = 10; // Buffer time waiting fault occurs or connection recover

        public const int WaitForDisconnectMilliseconds = 3 * DefaultDelayInSec * 1000;
        public const int WaitForReconnectMilliseconds = 2 * DefaultDurationInSec * 1000;
        public const int ShortRetryInMilliSec = (int)(DefaultDurationInSec / 2.0 * 1000);

        public const int RecoveryTimeMilliseconds = 5 * 60 * 1000;

        public static Client.Message ComposeErrorInjectionProperties(string faultType, string reason, int delayInSecs, int durationInSecs)
        {
            string dataBuffer = Guid.NewGuid().ToString();

            return new Client.Message(Encoding.UTF8.GetBytes(dataBuffer))
            {
                Properties =
                {
                    ["AzIoTHub_FaultOperationType"] = faultType,
                    ["AzIoTHub_FaultOperationCloseReason"] = reason,
                    ["AzIoTHub_FaultOperationDelayInSecs"] = delayInSecs.ToString(CultureInfo.InvariantCulture),
                    ["AzIoTHub_FaultOperationDurationInSecs"] = durationInSecs.ToString(CultureInfo.InvariantCulture)
                }
            };
        }

        public static bool FaultShouldDisconnect(string faultType)
        {
            return (faultType != FaultType_Auth) &&
                (faultType != FaultType_Throttle) &&
                (faultType != FaultType_QuotaExceeded);
        }

        // Fault timings:
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //  --- device in normal operation --- | FaultRequested | --- <delayInSec> --- | --- Device in fault mode for <durationInSec> --- | --- device in normal operation ---
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static async Task ActivateFaultInjectionAsync(Client.TransportType transport, string faultType, string reason, int delayInSec, int durationInSec, DeviceClient deviceClient, TestLogger logger)
        {
            logger.Trace($"{nameof(ActivateFaultInjectionAsync)}: Requesting fault injection type={faultType} reason={reason}, delay={delayInSec}s, duration={FaultInjection.DefaultDurationInSec}s");

            uint oldTimeout = deviceClient.OperationTimeoutInMilliseconds;

            try
            {
                // For MQTT FaultInjection will terminate the connection prior to a PUBACK
                // which leads to an infinite loop trying to resend the FaultInjection message.
                if (transport == Client.TransportType.Mqtt ||
                    transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    deviceClient.OperationTimeoutInMilliseconds = (uint)delayInSec * 1000;
                }

                await deviceClient
                    .SendEventAsync(
                        ComposeErrorInjectionProperties(
                            faultType,
                            reason,
                            delayInSec,
                            durationInSec))
                    .ConfigureAwait(false);
            }
            catch (IotHubCommunicationException ex)
            {
                logger.Trace($"{nameof(ActivateFaultInjectionAsync)}: {ex}");

                // For quota injection, the fault is only seen for the original HTTP request.
                if (transport == Client.TransportType.Http1) throw;
            }
            catch (TimeoutException ex)
            {
                logger.Trace($"{nameof(ActivateFaultInjectionAsync)}: {ex}");

                // For quota injection, the fault is only seen for the original HTTP request.
                if (transport == Client.TransportType.Http1) throw;
            }
            finally
            {
                deviceClient.OperationTimeoutInMilliseconds = oldTimeout;
                logger.Trace($"{nameof(ActivateFaultInjectionAsync)}: Fault injection requested.");
            }
        }

        // Error injection template method.
        public static async Task TestErrorInjectionAsync(
            string devicePrefix,
            TestDeviceType type,
            Client.TransportType transport,
            string faultType,
            string reason,
            int delayInSec,
            int durationInSec,
            Func<DeviceClient, TestDevice, Task> initOperation,
            Func<DeviceClient, TestDevice, Task> testOperation,
            Func<Task> cleanupOperation,
            TestLogger logger)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(logger, devicePrefix, type).ConfigureAwait(false);
            DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);

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
                if (transport != Client.TransportType.Http1)
                {
                    Assert.IsTrue(connectionStatusChangeCount >= 1, $"The expected connection status change should be equal or greater than 1 but was {connectionStatusChangeCount}"); // Normally one connection but in some cases, due to network issues we might have already retried several times to connect.
                    Assert.AreEqual(ConnectionStatus.Connected, lastConnectionStatus, $"The expected connection status should be {ConnectionStatus.Connected} but was {lastConnectionStatus}");
                    Assert.AreEqual(ConnectionStatusChangeReason.Connection_Ok, lastConnectionStatusChangeReason, $"The expected connection status change reason should be {ConnectionStatusChangeReason.Connection_Ok} but was {lastConnectionStatusChangeReason}");
                }

                await initOperation(deviceClient, testDevice).ConfigureAwait(false);

                logger.Trace($">>> {nameof(FaultInjection)} Testing baseline");
                await testOperation(deviceClient, testDevice).ConfigureAwait(false);

                int countBeforeFaultInjection = connectionStatusChangeCount;
                watch.Start();
                logger.Trace($">>> {nameof(FaultInjection)} Testing fault handling");
                await ActivateFaultInjectionAsync(transport, faultType, reason, delayInSec, durationInSec, deviceClient, logger).ConfigureAwait(false);
                logger.Trace($"{nameof(FaultInjection)}: Waiting for fault injection to be active: {delayInSec} seconds.");
                await Task.Delay(TimeSpan.FromSeconds(delayInSec)).ConfigureAwait(false);

                // For disconnect type faults, the device should disconnect and recover.
                if (FaultShouldDisconnect(faultType))
                {
                    logger.Trace($"{nameof(FaultInjection)}: Confirming fault injection has been actived.");
                    // Check that service issued the fault to the faulting device
                    bool isFaulted = false;
                    for (int i = 0; i < LatencyTimeBufferInSec; i++)
                    {
                        if (connectionStatusChangeCount > countBeforeFaultInjection)
                        {
                            isFaulted = true;
                            break;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }

                    Assert.IsTrue(isFaulted, $"The device {testDevice.Id} did not get faulted with fault type: {faultType}");
                    logger.Trace($"{nameof(FaultInjection)}: Confirmed fault injection has been actived.");

                    // Check the device is back online
                    logger.Trace($"{nameof(FaultInjection)}: Confirming device back online.");
                    for (int i = 0; lastConnectionStatus != ConnectionStatus.Connected && i < durationInSec + LatencyTimeBufferInSec; i++)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }

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
                    for (int i = 0; i < LatencyTimeBufferInSec; i++)
                    {
                        logger.Trace($">>> {nameof(FaultInjection)}: Performing test operation for device - Run {i}.");
                        await testOperation(deviceClient, testDevice).ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                }

                await deviceClient.CloseAsync().ConfigureAwait(false);

                if (transport != Client.TransportType.Http1)
                {
                    if (FaultInjection.FaultShouldDisconnect(faultType))
                    {
                        // 4 is the minimum notification count: connect, fault, reconnect, disable.
                        // There are cases where the retry must be timed out (i.e. very likely for MQTT where otherwise
                        // we would attempt to send the fault injection forever.)
                        Assert.IsTrue(connectionStatusChangeCount >= 4, $"The expected connection status change count for {testDevice.Id} should be equal or greater than 4 but was {connectionStatusChangeCount}");
                    }
                    else
                    {
                        // 2 is the minimum notification count: connect, disable.
                        // We will monitor the test environment real network stability and switch to >=2 if necessary to
                        // account for real network issues.
                        Assert.IsTrue(connectionStatusChangeCount == 2, $"The expected connection status change count for {testDevice.Id}  should be 2 but was {connectionStatusChangeCount}");
                    }
                    Assert.AreEqual(ConnectionStatus.Disabled, lastConnectionStatus, $"The expected connection status should be {ConnectionStatus.Disabled} but was {lastConnectionStatus}");
                    Assert.AreEqual(ConnectionStatusChangeReason.Client_Close, lastConnectionStatusChangeReason, $"The expected connection status change reason should be {ConnectionStatusChangeReason.Client_Close} but was {lastConnectionStatusChangeReason}");
                }
            }
            finally
            {
                await cleanupOperation().ConfigureAwait(false);
                logger.Trace($"{nameof(FaultInjection)}: Disposing deviceClient {TestLogger.GetHashCode(deviceClient)}");
                deviceClient.Dispose();

                watch.Stop();

                int timeToFinishFaultInjection = durationInSec * 1000 - (int)watch.ElapsedMilliseconds;
                if (timeToFinishFaultInjection > 0)
                {
                    logger.Trace($"{nameof(FaultInjection)}: Waiting {timeToFinishFaultInjection}ms to ensure that FaultInjection duration passed.");
                    await Task.Delay(timeToFinishFaultInjection).ConfigureAwait(false);
                }
            }
        }
    }
}
