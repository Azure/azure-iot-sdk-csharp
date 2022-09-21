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
    /// Handles a single AMQP session that holds the sender or receiver link that does the "work"
    /// for the AMQP connection (receiving file upload notification, sending cloud to device messages, etc).
    /// </summary>
    internal class AmqpSessionHandler
    {
        private readonly AmqpSendingLinkHandler _sendingLinkHandler;
        private readonly AmqpReceivingLinkHandler _receivingLinkHandler;
        private readonly EventHandler _connectionLossHandler;
        private readonly string _linkAddress;

        private AmqpSession _session;

        /// <summary>
        /// Construct an AMQP session for handling sending cloud to device messaging, receiving file
        /// upload notifications or receiving feedback messages.
        /// </summary>
        /// <param name="linkAddress">The link address to receive from/send to.</param>
        /// <param name="connectionLossHandler">The handler to invoke if this session or its links are dropped.</param>
        /// <param name="messageHandler">
        /// The handler for received feedback messages or file upload notifications. If this session is used
        /// for sending cloud to device messages, then this argument is ignored.
        /// </param>
        internal AmqpSessionHandler(string linkAddress, EventHandler connectionLossHandler, Action<AmqpMessage> messageHandler = null)
        {
            _linkAddress = linkAddress;

            _connectionLossHandler = connectionLossHandler;

            if (_linkAddress == AmqpsConstants.FeedbackMessageAddress
                || _linkAddress == AmqpsConstants.FileUploadNotificationsAddress)
            {
                _receivingLinkHandler = new AmqpReceivingLinkHandler(_linkAddress, messageHandler, _connectionLossHandler);
            }
            else if (_linkAddress == AmqpsConstants.CloudToDeviceMessageAddress)
            {
                _sendingLinkHandler = new AmqpSendingLinkHandler(_linkAddress, _connectionLossHandler);
            }
            else
            {
                // Should not happen since link addresses are hardcoded. If this throws, there is a bug in the SDK.
                throw new IotHubServiceException($"Unexpected link address {linkAddress}");
            }
        }

        /// <summary>
        /// Returns true if this session and its link are open. Returns false otherwise.
        /// </summary>
        /// <returns>True if this session and its link are open. False otherwise.</returns>
        internal bool IsOpen
        {
            get
            {
                // If one link is not null and open. The other link is not expected to be non-null.
                bool linkIsOpen = _receivingLinkHandler != null && _receivingLinkHandler.IsOpen
                    || _sendingLinkHandler != null && _sendingLinkHandler.IsOpen;

                return _session != null
                    && _session.State == AmqpObjectState.Opened
                    && linkIsOpen;
            }
        }

        /// <summary>
        /// Opens the session and then opens the worker link.
        /// </summary>
        /// <param name="connection">The connection to open this session on.</param>
        /// <param name="cancellationToken">The timeout for the open operation.</param>
        internal async Task OpenAsync(AmqpConnection connection, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Opening worker session.", nameof(OpenAsync));

            try
            {
                var sessionSettings = new AmqpSessionSettings
                {
                    Properties = new Fields(),
                    Descriptor = "Worker"
                };

                _session = connection.CreateSession(sessionSettings);
                _session.Closed += _connectionLossHandler;
                await _session.OpenAsync(cancellationToken).ConfigureAwait(false);

                if (_sendingLinkHandler != null)
                {
                    await _sendingLinkHandler.OpenAsync(_session, cancellationToken).ConfigureAwait(false);
                }

                if (_receivingLinkHandler != null)
                {
                    await _receivingLinkHandler.OpenAsync(_session, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Opening worker session.", nameof(OpenAsync));
            }
        }

        /// <summary>
        /// Closes the session then closes the worker link.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Closing worker session.", nameof(CloseAsync));

            try
            {
                if (_sendingLinkHandler != null)
                {
                    await _sendingLinkHandler.CloseAsync(cancellationToken).ConfigureAwait(false);
                }

                if (_receivingLinkHandler != null)
                {
                    await _receivingLinkHandler.CloseAsync(cancellationToken).ConfigureAwait(false);
                }

                if (_session != null)
                {
                    await _session.CloseAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Closing worker session.", nameof(CloseAsync));
            }
        }

        /// <summary>
        /// Sends the cloud to device message via the worker link.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="deliveryTag">The message delivery tag. Used for correlating messages and acknowledgements.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal async Task<Outcome> SendAsync(AmqpMessage message, ArraySegment<byte> deliveryTag, CancellationToken cancellationToken)
        {
            if (_sendingLinkHandler == null)
            {
                // Should never happen because it means a service client constructed an AMQP connection
                // to receive file upload notifications or feedback messages but then tried to send a cloud to device message.
                throw new IotHubServiceException("Cannot send the message because no sender links are open yet.");
            }

            return await _sendingLinkHandler.SendAsync(message, deliveryTag, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Acknowledges the received file upload notification or feedback message that was received with the provied delivery tag.
        /// </summary>
        /// <param name="deliveryTag">The delivery tag of the message to acknowledge.</param>
        /// <param name="outcome">The acknowledgement type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        internal async Task AcknowledgeMessageAsync(ArraySegment<byte> deliveryTag, Outcome outcome, CancellationToken cancellationToken)
        {
            if (_receivingLinkHandler == null)
            {
                // Should never happen because it means a service client constructed an AMQP connection
                // to send cloud to device messages but then tried to acknowledge a file upload notification or feedback message.
                throw new IotHubServiceException("Cannot acknowledge the message because no receiver links are open yet.");
            }

            await _receivingLinkHandler.AcknowledgeMessageAsync(deliveryTag, outcome, cancellationToken).ConfigureAwait(false);
        }
    }
}
