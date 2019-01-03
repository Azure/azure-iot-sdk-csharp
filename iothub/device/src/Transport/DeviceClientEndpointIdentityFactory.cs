using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Implementation of the IDeviceClientEndpointIdentityFactory interface
    /// </summary>
    internal class DeviceClientEndpointIdentityFactory : IDeviceClientEndpointIdentityFactory
    {
        public DeviceClientEndpointIdentity Create(IotHubConnectionString iotHubConnectionString, AmqpTransportSettings amqpTransportSettings)
        {
            DeviceClientEndpointIdentity deviceClientEndpointIdentity;

            if (iotHubConnectionString != null)
            {
                if (
                    (iotHubConnectionString.SharedAccessKey == null) &&
                    (iotHubConnectionString.SharedAccessKeyName == null) &&
                    (iotHubConnectionString.SharedAccessSignature == null)
                   )
                {
                    deviceClientEndpointIdentity = new DeviceClientEndpointIdentityX509(iotHubConnectionString, amqpTransportSettings);
                }
                else if (iotHubConnectionString.SharedAccessKeyName != null)
                {
                    deviceClientEndpointIdentity = new DeviceClientEndpointIdentityIoTHubSas(iotHubConnectionString, amqpTransportSettings);
                }
                else if (iotHubConnectionString.SharedAccessKeyName == null)
                {
                    if (amqpTransportSettings.AmqpConnectionPoolSettings.Pooling)
                    {
                        deviceClientEndpointIdentity = new DeviceClientEndpointIdentitySASMux(iotHubConnectionString, amqpTransportSettings);
                    }
                    else
                    {
                        deviceClientEndpointIdentity = new DeviceClientEndpointIdentitySasSingle(iotHubConnectionString, amqpTransportSettings);
                    }
                }
                else
                {
                    deviceClientEndpointIdentity = null;
                }
            }
            else
            {
                deviceClientEndpointIdentity = null;
            }

            return deviceClientEndpointIdentity;
        }
    }
}
