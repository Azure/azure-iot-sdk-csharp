﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Api.Test
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    [TestCategory("Unit")]
    public class ServiceClientTests
    {
        [TestMethod]
        public async Task PurgeMessageQueueWithCancellationTokenTest()
        {
            // Arrange Moq
            Tuple<Mock<IHttpClientHelper>, ServiceClient, PurgeMessageQueueResult> setupParameters = this.SetupPurgeMessageQueueTests();
            Mock<IHttpClientHelper> restOpMock = setupParameters.Item1;
            ServiceClient serviceClient = setupParameters.Item2;
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
            Func<TimeSpan, Task<AmqpSession>> onCreate = _ => Task.FromResult(new AmqpSession(null, new AmqpSessionSettings(), null));
            Action<AmqpSession> onClose = _ => { };
            // Instantiate AmqpServiceClient with Mock IHttpClientHelper and IotHubConnection
            var connection = new IotHubConnection(onCreate, onClose);
            var serviceClient = new ServiceClient(connection, null, restOpMock.Object, null);

            // Execute method under test
            PurgeMessageQueueResult result = await serviceClient.PurgeMessageQueueAsync("TestDevice", CancellationToken.None).ConfigureAwait(false);
        }

        private Tuple<Mock<IHttpClientHelper>, ServiceClient, PurgeMessageQueueResult> SetupPurgeMessageQueueTests()
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
            Func<TimeSpan, Task<AmqpSession>> onCreate = _ => Task.FromResult(new AmqpSession(null, new AmqpSessionSettings(), null));
            Action<AmqpSession> onClose = _ => { };
            // Instantiate AmqpServiceClient with Mock IHttpClientHelper and IotHubConnection
            var connection = new IotHubConnection(onCreate, onClose);
            var serviceClient = new ServiceClient(connection, null, restOpMock.Object, null);

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
            var serviceClient = new ServiceClient(connection, null, restOpMock.Object, null);
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
            var serviceClient = new ServiceClient(connection, null, restOpMock.Object, null);
            // This is required to cause onClose callback invocation.
            await connection.OpenAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
            restOpMock.Verify(restOp => restOp.Dispose(), Times.Never());
            Assert.IsTrue(connectionClosed);
        }
    }
}
