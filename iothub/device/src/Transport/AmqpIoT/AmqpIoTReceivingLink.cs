// Copyright (c) Microsoft. All rights reserved.
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
        private Action<MethodRequestInternal> _onMethodReceived;
        private Action<Twin, string, TwinCollection> _onDesiredPropertyReceived;

        public AmqpIoTReceivingLink(ReceivingAmqpLink receivingAmqpLink)
        {
            _receivingAmqpLink = receivingAmqpLink;
            _receivingAmqpLink.Closed += ReceivingAmqpLinkClosed;
        }

        private void ReceivingAmqpLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(ReceivingAmqpLinkClosed)}");
            Closed?.Invoke(this, e);
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(ReceivingAmqpLinkClosed)}");
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
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(ReceiveAmqpMessageAsync)}");
            try
            {
                var amqpMessage = await _receivingAmqpLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
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
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(ReceiveAmqpMessageAsync)}");
            }
        }

        internal async Task<AmqpIoTOutcome> DisposeMessageAsync(string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, outcome, $"{nameof(DisposeMessageAsync)}");

            ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);
            Outcome disposeOutcome =
                await _receivingAmqpLink.DisposeMessageAsync(
                    deliveryTag,
                    outcome,
                    batchable: true,
                    timeout: timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, outcome, $"{nameof(DisposeMessageAsync)}");

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

        #endregion Receive Message

        #region EventHandling

        internal void RegisterEventListener(Action<Message> onEventsReceived)
        {
            _onEventsReceived = onEventsReceived;
            _receivingAmqpLink.RegisterMessageListener(OnEventsReceived);
        }

        private void OnEventsReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, $"{nameof(OnEventsReceived)}");
            try
            {
                Message message = AmqpIoTMessageConverter.AmqpMessageToMessage(amqpMessage);
                message.LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString();
                _onEventsReceived?.Invoke(message);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, $"{nameof(OnMethodReceived)}");
            }
        }

        #endregion EventHandling

        #region Method handling

        internal void RegisterMethodListener(Action<MethodRequestInternal> onMethodReceived)
        {
            _onMethodReceived = onMethodReceived;
            _receivingAmqpLink.RegisterMessageListener(OnMethodReceived);
        }

        private void OnMethodReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, $"{nameof(OnMethodReceived)}");
            try
            {
                MethodRequestInternal methodRequestInternal = AmqpIoTMessageConverter.ConstructMethodRequestFromAmqpMessage(amqpMessage, new CancellationToken(false));
                DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);
                _onMethodReceived?.Invoke(methodRequestInternal);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, $"{nameof(OnMethodReceived)}");
            }
        }

        private void DisposeDelivery(AmqpMessage amqpMessage, bool settled, Accepted acceptedOutcome)
        {
            _receivingAmqpLink.DisposeDelivery(amqpMessage, settled, acceptedOutcome);
        }

        #endregion Method handling

        #region Twin handling

        internal void RegisterTwinListener(Action<Twin, string, TwinCollection> onDesiredPropertyReceived)
        {
            _onDesiredPropertyReceived = onDesiredPropertyReceived;
            _receivingAmqpLink.RegisterMessageListener(OnDesiredPropertyReceived);
        }

        private void OnDesiredPropertyReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpMessage, $"{nameof(OnDesiredPropertyReceived)}");
            try
            {
                _receivingAmqpLink.DisposeDelivery(amqpMessage, true, AmqpIoTConstants.AcceptedOutcome);
                string correlationId = amqpMessage.Properties?.CorrelationId?.ToString();

                if (!VerifyResponseMessage(amqpMessage))
                {
                    _onDesiredPropertyReceived.Invoke(null, correlationId, null);
                }

                Twin twin = null;
                TwinCollection twinProperties = null;

                if (correlationId != null)
                {
                    if (amqpMessage.BodyStream != null)
                    {
                        // This a result of a GET TWIN so return (set) the full twin
                        using (StreamReader reader = new StreamReader(amqpMessage.BodyStream, System.Text.Encoding.UTF8))
                        {
                            string body = reader.ReadToEnd();
                            var properties = JsonConvert.DeserializeObject<TwinProperties>(body);
                            twin = new Twin(properties);
                        }
                    }
                    else
                    {
                        // This is a desired property ack from the service
                        twin = new Twin();
                    }
                }
                else
                {
                    // No correlationId, this is a PATCH sent by the sevice so return (set) the TwinCollection

                    using (StreamReader reader = new StreamReader(amqpMessage.BodyStream, System.Text.Encoding.UTF8))
                    {
                        string patch = reader.ReadToEnd();
                        twinProperties = JsonConvert.DeserializeObject<TwinCollection>(patch);
                    }
                }
                _onDesiredPropertyReceived.Invoke(twin, correlationId, twinProperties);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, amqpMessage, $"{nameof(OnDesiredPropertyReceived)}");
            }
        }

        #endregion Twin handling

        internal static bool VerifyResponseMessage(AmqpMessage response)
        {
            bool retVal = true;
            if (response != null)
            {
                if (response.MessageAnnotations.Map.TryGetValue(AmqpIoTConstants.ResponseStatusName, out int status))
                {
                    if (status >= 400)
                    {
                        retVal = false;
                    }
                }
            }
            else
            {
                retVal = false;
            }
            return retVal;
        }
    }
}
