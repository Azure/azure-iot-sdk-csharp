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
    public class ServiceClientTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("API")]
        public async Task PurgeMessageQueueTest()
        {
            // Arrange Moq
            Tuple<Mock<IHttpClientHelper>, AmqpServiceClient, PurgeMessageQueueResult> setupParameters = this.SetupPurgeMessageQueueTests();
            Mock<IHttpClientHelper> restOpMock = setupParameters.Item1;
            AmqpServiceClient serviceClient = setupParameters.Item2;
            PurgeMessageQueueResult expectedResult = setupParameters.Item3;

            // Execute method under test
            PurgeMessageQueueResult result = await serviceClient.PurgeMessageQueueAsync("TestDevice");

            // Verify expected result
            Assert.AreSame(expectedResult, result);
            restOpMock.VerifyAll();
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("API")]
        public async Task PurgeMessageQueueWithCancellationTokenTest()
        {
            // Arrange Moq
            Tuple<Mock<IHttpClientHelper>, AmqpServiceClient, PurgeMessageQueueResult> setupParameters = this.SetupPurgeMessageQueueTests();
            Mock<IHttpClientHelper> restOpMock = setupParameters.Item1;
            AmqpServiceClient serviceClient = setupParameters.Item2;
            PurgeMessageQueueResult expectedResult = setupParameters.Item3;

            // Execute method under test
            PurgeMessageQueueResult result = await serviceClient.PurgeMessageQueueAsync("TestDevice", CancellationToken.None);

            // Verify expected result
            Assert.AreSame(expectedResult, result);
            restOpMock.VerifyAll();
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("API")]
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
            PurgeMessageQueueResult result = await serviceClient.PurgeMessageQueueAsync("TestDevice", CancellationToken.None);
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
        [TestCategory("CIT")]
        [TestCategory("API")]
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
            await connection.OpenAsync(TimeSpan.FromSeconds(1));
            serviceClient.Dispose();
            restOpMock.Verify(restOp => restOp.Dispose(), Times.Once());
            Assert.IsTrue(connectionClosed);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [TestCategory("API")]
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
            await connection.OpenAsync(TimeSpan.FromSeconds(1));
            await serviceClient.CloseAsync();
            restOpMock.Verify(restOp => restOp.Dispose(), Times.Never());
            Assert.IsTrue(connectionClosed);
        }
    }
}
