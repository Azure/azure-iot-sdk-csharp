// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class DigitalTwinsClientTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private static readonly string s_validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";
        private static readonly string s_connectionString = $"HostName={HostName};SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";

        private static readonly Uri s_httpUri = new($"https://{HostName}");
        private static readonly RetryHandler s_retryHandler = new(new IotHubServiceNoRetry());
        private static IotHubServiceClientOptions s_options = new()
        {
            Protocol = IotHubTransportProtocol.Tcp,
            RetryPolicy = new IotHubServiceNoRetry()
        };

        [TestMethod]
        public async Task DigitalTwinsClient_GetAsync()
        {
            // arrange
            string digitalTwinId = "foo";
            var digitalTwin = new BasicDigitalTwin
            {
                Id = digitalTwinId,
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(digitalTwin),
            };
            mockHttpResponse.Headers.Add("ETag", "1234");
            //mockHttpResponse.Headers.Add("ETag", new ETag("test");

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var digitalTwinsClient = new DigitalTwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await digitalTwinsClient.GetAsync<BasicDigitalTwin>(digitalTwinId).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_GetAsync_NullTwinIdThrows()
        {
            // arrange
            string digitalTwinId = "";
            using var serviceClient = new IotHubServiceClient(s_connectionString);
            DigitalTwinsClient digitalTwinsClient = serviceClient.DigitalTwins;

            // act
            Func<Task> act = async () => await digitalTwinsClient.GetAsync<BasicDigitalTwin>(digitalTwinId);

            // assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_GetAsync_HttpException()
        {
            // arrange
            string digitalTwinId = Guid.NewGuid().ToString();
            using var serviceClient = new IotHubServiceClient(s_connectionString);
            DigitalTwinsClient digitalTwinsClient = serviceClient.DigitalTwins;

            // act
            // deliberately throw http exception by searching for twin that does not exist
            Func<Task> act = async () => await digitalTwinsClient.GetAsync<string>(digitalTwinId);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_UpdateAsync()
        {
            // arrange
            string digitalTwinId = "foo";
            var digitalTwin = new BasicDigitalTwin
            {
                Id = digitalTwinId,
            };
            var contents = new Dictionary<string, object>
            {
                { "temperature", 8 }
            };
            var jsonPatch = new JsonPatchDocument();
            jsonPatch.AppendAdd("bar", contents);

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpMessageHelper.SerializePayload(digitalTwin),
            };

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            var digitalTwinsClient = new DigitalTwinsClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_retryHandler);

            // act
            Func<Task> act = async () => await digitalTwinsClient.UpdateAsync(digitalTwinId, jsonPatch.ToString());

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_UpdateAsync_HttpException()
        {
            // arrange
            string cs = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            string digitalTwinId = Guid.NewGuid().ToString();
            string jsonPatch = "test";
            using var serviceClient = new IotHubServiceClient(cs);
            DigitalTwinsClient digialTwinsClient = serviceClient.DigitalTwins;

            // act
            // deliberately throw http exception by searching for twin that does not exist
            Func<Task> act = async () => await digialTwinsClient.UpdateAsync(digitalTwinId, jsonPatch);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_InvokeCommandAsync()
        {
            // arrange
            var digitalTwinsClient = new Mock<DigitalTwinsClient>();
            string digitalTwinId = Guid.NewGuid().ToString();
            string commandName = "test";

            // act
            Func<Task> act = async () => await digitalTwinsClient.Object.InvokeCommandAsync(digitalTwinId, commandName);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_InvokeCommandAysnc_HttpException()
        {
            // arrange
            string cs = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            string digitalTwinId = Guid.NewGuid().ToString();
            string commandName = "test";
            using var serviceClient = new IotHubServiceClient(cs);
            DigitalTwinsClient digialTwinsClient = serviceClient.DigitalTwins;

            // act
            // deliberately throw http exception by searching for twin that does not exist
            Func<Task> act = async () => await digialTwinsClient.InvokeCommandAsync(digitalTwinId, commandName);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_InvokeComponentCommandAsync()
        {
            // arrange
            var digitalTwinsClient = new Mock<DigitalTwinsClient>();
            string digitalTwinId = Guid.NewGuid().ToString();
            string commandName = "test";
            string componentName = "test";

            // act
            Func<Task> act = async () => await digitalTwinsClient.Object.InvokeComponentCommandAsync(digitalTwinId, componentName, commandName);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_InvokeComponentCommandAsync_HttpException()
        {
            // arrange
            string cs = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            string digitalTwinId = Guid.NewGuid().ToString();
            string commandName = "test";
            string componentName = "test";
            using var serviceClient = new IotHubServiceClient(cs);
            DigitalTwinsClient digialTwinsClient = serviceClient.DigitalTwins;

            // act
            // deliberately throw http exception by searching for twin that does not exist
            Func<Task> act = async () => await digialTwinsClient.InvokeComponentCommandAsync(digitalTwinId, componentName, commandName);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }
    }
}
