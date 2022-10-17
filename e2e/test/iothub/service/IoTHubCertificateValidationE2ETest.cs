// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("InvalidServiceCertificate")]
    public class IotHubCertificateValidationE2ETest : E2EMsTestBase
    {
        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ServiceClient_QueryDevicesInvalidServiceCertificateHttp_Fails()
        {
            using var sc = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionStringInvalidServiceCertificate);
            var exception = await Assert.ThrowsExceptionAsync<IotHubServiceException>(
                () => sc.Query.CreateAsync<Twin>("select * from devices")).ConfigureAwait(false);

#if NET472
            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#else
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ServiceClient_SendMessageToDeviceInvalidServiceCertificateAmqpTcp_Fails()
        {
            IotHubTransportProtocol protocol = IotHubTransportProtocol.Tcp;
            await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestServiceClientInvalidServiceCertificate(protocol)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ServiceClient_SendMessageToDeviceInvalidServiceCertificateAmqpWs_Fails()
        {
            IotHubTransportProtocol protocol = IotHubTransportProtocol.WebSocket;
            WebSocketException exception = await Assert.ThrowsExceptionAsync<WebSocketException>(
                () => TestServiceClientInvalidServiceCertificate(protocol)).ConfigureAwait(false);

            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
        }

        private static async Task TestServiceClientInvalidServiceCertificate(IotHubTransportProtocol protocol)
        {
            IotHubServiceClientOptions options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };
            using var service = new IotHubServiceClient(
                TestConfiguration.IotHub.ConnectionStringInvalidServiceCertificate, options);
            var testMessage = new Message();
            await service.Messages.OpenAsync().ConfigureAwait(false);
            await service.Messages.SendAsync("testDevice1", testMessage).ConfigureAwait(false);
            await service.Messages.CloseAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task JobClient_ScheduleTwinUpdateInvalidServiceCertificateHttp_Fails()
        {
            using var sc = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionStringInvalidServiceCertificate);
            var ScheduledTwinUpdateOptions = new ScheduledJobsOptions
            {
                JobId = "testDevice",
                MaxExecutionTime = TimeSpan.FromSeconds(60)
            };
            var exception = await Assert.ThrowsExceptionAsync<IotHubServiceException>(
                () => sc.ScheduledJobs.ScheduleTwinUpdateAsync(
                    "DeviceId IN ['testDevice']",
                    new Twin(),
                    DateTimeOffset.UtcNow,
                    ScheduledTwinUpdateOptions))
                .ConfigureAwait(false);

#if NET472
            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#else
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateAmqpTcp_Fails()
        {
            await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(new IotHubClientAmqpSettings())).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateMqttTcp_Fails()
        {
            await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(new IotHubClientMqttSettings())).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateAmqpWs_Fails()
        {
            AuthenticationException exception = await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket))).ConfigureAwait(false);

            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateMqttWs_Fails()
        {
            AuthenticationException exception = await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket))).ConfigureAwait(false);

            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
        }

        private static async Task TestDeviceClientInvalidServiceCertificate(IotHubClientTransportSettings transportSettings)
        {
            using var deviceClient =
                new IotHubDeviceClient(
                    TestConfiguration.IotHub.DeviceConnectionStringInvalidServiceCertificate,
                    new IotHubClientOptions(transportSettings));
            var testMessage = new OutgoingMessage();
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.SendTelemetryAsync(testMessage).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
