// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("InvalidServiceCertificate")]
    public class IoTHubCertificateValidationE2ETest : IDisposable
    {
        private readonly TestLogging _log = TestLogging.GetInstance();
        private readonly ConsoleEventListener _listener;

        public IoTHubCertificateValidationE2ETest()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task RegistryManager_QueryDevicesInvalidServiceCertificateHttp_Fails()
        {
            var rm = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionStringInvalidServiceCertificate);
            IQuery query = rm.CreateQuery("select * from devices");
            IotHubCommunicationException exception = await Assert.ThrowsExceptionAsync<IotHubCommunicationException>(
                () => query.GetNextAsTwinAsync()).ConfigureAwait(false);
        }

#if !NETCOREAPP1_1 // web sockets are not yet supported for UWP

        [TestMethod]
        public async Task ServiceClient_SendMessageToDeviceInvalidServiceCertificateAmqpTcp_Fails()
        {
            var transport = TransportType.Amqp;
            await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestServiceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ServiceClient_SendMessageToDeviceInvalidServiceCertificateAmqpWs_Fails()
        {
            var transport = TransportType.Amqp_WebSocket_Only;
            var exception = await Assert.ThrowsExceptionAsync<WebSocketException>(
                () => TestServiceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);

            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
        }

#endif

        [TestMethod]
        public async Task JobClient_ScheduleTwinUpdateInvalidServiceCertificateHttp_Fails()
        {
            var job = JobClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionStringInvalidServiceCertificate);
            IotHubCommunicationException exception = await Assert.ThrowsExceptionAsync<IotHubCommunicationException>(
                () => job.ScheduleTwinUpdateAsync(
                    "testDevice",
                    "DeviceId IN ['testDevice']",
                    new Shared.Twin(),
                    DateTime.UtcNow,
                    60))
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Http1)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        public async Task DeviceClient_SendAsyncInvalidServiceCertificate_Fails(Client.TransportType transport)
        {
            Exception exception = await Assert
                .ThrowsExceptionAsync<AuthenticationException>(
                    async () =>
                    {
                        using (var deviceClient = DeviceClient.CreateFromConnectionString(
                            Configuration.IoTHub.DeviceConnectionStringInvalidServiceCertificate,
                            transport))
                        {
                            await deviceClient.SendEventAsync(new Client.Message()).ConfigureAwait(false);
                            await deviceClient.CloseAsync().ConfigureAwait(false);
                        }
                    })
            .ConfigureAwait(false);
        }

        private static async Task TestServiceClientInvalidServiceCertificate(TransportType transport)
        {
            var service = ServiceClient.CreateFromConnectionString(
                Configuration.IoTHub.ConnectionStringInvalidServiceCertificate,
                transport);
            await service.SendAsync("testDevice1", new Message()).ConfigureAwait(false);
        }

        private static async Task TestDeviceClientInvalidServiceCertificate(Client.TransportType transport)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
