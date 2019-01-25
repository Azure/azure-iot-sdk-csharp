// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using Microsoft.Azure.Devices.Shared;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Thread safe singleton to manage connection pools
    /// </summary>
    internal class AmqpClientConnectionManager
    {
        private AmqpClientConnectionPool amqpClientConnectionPool;

        /// <summary>
        /// 
        /// </summary>
        internal AmqpClientConnectionManager()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionManager)}");

            this.amqpClientConnectionPool = new AmqpClientConnectionPool();
        }

        private static readonly object padlock = new object();
        private static AmqpClientConnectionManager instance = null;

        /// <summary>
        /// Static member variable to store the single instance of this class
        /// </summary>
        internal static AmqpClientConnectionManager Instance
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
        internal AmqpClientConnection GetClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionManager)}.{nameof(GetClientConnection)}");

            return amqpClientConnectionPool.GetClientConnection(deviceClientEndpointIdentity);
        }

        internal void RemoveClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionManager)}.{nameof(RemoveClientConnection)}");

            amqpClientConnectionPool.RemoveClientConnection(deviceClientEndpointIdentity);
        }
    }
}
