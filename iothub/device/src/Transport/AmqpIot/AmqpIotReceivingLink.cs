// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotReceivingLink
    {
        public event EventHandler Closed;

        private readonly ReceivingAmqpLink _receivingAmqpLink;

        private Func<Message, Task> _onEventsReceived;
        private Func<Message, Task> _onDeviceMessageReceived;
        private Action<DirectMethodRequest> _onMethodReceived;
        private Action<Twin, string, TwinCollection, IotHubClientException> _onTwinMessageReceived;

        public AmqpIotReceivingLink(ReceivingAmqpLink receivingAmqpLink)
        {
            _receivingAmqpLink = receivingAmqpLink;
            _receivingAmqpLink.Closed += ReceivingAmqpLinkClosed;
        }

        private void ReceivingAmqpLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(ReceivingAmqpLinkClosed));

            Closed?.Invoke(this, e);

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

        internal async Task<AmqpIotOutcome> DisposeMessageAsync(string lockToken, Outcome outcome, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, outcome, nameof(DisposeMessageAsync));

            ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);
            Outcome disposeOutcome =
                await _receivingAmqpLink.DisposeMessageAsync(
                    deliveryTag,
                    outcome,
                    batchable: true,
                    cancellationToken).ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Exit(this, outcome, nameof(DisposeMessageAsync));

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

        internal void RegisterReceiveMessageListener(Func<Message, Task> onDeviceMessageReceived)
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
                if (Logging.IsEnabled)
                    Logging.Exit(this, amqpMessage, nameof(OnDeviceMessageReceived));
            }
        }

        #endregion Receive Message

        #region EventHandling

        internal void RegisterEventListener(Func<Message, Task> onEventsReceived)
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
                Message message = AmqpIotMessageConverter.AmqpMessageToMessage(amqpMessage);
                message.LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString();
                _onEventsReceived?.Invoke(message);
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
                DirectMethodRequest DirectMethodRequest = AmqpIotMessageConverter.ConstructMethodRequestFromAmqpMessage(amqpMessage);
                DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
                _onMethodReceived?.Invoke(DirectMethodRequest);
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

        internal void RegisterTwinListener(Action<Twin, string, TwinCollection, IotHubClientException> onDesiredPropertyReceived)
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
                _receivingAmqpLink.DisposeDelivery(amqpMessage, true, AmqpIotConstants.AcceptedOutcome);
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
                        using var reader = new StreamReader(amqpMessage.BodyStream, System.Text.Encoding.UTF8);
                        error = reader.ReadToEnd();

                        // Retry for Http status code request timeout, Too many requests and server errors
                        var exception = new IotHubClientException(error, status >= 500 || status == 429 || status == 408);
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
                        TwinProperties properties = JsonConvert.DeserializeObject<TwinProperties>(body);
                        twin = new Twin(properties);
                    }
                    else if (correlationId.StartsWith(AmqpTwinMessageType.Patch.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        // This can be used to coorelate success response with updating reported properties
                        if (Logging.IsEnabled)
                            Logging.Info("Updated twin reported properties successfully", nameof(OnTwinChangesReceived));

                        twin = new Twin
                        {
                            Version = long.Parse(amqpMessage.MessageAnnotations.Map["version"].ToString()),
                        };
                    }
                    else if (correlationId.StartsWith(AmqpTwinMessageType.Put.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        // This is an acknowledgement received from service for subscribing to desired property updates
                        if (Logging.IsEnabled)
                            Logging.Info("Subscribed for twin successfully", nameof(OnTwinChangesReceived));
                    }
                    else
                    {
                        // This shouldn't happen
                        if (Logging.IsEnabled)
                            Logging.Info("Received a correlation Id for Twin operation that does not match Get, Patch or Put request", nameof(OnTwinChangesReceived));
                    }
                    _onTwinMessageReceived.Invoke(twin, correlationId, twinProperties, null);
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
            if (response != null
                && response.MessageAnnotations.Map.TryGetValue(AmqpIotConstants.ResponseStatusName, out int status))
            {
                return status;
            }

            return -1;
        }
    }
}
