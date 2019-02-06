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

    internal class AmqpClientConnectionPool
    {
        ConcurrentDictionary<DeviceClientEndpointIdentity ,AmqpClientConnection> deviceConnectionDictionary;
        ConcurrentBag<AmqpClientConnection> connectionPool;

        AmqpClientConnectionFactory amqpClientConnectionFactory;
        AmqpClientConnection amqpClientConnectionIotHubSas;

        internal AmqpClientConnectionPool()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}");

            deviceConnectionDictionary = new ConcurrentDictionary<DeviceClientEndpointIdentity, AmqpClientConnection>();
            connectionPool = new ConcurrentBag<AmqpClientConnection>();

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
                if (deviceConnectionDictionary.ContainsKey(deviceClientEndpointIdentity))
                {
                    deviceConnectionDictionary.TryRemove(deviceClientEndpointIdentity, out AmqpClientConnection amqpClientConnection);

                    if (amqpClientConnection != null)
                    {
                        if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnectionPool)}.{"Removed from sasMuxPool:"}.{nameof(amqpClientConnection)}");
                    }
                }
                //amqpClientConnectionSasMux = null;
            }
            else if (deviceClientEndpointIdentity is DeviceClientEndpointIdentityIoTHubSas)
            {
                amqpClientConnectionIotHubSas = null;
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

        private AmqpClientConnection GetIoTHubSasClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (amqpClientConnectionIotHubSas == null)
            {
                amqpClientConnectionIotHubSas = amqpClientConnectionFactory.Create(deviceClientEndpointIdentity, RemoveClientConnection);
            }

            return amqpClientConnectionIotHubSas;
        }

        private AmqpClientConnection GetIoTHubSasMuxClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            AmqpClientConnection amqpClientConnection = null;

            if (connectionPool.Count < deviceClientEndpointIdentity.amqpTransportSettings.AmqpConnectionPoolSettings.MaxPoolSize)
            {
                amqpClientConnection = amqpClientConnectionFactory.Create(deviceClientEndpointIdentity, RemoveClientConnection);
                if (deviceConnectionDictionary.TryAdd(deviceClientEndpointIdentity, amqpClientConnection))
                {
                    connectionPool.Add(amqpClientConnection);
                }
                else
                {
                    amqpClientConnection = null;
                }
            }
            else
            {
                // Find the least used Mux
                var values = deviceConnectionDictionary.Values;

                int count = int.MaxValue;

                foreach (var value in values)
                {
                    int clientCount = value.GetNumberOfClients();
                    if (clientCount < count)
                    {
                        amqpClientConnection = value;
                        count = clientCount;
                    }
                }
                if (!(deviceConnectionDictionary.TryAdd(deviceClientEndpointIdentity, amqpClientConnection)))
                {
                    amqpClientConnection = null;
                }
            }
            return amqpClientConnection;
        }
    }
}
