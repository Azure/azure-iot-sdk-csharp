// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTSendingLink
    {
        public event EventHandler Closed;

        private readonly SendingAmqpLink _sendingAmqpLink;

        public AmqpIoTSendingLink(SendingAmqpLink sendingAmqpLink)
        {
            _sendingAmqpLink = sendingAmqpLink;
            _sendingAmqpLink.Closed += SendingAmqpLinkClosed;
        }

        private void SendingAmqpLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, $"{nameof(SendingAmqpLinkClosed)}");
            }

            Closed?.Invoke(this, e);
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, $"{nameof(SendingAmqpLinkClosed)}");
            }
        }

        internal Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, $"{nameof(CloseAsync)}");
            }

            return _sendingAmqpLink.CloseAsync(timeout);
        }

        internal void SafeClose()
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, $"{nameof(SafeClose)}");
            }

            _sendingAmqpLink.SafeClose();
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, $"{nameof(SafeClose)}");
            }
        }

        internal bool IsClosing()
        {
            return _sendingAmqpLink.IsClosing();
        }

        #region Telemetry handling

        internal async Task<AmqpIoTOutcome> SendMessageAsync(Message message, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, message, $"{nameof(SendMessageAsync)}");
            }

            AmqpMessage amqpMessage = AmqpIoTMessageConverter.MessageToAmqpMessage(message);
            Outcome outcome = await SendAmqpMessageAsync(amqpMessage, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, message, $"{nameof(SendMessageAsync)}");
            }

            return new AmqpIoTOutcome(outcome);
        }

        internal async Task<AmqpIoTOutcome> SendMessagesAsync(IEnumerable<Message> messages, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, $"{nameof(SendMessagesAsync)}");
            }

            // List to hold messages in Amqp friendly format
            var messageList = new List<Data>();

            foreach (Message message in messages)
            {
                using (AmqpMessage amqpMessage = AmqpIoTMessageConverter.MessageToAmqpMessage(message))
                {
                    var data = new Data()
                    {
                        Value = AmqpIoTMessageConverter.ReadStream(amqpMessage.ToStream())
                    };
                    messageList.Add(data);
                }
            }

            Outcome outcome;
            using (AmqpMessage amqpMessage = AmqpMessage.Create(messageList))
            {
                amqpMessage.MessageFormat = AmqpConstants.AmqpBatchedMessageFormat;
                outcome = await SendAmqpMessageAsync(amqpMessage, timeout).ConfigureAwait(false);
            }

            AmqpIoTOutcome amqpIoTOutcome = new AmqpIoTOutcome(outcome);
            if (amqpIoTOutcome != null)
            {
                amqpIoTOutcome.ThrowIfNotAccepted();
            }

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, $"{nameof(SendMessagesAsync)}");
            }

            return amqpIoTOutcome;
        }

        private async Task<Outcome> SendAmqpMessageAsync(AmqpMessage amqpMessage, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, $"{nameof(SendAmqpMessageAsync)}");
            }

            try
            {
                return await _sendingAmqpLink.SendMessageAsync(
                    amqpMessage,
                    new ArraySegment<byte>(Guid.NewGuid().ToByteArray()),
                    AmqpConstants.NullBinary,
                    timeout).ConfigureAwait(false);
            }
            catch (Exception e) when (!e.IsFatal())
            {
                Exception ex = AmqpIoTExceptionAdapter.ConvertToIoTHubException(e, _sendingAmqpLink);
                if (ReferenceEquals(e, ex))
                {
                    throw;
                }
                else
                {
                    if (ex is AmqpIoTResourceException)
                    {
                        _sendingAmqpLink.SafeClose();
                        throw new IotHubCommunicationException(ex.Message, ex);
                    }
                    throw ex;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"{nameof(SendAmqpMessageAsync)}");
                }
            }
        }

        #endregion Telemetry handling

        #region DeviceStreamResponse handling
        internal async Task<AmqpIoTOutcome> SendDeviceStreamResponseAsync(DeviceStreamResponse streamResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, streamResponse, $"{nameof(SendDeviceStreamResponseAsync)}");
            AmqpMessage amqpMessage = AmqpMessage.Create();
            amqpMessage.Properties.CorrelationId = new Guid(streamResponse.RequestId);
            if (amqpMessage.ApplicationProperties == null)
            {
                amqpMessage.ApplicationProperties = new ApplicationProperties();
            }

            amqpMessage.ApplicationProperties.Map[AmqpIoTConstants.DeviceStreamingFieldIsAccepted] = streamResponse.IsAccepted;
            Outcome outcome = await SendAmqpMessageAsync(amqpMessage, timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, streamResponse, $"{nameof(SendDeviceStreamResponseAsync)}");
            return new AmqpIoTOutcome(outcome);
        }
        #endregion

        #region Method handling

        internal async Task<AmqpIoTOutcome> SendMethodResponseAsync(MethodResponseInternal methodResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, methodResponse, $"{nameof(SendMethodResponseAsync)}");
            }

            AmqpMessage amqpMessage = AmqpIoTMessageConverter.ConvertMethodResponseInternalToAmqpMessage(methodResponse);
            AmqpIoTMessageConverter.PopulateAmqpMessageFromMethodResponse(amqpMessage, methodResponse);

            Outcome outcome = await SendAmqpMessageAsync(amqpMessage, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, $"{nameof(SendMethodResponseAsync)}");
            }

            return new AmqpIoTOutcome(outcome);
        }

        #endregion Method handling

        #region Twin handling

        internal async Task<AmqpIoTOutcome> SendTwinGetMessageAsync(string correlationId, TwinCollection reportedProperties, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, $"{nameof(SendTwinGetMessageAsync)}");
            }

            AmqpMessage amqpMessage = AmqpMessage.Create();
            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.MessageAnnotations.Map["operation"] = "GET";

            Outcome outcome = await SendAmqpMessageAsync(amqpMessage, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, $"{nameof(SendTwinGetMessageAsync)}");
            }

            return new AmqpIoTOutcome(outcome);
        }

        internal async Task<AmqpIoTOutcome> SendTwinPatchMessageAsync(string correlationId, TwinCollection reportedProperties, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, $"{nameof(SendTwinPatchMessageAsync)}");
            }

            var body = JsonConvert.SerializeObject(reportedProperties);
            var bodyStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));

            AmqpMessage amqpMessage = AmqpMessage.Create(bodyStream, true);
            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.MessageAnnotations.Map["operation"] = "PATCH";
            amqpMessage.MessageAnnotations.Map["resource"] = "/properties/reported";
            amqpMessage.MessageAnnotations.Map["version"] = null;

            Outcome outcome = await SendAmqpMessageAsync(amqpMessage, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, $"{nameof(SendTwinPatchMessageAsync)}");
            }

            return new AmqpIoTOutcome(outcome);
        }

        internal async Task<AmqpIoTOutcome> SubscribeToDesiredPropertiesAsync(string correlationId, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, $"{nameof(SubscribeToDesiredPropertiesAsync)}");
            }

            AmqpMessage amqpMessage = AmqpMessage.Create();
            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.MessageAnnotations.Map["operation"] = "PUT";
            amqpMessage.MessageAnnotations.Map["resource"] = "/notifications/twin/properties/desired";
            amqpMessage.MessageAnnotations.Map["version"] = null;

            Outcome outcome = await SendAmqpMessageAsync(amqpMessage, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, $"{nameof(SubscribeToDesiredPropertiesAsync)}");
            }

            return new AmqpIoTOutcome(outcome);
        }

        #endregion Twin handling
    }
}
