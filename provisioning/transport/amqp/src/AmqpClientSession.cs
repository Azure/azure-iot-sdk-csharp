// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class AmqpClientSession
    {
        private readonly AmqpClientConnection _amqpConnection;

        public AmqpClientSession(AmqpClientConnection amqpTestConnection)
        {
            _amqpConnection = amqpTestConnection;
            AmqpSessionSettings = new AmqpSessionSettings();
        }

        private bool _isSessionClosed;

        internal AmqpSession AmqpSession { get; private set; }

        public AmqpSessionSettings AmqpSessionSettings { get; private set; }

        public AmqpClientLink SendingLink { get; private set; }

        public AmqpClientLink ReceivingLink { get; private set; }

        public bool IsSessionClosed => _isSessionClosed;

        public async Task OpenAsync(CancellationToken cancellationToken)
        {
            // Create the Session
            var amqpTestLinkFactory = new AmqpLinkFactory();
            amqpTestLinkFactory.LinkCreated += OnLinkCreated;
            AmqpSession = new AmqpSession(_amqpConnection.AmqpConnection, AmqpSessionSettings, amqpTestLinkFactory);
            _amqpConnection.AmqpConnection.AddSession(AmqpSession, new ushort?());
            AmqpSession.Closed += OnSessionClosed;
            await AmqpSession.OpenAsync(cancellationToken).ConfigureAwait(false);
            _isSessionClosed = false;
        }

        public AmqpClientLink CreateSendingLink(Address address)
        {
            SendingLink = CreateLink();
            SendingLink.AmqpLinkSettings.SettleType = SettleMode.SettleOnDispose;
            SendingLink.AmqpLinkSettings.Role = false;  // sending link
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

        public event EventHandler<LinkCreatedEventArgs> LinkCreated;

        protected virtual void OnLinkCreated(object sender, LinkCreatedEventArgs args)
        {
            LinkCreated?.Invoke(this, new LinkCreatedEventArgs(args.Link));
        }

        void OnSessionClosed(object o, EventArgs args)
        {
            _isSessionClosed = true;
        }
    }
}
