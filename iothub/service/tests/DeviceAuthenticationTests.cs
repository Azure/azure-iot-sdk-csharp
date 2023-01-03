// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Api.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class DeviceAuthenticationTests
    {
        private const string HostName = "acme.azure-devices.net";
        private static readonly string s_validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";

        private static readonly Uri s_httpUri = new("https://" + HostName);
        private static readonly RetryHandler s_retryHandler = new(new IotHubServiceNoRetry());

        [TestMethod]
        public async Task DeviceAuthentication_GeneratedSymmetricKeysAuthConfigTest()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
                Authentication = new AuthenticationMechanism
                {
                    SymmetricKey = new SymmetricKey
                    {
                        PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                        SecondaryKey = CryptoKeyGenerator.GenerateKey(32),
                    },
                    X509Thumbprint = new(),
                },
            };

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceGoodAuthConfig);
            using var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            await devicesClient.CreateAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthentication_CertificateAuthConfigTest_WithMatchingSecondaryThumbprint()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
                Authentication = new AuthenticationMechanism
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                    },
                },
            };

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceGoodAuthConfig);
            using var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            await devicesClient.CreateAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthentication_CertificateAuthConfigTest_NullSecondaryThumbprint()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
                Authentication = new AuthenticationMechanism
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = null,
                    },
                },
            };

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceGoodAuthConfig);
            using var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            await devicesClient.CreateAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthentication_CertificateAuthConfigTest_NullPrimaryThumbprint()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
                Authentication = new AuthenticationMechanism
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint
                    {
                        PrimaryThumbprint = null,
                        SecondaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                    },
                },
            };

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceGoodAuthConfig);
            using var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            await devicesClient.CreateAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthentication_GeneratedSymmetricKeysAuthConfigTest_NullThumbprint()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
                Authentication = new AuthenticationMechanism
                {
                    SymmetricKey = new SymmetricKey
                    {
                        PrimaryKey = CryptoKeyGenerator.GenerateKey(32),
                        SecondaryKey = CryptoKeyGenerator.GenerateKey(32),
                    },
                    X509Thumbprint = null,
                },
            };

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceGoodAuthConfig);
            using var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            await devicesClient.CreateAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthentication_CertificateAuthConfigTest_NullSymmetricKey()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
                Authentication = new AuthenticationMechanism
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B",
                        SecondaryThumbprint = "781BC9694ADEB8929D4F7FE4B9A3A6DE58B07952",
                    },
                },
            };

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceGoodAuthConfig);
            using var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            await devicesClient.CreateAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthentication_CertificateAuthConfigTest_Sha256Thumbprint()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
                Authentication = new AuthenticationMechanism
                {
                    SymmetricKey = null,
                    X509Thumbprint = new X509Thumbprint
                    {
                        PrimaryThumbprint = "921BC9694ADEB8929D4F7FE4B9A3A6DE58B0790B7FE4B9A3A6DE58B0790B790B",
                        SecondaryThumbprint = "781BC9694ADEB8929D4F7FE4B9A3A6DE58B079527FE4B9A3A6DE58B079527952",
                    },
                },
            };

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceGoodAuthConfig);
            using var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            await devicesClient.CreateAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthentication_CertificateAuthConfig_NullSymmetricKeyAndThumbprint()
        {
            var deviceWithoutThumbprint = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
                Authentication = new AuthenticationMechanism
                {
                    Type = ClientAuthenticationType.CertificateAuthority,
                    SymmetricKey = null,
                    X509Thumbprint = null,
                },
            };

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceWithoutThumbprint);
            using var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            await devicesClient.CreateAsync(deviceWithoutThumbprint).ConfigureAwait(false);
        }
    }
}
