// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Thread safe singleton to manage connection pools
    /// </summary>
    public class AmqpClientConnectionManager
    {
        private AmqpClientConnectionPool amqpClientConnectionPool;

        AmqpClientConnectionManager()
        {
            this.amqpClientConnectionPool = new AmqpClientConnectionPool();
        }

        private static readonly object padlock = new object();
        private static AmqpClientConnectionManager instance = null;

        /// <summary>
        /// Static member variable to store the single instance of this class
        /// </summary>
        public static AmqpClientConnectionManager Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new AmqpClientConnectionManager();
                    }
                    return instance;
                }
            }
        }

        /// <summary>
        /// Get connection by device identity
        /// </summary>
        internal AmqpClientConnection GetClientConnection(IotHubConnectionString iotHubConnectionString, AmqpTransportSettings amqpTransportSettings)
        {
            DeviceClientEndpointIdentityFactory deviceClientEndpointIdentityFactory = new DeviceClientEndpointIdentityFactory();
            DeviceClientEndpointIdentity deviceClientEndpointIdentity = deviceClientEndpointIdentityFactory.Create(iotHubConnectionString, amqpTransportSettings);

            return amqpClientConnectionPool.GetClientConnection(deviceClientEndpointIdentity);
        }
    }
}
