using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Implementation of the IAmqpClientLinkFactory interface
    /// </summary>
    internal class AmqpClientLinkFactory : IAmqpClientLinkFactory
    {
        public AmqpClientLink Create(
            AmqpClientLinkType amqpClientLinkType, 
            AmqpSession amqpSession, 
            DeviceClientEndpointIdentity deviceClientEndpointIdentity, 
            TimeSpan timeout, 
            string correlationid = "",
            bool useTokenRefresher = false,
            AmqpClientSession amqpAuthenticationSession = null
            )
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLinkFactory)}.{nameof(Create)}.{amqpClientLinkType.ToString()}");

            AmqpClientLink amqpClientLink;

            switch (amqpClientLinkType)
            {
                case AmqpClientLinkType.TelemetrySender:
                    amqpClientLink = new AmqpClientTelemetrySenderLink(amqpClientLinkType, amqpSession, deviceClientEndpointIdentity, timeout, correlationid, useTokenRefresher, amqpAuthenticationSession);
                    break;
                case AmqpClientLinkType.C2D:
                    amqpClientLink = new AmqpClientTelemetryReceiverLink(amqpClientLinkType, amqpSession, deviceClientEndpointIdentity, timeout, correlationid, useTokenRefresher, amqpAuthenticationSession);
                    break;
                case AmqpClientLinkType.MethodsSender:
                    amqpClientLink = new AmqpClientMethodsSenderLink(amqpClientLinkType, amqpSession, deviceClientEndpointIdentity, timeout, correlationid, useTokenRefresher, amqpAuthenticationSession);
                    break;
                case AmqpClientLinkType.MethodsReceiver:
                    amqpClientLink = new AmqpClientMethodsReceiverLink(amqpClientLinkType, amqpSession, deviceClientEndpointIdentity, timeout, correlationid, useTokenRefresher, amqpAuthenticationSession);
                    break;
                case AmqpClientLinkType.TwinSender:
                    amqpClientLink = new AmqpClientTwinSenderLink(amqpClientLinkType, amqpSession, deviceClientEndpointIdentity, timeout, correlationid, useTokenRefresher, amqpAuthenticationSession);
                    break;
                case AmqpClientLinkType.TwinReceiver:
                    amqpClientLink = new AmqpClientTwinReceiverLink(amqpClientLinkType, amqpSession, deviceClientEndpointIdentity, timeout, correlationid, useTokenRefresher, amqpAuthenticationSession);
                    break;
                case AmqpClientLinkType.EventsReceiver:
                    amqpClientLink = new AmqpClientEventsReceiverLink(amqpClientLinkType, amqpSession, deviceClientEndpointIdentity, timeout, correlationid, useTokenRefresher, amqpAuthenticationSession);
                    break;
                default:
                    amqpClientLink = null;
                    break;
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientLinkFactory)}.{nameof(Create)}.{amqpClientLinkType.ToString()}");

            return amqpClientLink;
        }
    }
}
