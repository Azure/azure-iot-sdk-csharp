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
            if (deviceClientEndpointIdentity.GetType() == typeof(DeviceClientEndpointIdentitySasSingle))
            {
                return new AmqpClientConnectionSasSingle(deviceClientEndpointIdentity, removeDelegate);
            }
            else if (deviceClientEndpointIdentity.GetType() == typeof(DeviceClientEndpointIdentityX509))
            {
                throw new NotImplementedException();
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
