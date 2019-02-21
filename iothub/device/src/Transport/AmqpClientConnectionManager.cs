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

        private static Semaphore getConnectionSemaphore = new Semaphore(1, 1);


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
        /// Create connection by device identity
        /// </summary>
        internal AmqpClientConnection CreateClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            getConnectionSemaphore.WaitOne();

            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionManager)}.{nameof(CreateClientConnection)}");

            AmqpClientConnection amqpClientConnection = amqpClientConnectionPool.CreateClientConnection(deviceClientEndpointIdentity);

            getConnectionSemaphore.Release();

            return amqpClientConnection;
        }
    }
}
