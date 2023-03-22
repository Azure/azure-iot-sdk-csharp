// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal sealed class AmqpIotReceivingLink
    {
        public event EventHandler Closed;

        private readonly ReceivingAmqpLink _receivingAmqpLink;
        private readonly PayloadConvention _payloadConvention;

        private Func<IncomingMessage, ArraySegment<byte>, Task> _onEventsReceived;
        private Func<IncomingMessage, ArraySegment<byte>, Task> _onDeviceMessageReceived;
        private Action<DirectMethodRequest> _onMethodReceived;
        private Func<AmqpMessage, string, IotHubClientException, Task> _onTwinMessageReceived;

        public AmqpIotReceivingLink(ReceivingAmqpLink receivingAmqpLink, PayloadConvention payloadConvention)
        {
            _receivingAmqpLink = receivingAmqpLink;
            _receivingAmqpLink.Closed += ReceivingAmqpLinkClosed;

            _payloadConvention = payloadConvention;
        }

        // This event handler is not invoked by the AMQP library in an async fashion.
        // This also co-relates with the fact that ReceivingAmqpLink.SafeClose() is a sync method.
        private void ReceivingAmqpLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(ReceivingAmqpLinkClosed));

            Closed?.Invoke(this, e);

            // After the Closed event handler has been invoked, the ReceivingAmqpLink has now been effectively cleaned up.
            // This is a good point for us to detach the Closed event handler from the ReceivingAmqpLink instance.
            _receivingAmqpLink.Closed -= ReceivingAmqpLinkClosed;

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(ReceivingAmqpLinkClosed));
        }

        internal Task CloseAsync(CancellationToken cancellationToken)
        {
            return _receivingAmqpLink.CloseAsync(cancellationToken);
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

        internal async Task DisposeMessageAsync(ArraySegment<byte> deliveryTag, Outcome outcome, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, outcome, nameof(DisposeMessageAsync));

            Outcome disposeOutcome = await _receivingAmqpLink
                .DisposeMessageAsync(
                    deliveryTag,
                    outcome,
                    batchable: true,
                    cancellationToken)
                .ConfigureAwait(false);

            Debug.Assert(disposeOutcome is Accepted, "IoT hub rejected the ack, which we don't expect and if we find it does, we should handle it.");

            if (Logging.IsEnabled)
                Logging.Exit(this, outcome, nameof(DisposeMessageAsync));
        }

        internal void RegisterReceiveMessageListener(Func<IncomingMessage, ArraySegment<byte>, Task> onDeviceMessageReceived)
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
            if (Logging.IsEnabled)
                Logging.Enter(this, amqpMessage, nameof(OnDeviceMessageReceived));

            try
            {
                IncomingMessage message = null;
                if (amqpMessage != null)
                {
                    message = AmqpIotMessageConverter.AmqpMessageToIncomingMessage(amqpMessage, _payloadConvention);
                }
                _onDeviceMessageReceived?.Invoke(message, amqpMessage.DeliveryTag);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, amqpMessage, nameof(OnDeviceMessageReceived));
            }
        }

        #endregion Receive Message

        #region EventHandling

        internal void RegisterEventListener(Func<IncomingMessage, ArraySegment<byte>, Task> onEventsReceived)
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
                Logging.Enter(this, amqpMessage, nameof(OnEventsReceived));

            try
            {
                IncomingMessage message = AmqpIotMessageConverter.AmqpMessageToIncomingMessage(amqpMessage, _payloadConvention);
                _onEventsReceived?.Invoke(message, amqpMessage.DeliveryTag);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, amqpMessage, nameof(OnMethodReceived));
            }
        }

        #endregion EventHandling

        #region Method handling

        internal void RegisterMethodListener(Action<DirectMethodRequest> onMethodReceived)
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
                Logging.Enter(this, amqpMessage, nameof(OnMethodReceived));

            try
            {
                DirectMethodRequest directMethodRequest = AmqpIotMessageConverter.ConstructMethodRequestFromAmqpMessage(amqpMessage, _payloadConvention);
                DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
                _onMethodReceived?.Invoke(directMethodRequest);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, amqpMessage, nameof(OnMethodReceived));
            }
        }

        private void DisposeDelivery(AmqpMessage amqpMessage, bool settled, Accepted acceptedOutcome)
        {
            _receivingAmqpLink.DisposeDelivery(amqpMessage, settled, acceptedOutcome);
        }

        #endregion Method handling

        #region Twin handling

        internal void RegisterTwinListener(Func<AmqpMessage, string, IotHubClientException, Task> onDesiredPropertyReceived)
        {
            _onTwinMessageReceived = onDesiredPropertyReceived;
            _receivingAmqpLink.RegisterMessageListener(OnTwinChangesReceived);
        }

        private void OnTwinChangesReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, amqpMessage, nameof(OnTwinChangesReceived));

            try
            {
                _receivingAmqpLink.DisposeDelivery(amqpMessage, true, AmqpIotConstants._acceptedOutcome);
                string correlationId = amqpMessage.Properties?.CorrelationId?.ToString();
                int status = GetStatus(amqpMessage);

                if (status >= 400)
                {
                    // Handle failures
                    if (correlationId.StartsWith(AmqpTwinMessageType.Get.ToString(), StringComparison.OrdinalIgnoreCase)
                        || correlationId.StartsWith(AmqpTwinMessageType.Patch.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        IotHubClientErrorResponseMessage errorResponse = null;
                        IotHubClientException twinException = null;

                        // This will only ever contain an error message which is encoded based on service contract (UTF-8).
                        if (amqpMessage.BodyStream.Length > 0)
                        {
                            using var reader = new StreamReader(amqpMessage.BodyStream, Encoding.UTF8);
                            string errorResponseString = reader.ReadToEnd();

                            try
                            {
                                errorResponse = JsonConvert.DeserializeObject<IotHubClientErrorResponseMessage>(errorResponseString);
                            }
                            catch (JsonException ex)
                            {
                                if (Logging.IsEnabled)
                                    Logging.Error(this, $"Failed to parse twin patch error response JSON. Message body: '{errorResponseString}'. Exception: {ex}. ");

                                errorResponse = new IotHubClientErrorResponseMessage
                                {
                                    Message = errorResponseString,
                                };
                            }
                        }

                        // Check if we have an int to string error code translation for the service returned error code.
                        // The error code could be a part of the service returned error message, or it can be a part of the amqp message annotations map.
                        // We will check with the error code in the error message first (if present) since that is the more specific error code returned.
                        if ((Enum.TryParse(errorResponse.ErrorCode.ToString(CultureInfo.InvariantCulture), out IotHubClientErrorCode errorCode)
                            || Enum.TryParse(status.ToString(CultureInfo.InvariantCulture), out errorCode))
                            && Enum.IsDefined(typeof(IotHubClientErrorCode), errorCode))
                        {
                            twinException = new IotHubClientException(errorResponse.Message, errorCode)
                            {
                                TrackingId = errorResponse.TrackingId,
                            };
                        }
                        else if (status >= 500)
                        {
                            twinException = new IotHubClientException(errorResponse.Message, IotHubClientErrorCode.ServerError)
                            {
                                TrackingId = errorResponse.TrackingId,
                            };
                        }
                        else
                        {
                            twinException = new IotHubClientException(errorResponse.Message)
                            {
                                TrackingId = errorResponse.TrackingId,
                            };
                        }

                        _onTwinMessageReceived.Invoke(null, correlationId, twinException).GetAwaiter().GetResult();
                    }
                }
                else
                {
                    _onTwinMessageReceived.Invoke(amqpMessage, correlationId, null).GetAwaiter().GetResult();
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, amqpMessage, nameof(OnTwinChangesReceived));
            }
        }

        #endregion Twin handling

        internal static int GetStatus(AmqpMessage response)
        {
            return response == null
                    || !response.MessageAnnotations.Map.TryGetValue(AmqpIotConstants.ResponseStatusName, out int status)
                ? -1
                : status;
        }
    }
}
