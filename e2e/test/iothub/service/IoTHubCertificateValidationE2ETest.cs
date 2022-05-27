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
                    new Shared.Twin(),
                    DateTime.UtcNow,
                    60)).ConfigureAwait(false);

#if NET472
            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#else
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
        }

        [LoggedTestMethod]
        public async Task DeviceClient_SendAsyncInvalidServiceCertificateAmqpTcp_Fails()
        {
            Client.TransportType transport = Client.TransportType.Amqp_Tcp_Only;
            await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceClient_SendAsyncInvalidServiceCertificateMqttTcp_Fails()
        {
            Client.TransportType transport = Client.TransportType.Mqtt_Tcp_Only;
            await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DeviceClient_SendAsyncInvalidServiceCertificateHttp_Fails()
        {
            Client.TransportType transport = Client.TransportType.Http1;
            AuthenticationException exception = await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);

#if NET472
            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#else
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
        }

        [LoggedTestMethod]
        public async Task DeviceClient_SendAsyncInvalidServiceCertificateAmqpWs_Fails()
        {
            Client.TransportType transport = Client.TransportType.Amqp_WebSocket_Only;
            AuthenticationException exception = await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);

            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
        }

        [LoggedTestMethod]
        public async Task DeviceClient_SendAsyncInvalidServiceCertificateMqttWs_Fails()
        {
            Client.TransportType transport = Client.TransportType.Mqtt_WebSocket_Only;
            AuthenticationException exception = await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);

            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
        }

        private static async Task TestDeviceClientInvalidServiceCertificate(Client.TransportType transport)
        {
            using var deviceClient =
                DeviceClient.CreateFromConnectionString(
                    TestConfiguration.IoTHub.DeviceConnectionStringInvalidServiceCertificate,
                    transport);
            using var testMessage = new Client.Message();
            await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
