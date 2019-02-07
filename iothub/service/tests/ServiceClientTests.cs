// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Api.Test
{
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    [TestCategory("Unit")]
    public class ServiceClientTests
    {
        [TestMethod]
        public async Task PurgeMessageQueueTest()
        {
            // Arrange Moq
            Tuple<Mock<IHttpClientHelper>, AmqpServiceClient, PurgeMessageQueueResult> setupParameters = this.SetupPurgeMessageQueueTests();
            Mock<IHttpClientHelper> restOpMock = setupParameters.Item1;
            AmqpServiceClient serviceClient = setupParameters.Item2;
            PurgeMessageQueueResult expectedResult = setupParameters.Item3;

            // Execute method under test
            PurgeMessageQueueResult result = await serviceClient.PurgeMessageQueueAsync("TestDevice").ConfigureAwait(false);

            // Verify expected result
            Assert.AreSame(expectedResult, result);
            restOpMock.VerifyAll();
        }

        [TestMethod]
        public async Task PurgeMessageQueueWithCancellationTokenTest()
        {
            // Arrange Moq
            Tuple<Mock<IHttpClientHelper>, AmqpServiceClient, PurgeMessageQueueResult> setupParameters = this.SetupPurgeMessageQueueTests();
            Mock<IHttpClientHelper> restOpMock = setupParameters.Item1;
            AmqpServiceClient serviceClient = setupParameters.Item2;
            PurgeMessageQueueResult expectedResult = setupParameters.Item3;

            // Execute method under test
            PurgeMessageQueueResult result = await serviceClient.PurgeMessageQueueAsync("TestDevice", CancellationToken.None).ConfigureAwait(false);

            // Verify expected result
            Assert.AreSame(expectedResult, result);
            restOpMock.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(DeviceNotFoundException))]
        public async Task PurgeMessageQueueDeviceNotFoundTest()
        {
            // Arrange Moq
            var restOpMock = new Mock<IHttpClientHelper>();

            restOpMock.Setup(restOp => restOp.DeleteAsync<PurgeMessageQueueResult>(
                It.IsAny<Uri>(), It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(), null, It.IsAny<CancellationToken>())
                ).Throws(new DeviceNotFoundException("device-id"));

            // Instantiate AmqpServiceClient with Mock IHttpClientHelper
            var authMethod = new ServiceAuthenticationWithSharedAccessPolicyKey("test", "CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            var builder = IotHubConnectionStringBuilder.Create("acme.azure-devices.net", authMethod);
            var serviceClient = new AmqpServiceClient(builder.ToIotHubConnectionString(), false, restOpMock.Object);

            // Execute method under test
            PurgeMessageQueueResult result = await serviceClient.PurgeMessageQueueAsync("TestDevice", CancellationToken.None).ConfigureAwait(false);
        }

        Tuple<Mock<IHttpClientHelper>, AmqpServiceClient, PurgeMessageQueueResult> SetupPurgeMessageQueueTests()
        {
            // Create expected return object
            var deviceId = "TestDevice";
            var expectedResult = new PurgeMessageQueueResult()
            {
                DeviceId = deviceId,
                TotalMessagesPurged = 1
            };

            // Mock IHttpClientHelper to return expected object on DeleteAsync
            var restOpMock = new Mock<IHttpClientHelper>();

            restOpMock.Setup(restOp => restOp.DeleteAsync<PurgeMessageQueueResult>(
                It.IsAny<Uri>(), It.IsAny<IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>>(), null, It.IsAny<CancellationToken>())
                ).ReturnsAsync(expectedResult);

            // Instantiate AmqpServiceClient with Mock IHttpClientHelper
            var authMethod = new ServiceAuthenticationWithSharedAccessPolicyKey("test", "CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            var builder = IotHubConnectionStringBuilder.Create("acme.azure-devices.net", authMethod);
            var serviceClient = new AmqpServiceClient(builder.ToIotHubConnectionString(), false, restOpMock.Object);

            return Tuple.Create(restOpMock, serviceClient, expectedResult);
        }

        [TestMethod]
        public async Task DisposeTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.Dispose());
            var connectionClosed = false;
            Func<TimeSpan, Task<AmqpSession>> onCreate = _ => Task.FromResult(new AmqpSession(null, new AmqpSessionSettings(), null));
            Action<AmqpSession> onClose = _ => { connectionClosed = true; };
            // Instantiate AmqpServiceClient with Mock IHttpClientHelper and IotHubConnection
            var connection = new IotHubConnection(onCreate, onClose);
            var serviceClient = new AmqpServiceClient(connection, restOpMock.Object);
            // This is required to cause onClose callback invocation.
            await connection.OpenAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            serviceClient.Dispose();
            restOpMock.Verify(restOp => restOp.Dispose(), Times.Once());
            Assert.IsTrue(connectionClosed);
        }

        [TestMethod]
        public async Task CloseAsyncTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.Dispose());
            var connectionClosed = false;
            Func<TimeSpan, Task<AmqpSession>> onCreate = _ => Task.FromResult(new AmqpSession(null, new AmqpSessionSettings(), null));
            Action<AmqpSession> onClose = _ => { connectionClosed = true; };

            // Instantiate AmqpServiceClient with Mock IHttpClientHelper and IotHubConnection
            var connection = new IotHubConnection(onCreate, onClose);
            var serviceClient = new AmqpServiceClient(connection, restOpMock.Object);
            // This is required to cause onClose callback invocation.
            await connection.OpenAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
            restOpMock.Verify(restOp => restOp.Dispose(), Times.Never());
            Assert.IsTrue(connectionClosed);
        }

        #region Device Streaming
        const string DS_Http_Resp_Header_Is_Accepted = "iothub-streaming-is-accepted";
        const string DS_Http_Resp_Header_Url = "iothub-streaming-url";
        const string DS_Http_Resp_Header_Auth_Token = "iothub-streaming-auth-token";
        const string FakeDeviceStreamSGWUrl = "wss://sgw.eastus2euap-001.streams.azure-devices.net/bridges/iot-sdks-tcpstreaming/E2E_DeviceStreamingTests_Sasl_f88fd19b-ed0d-496b-b32c-6346ca61d289/E2E_DeviceStreamingTests_b82c9ec4-4fb3-432a-bfb5-af484966a7d4c002f7a841b8/3a6a2eba4b525c38bfcb";
        const string FakeDeviceStreamAuthToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJleHAiOjE1NDgzNTU0ODEsImp0aSI6InFfdlllQkF4OGpmRW5tTWFpOHhSNTM2QkpxdTZfRlBOa2ZWSFJieUc4bUUiLCJpb3RodWIRrcy10Y3BzdHJlYW1pbmciOiJpb3Qtc2ifQ.X_HIb53nDsCT2SZ0P4-vnA_Wz94jxYRLbk_5nvP9bj8";

        [TestMethod]
        public async Task CreateStreamAsyncDeviceClientAccepts()
        {
            await TestCreateStreamAsync("myDevice01", null, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CreateStreamAsyncDeviceClientRejects()
        {
            await TestCreateStreamAsync("myDevice01", null, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CreateStreamAsyncModuleClientAccepts()
        {
            await TestCreateStreamAsync("myDevice01", "myModule01", true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CreateStreamAsyncModuleClientRejects()
        {
            await TestCreateStreamAsync("myDevice01", "myModule01", false).ConfigureAwait(false);
        }

        private static async Task TestCreateStreamAsync(string deviceId, string moduleId, bool acceptRequest)
        {
            // arrange
            string streamName = "StreamA";

            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.Dispose());
            Func<TimeSpan, Task<AmqpSession>> onCreate = _ => Task.FromResult(new AmqpSession(null, new AmqpSessionSettings(), null));
            Action<AmqpSession> onClose = _ => { };

            // Instantiate AmqpServiceClient with Mock IHttpClientHelper and IotHubConnection
            var connection = new IotHubConnection(onCreate, onClose);
            var serviceClient = new AmqpServiceClient(connection, restOpMock.Object);

            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
            httpResponse.Headers.Add(DS_Http_Resp_Header_Is_Accepted, acceptRequest ? "true" : "false");
            httpResponse.Headers.Add(DS_Http_Resp_Header_Url, FakeDeviceStreamSGWUrl);
            httpResponse.Headers.Add(DS_Http_Resp_Header_Auth_Token, FakeDeviceStreamAuthToken);

            Uri requestUri;

            if (String.IsNullOrEmpty(moduleId))
            {
                requestUri = new Uri($"/twins/{WebUtility.UrlEncode(deviceId)}/streams/{streamName}?{ClientApiVersionHelper.ApiVersionQueryString}", UriKind.Relative);
            }
            else
            {
                requestUri = new Uri($"/twins/{WebUtility.UrlEncode(deviceId)}/modules/{WebUtility.UrlEncode(moduleId)}/streams/{streamName}?{ClientApiVersionHelper.ApiVersionQueryString}", UriKind.Relative);
            }

            restOpMock.Setup(m => m.PostAsync<byte[], HttpResponseMessage>(
                requestUri,
                null as byte[],
                It.IsAny<TimeSpan>(),
                null,
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(httpResponse));

            // run
            DeviceStreamRequest request = new DeviceStreamRequest(streamName);
            DeviceStreamResponse response;

            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
            {
                if (String.IsNullOrEmpty(moduleId))
                {
                    response = await serviceClient.CreateStreamAsync(deviceId, request, cts.Token).ConfigureAwait(false);
                }
                else
                {
                    response = await serviceClient.CreateStreamAsync(deviceId, moduleId, request, cts.Token).ConfigureAwait(false);
                }
            }

            // assert
            Assert.IsNotNull(response);
            Assert.AreEqual(response.StreamName, streamName);

            if (acceptRequest)
            {
                Assert.IsTrue(response.IsAccepted);
                Assert.AreEqual(response.Url.ToString(), FakeDeviceStreamSGWUrl);
                Assert.AreEqual(response.AuthorizationToken, FakeDeviceStreamAuthToken);
            }
            else
            {
                Assert.IsFalse(response.IsAccepted);
                Assert.IsNull(response.Url);
                Assert.IsNull(response.AuthorizationToken);
            }

        }

        #endregion Device Streaming
    }
}
