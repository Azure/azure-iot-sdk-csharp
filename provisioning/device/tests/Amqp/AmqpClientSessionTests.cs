// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpClientSessionTests
    {
        private static readonly string s_address = "fake-id-scope/registrations/registration-id";

        [TestMethod]
        public void AmqpClientSession_CreateSendingLink()
        {
            // arrange

            var mockClientSession = new Mock<AmqpClientSession>();

            var expectedTarget = new Target
            {
                Address = s_address,
            };

            // act
            AmqpClientLink sendingLink = mockClientSession.Object.CreateSendingLink(s_address);

            // assert
            sendingLink.AmqpLinkSettings.SettleType.Should().Be(SettleMode.SettleOnDispose);
            sendingLink.AmqpLinkSettings.Role.Should().BeFalse();
            sendingLink.AmqpLinkSettings.Target.Should().BeEquivalentTo(expectedTarget);
        }

        [TestMethod]
        public void AmqpClientSession_CreateReceivingLink()
        {
            // arrange

            var mockClientSession = new Mock<AmqpClientSession>();

            var expectedSource = new Source
            {
                Address = s_address,
            };

            // act
            AmqpClientLink receivingLink = mockClientSession.Object.CreateReceivingLink(s_address);

            // assert
            receivingLink.AmqpLinkSettings.SettleType.Should().Be(SettleMode.SettleOnReceive);
            receivingLink.AmqpLinkSettings.Role.Should().BeTrue();
            receivingLink.AmqpLinkSettings.Source.Should().BeEquivalentTo(expectedSource);
        }

        [TestMethod]
        public void AmqpClientSession_OnSessionClosed()
        {
            // arrange
            var mockClientSession = new Mock<AmqpClientSession>();

            // act
            Action act = () => mockClientSession.Object.OnSessionClosed(null, new EventArgs());

            // assert
            act.Should().NotThrow();
            mockClientSession.Object.IsSessionClosed.Should().BeTrue();
        }
    }
}
