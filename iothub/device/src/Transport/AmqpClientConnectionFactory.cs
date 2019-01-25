using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Implementation of the IAmqpClientConnectionFactory interface
    /// </summary>
    internal class AmqpClientConnectionFactory : IAmqpClientConnectionFactory
    {
        public AmqpClientConnection Create(DeviceClientEndpointIdentity deviceClientEndpointIdentity, RemoveClientConnectionFromPool removeDelegate)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionFactory)}.{nameof(Create)}");

            if (deviceClientEndpointIdentity.GetType() == typeof(DeviceClientEndpointIdentitySasSingle))
            {
                return new AmqpClientConnectionSasSingle(deviceClientEndpointIdentity);
            }
            else if (deviceClientEndpointIdentity.GetType() == typeof(DeviceClientEndpointIdentityX509))
            {
                return new AmqpClientConnectionX509(deviceClientEndpointIdentity);
            }
            else if (deviceClientEndpointIdentity.GetType() == typeof(DeviceClientEndpointIdentityIoTHubSas))
            {
                throw new NotImplementedException();
            }
            else if (deviceClientEndpointIdentity.GetType() == typeof(DeviceClientEndpointIdentitySASMux))
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new ArgumentException("Unknown type of DeviceClientEndpointIdentity");
            }
        }
    }
}
