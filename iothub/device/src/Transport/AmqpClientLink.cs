// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Encapsulates the AMQP library link object provides base class for IoTHubClient's sepcific links
    /// Adds IoTHubClient related link configrations and settings
    /// Implements generic AMQP operations: Open, Close, Send, Receive, Accept, Dispose
    /// Exposes event for link-closed and provides API for message received
    /// </summary>
    internal abstract class AmqpClientLink
    {
        #region Members-Constructor
        protected const string ClientVersionName = "client-version";

        protected AmqpClientSession amqpClientSession { get; private set; }
        protected DeviceClientEndpointIdentity deviceClientEndpointIdentity { get; private set; }

        protected AmqpClientLinkType amqpClientLinkType { get; private set; }
        protected AmqpLinkSettings amqpLinkSettings { get; set; }

        protected AmqpLink amqpLink { get; set; }

        protected string correlationId;
        protected bool isLinkClosed { get; private set; }

        internal event EventHandler OnAmqpClientLinkClosed;

        internal AmqpClientLink(AmqpClientLinkType amqpClientLinkType, AmqpClientSession amqpClientSession, DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout, string correlationId = "")
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLink)}");

            this.amqpClientLinkType = amqpClientLinkType;
            this.amqpClientSession = amqpClientSession;
            this.deviceClientEndpointIdentity = deviceClientEndpointIdentity;
            this.correlationId = correlationId;

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientLink)}");
        }
        #endregion

        #region Open-Close
        internal virtual async Task OpenAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLink)}.{nameof(OpenAsync)}.{amqpClientLinkType.ToString()}");

            try
            {
                await amqpLink.OpenAsync(timeout).ConfigureAwait(false);
                amqpLink.SafeAddClosed(OnLinkClosed);
                isLinkClosed = false;
            }
            catch (Exception exception)
            {
                amqpLink.SafeClose(exception);

                throw;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLink)}.{nameof(OpenAsync)}.{amqpClientLinkType.ToString()}");
            }
        }

        internal virtual async Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLink)}.{nameof(CloseAsync)}.{amqpClientLinkType.ToString()}");
            await amqpLink.CloseAsync(timeout).ConfigureAwait(false);
        }

        private void OnLinkClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLink)}.{nameof(OnLinkClosed)}.{amqpClientLinkType.ToString()}");
            isLinkClosed = true;
            OnAmqpClientLinkClosed(o, args);
        }
        #endregion

        #region Send
        internal virtual async Task<Outcome> SendMessageAsync(AmqpMessage message, ArraySegment<byte> deliveryTag, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLink)}.{nameof(SendMessageAsync)}.{amqpClientLinkType.ToString()}");

            if (!(amqpLink is SendingAmqpLink senderLink))
            {
                throw new InvalidOperationException("Link does not support sending. Link type: " + amqpClientLinkType.ToString());
            }

            Outcome outcome = await senderLink.SendMessageAsync(message, deliveryTag, AmqpConstants.NullBinary, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientLink)}.{nameof(SendMessageAsync)}.{amqpClientLinkType.ToString()}");

            return outcome;
        }
        #endregion

        #region Receive
        internal virtual async Task<AmqpMessage> ReceiveMessageAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLink)}.{nameof(ReceiveMessageAsync)}.{amqpClientLinkType.ToString()}");

            if (!(amqpLink is ReceivingAmqpLink receiverLink))
            {
                throw new InvalidOperationException("Link does not support receiving. Link type: " + amqpClientLinkType.ToString());
            }

            AmqpMessage message = await receiverLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientLink)}.{nameof(ReceiveMessageAsync)}.{amqpClientLinkType.ToString()}");

            return message;
        }

        internal virtual void RegisterMessageListener(Action<AmqpMessage> messageListener)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLink)}.{nameof(RegisterMessageListener)}.{amqpClientLinkType.ToString()}");

            if (!(amqpLink is ReceivingAmqpLink receiverLink))
            {
                throw new InvalidOperationException("Link does not support receiving. Link type: " + amqpClientLinkType.ToString());
            }
            receiverLink.RegisterMessageListener(messageListener);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientLink)}.{nameof(RegisterMessageListener)}.{amqpClientLinkType.ToString()}");
        }
        #endregion

        #region Accept-Dispose
        internal virtual void AcceptMessage(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLink)}.{nameof(AcceptMessage)}.{amqpClientLinkType.ToString()}");

            if (!(amqpLink is ReceivingAmqpLink receiverLink))
            {
                throw new InvalidOperationException("Link does not support receiving. Link type: " + amqpClientLinkType.ToString());
            }
            receiverLink.AcceptMessage(amqpMessage, false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientLink)}.{nameof(AcceptMessage)}.{amqpClientLinkType.ToString()}");
        }

        internal virtual async Task<Outcome> DisposeMessageAsync(ArraySegment<byte> deliveryTag, Outcome outcome, bool batchable, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLink)}.{nameof(DisposeMessageAsync)}.{amqpClientLinkType.ToString()}");

            if (!(amqpLink is ReceivingAmqpLink receiverLink))
            {
                throw new InvalidOperationException("Link does not support receiving. Link type: " + amqpClientLinkType.ToString());
            }

            Outcome retVal = await receiverLink.DisposeMessageAsync(deliveryTag, outcome, batchable, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientLink)}.{nameof(DisposeMessageAsync)}.{amqpClientLinkType.ToString()}");

            return retVal;
        }

        internal virtual void DisposeDelivery(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLink)}.{nameof(DisposeDelivery)}.{amqpClientLinkType.ToString()}");

            if (!(amqpLink is ReceivingAmqpLink receiverLink))
            {
                throw new InvalidOperationException("Link does not support receiving. Link type: " + amqpClientLinkType.ToString());
            }
            receiverLink.DisposeDelivery(amqpMessage, true, AmqpConstants.AcceptedOutcome);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientLink)}.{nameof(DisposeDelivery)}.{amqpClientLinkType.ToString()}");
        }
        #endregion

        #region Helpers
        protected string BuildPath(string deviceTemplate, string moduleTemplate)
        {
            string path;
            if (string.IsNullOrEmpty(this.deviceClientEndpointIdentity.iotHubConnectionString.ModuleId))
            {
                path = string.Format(CultureInfo.InvariantCulture, deviceTemplate, System.Net.WebUtility.UrlEncode(this.deviceClientEndpointIdentity.iotHubConnectionString.DeviceId));
            }
            else
            {
                path = string.Format(CultureInfo.InvariantCulture, moduleTemplate, System.Net.WebUtility.UrlEncode(this.deviceClientEndpointIdentity.iotHubConnectionString.DeviceId), System.Net.WebUtility.UrlEncode(this.deviceClientEndpointIdentity.iotHubConnectionString.ModuleId));
            }

            return path;
        }
        #endregion
    }
}
