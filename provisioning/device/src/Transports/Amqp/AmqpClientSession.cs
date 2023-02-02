// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class AmqpClientSession
    {
        private readonly AmqpClientConnection _amqpConnection;

        public AmqpClientSession(AmqpClientConnection amqpTestConnection)
        {
            _amqpConnection = amqpTestConnection;
            AmqpSessionSettings = new AmqpSessionSettings();
        }

        // For unit testing purpose only.
        internal AmqpClientSession()
        { }

        internal AmqpSession AmqpSession { get; set; }

        public AmqpSessionSettings AmqpSessionSettings { get; private set; }

        public AmqpClientLink SendingLink { get; private set; }

        public AmqpClientLink ReceivingLink { get; private set; }

        public bool IsSessionClosed { get; private set; }

        public async Task OpenAsync(CancellationToken cancellationToken)
        {
            // Create the Session
            var amqpTestLinkFactory = new AmqpLinkFactory();
            AmqpSession = new AmqpSession(_amqpConnection.AmqpConnection, AmqpSessionSettings, amqpTestLinkFactory);
            _amqpConnection.AmqpConnection.AddSession(AmqpSession, new ushort?());
            AmqpSession.Closed += OnSessionClosed;
            await AmqpSession.OpenAsync(cancellationToken).ConfigureAwait(false);
            IsSessionClosed = false;
        }

        public AmqpClientLink CreateSendingLink(Address address)
        {
            SendingLink = CreateLink();
            SendingLink.AmqpLinkSettings.SettleType = SettleMode.SettleOnDispose;
            SendingLink.AmqpLinkSettings.Role = false; // sending link
            SendingLink.AmqpLinkSettings.Target = new Target
            {
                Address = address
            };

            return SendingLink;
        }

        public AmqpClientLink CreateReceivingLink(Address address)
        {
            ReceivingLink = CreateLink();
            ReceivingLink.AmqpLinkSettings.SettleType = SettleMode.SettleOnReceive;
            ReceivingLink.AmqpLinkSettings.Role = true; // receiving link
            ReceivingLink.AmqpLinkSettings.Source = new Source
            {
                Address = address
            };

            return ReceivingLink;
        }

        public AmqpClientLink CreateLink()
        {
            return new AmqpClientLink(this);
        }

        internal void OnSessionClosed(object o, EventArgs args)
        {
            IsSessionClosed = true;
        }
    }
}
