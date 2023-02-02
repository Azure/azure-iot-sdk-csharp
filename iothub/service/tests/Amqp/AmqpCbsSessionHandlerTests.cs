// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests.Amqp
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpCbsSessionHandlerTests
    {
        private const string HostName = "contoso.azure-devices.net";

        [TestMethod]
        public void AmqpCbsSessionHandler_OpenAsync()
        {
            // arrange
            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);
            EventHandler ConnectionLossHandler = (object sender, EventArgs e) => { };

            using var cbsSessionHandler = new MockableAmqpCbsSessionHandler(tokenCredentialProperties, ConnectionLossHandler);
            var mockAmqpCbsLink = new Mock<MockableAmqpCbsLink>();
            DateTime tokenRefreshesAt = DateTime.UtcNow.AddMinutes(11);
            mockAmqpCbsLink
                .Setup(l => l.SendTokenAsync(It.IsAny<IotHubConnectionProperties>(), It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tokenRefreshesAt);

            var ct = new CancellationToken();

            // act
            Func<Task> act = async () => await cbsSessionHandler.OpenAsync(mockAmqpCbsLink.Object, ct).ConfigureAwait(false);
            act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async void AmqpCbsSessionHandler__Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);
            EventHandler ConnectionLossHandler = (object sender, EventArgs e) => { };

            var mockAmqpCbsLink = new Mock<MockableAmqpCbsLink>();
            DateTime tokenRefreshesAt = DateTime.UtcNow.AddMinutes(11);
            mockAmqpCbsLink
                .Setup(l => l.SendTokenAsync(It.IsAny<IotHubConnectionProperties>(), It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tokenRefreshesAt);

            using var cbsSessionHandler = new MockableAmqpCbsSessionHandler(tokenCredentialProperties, ConnectionLossHandler);
            var ct = new CancellationToken();
            await cbsSessionHandler.OpenAsync(mockAmqpCbsLink.Object, ct);

            cbsSessionHandler.IsOpen().Should().BeTrue();
            cbsSessionHandler.Close();

            // act
            cbsSessionHandler.IsOpen().Should().BeFalse();
        }
    }
}
