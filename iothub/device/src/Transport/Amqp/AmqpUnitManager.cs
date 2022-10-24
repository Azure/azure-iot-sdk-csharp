// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpUnitManager : IAmqpUnitManager
    {
        private static readonly AmqpUnitManager s_instance = new();

        private readonly IDictionary<string, IAmqpUnitManager> _amqpConnectionPools;
        private readonly object _connectionPoolLock = new();

        internal AmqpUnitManager()
        {
            _amqpConnectionPools = new Dictionary<string, IAmqpUnitManager>();
        }

        internal static AmqpUnitManager GetInstance()
        {
            return s_instance;
        }

        public AmqpUnit CreateAmqpUnit(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation additionalClientInformation,
            IotHubClientAmqpSettings amqpSettings,
            Func<DirectMethodRequest, Task> onMethodCallback,
            Action<AmqpMessage, string, IotHubClientException> twinMessageListener,
            Func<IncomingMessage, Task<MessageAcknowledgement>> onMessageReceivedCallback,
            Action onUnitDisconnected)
        {
            IAmqpUnitManager amqpConnectionPool = ResolveConnectionPool(connectionCredentials.HostName);
            return amqpConnectionPool.CreateAmqpUnit(
                connectionCredentials,
                additionalClientInformation,
                amqpSettings,
                onMethodCallback,
                twinMessageListener,
                onMessageReceivedCallback,
                onUnitDisconnected);
        }

        public void RemoveAmqpUnit(AmqpUnit amqpUnit)
        {
            (IConnectionCredentials connectionCredentials, IotHubClientAmqpSettings _) = amqpUnit.GetConnectionCredentialsAndAmqpSettings();
            IAmqpUnitManager amqpConnectionPool = ResolveConnectionPool(connectionCredentials.HostName);
            amqpConnectionPool.RemoveAmqpUnit(amqpUnit);
            amqpUnit.Dispose();
        }

        private IAmqpUnitManager ResolveConnectionPool(string host)
        {
            lock (_connectionPoolLock)
            {
                if (!_amqpConnectionPools.TryGetValue(host, out IAmqpUnitManager amqpConnectionPool)
                    || amqpConnectionPool == null)
                {
                    amqpConnectionPool = new AmqpConnectionPool();
                    _amqpConnectionPools.Add(host, amqpConnectionPool);
                }

                if (Logging.IsEnabled)
                    Logging.Associate(this, amqpConnectionPool, $"{nameof(ResolveConnectionPool)}");

                return amqpConnectionPool;
            }
        }
    }
}
