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
    /// Handles a single AMQP sending link.
    /// </summary>
    internal sealed class AmqpSendingLinkHandler
    {
        private readonly EventHandler _connectionLossHandler;
        private readonly string _linkAddress;

        private SendingAmqpLink _sendingLink;
        private string _linkName;

        public AmqpSendingLinkHandler(string linkAddress, EventHandler connectionLossHandler)
        {
            _linkAddress = linkAddress;
            _connectionLossHandler = connectionLossHandler;
        }

        /// <summary>
        /// Returns true if this link is open. Returns false otherwise.
        /// </summary>
        /// <returns>True if this link is open. False otherwise.</returns>
        public bool IsOpen => _sendingLink != null
            && _sendingLink.State == AmqpObjectState.Opened;

        public async Task OpenAsync(AmqpSession session, CancellationToken cancellationToken)
        {
            // By using a unique guid in the link's name, it becomes possible to correlate logs where a user
            // may have multiple instances of this type of link open. It also makes it easier to correlate
            // the state of this link with the service side logs if need be.
            _linkName = "CloudToDevieMessageSenderLink-" + Guid.NewGuid();

            if (Logging.IsEnabled)
                Logging.Enter(this, $"Opening sending link with address {_linkAddress} and link name {_linkName}", nameof(OpenAsync));

            try
            {
                var senderSettings = new AmqpLinkSettings
                {
                    Role = false, // "false" here means it is a sender. If false, it would be a receiver.
                    InitialDeliveryCount = 0,
                    Target = new Target { Address = _linkAddress },
                    SndSettleMode = null, // SenderSettleMode.Unsettled (null as it is the default and to avoid bytes on the wire)
                    RcvSettleMode = null, // (byte)ReceiverSettleMode.First (null as it is the default and to avoid bytes on the wire)
                    LinkName = _linkName,
                };

                string clientVersion = Utils.GetClientVersion();
                senderSettings.AddProperty(AmqpsConstants.ClientVersion, clientVersion);

                _sendingLink = new SendingAmqpLink(senderSettings);
                _sendingLink.Closed += _connectionLossHandler;
                _sendingLink.AttachTo(session);

                await _sendingLink.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Opening sending link with address {_linkAddress} and link name {_linkName}", nameof(OpenAsync));
            }
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Closing sending link with address {_linkAddress} and link name {_linkName}", nameof(CloseAsync));

            try
            {
                if (_sendingLink != null)
                {
                    await _sendingLink.CloseAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Closing sending link with address {_linkAddress} and link name {_linkName}", nameof(CloseAsync));
            }
        }

        public async Task<Outcome> SendAsync(AmqpMessage message, ArraySegment<byte> deliveryTag, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Sending message with correlation Id {message.Properties?.CorrelationId} and delivery tag {deliveryTag} on link with address {_linkAddress} and link name {_linkName}", nameof(SendAsync));

            try
            {
                return await _sendingLink.SendMessageAsync(message, deliveryTag, AmqpConstants.NullBinary, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Sending message with correlation Id {message.Properties?.CorrelationId} and delivery tag {deliveryTag} on link with address {_linkAddress} and link name {_linkName}", nameof(SendAsync));
            }
        }
    }
}
