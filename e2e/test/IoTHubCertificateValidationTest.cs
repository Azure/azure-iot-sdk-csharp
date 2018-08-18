// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("InvalidServiceCertificate")]
    public class IoTHubCertificateValidationTest : IDisposable
    {
        private readonly TestLogging _log = TestLogging.GetInstance();
        private readonly ConsoleEventListener _listener;

        public IoTHubCertificateValidationTest()
        {
            _listener = new ConsoleEventListener("Microsoft-Azure-");
        }

        [TestMethod]
        public async Task RegistryManager_QueryDevicesInvalidServiceCertificateHttp_Fails()
        {
            var rm = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionStringInvalidServiceCertificate);
            IQuery query = rm.CreateQuery("select * from devices");
            var exception = await Assert.ThrowsExceptionAsync<IotHubCommunicationException>(
                () => query.GetNextAsTwinAsync()).ConfigureAwait(false);

#if NET451 || NET47
            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#elif NETCOREAPP2_0
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // WinHttpException (0x80072F8F): A security error occurred
                Assert.AreEqual(unchecked((int)0x80072F8F), exception.InnerException.InnerException.HResult);
            }
            else
            {
                // CURLE_SSL_CACERT (60): Peer certificate cannot be authenticated with known CA certificates.
                Assert.AreEqual(60, exception.InnerException.InnerException.HResult);
            }
#else
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
        }

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

#if !NETCOREAPP2_0
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#else
            // WinHttpException (0x80072F8F): A security error occurred
            Assert.AreEqual(unchecked((int)0x80072F8F), exception.InnerException.InnerException.HResult);
#endif
        }

        private static async Task TestServiceClientInvalidServiceCertificate(TransportType transport)
        {
            var service = ServiceClient.CreateFromConnectionString(
                Configuration.IoTHub.ConnectionStringInvalidServiceCertificate,
                transport);
            await service.SendAsync("testDevice1", new Message()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task JobClient_ScheduleTwinUpdateInvalidServiceCertificateHttp_Fails()
        {
            var job = JobClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionStringInvalidServiceCertificate);
            var exception = await Assert.ThrowsExceptionAsync<IotHubCommunicationException>(
                () => job.ScheduleTwinUpdateAsync(
                    "testDevice",
                    "DeviceId IN ['testDevice']",
                    new Shared.Twin(),
                    DateTime.UtcNow,
                    60)).ConfigureAwait(false);

#if NET451 || NET47
            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#elif NETCOREAPP2_0
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // WinHttpException (0x80072F8F): A security error occurred
                Assert.AreEqual(unchecked((int)0x80072F8F), exception.InnerException.InnerException.HResult);
            }
            else
            {
                // CURLE_SSL_CACERT (60): Peer certificate cannot be authenticated with known CA certificates.
                Assert.AreEqual(60, exception.InnerException.InnerException.HResult);
            }
#else
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
        }

        [TestMethod]
        public async Task DeviceClient_SendAsyncInvalidServiceCertificateAmqpTcp_Fails()
        {
            var transport = Client.TransportType.Amqp_Tcp_Only;
            await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_SendAsyncInvalidServiceCertificateMqttTcp_Fails()
        {
            var transport = Client.TransportType.Mqtt_Tcp_Only;
            await Assert.ThrowsExceptionAsync<AuthenticationException>(
                () => TestDeviceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_SendAsyncInvalidServiceCertificateHttp_Fails()
        {
            var transport = Client.TransportType.Http1;
            var exception = await Assert.ThrowsExceptionAsync<Client.Exceptions.IotHubCommunicationException>(
                () => TestDeviceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);

#if NET451 || NET47
            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#elif NETCOREAPP2_0
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // WinHttpException (0x80072F8F): A security error occurred
                Assert.AreEqual(unchecked((int)0x80072F8F), exception.InnerException.InnerException.HResult);
            }
            else
            {
                // CURLE_SSL_CACERT (60): Peer certificate cannot be authenticated with known CA certificates.
                Assert.AreEqual(60, exception.InnerException.InnerException.HResult);
            }
#else
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
        }

        [TestMethod]
        public async Task DeviceClient_SendAsyncInvalidServiceCertificateAmqpWs_Fails()
        {
            var transport = Client.TransportType.Amqp_WebSocket_Only;
            var exception = await Assert.ThrowsExceptionAsync<WebSocketException>(
                () => TestDeviceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);

#if !NETCOREAPP2_0
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#else
            // WinHttpException (0x80072F8F): A security error occurred
            Assert.AreEqual(unchecked((int)0x80072F8F), exception.InnerException.InnerException.HResult);
#endif
        }

        [TestMethod]
        public async Task DeviceClient_SendAsyncInvalidServiceCertificateMqttWs_Fails()
        {
            var transport = Client.TransportType.Mqtt_WebSocket_Only;
            var exception = await Assert.ThrowsExceptionAsync<WebSocketException>(
                () => TestDeviceClientInvalidServiceCertificate(transport)).ConfigureAwait(false);

#if !NETCOREAPP2_0
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // WinHttpException (0x80072F8F): A security error occurred
                Assert.AreEqual(unchecked((int)0x80072F8F), exception.InnerException.InnerException.HResult);
            }
            else
            {
                Assert.IsInstanceOfType(exception.InnerException, typeof(AuthenticationException));
            }
#endif
        }

        private static async Task TestDeviceClientInvalidServiceCertificate(Client.TransportType transport)
        {
            using (DeviceClient deviceClient = 
                DeviceClient.CreateFromConnectionString(
                    Configuration.IoTHub.DeviceConnectionStringInvalidServiceCertificate, 
                    transport))
            {
                await deviceClient.SendEventAsync(new Client.Message()).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listener.Dispose();
            }
        }
    }
}
