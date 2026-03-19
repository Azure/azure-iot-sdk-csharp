// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading;
using FluentAssertions;
using Azure;

namespace Microsoft.Azure.Devices.Tests.Configurations
{
    [TestClass]
    [TestCategory("Unit")]
    public class ConfigurationsClientTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private static readonly string s_validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";

        private static readonly Uri s_httpUri = new($"https://{HostName}");
        private static readonly RetryHandler s_retryHandler = new(new IotHubServiceNoRetry());

        [TestMethod]
        public async Task ConfigurationClients_CreateAsync()
        {
            //arrange
            string configurationId = Guid.NewGuid().ToString().ToLower(); // Configuration Id characters must be all lower-case.
            var configuration = new Configuration(configurationId)
            {
                Priority = 1,
                Labels = { { "testLabelName", "testLabelValue " } },
                TargetCondition = "deviceId = testId"
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(configuration)
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.CreateAsync(configuration).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_CreateAsync_NullConfigurationIdThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.CreateAsync(null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_CreateAsync_NullConfigurationThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.CreateAsync(null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_GetAsync()
        {
            //arrange
            string configurationId = Guid.NewGuid().ToString().ToLower(); // Configuration Id characters must be all lower-case.
            var configuration = new Configuration(configurationId)
            {
                Priority = 2,
                Labels = { { "testLabelName", "testLabelValue " } },
                TargetCondition = "deviceId = testId"
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(configuration)
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.GetAsync(configurationId).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_GetAsync_NullConfigurationIdThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.GetAsync(null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_GetAsync_NullConfigurationThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();
            var invalidConfigurationId = "Configuration Id";

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.GetAsync(invalidConfigurationId).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_GetAsync_NegativeMaxCountThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.GetAsync(-1).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_GetAsync_WithMaxCount()
        {
            // arrange
            string configurationId1 = Guid.NewGuid().ToString().ToLower(); // Configuration Id characters must be all lower-case.
            string configurationId2 = Guid.NewGuid().ToString().ToLower(); // Configuration Id characters must be all lower-case.
            var configuration1 = new Configuration(configurationId1)
            {
                Priority = 2,
                Labels = { { "testLabelName", "testLabelValue " } },
                TargetCondition = "deviceId = testId"
            };
            var configuration2 = new Configuration(configurationId2)
            {
                Priority = 1,
                Labels = new Dictionary<string, string>
                {
                    { "App", "Mongo" }
                }
            };
            var configurationsToReturn = new List<Configuration> { configuration1, configuration2 };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(configurationsToReturn)
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            var configurationsResult = await configurationsClient.GetAsync(2).ConfigureAwait(false);

            // assert
            configurationsResult.ElementAt(0).Id.Should().Be(configurationId1);
            configurationsResult.ElementAt(1).Id.Should().Be(configurationId2);
        }

        [TestMethod]
        public async Task ConfigurationClients_SetAsync()
        {
            // arrange
            string configurationId = Guid.NewGuid().ToString().ToLower(); // Configuration Id characters must be all lower-case.

            var configurationToReturn = new Configuration(configurationId)
            {
                Priority = 1,
                Labels = new Dictionary<string, string>
                {
                    { "App", "Mongo" }
                },
                ETag = new ETag("123")
            };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(configurationToReturn),
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            var returnedConfiguration = await configurationsClient.SetAsync(configurationToReturn).ConfigureAwait(false);

            // assert
            returnedConfiguration.Id.Should().Be(configurationId);
            mockHttpClient.VerifyAll();
        }

        [TestMethod]
        public async Task ConfigurationClients_SetAsync_NullConfigurationThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.SetAsync(null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_SetAsync_WithOnlyIfUnchangedTrueWithETag()
        {
            // arrange
            string configurationId = Guid.NewGuid().ToString().ToLower(); // Configuration Id characters must be all lower-case.
            var configurationToReturnWithoutETag = new Configuration(configurationId)
            {
                Priority = 1,
                Labels = new Dictionary<string, string>
                {
                    { "App", "Mongo" }
                },
                ETag = new ETag("123")
            };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(configurationToReturnWithoutETag),
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.SetAsync(configurationToReturnWithoutETag, true).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_DeleteAsync()
        {
            // arrange
            string configurationId = Guid.NewGuid().ToString().ToLower(); // Configuration Id characters must be all lower-case.
            var configuration = new Configuration(configurationId)
            {
                Priority = 1,
                Labels = new Dictionary<string, string>
                {
                    { "App", "Mongo" }
                }
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

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.DeleteAsync(configuration).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }


        [TestMethod]
        public async Task ConfigurationClients_DeleteAsync_NullConfigurationIdThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.DeleteAsync(null, false).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_DeleteAsync_InvalidConfigurationIdThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.DeleteAsync("Invalid Id").ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_ApplyConfigurationContentOnDeviceAsync_NullDeviceIdThrows()
        {
            // arrange
            var configurationContent = new ConfigurationContent
            {
                ModulesContent = new Dictionary<string, IDictionary<string, object>>
                {
                    {
                        "Module1", new Dictionary<string, object>
                        {
                            { "setting1", "value1" },
                            { "setting2", "value2" }
                        }
                    },
                    {
                        "Module2", new Dictionary<string, object>
                        {
                            { "settings3", true },
                            { "settings4", 123 }
                        }
                    }
                }
            };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.ApplyConfigurationContentOnDeviceAsync(null, configurationContent).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_ApplyConfigurationContentOnDeviceAsync_NullConfigurationContentThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.ApplyConfigurationContentOnDeviceAsync("1234", null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_ApplyConfigurationContentOnDeviceAsync_EmptyDeviceIdThrows()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var mockHttpRequestFactory = new Mock<HttpRequestMessageFactory>();
            var mockHttpClient = new Mock<HttpClient>();

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory.Object,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.ApplyConfigurationContentOnDeviceAsync(string.Empty, null).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<ArgumentException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ConfigurationClients_ApplyConfigurationContentOnDeviceAsync()
        {
            // arrange
            var deviceId = Guid.NewGuid().ToString();
            var configurationContent = new ConfigurationContent
            {
                ModulesContent = new Dictionary<string, IDictionary<string, object>>
                {
                    {
                        "Module1", new Dictionary<string, object>
                        {
                            { "setting1", "value1" },
                            { "setting2", "value2" }
                        }
                    },
                    {
                        "Module2", new Dictionary<string, object>
                        {
                            { "settings3", true },
                            { "settings4", 123 }
                        }
                    }
                }
            };
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");
            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            };
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var configurationsClient = new ConfigurationsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await configurationsClient.ApplyConfigurationContentOnDeviceAsync(deviceId, configurationContent).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }
    }
}
