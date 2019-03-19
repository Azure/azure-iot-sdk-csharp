// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
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
        public const int DefaultDurationInSec = 5; // Duration in seconds 

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
                    ["AzIoTHub_FaultOperationDelayInSecs"] = delayInSecs.ToString(CultureInfo.InvariantCulture),
                    ["AzIoTHub_FaultOperationDurationInSecs"] = durationInSecs.ToString(CultureInfo.InvariantCulture)
                }
            };
        }

        public static bool FaultShouldDisconnect(string faultType)
        {
            return
                (faultType != FaultType_Throttle) &&
                (faultType != FaultType_QuotaExceeded);
        }

        public static bool FaultShouldRecover(string faultType)
        {
            return
                (faultType != FaultType_Auth) &&
                (faultType != FaultType_Throttle) &&
                (faultType != FaultType_QuotaExceeded);
        }

        public static List<Type> GetExpectedExceptions(string faultType)
        {
            switch (faultType)
            {
                case FaultType_Auth: return new List<Type> { typeof(UnauthorizedException) };
                case FaultType_Throttle: return new List<Type>
                                                    {
                                                        typeof(IotHubThrottledException),
                                                        typeof(TimeoutException),
                                                        typeof(IotHubCommunicationException)
                                                    };
                case FaultType_QuotaExceeded: return new List<Type> { typeof(DeviceMaximumQueueDepthExceededException) };
                default: return new List<Type> { };
            }
        }

        // WIP: che3ck if required
        public static bool FaultShouldDisconnect_MODIFIED(string faultType)
        {
            return
                (faultType != FaultType_Throttle) &&
                (faultType != FaultType_QuotaExceeded) &&
                (faultType != FaultType_AmqpC2D) &&
                (faultType != FaultType_AmqpD2C) &&
                // AMQP link/session disconnect will not disconnect the associated AMQP connection for non-X509 connections (sas single, sas mux)
                (faultType != FaultType_AmqpSess) &&
                (faultType != FaultType_AmqpMethodReq) &&
                (faultType != FaultType_AmqpMethodResp);
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
                    deviceClient.OperationTimeoutInMilliseconds = (uint)delayInSec * 1000;
                }

                await deviceClient.SendEventAsync(
                    FaultInjection.ComposeErrorInjectionProperties(
                        faultType,
                        reason,
                        delayInSec,
                        durationInSec)).ConfigureAwait(false);
            }
            catch (IotHubCommunicationException ex)
            {
                s_log.WriteLine($"{nameof(ActivateFaultInjection)}: {ex}");

                // For quota injection, the fault is only seen for the original HTTP request.
                if (transport == Client.TransportType.Http1) throw;
            }
            catch (TimeoutException ex)
            {
                s_log.WriteLine($"{nameof(ActivateFaultInjection)}: {ex}");
                
                // For quota injection, the fault is only seen for the original HTTP request.
                if (transport == Client.TransportType.Http1) throw;
            }
            finally
            {
                deviceClient.OperationTimeoutInMilliseconds = oldTimeout;
                s_log.WriteLine($"{nameof(ActivateFaultInjection)}: Fault injection requested.");
            }
        }

        // Error injection template method (single device connection).
        public static async Task TestErrorInjectionSingleDeviceAsync(
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

            try
            {
                await TestErrorInjection(deviceClient, testDevice, transport, faultType, reason, delayInSec, durationInSec, initOperation, testOperation).ConfigureAwait(false);
            }
            finally
            {
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await cleanupOperation().ConfigureAwait(false);

                s_log.WriteLine($"{nameof(FaultInjection)}: Disposing deviceClient {TestLogging.GetHashCode(deviceClient)}");
                deviceClient.Dispose();
            }
        }

        // Error injection template method (multiplexing over amqp).
        public static async Task TestErrorInjectionMuxedOverAmqpAsync(
            string devicePrefix,
            ConnectionStringAuthScope authScope,
            TestDeviceType type,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            int delayInSec,
            int durationInSec,
            Func<DeviceClient, TestDevice, Task> initOperation,
            Func<DeviceClient, TestDevice, Task> testOperation,
            Func<Task> cleanupOperation)
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

            IList<DeviceClient> deviceClients = new List<DeviceClient>();
            // WIP: Added to debug muxed devices recovery after fault
            IList<TestDevice> testDevices = new List<TestDevice>();

            try
            {
                s_log.WriteLine($"{nameof(FaultInjection)}: Starting the test execution for {devicesCount} devices");

                for (int i = 0; i < devicesCount; i++)
                {
                    TestDevice testDevice = await TestDevice.GetTestDeviceAsync($"{devicePrefix}_{i}_", type).ConfigureAwait(false);
                    DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings, authScope);
                    deviceClients.Add(deviceClient);
                    // WIP: Added to debug muxed devices recovery after fault
                    testDevices.Add(testDevice);

                    await TestErrorInjection(deviceClient, testDevice, transport, faultType, reason, delayInSec, durationInSec, initOperation, testOperation).ConfigureAwait(false);
                }
            }
            finally
            {
                int count = 0;

                // Close and dispose all of the device client instances here
                // WIP: Added to debug muxed devices recovery after fault
                //foreach (DeviceClient deviceClient in deviceClients)
                for (int i=0; i<deviceClients.Count; i++)
                {
                    // WIP: Added to debug muxed devices recovery after fault
                    count++;
                    var deviceClient = deviceClients[i];
                    var testDevice = testDevices[i];

                    s_log.WriteLine($"{nameof(FaultInjection)}: Test baseline again deviceClient {TestLogging.GetHashCode(deviceClient)}");
                    await initOperation(deviceClient, testDevice).ConfigureAwait(false);
                    await testOperation(deviceClient, testDevice).ConfigureAwait(false);

                    s_log.WriteLine($"{nameof(FaultInjection)}: Count {count} - Closing deviceClient {TestLogging.GetHashCode(deviceClient)}");
                    await deviceClient.CloseAsync().ConfigureAwait(false);
                    await cleanupOperation().ConfigureAwait(false);

                    s_log.WriteLine($"{nameof(FaultInjection)}: Disposing deviceClient {TestLogging.GetHashCode(deviceClient)}");
                    deviceClient.Dispose();
                }
            }
        }

        // Error injection template method.
        public static async Task TestErrorInjection(
            DeviceClient deviceClient,
            TestDevice testDevice,
            Client.TransportType transport,
            string faultType,
            string reason,
            int delayInSec,
            int durationInSec,
            Func<DeviceClient, TestDevice, Task> initOperation,
            Func<DeviceClient, TestDevice, Task> testOperation)
        {
            List<Type> expectedExceptions = GetExpectedExceptions(faultType);

            ConnectionStatus? lastConnectionStatus = null;
            ConnectionStatusChangeReason? lastConnectionStatusChangeReason = null;
            int setConnectionStatusChangesHandlerCount = 0;

            deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
            {
                setConnectionStatusChangesHandlerCount++;
                lastConnectionStatus = status;
                lastConnectionStatusChangeReason = statusChangeReason;
                s_log.WriteLine($"{nameof(FaultInjection)}.{nameof(ConnectionStatusChangesHandler)}: {TestLogging.GetHashCode(deviceClient)}: status={status} statusChangeReason={statusChangeReason} count={setConnectionStatusChangesHandlerCount}");
            });

            var watch = new Stopwatch();

            try
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                if (transport != Client.TransportType.Http1)
                {
                    Assert.IsTrue(setConnectionStatusChangesHandlerCount >= 1); // Normally one connection but in some cases, due to network issues we might have already retried several times to connect.
                    Assert.AreEqual(ConnectionStatus.Connected, lastConnectionStatus);
                    Assert.AreEqual(ConnectionStatusChangeReason.Connection_Ok, lastConnectionStatusChangeReason);
                }

                await initOperation(deviceClient, testDevice).ConfigureAwait(false);

                s_log.WriteLine($">>> {nameof(FaultInjection)} Testing baseline");
                await testOperation(deviceClient, testDevice).ConfigureAwait(false);

                watch.Start();
                s_log.WriteLine($">>> {nameof(FaultInjection)} Testing fault handling");
                await ActivateFaultInjection(transport, faultType, reason, delayInSec, durationInSec, deviceClient).ConfigureAwait(false);

                int delay = FaultInjection.WaitForDisconnectMilliseconds - (int)watch.ElapsedMilliseconds;
                if (delay < 0) delay = 0;

                s_log.WriteLine($"{nameof(FaultInjection)}: Waiting for fault injection to be active: {delay}ms");
                await Task.Delay(delay).ConfigureAwait(false);

                s_log.WriteLine($">>> {nameof(FaultInjection)} Testing operation after fault recovery");
                await testOperation(deviceClient, testDevice).ConfigureAwait(false);

                if (transport != Client.TransportType.Http1)
                {
                    s_log.WriteLine($"Current count of connection status for transport type {transport} is: {setConnectionStatusChangesHandlerCount}.");
                    if (FaultShouldDisconnect(faultType))
                    {
                        // 3 is the minimum notification count: connect, fault, reconnect.
                        // There are cases where the retry must be timed out (i.e. very likely for MQTT where otherwise 
                        // we would attempt to send the fault injection forever.)
                        Assert.IsTrue(setConnectionStatusChangesHandlerCount >= 3);
                    }
                    else
                    {
                        // 1 is the minimum notification count: connect.
                        // We will monitor the test environment real network stability and switch to >=1 if necessary to 
                        // account for real network issues.
                        Assert.IsTrue(setConnectionStatusChangesHandlerCount == 1);
                    }
                }

                watch.Stop();

                int timeToFinishFaultInjection = durationInSec * 1000 - (int)watch.ElapsedMilliseconds;
                if (timeToFinishFaultInjection > 0)
                {
                    s_log.WriteLine($"{nameof(FaultInjection)}: Waiting {timeToFinishFaultInjection}ms to ensure that FaultInjection duration passed.");
                    await Task.Delay(timeToFinishFaultInjection).ConfigureAwait(false);
                }

                // WIP: Assert that AuthError and MaxQuota ONLY are actually throwing exceptions
                // throttling may or may not throw exception??
                if (faultType == FaultType_Auth || faultType == FaultType_QuotaExceeded)
                {
                    throw new Exception($"Exception expected for deviceOd {testDevice.Id} with fault type {faultType}");
                }
            }
            catch (Exception ex)
            {
                if (FaultShouldRecover(faultType))
                {
                    Assert.Fail($"Exception thrown for deviceId {testDevice.Id}: {ex}");
                }
                else
                {
                    if (!expectedExceptions.Contains(ex.GetType()))
                    {
                        Assert.Fail($"Expected exception for {faultType} was not thrown for deviceId {testDevice.Id}: {ex}");
                    }
                }
            }
        }
    }
}
