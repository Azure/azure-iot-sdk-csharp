﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Provisioning
{
    [TestClass]
    [TestCategory("InvalidServiceCertificate")]
    public class ProvisioningCertificateValidationE2ETest : E2EMsTestBase
    {
        private static DirectoryInfo s_x509CertificatesFolder;

        [ClassInitialize]
        public static void TestClassSetup(TestContext _)
        {
            // Create a folder to hold the DPS client certificates and X509 self-signed certificates. If a folder by the same name already exists, it will be used.
            // Shorten the folder name to avoid overall file path become too long and cause error in the test
            string s_folderName = "x509-" + nameof(ProvisioningCertificateValidationE2ETest).Split('.').Last() + "-" + Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace('+', '-').Replace('/', '.').Trim('=');
            s_x509CertificatesFolder = Directory.CreateDirectory(s_folderName);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningServiceClient_QueryInvalidServiceCertificateHttp_Fails()
        {
            // arrange
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionStringInvalidServiceCertificate);
            Query q = provisioningServiceClient.EnrollmentGroups.CreateQuery("SELECT * FROM enrollmentGroups");

            // act
            Func<Task> act = async () => await q.NextAsync();

            // assert
            var error = await act.Should().ThrowAsync<ProvisioningServiceException>().ConfigureAwait(false);
            error.And.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            error.And.ErrorCode.Should().Be(0);
            error.And.IsTransient.Should().BeFalse();
#if NET472
            error.And.InnerException.InnerException.InnerException.Should().BeOfType<AuthenticationException>();
#else
            error.And.InnerException.InnerException.Should().BeOfType<AuthenticationException>();
#endif
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateAmqpTcp_Fails()
        {
            // arrange
            var clientOptions = new ProvisioningClientOptions(new ProvisioningClientAmqpSettings(ProvisioningClientTransportProtocol.Tcp));

            // act
            Func<Task> act = async () => await TestInvalidServiceCertificateAsync(clientOptions);

            //assert
            var error = await act.Should().ThrowAsync<ProvisioningClientException>().ConfigureAwait(false);
            error.And.ErrorCode.Should().Be(0);
            error.And.IsTransient.Should().BeFalse();
            error.And.InnerException.Should().BeOfType<AuthenticationException>();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateMqttTcp_Fails()
        {
            // arrange
            var clientOptions = new ProvisioningClientOptions(new ProvisioningClientMqttSettings(ProvisioningClientTransportProtocol.Tcp));

            // act
            Func<Task> act = async () => await TestInvalidServiceCertificateAsync(clientOptions);

            // assert
            var error = await act.Should().ThrowAsync<ProvisioningClientException>().ConfigureAwait(false);
            error.And.ErrorCode.Should().Be(0);
            error.And.IsTransient.Should().BeFalse();
            error.And.InnerException.InnerException.Should().BeOfType<AuthenticationException>();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateAmqpWs_Fails()
        {
            // arrange
            var clientOptions = new ProvisioningClientOptions(new ProvisioningClientAmqpSettings(ProvisioningClientTransportProtocol.WebSocket));

            // act
            Func<Task> act = async () => await TestInvalidServiceCertificateAsync(clientOptions);

            // assert
            var error = await act.Should().ThrowAsync<ProvisioningClientException>().ConfigureAwait(false);
            error.And.ErrorCode.Should().Be(0);
            error.And.IsTransient.Should().BeFalse();
            error.And.InnerException.InnerException.InnerException.Should().BeOfType<AuthenticationException>();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_RegisterAsyncInvalidServiceCertificateMqttWs_Fails()
        {
            // arrange
            var clientOptions = new ProvisioningClientOptions(new ProvisioningClientMqttSettings(ProvisioningClientTransportProtocol.WebSocket));

            // act
            Func<Task> act = async () => await TestInvalidServiceCertificateAsync(clientOptions);

            // assert
            var error = await act.Should().ThrowAsync<ProvisioningClientException>().ConfigureAwait(false);
            error.And.ErrorCode.Should().Be(0);
            error.And.IsTransient.Should().BeFalse();
            error.And.InnerException.InnerException.InnerException.InnerException.Should().BeOfType<AuthenticationException>();
        }

        private static async Task TestInvalidServiceCertificateAsync(ProvisioningClientOptions clientOptions)
        {
            // Shorten the file name to avoid overall file path become too long and cause error in the test
            string certificateSubject = "cert-" + Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace('+', '-').Replace('/', '.').Trim('=');
            X509Certificate2Helper.GenerateSelfSignedCertificateFiles(certificateSubject, s_x509CertificatesFolder);

            using X509Certificate2 cert = X509Certificate2Helper.CreateX509Certificate2FromPfxFile(certificateSubject, s_x509CertificatesFolder);
            var auth = new AuthenticationProviderX509(cert);
            var provisioningDeviceClient = new ProvisioningDeviceClient(
                TestConfiguration.Provisioning.GlobalDeviceEndpointInvalidServiceCertificate,
                "0ne00000001",
                auth,
                clientOptions);

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