// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Http2;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Api.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class DeviceAuthenticationTests
    {
        private const string IotHubName = "acme";
        private const string validMockConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest1()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = new SymmetricKey()
                    {
                        PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                        SecondaryKey = CryptoKeyGenerator.GenerateKey(32)
                    },
                    X509Thumbprint = new X509Thumbprint()
                }
            };

            HttpContent mockContent = HttpMessageHelper2.SerializePayload(deviceGoodAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(validMockConnectionString, settings);
            await registryClient.AddDeviceAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest2()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                    }
                }
            };

            HttpContent mockContent = HttpMessageHelper2.SerializePayload(deviceGoodAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;

            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(validMockConnectionString, settings);
            await registryClient.AddDeviceAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest3()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = null
                    }
                }
            };

            HttpContent mockContent = HttpMessageHelper2.SerializePayload(deviceGoodAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;

            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(validMockConnectionString, settings);
            await registryClient.AddDeviceAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest4()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = null,
                        SecondaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B"
                    }
                }
            };

            HttpContent mockContent = HttpMessageHelper2.SerializePayload(deviceGoodAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;

            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(validMockConnectionString, settings);
            await registryClient.AddDeviceAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest5()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = new SymmetricKey()
                    {
                        PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                        SecondaryKey = CryptoKeyGenerator.GenerateKey(32)
                    },
                    X509Thumbprint = null
                }
            };

            string responsePayload = JsonConvert.SerializeObject(deviceGoodAuthConfig);
            HttpContent mockContent = new StringContent(responsePayload);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;

            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(validMockConnectionString, settings);
            await registryClient.AddDeviceAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest6()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = "781BC9694ADEB8929D4F7FE4B9A3A6DE58B07952"
                    }
                }
            };

            HttpContent mockContent = HttpMessageHelper2.SerializePayload(deviceBadAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;

            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(validMockConnectionString, settings);
            await registryClient.AddDeviceAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthSHA256()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism()
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint()
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B7FE4B9A3A6DE58B0790B790B",
                        SecondaryThumbprint = "781BC9694ADEB8929D4F7FE4B9A3A6DE58B079527FE4B9A3A6DE58B079527952"
                    }
                }
            };

            HttpContent mockContent = HttpMessageHelper2.SerializePayload(deviceBadAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;

            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(validMockConnectionString, settings);
            await registryClient.AddDeviceAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationIsCertificateAuthority()
        {
            var deviceBadThumbprint = new Device("123")
            {
                ConnectionState = DeviceConnectionState.Connected,
                Authentication = new AuthenticationMechanism
                {
                    Type = AuthenticationType.CertificateAuthority,
                    SymmetricKey = null,
                    X509Thumbprint = null
                }
            };

            HttpContent mockContent = HttpMessageHelper2.SerializePayload(deviceBadThumbprint);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;

            var restOpMock = new Mock<HttpClient>();
            restOpMock.Setup(
                restOp =>
                    restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);
            HttpTransportSettings2 settings = new HttpTransportSettings2();
            settings.HttpClient = restOpMock.Object;
            var registryClient = new RegistryClient(validMockConnectionString, settings);
            await registryClient.AddDeviceAsync(deviceBadThumbprint).ConfigureAwait(false);
        }
    }
}
