using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Implementation of the IAmqpClientLinkFactory interface
    /// </summary>
    internal class AmqpClientLinkFactory : IAmqpClientLinkFactory
    {
        public AmqpClientLink Create(AmqpClientLinkType amqpClientLinkType, AmqpClientSession amqpClientSession, DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout, string correlationid = "")
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientLinkFactory)}.{nameof(Create)}.{amqpClientLinkType.ToString()}");

            AmqpClientLink amqpClientLink;

            switch (amqpClientLinkType)
            {
                case AmqpClientLinkType.TelemetrySender:
                    amqpClientLink = new AmqpClientTelemetrySenderLink(amqpClientLinkType, amqpClientSession, deviceClientEndpointIdentity, timeout, correlationid);
                    break;
                case AmqpClientLinkType.C2D:
                    amqpClientLink = new AmqpClientTelemetryReceiverLink(amqpClientLinkType, amqpClientSession, deviceClientEndpointIdentity, timeout, correlationid);
                    break;
                case AmqpClientLinkType.MethodsSender:
                    amqpClientLink = new AmqpClientMethodsSenderLink(amqpClientLinkType, amqpClientSession, deviceClientEndpointIdentity, timeout, correlationid);
                    break;
                case AmqpClientLinkType.MethodsReceiver:
                    amqpClientLink = new AmqpClientMethodsReceiverLink(amqpClientLinkType, amqpClientSession, deviceClientEndpointIdentity, timeout, correlationid);
                    break;
                case AmqpClientLinkType.TwinSender:
                    amqpClientLink = new AmqpClientTwinSenderLink(amqpClientLinkType, amqpClientSession, deviceClientEndpointIdentity, timeout, correlationid);
                    break;
                case AmqpClientLinkType.TwinReceiver:
                    amqpClientLink = new AmqpClientTwinReceiverLink(amqpClientLinkType, amqpClientSession, deviceClientEndpointIdentity, timeout, correlationid);
                    break;
                case AmqpClientLinkType.EventsReceiver:
                    amqpClientLink = new AmqpClientEventsReceiverLink(amqpClientLinkType, amqpClientSession, deviceClientEndpointIdentity, timeout, correlationid);
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
