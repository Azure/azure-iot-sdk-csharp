using Microsoft.Azure.Devices.Shared;
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
        public DeviceClientEndpointIdentity Create(IotHubConnectionString iotHubConnectionString, AmqpTransportSettings amqpTransportSettings, ProductInfo productInfo)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(DeviceClientEndpointIdentityFactory)}.{nameof(Create)}");

            DeviceClientEndpointIdentity deviceClientEndpointIdentity;

            if (iotHubConnectionString != null)
            {
                if (
                    (iotHubConnectionString.SharedAccessKey == null) &&
                    (iotHubConnectionString.SharedAccessKeyName == null) &&
                    (iotHubConnectionString.SharedAccessSignature == null) &&
                    (iotHubConnectionString.TokenRefresher == null)
                   )
                {
                    deviceClientEndpointIdentity = new DeviceClientEndpointIdentityX509(iotHubConnectionString, amqpTransportSettings, productInfo);
                }
                else if (iotHubConnectionString.SharedAccessKeyName != null)
                {
                    deviceClientEndpointIdentity = new DeviceClientEndpointIdentityIoTHubSas(iotHubConnectionString, amqpTransportSettings, productInfo);
                }
                else if ((iotHubConnectionString.SharedAccessKeyName == null) || (iotHubConnectionString.TokenRefresher != null))
                {
                    if (amqpTransportSettings.AmqpConnectionPoolSettings.Pooling)
                    {
                        deviceClientEndpointIdentity = new DeviceClientEndpointIdentitySASMux(iotHubConnectionString, amqpTransportSettings, productInfo);
                    }
                    else
                    {
                        deviceClientEndpointIdentity = new DeviceClientEndpointIdentitySasSingle(iotHubConnectionString, amqpTransportSettings, productInfo);
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

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(DeviceClientEndpointIdentityFactory)}.{nameof(Create)}");

            return deviceClientEndpointIdentity;
        }
    }
}
