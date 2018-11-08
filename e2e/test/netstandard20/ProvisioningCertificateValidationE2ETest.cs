// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Provisioning.Service.Models;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("InvalidServiceCertificate")]
    public class ProvisioningCertificateValidationE2ETest : IDisposable
    {
        private readonly ConsoleEventListener _listener;

        public ProvisioningCertificateValidationE2ETest()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task ProvisioningServiceClient_QueryInvalidServiceCertificateHttp_Fails()
        {
            using (var provisioningServiceClient = 
                ProvisioningServiceClientFactory.CreateFromConnectionString(
                    Configuration.Provisioning.ConnectionStringInvalidServiceCertificate))
            {
                var querySpecification = new QuerySpecification("SELECT * FROM enrollmentGroups");

                var exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(
                    () => provisioningServiceClient.QueryEnrollmentGroupsAsync(querySpecification)).ConfigureAwait(false);

#if NET47 || NET451
                Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#else
                Assert.IsInstanceOfType(exception.InnerException, typeof(AuthenticationException));
#endif
            }
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateAmqpTcp_Fails()
        {
            using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
            {
                var exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                    () => TestInvalidServiceCertificate(transport)).ConfigureAwait(false);

                Assert.IsInstanceOfType(exception.InnerException, typeof(AuthenticationException));
            }
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateMqttTcp_Fails()
        {
            using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
            {
                var exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                    () => TestInvalidServiceCertificate(transport)).ConfigureAwait(false);

                if (exception.InnerException == null)
                {
                    Assert.AreEqual("MQTT Protocol Exception: Channel closed.", exception.Message);
                }
                else
                {
                    Assert.IsInstanceOfType(exception.InnerException, typeof(AuthenticationException));
                }
            }
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateHttp_Fails()
        {
            using (var transport = new ProvisioningTransportHandlerHttp())
            {
                var exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                    () => TestInvalidServiceCertificate(transport)).ConfigureAwait(false);

#if NET47 || NET451
                Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#else
                Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
            }
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateAmqpWs_Fails()
        {
            using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.WebSocketOnly))
            {
                var exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                    () => TestInvalidServiceCertificate(transport)).ConfigureAwait(false);

                Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
            }
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateMqttWs_Fails()
        {
            using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly))
            {
                var exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                    () => TestInvalidServiceCertificate(transport)).ConfigureAwait(false);

                Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
            }
        }

        private static async Task TestInvalidServiceCertificate(ProvisioningTransportHandler transport)
        {
            using (var security = 
                new SecurityProviderX509Certificate(Configuration.Provisioning.GetIndividualEnrollmentCertificate()))
            {
                ProvisioningDeviceClient provisioningDeviceClient = ProvisioningDeviceClient.Create(
                    Configuration.Provisioning.GlobalDeviceEndpointInvalidServiceCertificate,
                    "0ne00000001",
                    security,
                    transport);

                await provisioningDeviceClient.RegisterAsync().ConfigureAwait(false);
            }
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
