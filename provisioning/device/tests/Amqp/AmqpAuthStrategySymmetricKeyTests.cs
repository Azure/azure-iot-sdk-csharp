// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpAuthStrategySymmetricKeyTests
    {
        private static readonly string s_fakeRegistrationId = "registrationId";
        private static readonly string s_fakePrimaryKey = "dGVzdFN0cmluZbB=";
        private static readonly string s_fakeSecondaryKey = "wGVzdFN9CmluZaA=";

        [TestMethod]
        public void AmqpAuthStrategySymmetricKey_CreateAmqpSettings()
        {
            // arrange

            var authProvider = new AuthenticationProviderSymmetricKey(s_fakeRegistrationId, s_fakePrimaryKey, s_fakeSecondaryKey);
            var amqpAuthStrategy = new AmqpAuthStrategySymmetricKey(authProvider);

            // act
            AmqpSettings settings = amqpAuthStrategy.CreateAmqpSettings("fake-id-scope");

            // assert
            settings.TransportProviders.First().Versions.First().Should().Be(AmqpConstants.DefaultProtocolVersion);
        }

        [TestMethod]
        public async Task AmqpAuthStrategySymmetricKey_OpenConnectionAsync()
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

            var mockAuthProvider = new Mock<AuthenticationProviderSymmetricKey>();
            var mockAmqpAuthStrategy = new AmqpAuthStrategySymmetricKey(mockAuthProvider.Object);
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

        [TestMethod]
        public void AmqpAuthStrategySymmetricKey_CreateConnection()
        {
            // arrange

            var authProvider = new AuthenticationProviderSymmetricKey(s_fakeRegistrationId, s_fakePrimaryKey, s_fakeSecondaryKey);
            var amqpAuthStrategy = new AmqpAuthStrategySymmetricKey(authProvider);

            var mockAction = new Mock<Action>();
            var mockClientSettings = new ProvisioningClientAmqpSettings();

            // act

            AmqpClientConnection connection = amqpAuthStrategy.CreateConnection("fake-host", "fake-id-scope", mockAction.Object, mockClientSettings);
            AmqpSettings settings = connection.AmqpSettings;

            // assert
            settings.TransportProviders.First().Versions.First().Should().Be(AmqpConstants.DefaultProtocolVersion);
        }
    }
}
