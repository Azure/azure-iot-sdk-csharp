using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Factory interface to create AmqpClientConnection objects for Amqp transport layer
    /// </summary>
    interface IAmqpClientConnectionFactory
    {
        AmqpClientConnection Create(DeviceClientEndpointIdentity deviceClientEndpointIdentity, RemoveClientConnectionFromPool removeDelegate);
    }
}
