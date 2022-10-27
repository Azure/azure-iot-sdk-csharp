// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
        private const string HostName = "acme.azure-devices.net";
        private static Uri HttpUri = new Uri("https://" + HostName);
        private const string validMockConnectionString = $"HostName={HostName};SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";
        private const string validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest1()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
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

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceGoodAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new RetryHandler(new IotHubServiceNoRetry()));

            await devicesClient.CreateAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest2()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
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

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceGoodAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new RetryHandler(new IotHubServiceNoRetry()));

            await devicesClient.CreateAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest3()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
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

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceGoodAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new RetryHandler(new IotHubServiceNoRetry()));

            await devicesClient.CreateAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest4()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
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

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceGoodAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new RetryHandler(new IotHubServiceNoRetry()));

            await devicesClient.CreateAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest5()
        {
            var deviceGoodAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
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

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceGoodAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new RetryHandler(new IotHubServiceNoRetry()));

            await devicesClient.CreateAsync(deviceGoodAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthConfigTest6()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
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

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceBadAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new RetryHandler(new IotHubServiceNoRetry()));

            await devicesClient.CreateAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationGoodAuthSHA256()
        {
            var deviceBadAuthConfig = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
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

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceBadAuthConfig);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new RetryHandler(new IotHubServiceNoRetry()));

            await devicesClient.CreateAsync(deviceBadAuthConfig).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceAuthenticationIsCertificateAuthority()
        {
            var deviceBadThumbprint = new Device("123")
            {
                ConnectionState = ClientConnectionState.Connected,
                Authentication = new AuthenticationMechanism
                {
                    Type = ClientAuthenticationType.CertificateAuthority,
                    SymmetricKey = null,
                    X509Thumbprint = null
                }
            };

            HttpContent mockContent = HttpMessageHelper.SerializePayload(deviceBadThumbprint);
            var mockHttpResponse = new HttpResponseMessage();
            mockHttpResponse.Content = mockContent;
            mockHttpResponse.StatusCode = HttpStatusCode.OK;
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider.Setup(getCredential => getCredential.GetAuthorizationHeader()).Returns(validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(HttpUri, "");
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient.Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockHttpResponse);

            var devicesClient = new DevicesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                new RetryHandler(new IotHubServiceNoRetry()));

            await devicesClient.CreateAsync(deviceBadThumbprint).ConfigureAwait(false);
        }
    }
}
