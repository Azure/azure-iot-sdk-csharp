﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTReceivingLink
    {
        public event EventHandler Closed;

        private readonly ReceivingAmqpLink _receivingAmqpLink;

        private Action<Message> _onEventsReceived;
        private Action<Message> _onDeviceMessageReceived;
        private Action<MethodRequestInternal> _onMethodReceived;
        private Action<Twin, string, TwinCollection, IotHubException> _onTwinMessageReceived;

        public AmqpIoTReceivingLink(ReceivingAmqpLink receivingAmqpLink)
        {
            _receivingAmqpLink = receivingAmqpLink;
            _receivingAmqpLink.Closed += ReceivingAmqpLinkClosed;
        }

        private void ReceivingAmqpLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, $"{nameof(ReceivingAmqpLinkClosed)}");
            }

            Closed?.Invoke(this, e);
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, $"{nameof(ReceivingAmqpLinkClosed)}");
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
                Logging.Enter(this, $"{nameof(ReceiveAmqpMessageAsync)}");
            }

            try
            {
                AmqpMessage amqpMessage = await _receivingAmqpLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
                Message message = null;
                if (amqpMessage != null)
                {
                    message = AmqpIoTMessageConverter.AmqpMessageToMessage(amqpMessage);
                    message.LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString();
                }
                return message;
            }
            catch (Exception e) when (!e.IsFatal())
            {
                Exception ex = AmqpIoTExceptionAdapter.ConvertToIoTHubException(e, _receivingAmqpLink);
                if (ReferenceEquals(e, ex))
                {
                    throw;
                }
                else
                {
                    if (ex is AmqpIoTResourceException)
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
                    Logging.Exit(this, $"{nameof(ReceiveAmqpMessageAsync)}");
                }
            }
        }

        internal async Task<AmqpIoTOutcome> DisposeMessageAsync(string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, outcome, $"{nameof(DisposeMessageAsync)}");
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
                Logging.Exit(this, outcome, $"{nameof(DisposeMessageAsync)}");
            }

            return new AmqpIoTOutcome(disposeOutcome);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "The callback that is invoked is responsible for disposing the message.")]
        private void OnDeviceMessageReceived(AmqpMessage amqpMessage)
        {
            Logging.Enter(this, amqpMessage, $"{nameof(OnDeviceMessageReceived)}");

            try
            {
                Message message = null;
                if (amqpMessage != null)
                {
                    message = AmqpIoTMessageConverter.AmqpMessageToMessage(amqpMessage);
                    message.LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString();
                }
                _onDeviceMessageReceived?.Invoke(message);
            }
            finally
            {
                Logging.Exit(this, amqpMessage, $"{nameof(OnDeviceMessageReceived)}");
            }
        }

        #endregion Receive Message

        #region EventHandling

        internal void RegisterEventListener(Action<Message> onEventsReceived)
        {
            _onEventsReceived = onEventsReceived;
            _receivingAmqpLink.RegisterMessageListener(OnEventsReceived);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "The callback that is invoked is responsible for disposing the message.")]
        private void OnEventsReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, amqpMessage, $"{nameof(OnEventsReceived)}");
            }

            try
            {
                Message message = AmqpIoTMessageConverter.AmqpMessageToMessage(amqpMessage);
                message.LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString();
                _onEventsReceived?.Invoke(message);
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, amqpMessage, $"{nameof(OnMethodReceived)}");
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "The callback that is invoked is responsible for disposing the message.")]
        private void OnMethodReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, amqpMessage, $"{nameof(OnMethodReceived)}");
            }

            try
            {
                MethodRequestInternal methodRequestInternal = AmqpIoTMessageConverter.ConstructMethodRequestFromAmqpMessage(amqpMessage, new CancellationToken(false));
                DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
                _onMethodReceived?.Invoke(methodRequestInternal);
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, amqpMessage, $"{nameof(OnMethodReceived)}");
                }
            }
        }

        private void DisposeDelivery(AmqpMessage amqpMessage, bool settled, Accepted acceptedOutcome)
        {
            _receivingAmqpLink.DisposeDelivery(amqpMessage, settled, acceptedOutcome);
        }

        #endregion Method handling

        #region Twin handling

        internal void RegisterTwinListener(Action<Twin, string, TwinCollection, IotHubException> onDesiredPropertyReceived)
        {
            _onTwinMessageReceived = onDesiredPropertyReceived;
            _receivingAmqpLink.RegisterMessageListener(OnTwinChangesReceived);
        }

        private void OnTwinChangesReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, amqpMessage, $"{nameof(OnTwinChangesReceived)}");
            }

            try
            {
                _receivingAmqpLink.DisposeDelivery(amqpMessage, true, AmqpIoTConstants.AcceptedOutcome);
                string correlationId = amqpMessage.Properties?.CorrelationId?.ToString();
                int status = GetStatus(amqpMessage);

                Twin twin = null;
                TwinCollection twinProperties = null;

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
                        _onTwinMessageReceived.Invoke(null, correlationId, null, exception);
                    }
                }
                else
                {
                    if (correlationId == null)
                    {
                        // Here we are getting desired property update notifications and want to handle it first
                        using var reader = new StreamReader(amqpMessage.BodyStream, System.Text.Encoding.UTF8);
                        string patch = reader.ReadToEnd();
                        twinProperties = JsonConvert.DeserializeObject<TwinCollection>(patch);
                    }
                    else if (correlationId.StartsWith(AmqpTwinMessageType.Get.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        // This a response of a GET TWIN so return (set) the full twin
                        using var reader = new StreamReader(amqpMessage.BodyStream, System.Text.Encoding.UTF8);
                        string body = reader.ReadToEnd();
                        var properties = JsonConvert.DeserializeObject<TwinProperties>(body);
                        twin = new Twin(properties);
                    }
                    else if (correlationId.StartsWith(AmqpTwinMessageType.Patch.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        // This can be used to coorelate success response with updating reported properties 
                        // However currently we do not have it as request response style implementation
                        Logging.Info("Updated twin reported properties successfully", nameof(OnTwinChangesReceived));
                    }
                    else if (correlationId.StartsWith(AmqpTwinMessageType.Put.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        // This is an acknowledgement received from service for subscribing to desired property updates
                        Logging.Info("Subscribed for twin successfully", nameof(OnTwinChangesReceived));
                    }
                    else
                    {
                        // This shouldn't happen
                        Logging.Info("Received a correlation Id for Twin operation that does not match Get, Patch or Put request", nameof(OnTwinChangesReceived));
                    }
                    _onTwinMessageReceived.Invoke(twin, correlationId, twinProperties, null);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, amqpMessage, $"{nameof(OnTwinChangesReceived)}");
                }
            }
        }

        #endregion Twin handling

        internal static int GetStatus(AmqpMessage response)
        {
            if (response != null)
            {
                if (response.MessageAnnotations.Map.TryGetValue(AmqpIoTConstants.ResponseStatusName, out int status))
                {
                    return status;
                }
            }
            return -1;
        }
    }
}
