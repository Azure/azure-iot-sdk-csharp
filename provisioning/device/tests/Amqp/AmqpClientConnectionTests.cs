// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.WebSockets;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpClientConnectionTests
    {
        [TestMethod]
        public void AmqpClientConnection_CreateClientWebSocket()
        {
            // arrange

            var amqpSettings = new AmqpSettings();
            var mockAction = new Mock<Action>();
            var clientSettings = new ProvisioningClientAmqpSettings();
            using var connection = new AmqpClientConnection("fake-host", amqpSettings, mockAction.Object, clientSettings)
            {
                TransportSettings = new TlsTransportSettings(),
            };

            var mockProxy = new Mock<TestWebProxy>();

            // act
            ClientWebSocket socket = connection.CreateClientWebSocket(mockProxy.Object);

            // assert

            socket.Options.Proxy.Should().Be(mockProxy.Object);
        }

        internal class TestWebProxy : IWebProxy
        {
            public ICredentials Credentials { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public Uri GetProxy(Uri destination)
            {
                throw new NotImplementedException();
            }

            public bool IsBypassed(Uri host)
            {
                throw new NotImplementedException();
            }
        }
    }
}
