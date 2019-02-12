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

        ConcurrentDictionary<DeviceClientEndpointIdentity, AmqpClientConnection> deviceConnectionDictionaryIoTHubSas;
        ConcurrentBag<AmqpClientConnection> connectionPoolIotHubSas;

        AmqpClientConnectionFactory amqpClientConnectionFactory;

        internal AmqpClientConnectionPool()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}");

            deviceConnectionDictionary = new ConcurrentDictionary<DeviceClientEndpointIdentity, AmqpClientConnection>();
            connectionPool = new ConcurrentBag<AmqpClientConnection>();

            deviceConnectionDictionaryIoTHubSas = new ConcurrentDictionary<DeviceClientEndpointIdentity, AmqpClientConnection>();
            connectionPoolIotHubSas = new ConcurrentBag<AmqpClientConnection>();

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
            }
            else if (deviceClientEndpointIdentity is DeviceClientEndpointIdentityIoTHubSas)
            {
                if (deviceConnectionDictionaryIoTHubSas.ContainsKey(deviceClientEndpointIdentity))
                {
                    deviceConnectionDictionaryIoTHubSas.TryRemove(deviceClientEndpointIdentity, out AmqpClientConnection amqpClientConnection);

                    if (amqpClientConnection != null)
                    {
                        if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnectionPool)}.{"Removed from IoTHubSasPool:"}.{nameof(amqpClientConnection)}");
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

        private AmqpClientConnection GetIoTHubSasClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(GetIoTHubSasClientConnection)}");

            AmqpClientConnection amqpClientConnection = null;

            if (connectionPoolIotHubSas.Count < deviceClientEndpointIdentity.amqpTransportSettings.AmqpConnectionPoolSettings.MaxPoolSize)
            {
                amqpClientConnection = amqpClientConnectionFactory.Create(deviceClientEndpointIdentity, RemoveClientConnection);
                if (deviceConnectionDictionaryIoTHubSas.TryAdd(deviceClientEndpointIdentity, amqpClientConnection))
                {
                    connectionPoolIotHubSas.Add(amqpClientConnection);
                }
                else
                {
                    amqpClientConnection = null;
                }
            }
            else
            {
                amqpClientConnection = GetLeastUsedConnection(amqpClientConnection);
                if (!(deviceConnectionDictionaryIoTHubSas.TryAdd(deviceClientEndpointIdentity, amqpClientConnection)))
                {
                    amqpClientConnection = null;
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(GetIoTHubSasClientConnection)}");

            return amqpClientConnection;
        }

        private AmqpClientConnection GetIoTHubSasMuxClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(GetIoTHubSasMuxClientConnection)}");

            AmqpClientConnection amqpClientConnection = null;

            if (deviceConnectionDictionary.TryGetValue(deviceClientEndpointIdentity, out amqpClientConnection))
            {
                return amqpClientConnection;
            }

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
                amqpClientConnection = GetLeastUsedConnection(amqpClientConnection);
                if (!(deviceConnectionDictionary.TryAdd(deviceClientEndpointIdentity, amqpClientConnection)))
                {
                    amqpClientConnection = null;
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(GetIoTHubSasMuxClientConnection)}");

            return amqpClientConnection;
        }

        private AmqpClientConnection GetLeastUsedConnection(AmqpClientConnection amqpClientConnection)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(GetLeastUsedConnection)}");

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

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(GetLeastUsedConnection)}");

            return amqpClientConnection;
        }
    }
}
