// Copyright (c) Microsoft. All rights reserved.
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
        public async Task DisposeTest()
        {
            var restOpMock = new Mock<IHttpClientHelper>();
            restOpMock.Setup(restOp => restOp.Dispose());
            var connectionClosed = false;
            Func<TimeSpan, Task<AmqpSession>> onCreate = _ => Task.FromResult(new AmqpSession(null, new AmqpSessionSettings(), null));
            Action<AmqpSession> onClose = _ => { connectionClosed = true; };
            // Instantiate AmqpServiceClient with Mock IHttpClientHelper and IotHubConnection
            var connection = new IotHubConnection(onCreate, onClose);
            var serviceClient = new ServiceClient(connection, restOpMock.Object);
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
            var serviceClient = new ServiceClient(connection, restOpMock.Object);
            // This is required to cause onClose callback invocation.
            await connection.OpenAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
            restOpMock.Verify(restOp => restOp.Dispose(), Times.Never());
            Assert.IsTrue(connectionClosed);
        }
    }
}
