﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Provisioning
{
    [TestClass]
    [Ignore("TODO: Enable when invalid cert server is back online.")]
    [TestCategory("InvalidServiceCertificate")]
    public class ProvisioningCertificateValidationE2ETest : E2EMsTestBase
    {
        private static DirectoryInfo s_x509CertificatesFolder;

        [ClassInitialize]
        public static void TestClassSetup(TestContext _)
        {
            // Create a folder to hold the DPS client certificates and X509 self-signed certificates. If a folder by the same name already exists, it will be used.
            s_x509CertificatesFolder = Directory.CreateDirectory($"x509Certificates-{nameof(ProvisioningCertificateValidationE2ETest)}-{Guid.NewGuid()}");
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_QueryInvalidServiceCertificateHttp_Fails()
        {
            using var provisioningServiceClient = new ProvisioningServiceClient(
                TestConfiguration.Provisioning.ConnectionStringInvalidServiceCertificate);
            Query q = provisioningServiceClient.CreateEnrollmentGroupQuery(
                "SELECT * FROM enrollmentGroups");

            ProvisioningServiceClientTransportException exception = await Assert.ThrowsExceptionAsync<ProvisioningServiceClientTransportException>(
                () => q.NextAsync()).ConfigureAwait(false);

#if NET472
                Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#else
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateAmqpTcp_Fails()
        {
            using var transport = new ProvisioningTransportHandlerAmqp(ProvisioningClientTransportProtocol.Tcp);
            ProvisioningTransportException exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                () => TestInvalidServiceCertificate(transport)).ConfigureAwait(false);

            Assert.IsInstanceOfType(exception.InnerException, typeof(AuthenticationException));
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateMqttTcp_Fails()
        {
            using var transport = new ProvisioningTransportHandlerMqtt(ProvisioningClientTransportProtocol.Tcp);
            ProvisioningTransportException exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
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

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateHttp_Fails()
        {
            using var transport = new ProvisioningTransportHandlerHttp();
            ProvisioningTransportException exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                () => TestInvalidServiceCertificate(transport)).ConfigureAwait(false);

#if NET472
                Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
#else
            Assert.IsInstanceOfType(exception.InnerException.InnerException, typeof(AuthenticationException));
#endif
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateAmqpWs_Fails()
        {
            using var transport = new ProvisioningTransportHandlerAmqp(ProvisioningClientTransportProtocol.WebSocket);
            ProvisioningTransportException exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                () => TestInvalidServiceCertificate(transport)).ConfigureAwait(false);

            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateMqttWs_Fails()
        {
            using var transport = new ProvisioningTransportHandlerMqtt(ProvisioningClientTransportProtocol.WebSocket);
            ProvisioningTransportException exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                () => TestInvalidServiceCertificate(transport)).ConfigureAwait(false);

            Assert.IsInstanceOfType(exception.InnerException.InnerException.InnerException, typeof(AuthenticationException));
        }

        private async Task TestInvalidServiceCertificate(ProvisioningTransportHandler transport)
        {
            string certificateSubject = $"{nameof(ProvisioningCertificateValidationE2ETest)}-{Guid.NewGuid()}";
            X509Certificate2Helper.GenerateSelfSignedCertificateFiles(certificateSubject, s_x509CertificatesFolder, Logger);

            using X509Certificate2 cert = X509Certificate2Helper.CreateX509Certificate2FromPfxFile(certificateSubject, s_x509CertificatesFolder);
            var auth = new AuthenticationProviderX509Certificate(cert);
            var provisioningDeviceClient = new ProvisioningDeviceClient(
                TestConfiguration.Provisioning.GlobalDeviceEndpointInvalidServiceCertificate,
                "0ne00000001",
                auth,
                new ProvisioningClientOptions(transport));

            await provisioningDeviceClient.RegisterAsync().ConfigureAwait(false);
        }

        [ClassCleanup]
        public static void CleanupCertificates()
        {
            // Delete all the test client certificates created
            try
            {
                s_x509CertificatesFolder.Delete(true);
            }
            catch (Exception)
            {
                // In case of an exception, silently exit. All systems images on Microsoft hosted agents will be cleaned up by the system.
            }
        }
    }
}
