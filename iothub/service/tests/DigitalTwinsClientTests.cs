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
using Microsoft.Azure.Amqp;
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
            mockHttpResponse.Headers.Add("ETag", "\"AAAAAAAAAAE=\"");
            
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
            DigitalTwinGetResponse<BasicDigitalTwin> result =  await digitalTwinsClient.GetAsync<BasicDigitalTwin>(digitalTwinId).ConfigureAwait(false);

            // assert
            result.DigitalTwin.Id.Should().Be(digitalTwinId);
        }

        [TestMethod]
        public async Task DigitalTwinsClient_GetAsync_EmptyTwinIdThrows()
        {
            // arrange
            string digitalTwinId = null;
            using var serviceClient = new IotHubServiceClient(s_connectionString);

            // act
            Func<Task> act = async () => await serviceClient.DigitalTwins.GetAsync<BasicDigitalTwin>(digitalTwinId);

            // assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_GetAsync_HttpException()
        {
            // arrange
            string digitalTwinId = "foo";
            var digitalTwin = new BasicDigitalTwin
            {
                Id = digitalTwinId,
            };
            var responseMessage = new ResponseMessage2
            {
                Message = "test",
                ExceptionMessage = "test"
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = HttpMessageHelper.SerializePayload(responseMessage),
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
                StatusCode = HttpStatusCode.Accepted,
                Content = HttpMessageHelper.SerializePayload(digitalTwin),
            };
            mockHttpResponse.Headers.Add("ETag", "\"AAAAAAAAAAE=\"");
            mockHttpResponse.Headers.Add("Location", "foo");
            
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
            DigitalTwinUpdateResponse response = await digitalTwinsClient.UpdateAsync(digitalTwinId, jsonPatch.ToString());

            // assert
            response.Location.Should().Be(digitalTwinId);
        }

        [TestMethod]
        [DataRow(null, "foo")]
        [DataRow("bar", null)]
        [DataRow(null, null)]
        public async Task DigitalTwinsClient_UpdateAsync_NullParamater_Throws(string digitalTwinId, string jsonPatch)
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);

            // act
            Func<Task> act = async () => await serviceClient.DigitalTwins.UpdateAsync(digitalTwinId, jsonPatch);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_UpdateAsync_DigitalTwinNotFound_ThrowsIotHubServiceException()
        {
            // arrange
            string jsonPatch = "test";
            string digitalTwinId = "foo";
            var digitalTwin = new BasicDigitalTwin
            {
                Id = digitalTwinId,
            };
            var responseMessage = new ResponseMessage2
            {
                Message = "test",
                ExceptionMessage = "test"
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = HttpMessageHelper.SerializePayload(responseMessage),
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
            Func<Task> act = async () => await digitalTwinsClient.UpdateAsync(digitalTwinId, jsonPatch);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_InvokeCommandAsync()
        {
            // arrange
            string digitalTwinId = "foo";
            string commandName = "foo";
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
            mockHttpResponse.Headers.Add("x-ms-command-statuscode", "200");
            mockHttpResponse.Headers.Add("x-ms-request-id", "200");

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
            InvokeDigitalTwinCommandResponse response = await digitalTwinsClient.InvokeCommandAsync(digitalTwinId, commandName).ConfigureAwait(false);

            // assert
            response.Status.Should().Be(200);
        }

        [TestMethod]
        [DataRow(null, "foo")]
        [DataRow("bar", null)]
        [DataRow(null, null)]
        public async Task DigitalTwinsClient_InvokeCommandAsync_NullParamater_Throws(string digitalTwinId, string commandName)
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);

            // act
            Func<Task> act = async () => await serviceClient.DigitalTwins.InvokeCommandAsync(digitalTwinId, commandName);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_InvokeCommandAysnc_HttpException()
        {
            // arrange
            string commandName = "test";
            string digitalTwinId = "foo";
            var digitalTwin = new BasicDigitalTwin
            {
                Id = digitalTwinId,
            };
            var responseMessage = new ResponseMessage2
            {
                Message = "test",
                ExceptionMessage = "test"
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = HttpMessageHelper.SerializePayload(responseMessage),
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
            Func<Task> act = async () => await digitalTwinsClient.InvokeCommandAsync(digitalTwinId, commandName);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_InvokeComponentCommandAsync()
        {
            // arrange
            string digitalTwinId = "foo";
            string commandName = "foo";
            string componentName = "bar";
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
            mockHttpResponse.Headers.Add("x-ms-command-statuscode", "200");
            mockHttpResponse.Headers.Add("x-ms-request-id", "200");

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
            InvokeDigitalTwinCommandResponse response = await digitalTwinsClient.InvokeComponentCommandAsync(digitalTwinId, componentName, commandName);

            // assert
            response.Status.Should().Be(200);
        }

        [TestMethod]
        [DataRow(null, "foo", null)]
        [DataRow("bar", null, null)]
        [DataRow(null, null, "baz")]
        [DataRow(null, null, null)]
        public async Task DigitalTwinsClient_InvokeComponentCommandAsync_NullParamater_Throws(string digitalTwinId, string commandName, string componentName)
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(s_connectionString);

            // act
            Func<Task> act = async () => await serviceClient.DigitalTwins.InvokeComponentCommandAsync(digitalTwinId, componentName, commandName);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_InvokeComponentCommandAsync_HttpException()
        {
            // arrange
            string commandName = "test";
            string componentName = "test";
            string digitalTwinId = "foo";
            var digitalTwin = new BasicDigitalTwin
            {
                Id = digitalTwinId,
            };
            var responseMessage = new ResponseMessage2
            {
                Message = "test",
                ExceptionMessage = "test"
            };

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);
            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            using var mockHttpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = HttpMessageHelper.SerializePayload(responseMessage),
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
            Func<Task> act = async () => await digitalTwinsClient.InvokeComponentCommandAsync(digitalTwinId, componentName, commandName);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }
    }
}
