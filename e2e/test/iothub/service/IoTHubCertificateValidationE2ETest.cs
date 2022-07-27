// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Iothub.Service
{
    [TestClass]
    [Ignore("TODO: Enable when invalid cert server is back online.")]
    [TestCategory("InvalidServiceCertificate")]
    public class IoTHubCertificateValidationE2ETest : E2EMsTestBase
    {
        [LoggedTestMethod]
        public async Task RegistryManager_QueryDevicesInvalidServiceCertificateHttp_Fails()
        {
            using var rm = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionStringInvalidServiceCertificate);
            IQuery query = rm.CreateQuery("select * from devices");
            IotHubCommunicationException exception = await Assert.ThrowsExceptionAsync<IotHubCommunicationException>(
                () => query.GetNextAsTwinAsync()).ConfigureAwait(false);

#if NET472
            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#else
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
        }

        [LoggedTestMethod]
        public async Task ServiceClient_SendMessageToDeviceInvalidServiceCertificateAmqpTcp_Fails()
        {
            TransportType transport = TransportType.Amqp;
            await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestServiceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ServiceClient_SendMessageToDeviceInvalidServiceCertificateAmqpWs_Fails()
        {
            TransportType transport = TransportType.Amqp_WebSocket_Only;
            WebSocketException exception = await Assert.ThrowsExceptionAsync<WebSocketException>(
                () => TestServiceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);

            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
        }

        private static async Task TestServiceClientInvalidServiceCertificate(TransportType transport)
        {
            using var service = ServiceClient.CreateFromConnectionString(
                TestConfiguration.IoTHub.ConnectionStringInvalidServiceCertificate,
                transport);
            using var testMessage = new Message();
            await service.SendAsync("testDevice1", testMessage).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task JobClient_ScheduleTwinUpdateInvalidServiceCertificateHttp_Fails()
        {
            using var jobClient = JobClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionStringInvalidServiceCertificate);
            IotHubCommunicationException exception = await Assert.ThrowsExceptionAsync<IotHubCommunicationException>(
                () => jobClient.ScheduleTwinUpdateAsync(
                    "testDevice",
                    "DeviceId IN ['testDevice']",
                    new Twin(),
                    DateTime.UtcNow,
                    60)).ConfigureAwait(false);

#if NET472
            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#else
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
        }

        [LoggedTestMethod]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateAmqpTcp_Fails()
        {
            await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(new IotHubClientAmqpSettings())).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateMqttTcp_Fails()
        {
            await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(new IotHubClientMqttSettings())).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateHttp_Fails()
        {
            AuthenticationException exception = await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(new IotHubClientHttpSettings())).ConfigureAwait(false);

#if NET472
            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#else
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
        }

        [LoggedTestMethod]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateAmqpWs_Fails()
        {
            AuthenticationException exception = await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(new IotHubClientAmqpSettings(TransportProtocol.WebSocket))).ConfigureAwait(false);

            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
        }

        [LoggedTestMethod]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateMqttWs_Fails()
        {
            AuthenticationException exception = await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(new IotHubClientMqttSettings(TransportProtocol.WebSocket))).ConfigureAwait(false);

            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
        }

        private static async Task TestDeviceClientInvalidServiceCertificate(IotHubClientTransportSettings transportSettings)
        {
            using var deviceClient =
                IotHubDeviceClient.CreateFromConnectionString(
                    TestConfiguration.IoTHub.DeviceConnectionStringInvalidServiceCertificate,
                    new IotHubClientOptions (transportSettings));
            using var testMessage = new Client.Message();
            await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
