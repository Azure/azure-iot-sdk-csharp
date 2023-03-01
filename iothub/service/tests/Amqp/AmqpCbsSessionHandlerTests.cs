// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests.Amqp
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpCbsSessionHandlerTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private static void ConnectionLossHandler(object sender, EventArgs e) { }

        [TestMethod]
        public void AmqpCbsSessionHandler_OpenAsync_IsOpenIsTrue()
        {
            // arrange
            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);

            using var cbsSessionHandler = new MockableAmqpCbsSessionHandler(tokenCredentialProperties, ConnectionLossHandler);
            var mockAmqpCbsLink = new Mock<MockableAmqpCbsLink>();

            mockAmqpCbsLink
                .Setup(l => l.SendTokenAsync(It.IsAny<IotHubConnectionProperties>(), It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(DateTime.UtcNow.AddMinutes(11)); // the returned value does not matter in this context.

            // act
            Func<Task> act = async () => await cbsSessionHandler.OpenAsync(mockAmqpCbsLink.Object, CancellationToken.None).ConfigureAwait(false);

            // assert
            act.Should().NotThrowAsync();
            cbsSessionHandler.IsOpen().Should().BeTrue();
        }

        [TestMethod]
        public void AmqpCbsSessionHandler_OpenAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);

            using var cbsSessionHandler = new MockableAmqpCbsSessionHandler(tokenCredentialProperties, ConnectionLossHandler);
            var mockAmqpCbsLink = new Mock<MockableAmqpCbsLink>();

            var ct = new CancellationToken(true);

            // act
            Func<Task> act = async () => await cbsSessionHandler.OpenAsync(mockAmqpCbsLink.Object, ct);

            // assert
            act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
