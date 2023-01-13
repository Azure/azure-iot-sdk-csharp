// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading;
using Azure;

namespace Microsoft.Azure.Devices.Tests.Registry
{
    [TestClass]
    [TestCategory("Unit")]
    public class ModulesClientTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private static readonly string s_validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";
        private static readonly string s_connectionString = $"HostName={HostName};SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";

        private static readonly Uri s_httpUri = new($"https://{HostName}");
        private static readonly RetryHandler s_retryHandler = new(new IotHubServiceNoRetry());
        private static IotHubServiceClientOptions s_options = new IotHubServiceClientOptions
        {
            Protocol = IotHubTransportProtocol.Tcp,
            RetryPolicy = new IotHubServiceNoRetry()
        };

        [TestMethod]
        public async Task ModulesClient_CreateAsync_NullModuleThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);
            // act
            Func<Task> act = async () => await serviceClient.Modules.CreateAsync((Module)null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModulesClient_CreateAsync()
        {
            // arrange
            var module = new Module("device123", "module123")
            {
                ConnectionState = ClientConnectionState.Connected
            };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(module)
            };

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var modulesClient = new ModulesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);
            // act
            Func<Task> act = async () => await modulesClient.CreateAsync(module).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModulesClient_GetAsync_NullDeviceIdThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);
            // act
            Func<Task> act = async () => await serviceClient.Modules.GetAsync(null, "moduleId").ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModulesClient_GetAsync_NullModuleIdThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);
            // act
            Func<Task> act = async () => await serviceClient.Modules.GetAsync("deviceId", null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModulesClient_GetAsync_EmptyDeviceIdThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);
            // act
            Func<Task> act = async () => await serviceClient.Modules.GetAsync(String.Empty, "moduleId").ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModulesClient_GetAsync_EmptyModuleIdThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);
            // act
            Func<Task> act = async () => await serviceClient.Modules.GetAsync("deviceId", String.Empty).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModulesClient_GetAsync()
        {
            // arrange
            var moduleId = "moduleId123";
            var deviceId = "deviceId123";

            var moduleToReturn = new Module("deviceId123", "moduleId123")
            {
                ConnectionState = ClientConnectionState.Connected
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(moduleToReturn)
            };

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var modulesClient = new ModulesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);
            // act
            Func<Task> act = async () => await modulesClient.GetAsync(deviceId, moduleId).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModulesClient_SetAsync_NullModuleThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);
            // act
            Func<Task> act = async () => await serviceClient.Modules.SetAsync((Module)null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModulesClient_SetAsync()
        {
            // arrange
            var moduleToReplace = new Module("deviceId123", "moduleId123")
            {
                ConnectionState = ClientConnectionState.Disconnected,
                ETag = new ETag("45678")
            };

            var replacedModule = new Module("deviceId123", "moduleId123")
            {
                ConnectionState = ClientConnectionState.Connected,
                ETag = new ETag("12345")
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(replacedModule)
            };

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var modulesClient = new ModulesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);
            // act
            Func<Task> act = async () => await modulesClient.SetAsync(moduleToReplace).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModulesClient_DeleteAsync_NullDeviceIdThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);
            // act
            Func<Task> act = async () => await serviceClient.Modules.DeleteAsync(null, "moduleId").ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModulesClient_DeleteAsync_NullModuleIdThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);
            // act
            Func<Task> act = async () => await serviceClient.Modules.DeleteAsync("deviceId", null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModulesClient_DeleteAsync_EmptyDeviceIdThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);
            // act
            Func<Task> act = async () => await serviceClient.Modules.GetAsync(String.Empty, "moduleId").ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModulesClient_DeleteAsync_EmptyModuleIdThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);
            // act
            Func<Task> act = async () => await serviceClient.Modules.GetAsync("deviceId", String.Empty).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }
        [TestMethod]
        public async Task ModulesClient_DeleteAsync()
        {
            // arrange
            var module = new Module("deviceId123", "moduleId123")
            {
                ConnectionState = ClientConnectionState.Disconnected,
                ETag = new ETag("45678")
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            };

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var modulesClient = new ModulesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);
            // act
            Func<Task> act = async () => await modulesClient.DeleteAsync("deviceId123", "moduleId123").ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }
    }
}
