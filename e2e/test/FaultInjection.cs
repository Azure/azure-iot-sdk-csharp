// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// If you see intermittent failures on devices that are created by this file, check to see if you have multiple suites 
// running at the same time because one test run could be accidentally destroying devices created by a different test run.

namespace Microsoft.Azure.Devices.E2ETests
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

        public const int DefaultDelayInSec = 1; // Time in seconds after service initiates the fault.
        public const int DefaultDurationInSec = 10; // Duration in seconds 
        public const int WaitForDisconnectMilliseconds = 3 * DefaultDelayInSec * 1000;
        public const int ShortRetryInMilliSec = (int)(DefaultDurationInSec / 2.0 * 1000);

        public const int RecoveryTimeMilliseconds = 5 * 60 * 1000;

        private static TestLogging s_log = TestLogging.GetInstance();

        public static Client.Message ComposeErrorInjectionProperties(string faultType, string reason, int delayInSecs, int durationInSecs)
        {
            string dataBuffer = Guid.NewGuid().ToString();

            return new Client.Message(Encoding.UTF8.GetBytes(dataBuffer))
            {
                Properties =
                {
                    ["AzIoTHub_FaultOperationType"] = faultType,
                    ["AzIoTHub_FaultOperationCloseReason"] = reason,
                    ["AzIoTHub_FaultOperationDelayInSecs"] = delayInSecs.ToString(),
                    ["AzIoTHub_FaultOperationDurationInSecs"] = durationInSecs.ToString()
                }
            };
        }

        // Fault timings:
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //  --- device in normal operation --- | FaultRequested | --- <delayInSec> --- | --- Device in fault mode for <durationInSec> --- | --- device in normal operation ---
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static async Task ActivateFaultInjection(Client.TransportType transport, string faultType, string reason, int delayInSec, int durationInSec, DeviceClient deviceClient)
        {
            s_log.WriteLine($"{nameof(ActivateFaultInjection)}: Requesting fault injection type={faultType} reason={reason}, delay={delayInSec}s, duration={FaultInjection.DefaultDurationInSec}s");

            uint oldTimeout = deviceClient.OperationTimeoutInMilliseconds;

            try
            {
                // For MQTT FaultInjection will terminate the connection prior to a PUBACK
                // which leads to an infinite loop trying to resend the FaultInjection message.
                if (transport == Client.TransportType.Mqtt ||
                    transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    deviceClient.OperationTimeoutInMilliseconds = 1000;
                }

                await deviceClient.SendEventAsync(
                    FaultInjection.ComposeErrorInjectionProperties(
                        faultType,
                        reason,
                        delayInSec,
                        durationInSec)).ConfigureAwait(false);
            }
            catch (TimeoutException ex)
            {
                s_log.WriteLine($"{nameof(ActivateFaultInjection)}: {ex}");
            }
            finally
            {
                deviceClient.OperationTimeoutInMilliseconds = oldTimeout;
                s_log.WriteLine($"{nameof(ActivateFaultInjection)}: Fault injection requested.");
            }
        }

        public static async Task TestErrorInjectionTemplate(
            string devicePrefix,
            TestDeviceType type,
            Client.TransportType transport,
            string faultType,
            string reason,
            int delayInSec,
            int durationInSec,
            Func<DeviceClient, TestDevice, Task> initOperation,
            Func<DeviceClient, TestDevice, Task> testOperation,
            Func<Task> cleanupOperation)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(devicePrefix, type).ConfigureAwait(false);
            DeviceClient deviceClient = testDevice.CreateDeviceClient(transport);

            ConnectionStatus? lastConnectionStatus = null;
            ConnectionStatusChangeReason? lastConnectionStatusChangeReason = null;
            int setConnectionStatusChangesHandlerCount = 0;

            deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
            {
                s_log.WriteLine($"{nameof(FaultInjection)}.{nameof(ConnectionStatusChangesHandler)}: status={status} statusChangeReason={statusChangeReason} count={setConnectionStatusChangesHandlerCount}");
                lastConnectionStatus = status;
                lastConnectionStatusChangeReason = statusChangeReason;
                setConnectionStatusChangesHandlerCount++;
            });

            var watch = new Stopwatch();

            try
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                if (transport != Client.TransportType.Http1)
                {
                    Assert.AreEqual(1, setConnectionStatusChangesHandlerCount);
                    Assert.AreEqual(ConnectionStatus.Connected, lastConnectionStatus);
                    Assert.AreEqual(ConnectionStatusChangeReason.Connection_Ok, lastConnectionStatusChangeReason);
                }

                await initOperation(deviceClient, testDevice).ConfigureAwait(false);

                s_log.WriteLine($">>> {nameof(FaultInjection)} Testing baseline");
                await testOperation(deviceClient, testDevice).ConfigureAwait(false);
                
                await ActivateFaultInjection(transport, faultType, reason, delayInSec, durationInSec, deviceClient).ConfigureAwait(false);

                s_log.WriteLine($">>> {nameof(FaultInjection)} Testing fault handling");
                watch.Start();
                s_log.WriteLine($"{nameof(FaultInjection)}: Waiting for fault injection to be active: {FaultInjection.WaitForDisconnectMilliseconds}ms");
                await Task.Delay(FaultInjection.WaitForDisconnectMilliseconds).ConfigureAwait(false);

                await testOperation(deviceClient, testDevice).ConfigureAwait(false);

                await deviceClient.CloseAsync().ConfigureAwait(false);

                if (transport == Client.TransportType.Mqtt ||
                    transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    // Our fault injection is only terminating the connection for MQTT. (HTTP is not connection-oriented, AMQP is not actually terminating the TCP layer.)
                    Assert.IsTrue(setConnectionStatusChangesHandlerCount >= 4);
                    Assert.AreEqual(ConnectionStatus.Disabled, lastConnectionStatus);
                    Assert.AreEqual(ConnectionStatusChangeReason.Client_Close, lastConnectionStatusChangeReason);
                }
            }
            finally
            {
                await cleanupOperation().ConfigureAwait(false);
                deviceClient.Dispose();

                watch.Stop();

                int timeToFinishFaultInjection = durationInSec * 1000 - (int)watch.ElapsedMilliseconds;
                if (timeToFinishFaultInjection > 0)
                {
                    s_log.WriteLine($"{nameof(FaultInjection)}: Waiting {timeToFinishFaultInjection}ms to ensure that FaultInjection duration passed.");
                    await Task.Delay(timeToFinishFaultInjection).ConfigureAwait(false);
                }
            }
        }
    }
}
