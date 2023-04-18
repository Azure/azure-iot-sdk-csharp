﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal sealed class AmqpIotSendingLink
    {
        public event EventHandler Closed;

        private readonly SendingAmqpLink _sendingAmqpLink;

        public AmqpIotSendingLink(SendingAmqpLink sendingAmqpLink)
        {
            _sendingAmqpLink = sendingAmqpLink;
            _sendingAmqpLink.Closed += SendingAmqpLinkClosed;
        }

        // This event handler is not invoked by the AMQP library in an async fashion.
        // This also co-relates with the fact that SendingAmqpLink.SafeClose() is a sync method.
        private void SendingAmqpLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(SendingAmqpLinkClosed));

            Closed?.Invoke(this, e);

            // After the Closed event handler has been invoked, the SendingAmqpLink has now been effectively cleaned up.
            // This is a good point for us to detach the Closed event handler from the SendingAmqpLink instance.
            _sendingAmqpLink.Closed -= SendingAmqpLinkClosed;

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(SendingAmqpLinkClosed));
        }

        internal Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(CloseAsync));

            return _sendingAmqpLink.CloseAsync(cancellationToken);
        }

        internal void SafeClose()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(SafeClose));

            _sendingAmqpLink.SafeClose();

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(SafeClose));
        }

        internal bool IsClosing()
        {
            return _sendingAmqpLink.IsClosing();
        }

        #region Telemetry handling

        internal async Task<AmqpIotOutcome> SendMessageAsync(TelemetryMessage message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, nameof(SendMessageAsync));

            // After this message is sent, we will return the outcome that has no references to the message
            // So it can safely be disposed.
            using AmqpMessage amqpMessage = AmqpIotMessageConverter.OutgoingMessageToAmqpMessage(message);
            Outcome outcome = await SendAmqpMessageAsync(amqpMessage, cancellationToken).ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Exit(this, message, nameof(SendMessageAsync));

            return new AmqpIotOutcome(outcome);
        }

        internal async Task<AmqpIotOutcome> SendMessagesAsync(IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(SendMessagesAsync));

            cancellationToken.ThrowIfCancellationRequested();

            // List to hold messages in AMQP friendly format
            var messageList = new List<Data>(messages.Count());

            foreach (TelemetryMessage message in messages)
            {
                using AmqpMessage amqpMessage = AmqpIotMessageConverter.OutgoingMessageToAmqpMessage(message);
                var data = new Data
                {
                    Value = AmqpIotMessageConverter.ReadStream(amqpMessage.ToStream()),
                };
                messageList.Add(data);
            }

            using var batchMessage = AmqpMessage.Create(messageList);
            batchMessage.MessageFormat = AmqpConstants.AmqpBatchedMessageFormat;
            Outcome outcome = await SendAmqpMessageAsync(batchMessage, cancellationToken).ConfigureAwait(false);
            var amqpIotOutcome = new AmqpIotOutcome(outcome);
            amqpIotOutcome.ThrowIfNotAccepted();

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(SendMessagesAsync));

            return amqpIotOutcome;
        }

        private async Task<Outcome> SendAmqpMessageAsync(AmqpMessage amqpMessage, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(SendAmqpMessageAsync));

            try
            {
                return await _sendingAmqpLink
                    .SendMessageAsync(
                        amqpMessage,
                        new ArraySegment<byte>(Guid.NewGuid().ToByteArray()),
                        AmqpConstants.NullBinary,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (!cancellationToken.IsCancellationRequested)
            {
                // OperationCanceledException may be thrown here when there is networking disconnect
                // even if cancellation has not been requested yet. This case is treated as a transient
                // network error rather than an OperationCanceledException.
                throw new IotHubClientException(oce.Message, IotHubClientErrorCode.NetworkErrors, oce);
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
                Exception iotEx = AmqpIotExceptionAdapter.ConvertToIotHubException(ex, _sendingAmqpLink);
                if (ReferenceEquals(ex, iotEx))
                {
                    throw;
                }

                if (iotEx is IotHubClientException hubEx && hubEx.InnerException is AmqpException)
                {
                    _sendingAmqpLink.SafeClose();
                    throw iotEx;
                }

                throw iotEx;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(SendAmqpMessageAsync));
            }
        }

        #endregion Telemetry handling

        #region Method handling

        internal async Task<AmqpIotOutcome> SendMethodResponseAsync(DirectMethodResponse methodResponse, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, methodResponse, nameof(SendMethodResponseAsync));

            cancellationToken.ThrowIfCancellationRequested();

            using AmqpMessage amqpMessage = AmqpIotMessageConverter.ConvertDirectMethodResponseToAmqpMessage(methodResponse);
            AmqpIotMessageConverter.PopulateAmqpMessageFromMethodResponse(amqpMessage, methodResponse);

            Outcome outcome = await SendAmqpMessageAsync(amqpMessage, cancellationToken).ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(SendMethodResponseAsync));

            return new AmqpIotOutcome(outcome);
        }

        #endregion Method handling

        #region Twin handling

        internal async Task<AmqpIotOutcome> SendTwinGetMessageAsync(string correlationId, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(SendTwinGetMessageAsync));

            using var amqpMessage = AmqpMessage.Create();
            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.MessageAnnotations.Map["operation"] = "GET";

            Outcome outcome = await SendAmqpMessageAsync(amqpMessage, cancellationToken).ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(SendTwinGetMessageAsync));

            return new AmqpIotOutcome(outcome);
        }

        internal async Task<AmqpIotOutcome> SendTwinPatchMessageAsync(
            string correlationId,
            ReportedProperties reportedProperties,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(SendTwinPatchMessageAsync));

            var bodyStream = new MemoryStream(reportedProperties.GetObjectBytes());

            using var amqpMessage = AmqpMessage.Create(bodyStream, true);
            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.MessageAnnotations.Map["operation"] = "PATCH";
            amqpMessage.MessageAnnotations.Map["resource"] = "/properties/reported";
            amqpMessage.MessageAnnotations.Map["version"] = null;

            Outcome outcome = await SendAmqpMessageAsync(amqpMessage, cancellationToken).ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(SendTwinPatchMessageAsync));

            return new AmqpIotOutcome(outcome);
        }

        internal async Task<AmqpIotOutcome> SubscribeToDesiredPropertiesAsync(string correlationId, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(SubscribeToDesiredPropertiesAsync));

            using var amqpMessage = AmqpMessage.Create();
            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.MessageAnnotations.Map["operation"] = "PUT";
            amqpMessage.MessageAnnotations.Map["resource"] = "/notifications/twin/properties/desired";
            amqpMessage.MessageAnnotations.Map["version"] = null;

            Outcome outcome = await SendAmqpMessageAsync(amqpMessage, cancellationToken).ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(SubscribeToDesiredPropertiesAsync));

            return new AmqpIotOutcome(outcome);
        }

        #endregion Twin handling
    }
}
