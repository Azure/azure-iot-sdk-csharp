// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Api.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Newtonsoft.Json;

    [TestClass]
    [TestCategory("Unit")]
    public class ServiceClientTests
    {
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
            var authMethod = new ServiceAuthenticationWithSharedAccessPolicyKey("test", "dGVzdFN0cmluZzE=");
            var builder = IotHubConnectionStringBuilder.Create("acme.azure-devices.net", authMethod);
            var serviceClient = new AmqpServiceClient(restOpMock.Object);

            // Execute method under test
            PurgeMessageQueueResult result = await serviceClient.PurgeMessageQueueAsync("TestDevice", CancellationToken.None).ConfigureAwait(false);
        }

        private Tuple<Mock<IHttpClientHelper>, AmqpServiceClient, PurgeMessageQueueResult> SetupPurgeMessageQueueTests()
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
            var authMethod = new ServiceAuthenticationWithSharedAccessPolicyKey("test", "dGVzdFN0cmluZzE=");
            var builder = IotHubConnectionStringBuilder.Create("acme.azure-devices.net", authMethod);
            var serviceClient = new AmqpServiceClient(restOpMock.Object);

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

        [TestMethod]
        public void ServiceClient_DefaultMaxDepth()
        {
            // arrange
            string fakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            var serviceClinet = ServiceClient.CreateFromConnectionString(fakeConnectionString);
            // above arragement is only for setting the defaultJsonSerializerSettings

            var defaultSettings = JsonSerializerSettingsInitializer.GetDefaultJsonSerializerSettings();
            defaultSettings.MaxDepth.Should().Be(128);
        }

        [TestMethod]
        public void ServiceClient_Json_OverrideDefaultJsonSerializer_ExceedMaxDepthThrows()
        {
            // arrange
            string fakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            var serviceClinet = ServiceClient.CreateFromConnectionString(fakeConnectionString);
            // above arragement is only for setting the defaultJsonSerializerSettings

            //Create a string representation of a nested object (JSON serialized)
            int nRep = 3;
            string json = string.Concat(Enumerable.Repeat("{a:", nRep)) + "1" +
            string.Concat(Enumerable.Repeat("}", nRep));

            var settings = new JsonSerializerSettings { MaxDepth = 2 };
            //// deserialize
            // act
            Func<object> act = () => JsonConvert.DeserializeObject(json, settings);

            // assert
            act.Should().Throw<Newtonsoft.Json.JsonReaderException>();
        }
    }
}
