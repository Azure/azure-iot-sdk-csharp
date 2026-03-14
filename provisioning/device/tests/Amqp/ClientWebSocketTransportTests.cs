// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests.Amqp
{
    [TestClass]
    [TestCategory("Unit")]
    public class ClientWebSocketTransportTests
    {
        [TestMethod]
        public async Task ClientWebSocketTransport_CloseInternalAsync()
        {
            // arrange
            var mockTransport = new Mock<ClientWebSocketTransport>();

            // act
            Func<Task> act = async () => await mockTransport.Object.CloseInternalAsync(TimeSpan.Zero).ConfigureAwait(false);

            // assert

            await act.Should().NotThrowAsync();
            mockTransport.Verify(x => x.CancelPendingWrite(), Times.Once);
        }
    }
}
