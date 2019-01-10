// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    public enum AmqpClientSenderLinkType
    {
        Telemetry,
        Methods,
        Twin
    }
    public enum AmqpClientReceiverLinkType
    {
        Methods,
        Twin,
        C2D,
        Events
    }

    /// <summary>
    /// 
    /// </summary>
    internal class AmqpClientLink
    {
        internal const string ClientVersionName = "client-version";
        private readonly AmqpClientSession amqpClientSession;
        private readonly DeviceClientEndpointIdentity deviceClientEndpointIdentity;

        internal AmqpLink amqpLink { get; private set; }

        internal AmqpLinkSettings amqpLinkSettings { get; private set; }

        internal bool isLinkClosed { get; private set; }

        public AmqpClientLink(AmqpClientSenderLinkType amqpClientSenderLinkType, AmqpClientSession amqpClientSession, DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            this.amqpClientSession = amqpClientSession;
            this.deviceClientEndpointIdentity = deviceClientEndpointIdentity;

            AmqpLinkSettings amqpLinkSettings;

            string path = this.BuildPath(CommonConstants.DeviceEventPathTemplate, CommonConstants.ModuleEventPathTemplate);
            Uri uri = deviceClientEndpointIdentity.iotHubConnectionString.BuildLinkAddress(path);

            amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = Guid.NewGuid().ToString("N"), // Use a human readable link name to help with debugging
                Role = false,
                InitialDeliveryCount = 0,
                Target = new Target() { Address = uri.AbsoluteUri }
            };

            // Set common properties
            var timeoutHelper = new TimeoutHelper(timeout);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeoutHelper.RemainingTime().TotalMilliseconds);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, deviceClientEndpointIdentity.productInfo.ToString());

            switch (amqpClientSenderLinkType)
            {
                case AmqpClientSenderLinkType.Telemetry:
                    amqpLinkSettings.SndSettleMode = null; // SenderSettleMode.Unsettled (null as it is the default and to avoid bytes on the wire)
                    amqpLinkSettings.RcvSettleMode = null; // (byte)ReceiverSettleMode.First (null as it is the default and to avoid bytes on the wire)
                    break;
                case AmqpClientSenderLinkType.Methods:
                    amqpLinkSettings.SndSettleMode = (byte)SenderSettleMode.Settled;
                    amqpLinkSettings.RcvSettleMode = (byte)ReceiverSettleMode.First;
                    amqpLinkSettings.AddProperty(IotHubAmqpProperty.ApiVersion, ClientApiVersionHelper.ApiVersionString);
                    amqpLinkSettings.AddProperty(IotHubAmqpProperty.ChannelCorrelationId, "methods:" + deviceClientEndpointIdentity.iotHubConnectionString.DeviceId);
                    break;
                case AmqpClientSenderLinkType.Twin:
                    amqpLinkSettings.SndSettleMode = (byte)SenderSettleMode.Settled;
                    amqpLinkSettings.RcvSettleMode = (byte)ReceiverSettleMode.First;
                    amqpLinkSettings.AddProperty(IotHubAmqpProperty.ApiVersion, ClientApiVersionHelper.ApiVersionString);
                    amqpLinkSettings.AddProperty(IotHubAmqpProperty.ChannelCorrelationId, "twin:" + deviceClientEndpointIdentity.iotHubConnectionString.DeviceId);
                    break;
                default:
                    break;
            }

            var amqpLink = new SendingAmqpLink(amqpLinkSettings);
            amqpLink.AttachTo(this.amqpClientSession.amqpSession);
        }

        public AmqpClientLink(AmqpClientReceiverLinkType amqpClienReceiverLinkType, AmqpClientSession amqpClientSession, DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            this.amqpClientSession = amqpClientSession;
            this.deviceClientEndpointIdentity = deviceClientEndpointIdentity;

            AmqpLinkSettings amqpLinkSettings;

            string path = this.BuildPath(CommonConstants.DeviceEventPathTemplate, CommonConstants.ModuleEventPathTemplate);
            Uri uri = deviceClientEndpointIdentity.iotHubConnectionString.BuildLinkAddress(path);
            uint prefetchCount = deviceClientEndpointIdentity.amqpTransportSettings.PrefetchCount;

            amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = Guid.NewGuid().ToString("N"), // Use a human readable link name to help with debugging
                Role = true,
                TotalLinkCredit = prefetchCount,
                AutoSendFlow = prefetchCount > 0,
                Target = new Source() { Address = uri.AbsoluteUri }
            };

            // Set common properties
            var timeoutHelper = new TimeoutHelper(timeout);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeoutHelper.RemainingTime().TotalMilliseconds);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, deviceClientEndpointIdentity.productInfo.ToString());

            switch (amqpClienReceiverLinkType)
            {
                case AmqpClientReceiverLinkType.Methods:
                    amqpLinkSettings.SndSettleMode = (byte)SenderSettleMode.Settled;
                    amqpLinkSettings.RcvSettleMode = (byte)ReceiverSettleMode.First;
                    break;
                case AmqpClientReceiverLinkType.Twin:
                    amqpLinkSettings.SndSettleMode = (byte)SenderSettleMode.Settled;
                    amqpLinkSettings.RcvSettleMode = (byte)ReceiverSettleMode.First;
                    break;
                case AmqpClientReceiverLinkType.C2D:
                    amqpLinkSettings.SndSettleMode = null; // SenderSettleMode.Unsettled (null as it is the default and to avoid bytes on the wire)
                    amqpLinkSettings.RcvSettleMode = (byte)ReceiverSettleMode.Second;
                    break;
                case AmqpClientReceiverLinkType.Events:
                    amqpLinkSettings.SndSettleMode = null; // SenderSettleMode.Unsettled (null as it is the default and to avoid bytes on the wire)
                    amqpLinkSettings.RcvSettleMode = (byte)ReceiverSettleMode.First;
                    break;
                default:
                    break;
            }

            var amqpLink = new ReceivingAmqpLink(amqpLinkSettings);
            amqpLink.AttachTo(this.amqpClientSession.amqpSession);
        }

        public async Task OpenAsync(TimeSpan timeout)
        {
            string path = this.BuildPath(CommonConstants.DeviceEventPathTemplate, CommonConstants.ModuleEventPathTemplate);
            var audience = deviceClientEndpointIdentity.iotHubConnectionString.Audience + path;

            try
            {
                await amqpLink.OpenAsync(timeout).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                amqpLink.SafeClose(exception);

                throw;
            }
            //await this.OpenLinkAsync(amqpLink, connectionString, audience, timeoutHelper.RemainingTime(), cancellationToken).ConfigureAwait(false);

            //return link;

            //if (Extensions.IsReceiver(AmqpLinkSettings))
            //{
            //    AmqpLink = new ReceivingAmqpLink(amqpClientSession.AmqpSession, AmqpLinkSettings);
            //}
            //else
            //{
            //    AmqpLink = new SendingAmqpLink(amqpClientSession.AmqpSession, AmqpLinkSettings);
            //}

            //AmqpLink.SafeAddClosed(OnLinkClosed);
            //await AmqpLink.OpenAsync(timeout).ConfigureAwait(false);
            //_isLinkClosed = false;
        }

        //void AddProperty(AmqpSymbol symbol, object value)
        //{
        //    //Extensions.AddProperty((Attach)AmqpLinkSettings, symbol, value);
        //}

        //public void AddApiVersion(string apiVersion)
        //{
        //    //AddProperty(AmqpConstants.Vendor + ":" + ClientApiVersionHelper.ApiVersionName, apiVersion);
        //}

        //public void AddClientVersion(string clientVersion)
        //{
        //    AddProperty(AmqpConstants.Vendor + ":" + ClientVersionName, clientVersion);
        //}

        public async Task<Outcome> SendMessageAsync(AmqpMessage message, ArraySegment<byte> deliveryTag, TimeSpan timeout)
        {
            var sendLink = amqpLink as SendingAmqpLink;
            if (sendLink == null)
            {
                throw new InvalidOperationException("Link does not support sending.");
            }

            return await sendLink.SendMessageAsync(message,
                deliveryTag,
                AmqpConstants.NullBinary,
                timeout).ConfigureAwait(false);
        }

        public async Task<AmqpMessage> ReceiveMessageAsync(TimeSpan timeout)
        {
            var receiveLink = amqpLink as ReceivingAmqpLink;
            if (receiveLink == null)
            {
                throw new InvalidOperationException("Link does not support receiving.");
            }

            return await receiveLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
        }

        public void AcceptMessage(AmqpMessage amqpMessage)
        {
            var receiveLink = amqpLink as ReceivingAmqpLink;
            if (receiveLink == null)
            {
                throw new InvalidOperationException("Link does not support receiving.");
            }
            receiveLink.AcceptMessage(amqpMessage, false);
        }

        void OnLinkClosed(object o, EventArgs args)
        {
            isLinkClosed = true;
        }

        private string BuildPath(string deviceTemplate, string moduleTemplate)
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
    }
}
