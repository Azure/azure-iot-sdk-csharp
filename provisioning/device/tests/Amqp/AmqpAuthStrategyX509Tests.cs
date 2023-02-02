// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpAuthStrategyX509Tests
    {
        [TestMethod]
        public async Task AmqpAuthStrategyX509_OpenConnectionAsync()
        {
            // arrange

            var mockConnection = new Mock<AmqpClientConnection>();
            mockConnection
                .Setup(x => x.OpenAsync(
                    It.IsAny<bool>(),
                    It.IsAny<X509Certificate2>(),
                    It.IsAny<WebProxy>(),
                    It.IsAny<RemoteCertificateValidationCallback>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockAuthProvider = new Mock<AuthenticationProviderX509>();
            var mockAmqpAuthStrategy = new AmqpAuthStrategyX509(mockAuthProvider.Object);
            var mockTransportSettings = new Mock<ProvisioningClientTransportSettings>();
            var ct = new CancellationToken(false);

            // act
            Func<Task> act = async () => await mockAmqpAuthStrategy
                .OpenConnectionAsync(
                    mockConnection.Object,
                    false,
                    mockTransportSettings.Object.Proxy,
                    mockTransportSettings.Object.RemoteCertificateValidationCallback,
                    ct)
                .ConfigureAwait(false);

            // assert

            await act.Should().NotThrowAsync().ConfigureAwait(false);
            mockConnection.Verify(
                x => x.OpenAsync(
                    It.IsAny<bool>(),
                    It.IsAny<X509Certificate2>(),
                    It.IsAny<WebProxy>(),
                    It.IsAny<RemoteCertificateValidationCallback>(),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }
    }
}
