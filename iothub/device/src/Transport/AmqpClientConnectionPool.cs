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
        ConcurrentDictionary<DeviceClientEndpointIdentity, AmqpClientConnection> iotHubSasPool;
        ConcurrentDictionary<DeviceClientEndpointIdentity ,AmqpClientConnection> singleSasPool;
        ConcurrentDictionary<DeviceClientEndpointIdentity ,AmqpClientConnection> sasMuxPool;
        ConcurrentDictionary<DeviceClientEndpointIdentity ,AmqpClientConnection> x509Pool;

        /// <summary>
        /// 
        /// </summary>
        internal AmqpClientConnectionPool()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}");

            singleSasPool = new ConcurrentDictionary<DeviceClientEndpointIdentity, AmqpClientConnection>();
            x509Pool = new ConcurrentDictionary<DeviceClientEndpointIdentity, AmqpClientConnection>();
            sasMuxPool = new ConcurrentDictionary<DeviceClientEndpointIdentity, AmqpClientConnection>();
            iotHubSasPool = new ConcurrentDictionary<DeviceClientEndpointIdentity, AmqpClientConnection>();
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
            else if (deviceClientEndpointIdentity is DeviceClientEndpointIdentitySASMux)
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

            if (singleSasPool.ContainsKey(deviceClientEndpointIdentity))
            {
                AmqpClientConnection amqpClientConnection;
                singleSasPool.TryRemove(deviceClientEndpointIdentity, out amqpClientConnection);

                if (amqpClientConnection != null)
                {
                    if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnectionPool)}.{"Removed:"}.{nameof(amqpClientConnection)}");
                }
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(RemoveClientConnection)}");
        }


        private AmqpClientConnection GetIoTHubSasSingleClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (singleSasPool.ContainsKey(deviceClientEndpointIdentity))
            {
                // Error handling!!!
                return null;
            }
            else
            {
                AmqpClientConnectionFactory amqpClientConnectionFactory = new AmqpClientConnectionFactory();

                AmqpClientConnection amqpClientConnection = amqpClientConnectionFactory.Create(deviceClientEndpointIdentity, RemoveClientConnection);
                if (singleSasPool.TryAdd(deviceClientEndpointIdentity, amqpClientConnection))
                {
                    return amqpClientConnection;
                }
                else
                {
                    // Error handling!!!
                    return null;
                }
            }
        }

        private AmqpClientConnection GetIoTHubX509ClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            throw new NotImplementedException();
        }

        private AmqpClientConnection GetIoTHubSasMuxClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            throw new NotImplementedException();
        }

        private AmqpClientConnection GetIoTHubSasClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            throw new NotImplementedException();
        }
    }
}