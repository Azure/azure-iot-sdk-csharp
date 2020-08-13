// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("FaultInjection")]
    public partial class MessageSendFaultInjectionTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"E2E_{nameof(MessageSendFaultInjectionTests)}_";

        [LoggedTestMethod]
        public async Task Message_TcpConnectionLossSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_TcpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_TcpConnectionLossSendRecovery_Mqtt()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Mqtt_Tcp_Only,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_TcpConnectionLossSendRecovery_MqttWs()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Mqtt_WebSocket_Only,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_AmqpConnectionLossSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjection.FaultType_AmqpConn,
                    "",
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_AmqpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjection.FaultType_AmqpConn,
                    "",
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_AmqpSessionLossSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjection.FaultType_AmqpSess,
                    "",
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_AmqpSessionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjection.FaultType_AmqpSess,
                    "",
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_AmqpD2CLinkDropSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjection.FaultType_AmqpD2C,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_AmqpD2CLinkDropSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjection.FaultType_AmqpD2C,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_ThrottledConnectionRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Flaky")]
        public async Task Message_ThrottledConnectionRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_Amqp()
        {
            try
            {
                await SendMessageRecoveryAsync(
                        TestDeviceType.Sasl,
                        Client.TransportType.Amqp_Tcp_Only,
                        FaultInjection.FaultType_Throttle,
                        FaultInjection.FaultCloseReason_Boom,
                        FaultInjection.DefaultDelayInSec,
                        FaultInjection.DefaultDurationInSec,
                        FaultInjection.ShortRetryInMilliSec)
                    .ConfigureAwait(false);

                Assert.Fail("None of the expected exceptions were thrown.");
            }
            catch (IotHubThrottledException) { }
            catch (IotHubCommunicationException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(OperationCanceledException));
            }
            catch (TimeoutException) { }
        }

        [LoggedTestMethod]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_AmqpWs()
        {
            try
            {
                await SendMessageRecoveryAsync(
                        TestDeviceType.Sasl,
                        Client.TransportType.Amqp_WebSocket_Only,
                        FaultInjection.FaultType_Throttle,
                        FaultInjection.FaultCloseReason_Boom,
                        FaultInjection.DefaultDelayInSec,
                        FaultInjection.DefaultDurationInSec,
                        FaultInjection.ShortRetryInMilliSec)
                    .ConfigureAwait(false);
                Assert.Fail("None of the expected exceptions were thrown.");
            }
            catch (IotHubThrottledException) { }
            catch (IotHubCommunicationException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(OperationCanceledException));
            }
            catch (TimeoutException) { }
        }

        [LoggedTestMethod]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_Http()
        {
            try
            {
                await SendMessageRecoveryAsync(
                        TestDeviceType.Sasl,
                        Client.TransportType.Http1,
                        FaultInjection.FaultType_Throttle,
                        FaultInjection.FaultCloseReason_Boom,
                        FaultInjection.DefaultDelayInSec,
                        FaultInjection.DefaultDurationInSec,
                        FaultInjection.ShortRetryInMilliSec)
                    .ConfigureAwait(false);

                Assert.Fail("None of the expected exceptions were thrown.");
            }
            catch (IotHubThrottledException) { }
            catch (IotHubCommunicationException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(OperationCanceledException));
            }
            catch (TimeoutException) { }
        }

        [LoggedTestMethod]
        [DoNotParallelize]
        [ExpectedException(typeof(DeviceMaximumQueueDepthExceededException))]
        public async Task Message_QuotaExceededRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [ExpectedException(typeof(DeviceMaximumQueueDepthExceededException))]
        [DoNotParallelize]
        [TestCategory("LongRunning")]
        public async Task Message_QuotaExceededRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjection.FaultType_QuotaExceeded,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DoNotParallelize]
        public async Task Message_QuotaExceededRecovery_Http()
        {
            try
            {
                await SendMessageRecoveryAsync(
                        TestDeviceType.Sasl,
                        Client.TransportType.Http1,
                        FaultInjection.FaultType_QuotaExceeded,
                        FaultInjection.FaultCloseReason_Boom,
                        FaultInjection.DefaultDelayInSec,
                        FaultInjection.DefaultDurationInSec,
                        FaultInjection.ShortRetryInMilliSec)
                    .ConfigureAwait(false);

                Assert.Fail("None of the expected exceptions were thrown.");
            }
            catch (QuotaExceededException) { }
            catch (IotHubCommunicationException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(OperationCanceledException));
            }
            catch (TimeoutException) { }
        }

        [LoggedTestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task Message_AuthenticationRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task Message_AuthenticationRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task Message_AuthenticationWontRecover_Http()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Http1,
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.RecoveryTimeMilliseconds)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_GracefulShutdownSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_GracefulShutdownSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_GracefulShutdownSendRecovery_Mqtt()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Mqtt_Tcp_Only,
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_GracefulShutdownSendRecovery_MqttWs()
        {
            await SendMessageRecoveryAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Mqtt_WebSocket_Only,
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultDelayInSec)
                .ConfigureAwait(false);
        }

        internal async Task SendMessageRecoveryAsync(
            TestDeviceType type,
            Client.TransportType transport,
            string faultType,
            string reason,
            int delayInSec,
            int durationInSec = FaultInjection.DefaultDurationInSec,
            int retryDurationInMilliSec = FaultInjection.RecoveryTimeMilliseconds)
        {
            Func<DeviceClient, TestDevice, Task> init = (deviceClient, testDevice) =>
            {
                deviceClient.OperationTimeoutInMilliseconds = (uint)retryDurationInMilliSec;
                return Task.FromResult(0);
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                (Client.Message testMessage, string payload, string p1Value) = MessageSendE2ETests.ComposeD2cTestMessage(Logger);
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);
            };

            await FaultInjection
                .TestErrorInjectionAsync(
                    _devicePrefix,
                    type,
                    transport,
                    faultType,
                    reason,
                    delayInSec,
                    durationInSec,
                    init,
                    testOperation,
                    () => { return Task.FromResult(false); },
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
