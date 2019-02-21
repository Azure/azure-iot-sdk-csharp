// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Specialization of AmqpClientLink for sending methods response
    /// </summary>
    internal class AmqpClientTwinReceiverLink : AmqpClientLink
    {
        const string linkNamePostFix = "_TwinReceiverLink";
        const string correlationIdPrefix = "twin:";

        internal AmqpClientTwinReceiverLink(
            AmqpClientLinkType amqpClientLinkType, 
            AmqpSession amqpSession, 
            DeviceClientEndpointIdentity deviceClientEndpointIdentity, 
            TimeSpan timeout,
            string correlationId,
            bool useTokenRefresher,
            AmqpClientSession amqpAuthenticationSession
            )
            : base(amqpClientLinkType, amqpSession, deviceClientEndpointIdentity, correlationId, useTokenRefresher, amqpAuthenticationSession)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientTwinReceiverLink)}");

            linkPath = this.BuildPath(CommonConstants.DeviceTwinPathTemplate, CommonConstants.ModuleTwinPathTemplate);
            Uri uri = deviceClientEndpointIdentity.iotHubConnectionString.BuildLinkAddress(linkPath);
            uint prefetchCount = deviceClientEndpointIdentity.amqpTransportSettings.PrefetchCount;

            amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = CommonResources.GetNewStringGuid(linkNamePostFix),
                Role = true,
                TotalLinkCredit = prefetchCount,
                AutoSendFlow = prefetchCount > 0,
                Source = new Source() { Address = uri.AbsoluteUri }
            };
            amqpLinkSettings.SndSettleMode = (byte)SenderSettleMode.Settled;
            amqpLinkSettings.RcvSettleMode = (byte)ReceiverSettleMode.First;

            var timeoutHelper = new TimeoutHelper(timeout);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeoutHelper.RemainingTime().TotalMilliseconds);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, deviceClientEndpointIdentity.productInfo.ToString());

            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ApiVersion, ClientApiVersionHelper.ApiVersionString);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ChannelCorrelationId, correlationIdPrefix + correlationId);

            amqpLink = new ReceivingAmqpLink(amqpLinkSettings);
            amqpLink.AttachTo(amqpSession);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientTwinReceiverLink)}");
        }
    }
}
