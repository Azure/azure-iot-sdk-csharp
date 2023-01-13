// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Client.HsmAuthentication.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.Transport
{
    [TestClass]
    [TestCategory("Unit")]
    public class ClientWebSocketTransportTests
    {
        private const string LocalEndPoint = "/tmp/foo.sock";
        private const string RemoteEndPoint = "/tmp/bar.sock";

        [TestMethod]
        public void ClientWebSocket_CreateClient()
        {
            // arrange
            using ClientWebSocket webSocket = new ();
            EndPoint local = new UnixDomainSocketEndPoint(LocalEndPoint);
            EndPoint remote = new UnixDomainSocketEndPoint(RemoteEndPoint);

            // act
            using ClientWebSocketTransport transport = new ClientWebSocketTransport(webSocket, local, remote);

            // assert
            transport.LocalEndPoint.Should().NotBeNull();
            transport.RemoteEndPoint.Should().NotBeNull();
            transport.RequiresCompleteFrames.Should().BeTrue();
            transport.IsSecure.Should().BeTrue();
        }

        [TestMethod]
        public void ClientWebSocket_WriteAsync_NullParameter_Throws()
        {
            // arrange
            using ClientWebSocket webSocket = new();
            EndPoint local = new UnixDomainSocketEndPoint(LocalEndPoint);
            EndPoint remote = new UnixDomainSocketEndPoint(RemoteEndPoint);
            using ClientWebSocketTransport transport = new ClientWebSocketTransport(webSocket, local, remote);

            // act
            transport.SetMonitor(null);
            Action act = () => transport.WriteAsync(null);
            
            // assert
            act.Should().Throw<AmqpException>();
        }
    }
}
