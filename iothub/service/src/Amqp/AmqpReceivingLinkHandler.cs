// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Amqp
{
    /// <summary>
    /// Handles a single AMQP receiving link.
    /// </summary>
    internal class AmqpReceivingLinkHandler
    {
        private ReceivingAmqpLink _receivingLink;
        private Action<AmqpMessage> _messageHandler;

        private EventHandler _connectionLossHandler;

        private string _linkAddress;
        private string _linkName;

        public AmqpReceivingLinkHandler(string linkAddress, Action<AmqpMessage> messageHandler, EventHandler connectionLossHandler)
        {
            _linkAddress = linkAddress;
            _messageHandler = messageHandler;
            _connectionLossHandler = connectionLossHandler;
        }

        public async Task OpenAsync(AmqpSession session, CancellationToken cancellationToken)
        {
            // By using a unique guid in the link's name, it becomes possible to correlate logs where a user
            // may have multiple instances of this type of link open. It also makes it easier to correlate
            // the state of this link with the service side logs if need be.
            _linkName = "ReceiverLink-" + Guid.NewGuid();

            if (Logging.IsEnabled)
                Logging.Enter(this, $"Opening receiving link with address {_linkAddress} and link name {_linkName}");

            try
            {
                var receiverSettings = new AmqpLinkSettings
                {
                    Role = true, // "true" here means it is a receiver. If false, it would be a sender.
                    TotalLinkCredit = 1024,
                    AutoSendFlow = true, // Automatically replenish link credit after acknowledging a message
                    Source = new Source { Address = _linkAddress },
                    SndSettleMode = null, // SenderSettleMode.Unsettled (null as it is the default and to avoid bytes on the wire)
                    RcvSettleMode = (byte)ReceiverSettleMode.Second,
                    LinkName = _linkName,
                };

                string clientVersion = Utils.GetClientVersion();
                receiverSettings.AddProperty(AmqpsConstants.ClientVersion, clientVersion);

                _receivingLink = new ReceivingAmqpLink(receiverSettings);
                _receivingLink.Closed += _connectionLossHandler;
                _receivingLink.AttachTo(session);

                _receivingLink.RegisterMessageListener(_messageHandler);
                await _receivingLink.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Opening receiving link with address {_linkAddress} and link name {_linkName}");
            }
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Closing receiving link with address {_linkAddress} and link name {_linkName}");

            try
            {
                if (_receivingLink != null)
                {
                    await _receivingLink.CloseAsync(cancellationToken);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Closing receiving link with address {_linkAddress} and link name {_linkName}");
            }
        }

        public async Task AcknowledgeMessageAsync(ArraySegment<byte> deliveryTag, Outcome outcome, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Acknowledging message with delivery tag {deliveryTag} on receiving link with address {_linkAddress} and link name {_linkName}");

            try
            {
                await _receivingLink.DisposeMessageAsync(deliveryTag, outcome, false, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Acknowledging message with delivery tag {deliveryTag} on receiving link with address {_linkAddress} and link name {_linkName}");
            }
        }

        /// <summary>
        /// Returns true if this link is open. Returns false otherwise.
        /// </summary>
        /// <returns>True if this link is open. False otherwise.</returns>
        public bool IsOpen()
        {
            return _receivingLink != null
                && _receivingLink.State == AmqpObjectState.Opened;
        }
    }
}
