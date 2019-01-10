// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class AmqpClientSession
    {
        internal AmqpClientConnection amqpClientConnection { get; private set; }

        internal AmqpSession amqpSession { get; private set; }

        internal AmqpSessionSettings amqpSessionSettings { get; private set; }

        internal AmqpClientLink sendingLink { get; private set; }

        internal AmqpClientLink receivingLink { get; private set; }

        internal bool isSessionClosed { get; private set; }


        public AmqpClientSession(AmqpClientConnection amqpClientConnection)
        {
            this.amqpClientConnection = amqpClientConnection;
            amqpSessionSettings = new AmqpSessionSettings();

            var amqpLinkFactory = new AmqpLinkFactory();
            amqpLinkFactory.LinkCreated += OnLinkCreated;

            // Create Session
            amqpSession = new AmqpSession(amqpClientConnection.amqpConnection, amqpSessionSettings, amqpLinkFactory);

            // Add Session to the Connection
            amqpClientConnection.amqpConnection.AddSession(amqpSession, new ushort?());

            amqpSession.Closed += OnSessionClosed;
        }

        public async Task OpenAsync(TimeSpan timeout)
        {
            // Open Session
            await amqpSession.OpenAsync(timeout).ConfigureAwait(false);

            sendingLink = new AmqpClientLink(AmqpClientSenderLinkType.Telemetry, this, amqpClientConnection.deviceClientEndpointIdentity, timeout);
            receivingLink = new AmqpClientLink(AmqpClientReceiverLinkType.Events, this, amqpClientConnection.deviceClientEndpointIdentity, timeout);

            isSessionClosed = false;
        }

        public async Task<Outcome> SendMessageAsync(AmqpMessage amqpMessage, TimeSpan operationTimeout)
        {
            Outcome outcome = null;
            if (!isSessionClosed)
            {
                outcome = await sendingLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), operationTimeout).ConfigureAwait(false);
            }
            return outcome;
        }

        public async Task CloseAsync(TimeSpan timeout)
        {
            var session = amqpSession;
            if (session != null)
            {
                await session.CloseAsync(timeout).ConfigureAwait(false);
            }
        }

        public event EventHandler<LinkCreatedEventArgs> LinkCreated;

        protected virtual void OnLinkCreated(object sender, LinkCreatedEventArgs args)
        {
            LinkCreated?.Invoke(this, new LinkCreatedEventArgs(args.Link));
        }

        void OnSessionClosed(object o, EventArgs args)
        {
            isSessionClosed = true;
        }
    }
}
