﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotReceivingLink
    {
        public event EventHandler Closed;

        private readonly ReceivingAmqpLink _receivingAmqpLink;

        private Action<Message> _onEventsReceived;
        private Action<Message> _onDeviceMessageReceived;
        private Action<MethodRequestInternal> _onMethodReceived;
        private Action<AmqpMessage, string, IotHubException> _onTwinMessageReceived;

        public AmqpIotReceivingLink(ReceivingAmqpLink receivingAmqpLink)
        {
            _receivingAmqpLink = receivingAmqpLink;
            _receivingAmqpLink.Closed += ReceivingAmqpLinkClosed;
        }

        private void ReceivingAmqpLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, nameof(ReceivingAmqpLinkClosed));
            }

            Closed?.Invoke(this, e);

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, nameof(ReceivingAmqpLinkClosed));
            }
        }

        internal Task CloseAsync(TimeSpan timeout)
        {
            return _receivingAmqpLink.CloseAsync(timeout);
        }

        internal bool IsClosing()
        {
            return _receivingAmqpLink.IsClosing();
        }

        internal void SafeClose()
        {
            _receivingAmqpLink.SafeClose();
        }

        #region Receive Message

        internal async Task<Message> ReceiveAmqpMessageAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, nameof(ReceiveAmqpMessageAsync));
            }

            try
            {
                AmqpMessage amqpMessage = await _receivingAmqpLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
                Message message = null;
                if (amqpMessage != null)
                {
                    message = AmqpIotMessageConverter.AmqpMessageToMessage(amqpMessage);
                    message.LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString();
                }
                return message;
            }
            catch (Exception e) when (!e.IsFatal())
            {
                Exception ex = AmqpIotExceptionAdapter.ConvertToIotHubException(e, _receivingAmqpLink);
                if (ReferenceEquals(e, ex))
                {
                    throw;
                }
                else
                {
                    if (ex is AmqpIotResourceException)
                    {
                        _receivingAmqpLink.SafeClose();
                        throw new IotHubCommunicationException(ex.Message, ex);
                    }
                    throw ex;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, nameof(ReceiveAmqpMessageAsync));
                }
            }
        }

        internal async Task<AmqpIotOutcome> DisposeMessageAsync(string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, outcome, nameof(DisposeMessageAsync));
            }

            ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);
            Outcome disposeOutcome =
                await _receivingAmqpLink.DisposeMessageAsync(
                    deliveryTag,
                    outcome,
                    batchable: true,
                    timeout: timeout).ConfigureAwait(false);

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, outcome, nameof(DisposeMessageAsync));
            }

            return new AmqpIotOutcome(disposeOutcome);
        }

        private static ArraySegment<byte> ConvertToDeliveryTag(string lockToken)
        {
            if (lockToken == null)
            {
                throw new ArgumentNullException(nameof(lockToken));
            }

            if (!Guid.TryParse(lockToken, out Guid lockTokenGuid))
            {
                throw new ArgumentException("Should be a valid Guid", nameof(lockToken));
            }

            return new ArraySegment<byte>(lockTokenGuid.ToByteArray());
        }

        internal void RegisterReceiveMessageListener(Action<Message> onDeviceMessageReceived)
        {
            _onDeviceMessageReceived = onDeviceMessageReceived;
            _receivingAmqpLink.RegisterMessageListener(OnDeviceMessageReceived);
        }

        [SuppressMessage(
            "Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "The callback that is invoked is responsible for disposing the message.")]
        private void OnDeviceMessageReceived(AmqpMessage amqpMessage)
        {
            Logging.Enter(this, amqpMessage, nameof(OnDeviceMessageReceived));

            try
            {
                Message message = null;
                if (amqpMessage != null)
                {
                    message = AmqpIotMessageConverter.AmqpMessageToMessage(amqpMessage);
                    message.LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString();
                }
                _onDeviceMessageReceived?.Invoke(message);
            }
            finally
            {
                Logging.Exit(this, amqpMessage, nameof(OnDeviceMessageReceived));
            }
        }

        #endregion Receive Message

        #region EventHandling

        internal void RegisterEventListener(Action<Message> onEventsReceived)
        {
            _onEventsReceived = onEventsReceived;
            _receivingAmqpLink.RegisterMessageListener(OnEventsReceived);
        }

        [SuppressMessage(
            "Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "The callback that is invoked is responsible for disposing the message.")]
        private void OnEventsReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, amqpMessage, nameof(OnEventsReceived));
            }

            try
            {
                Message message = AmqpIotMessageConverter.AmqpMessageToMessage(amqpMessage);
                message.LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString();
                _onEventsReceived?.Invoke(message);
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, amqpMessage, nameof(OnMethodReceived));
                }
            }
        }

        #endregion EventHandling

        #region Method handling

        internal void RegisterMethodListener(Action<MethodRequestInternal> onMethodReceived)
        {
            _onMethodReceived = onMethodReceived;
            _receivingAmqpLink.RegisterMessageListener(OnMethodReceived);
        }

        [SuppressMessage(
            "Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "The callback that is invoked is responsible for disposing the message.")]
        private void OnMethodReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, amqpMessage, nameof(OnMethodReceived));
            }

            try
            {
                MethodRequestInternal methodRequestInternal = AmqpIotMessageConverter.ConstructMethodRequestFromAmqpMessage(
                    amqpMessage,
                    new CancellationToken(false));
                DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
                _onMethodReceived?.Invoke(methodRequestInternal);
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, amqpMessage, nameof(OnMethodReceived));
                }
            }
        }

        private void DisposeDelivery(AmqpMessage amqpMessage, bool settled, Accepted acceptedOutcome)
        {
            _receivingAmqpLink.DisposeDelivery(amqpMessage, settled, acceptedOutcome);
        }

        #endregion Method handling

        #region Twin handling

        internal void RegisterTwinListener(Action<AmqpMessage, string, IotHubException> onDesiredPropertyReceived)
        {
            _onTwinMessageReceived = onDesiredPropertyReceived;
            _receivingAmqpLink.RegisterMessageListener(OnTwinChangesReceived);
        }

        private void OnTwinChangesReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, amqpMessage, nameof(OnTwinChangesReceived));
            }

            try
            {
                _receivingAmqpLink.DisposeDelivery(amqpMessage, true, AmqpIotConstants.AcceptedOutcome);
                string correlationId = amqpMessage.Properties?.CorrelationId?.ToString();
                int status = GetStatus(amqpMessage);

                if (status >= 400)
                {
                    // Handle failures
                    if (correlationId.StartsWith(AmqpTwinMessageType.Get.ToString(), StringComparison.OrdinalIgnoreCase)
                        || correlationId.StartsWith(AmqpTwinMessageType.Patch.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        string error = null;
                        using (var reader = new StreamReader(amqpMessage.BodyStream, System.Text.Encoding.UTF8))
                        {
                            error = reader.ReadToEnd();
                        };

                        // Retry for Http status code request timeout, Too many requests and server errors
                        var exception = new IotHubException(error, status >= 500 || status == 429 || status == 408);
                        _onTwinMessageReceived.Invoke(null, correlationId, exception);
                    }
                }
                else
                {
                    _onTwinMessageReceived.Invoke(amqpMessage, correlationId, null);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, amqpMessage, nameof(OnTwinChangesReceived));
                }
            }
        }

        #endregion Twin handling

        internal static int GetStatus(AmqpMessage response)
        {
            if (response != null)
            {
                if (response.MessageAnnotations.Map.TryGetValue(AmqpIotConstants.ResponseStatusName, out int status))
                {
                    return status;
                }
            }

            return -1;
        }
    }
}
