// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Collections.Concurrent;
    using Microsoft.Azure.Devices.Shared;

    delegate void RemoveClientConnectionFromPool(DeviceClientEndpointIdentity deviceClientEndpointIdentity);

    /// <summary>
    /// 
    /// </summary>
    internal class AmqpClientConnectionPool
    {
        ConcurrentDictionary<DeviceClientEndpointIdentity ,AmqpClientConnection> sasMuxPool;

        AmqpClientConnectionFactory amqpClientConnectionFactory;
        AmqpClientConnection amqpClientConnectionSasMux;

        /// <summary>
        /// 
        /// </summary>
        internal AmqpClientConnectionPool()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}");

            sasMuxPool = new ConcurrentDictionary<DeviceClientEndpointIdentity, AmqpClientConnection>();

            amqpClientConnectionFactory = new AmqpClientConnectionFactory();
        }

        internal AmqpClientConnection GetClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(GetClientConnection)}");

            if (deviceClientEndpointIdentity is DeviceClientEndpointIdentitySasSingle)
            {
                return GetIoTHubSasSingleClientConnection(deviceClientEndpointIdentity);
            }
            else if (deviceClientEndpointIdentity is DeviceClientEndpointIdentityX509)
            {
                return GetIoTHubX509ClientConnection(deviceClientEndpointIdentity);
            }
            else if (deviceClientEndpointIdentity is DeviceClientEndpointIdentitySasMux)
            {
                return GetIoTHubSasMuxClientConnection(deviceClientEndpointIdentity);
            }
            else if (deviceClientEndpointIdentity is DeviceClientEndpointIdentityIoTHubSas)
            {
                return GetIoTHubSasClientConnection(deviceClientEndpointIdentity);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        internal void RemoveClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(RemoveClientConnection)}");

            if (deviceClientEndpointIdentity is DeviceClientEndpointIdentitySasMux)
            {
                if (sasMuxPool.ContainsKey(deviceClientEndpointIdentity))
                {
                    sasMuxPool.TryRemove(deviceClientEndpointIdentity, out AmqpClientConnection amqpClientConnection);
                    if (amqpClientConnection != null)
                    {
                        if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnectionPool)}.{"Removed from sasMuxPool:"}.{nameof(amqpClientConnection)}");
                    }
                }
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(RemoveClientConnection)}");
        }


        private AmqpClientConnection GetIoTHubSasSingleClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            return amqpClientConnectionFactory.Create(deviceClientEndpointIdentity, RemoveClientConnection);
        }

        private AmqpClientConnection GetIoTHubX509ClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            return amqpClientConnectionFactory.Create(deviceClientEndpointIdentity, RemoveClientConnection);
        }

        private AmqpClientConnection GetIoTHubSasMuxClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (sasMuxPool.ContainsKey(deviceClientEndpointIdentity))
            {
                // Error handling!!!
                return null;
            }
            else
            {
                if (amqpClientConnectionSasMux == null)
                {
                    amqpClientConnectionSasMux = amqpClientConnectionFactory.Create(deviceClientEndpointIdentity, RemoveClientConnection);
                }

                if (amqpClientConnectionSasMux.AddToMux(deviceClientEndpointIdentity) && (sasMuxPool.TryAdd(deviceClientEndpointIdentity, amqpClientConnectionSasMux)))
                {
                    return amqpClientConnectionSasMux;
                }
                else
                {
                    // Error handling!!!
                    return null;
                }
            }
        }

        private AmqpClientConnection GetIoTHubSasClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            throw new NotImplementedException();
        }
    }
}
