// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpConnectionPool : IAmqpUnitManager
    {
        private AmqpConnectionHolder[] _amqpSasIndividualPool;
        private readonly Dictionary<string, AmqpConnectionHolder[]> _amqpSasGroupedPool = new();
        private readonly object _lock = new();

        protected virtual IDictionary<string, AmqpConnectionHolder[]> GetAmqpSasGroupedPoolDictionary()
        {
            return _amqpSasGroupedPool;
        }

        public AmqpUnit CreateAmqpUnit(
            IClientIdentity clientIdentity,
            Func<MethodRequestInternal, Task> onMethodCallback,
            Action<Twin, string, TwinCollection, IotHubException> twinMessageListener,
            Func<string, Message, Task> onModuleMessageReceivedCallback,
            Func<Message, Task> onDeviceMessageReceivedCallback,
            Action onUnitDisconnected)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, clientIdentity, nameof(CreateAmqpUnit));

            if (clientIdentity.IsPooling())
            {
                AmqpConnectionHolder amqpConnectionHolder;
                lock (_lock)
                {
                    AmqpConnectionHolder[] amqpConnectionHolders = ResolveConnectionGroup(clientIdentity);
                    amqpConnectionHolder = ResolveConnectionByHashing(amqpConnectionHolders, clientIdentity);
                }

                if (Logging.IsEnabled)
                    Logging.Exit(this, clientIdentity, nameof(CreateAmqpUnit));

                return amqpConnectionHolder.CreateAmqpUnit(
                    clientIdentity,
                    onMethodCallback,
                    twinMessageListener,
                    onModuleMessageReceivedCallback,
                    onDeviceMessageReceivedCallback,
                    onUnitDisconnected);
            }
            else
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, clientIdentity, nameof(CreateAmqpUnit));

                return new AmqpConnectionHolder(clientIdentity)
                    .CreateAmqpUnit(
                        clientIdentity,
                        onMethodCallback,
                        twinMessageListener,
                        onModuleMessageReceivedCallback,
                        onDeviceMessageReceivedCallback,
                        onUnitDisconnected);
            }
        }

        public void RemoveAmqpUnit(AmqpUnit amqpUnit)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, amqpUnit, nameof(RemoveAmqpUnit));

            IClientIdentity clientIdentity = amqpUnit.GetClientIdentity();
            if (clientIdentity.IsPooling())
            {
                AmqpConnectionHolder amqpConnectionHolder;
                lock (_lock)
                {
                    AmqpConnectionHolder[] amqpConnectionHolders = ResolveConnectionGroup(clientIdentity);
                    amqpConnectionHolder = ResolveConnectionByHashing(amqpConnectionHolders, clientIdentity);

                    amqpConnectionHolder.RemoveAmqpUnit(amqpUnit);

                    // If the connection holder does not have any more units, the entry needs to be nullified.
                    if (amqpConnectionHolder.IsEmpty())
                    {
                        int index = GetClientIdentityIndex(clientIdentity, amqpConnectionHolders.Length);
                        amqpConnectionHolders[index] = null;
                        amqpConnectionHolder?.Dispose();
                    }
                }
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, amqpUnit, nameof(RemoveAmqpUnit));
        }

        private AmqpConnectionHolder[] ResolveConnectionGroup(IClientIdentity clientIdentity)
        {
            if (clientIdentity.AmqpTransportSettings.ConnectionPoolSettings == null)
            {
                clientIdentity.AmqpTransportSettings.ConnectionPoolSettings = new AmqpConnectionPoolSettings();
            }

            if (clientIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
            {
                if (_amqpSasIndividualPool == null)
                {
                    _amqpSasIndividualPool = new AmqpConnectionHolder[clientIdentity.AmqpTransportSettings.ConnectionPoolSettings.MaxPoolSize];
                }

                return _amqpSasIndividualPool;
            }

            string scope = clientIdentity.SharedAccessKeyName;
            GetAmqpSasGroupedPoolDictionary().TryGetValue(scope, out AmqpConnectionHolder[] amqpConnectionHolders);
            if (amqpConnectionHolders == null)
            {
                amqpConnectionHolders = new AmqpConnectionHolder[clientIdentity.AmqpTransportSettings.ConnectionPoolSettings.MaxPoolSize];
                GetAmqpSasGroupedPoolDictionary().Add(scope, amqpConnectionHolders);
            }

            return amqpConnectionHolders;
        }

        private AmqpConnectionHolder ResolveConnectionByHashing(AmqpConnectionHolder[] pool, IClientIdentity clientIdentity)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, clientIdentity, nameof(ResolveConnectionByHashing));

            int index = GetClientIdentityIndex(clientIdentity, pool.Length);

            if (pool[index] == null)
            {
                pool[index] = new AmqpConnectionHolder(clientIdentity);
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, clientIdentity, nameof(ResolveConnectionByHashing));

            return pool[index];
        }

        private static int GetClientIdentityIndex(IClientIdentity clientIdentity, int poolLength)
        {
            return clientIdentity == null
                ? throw new ArgumentNullException(nameof(clientIdentity))
                : Math.Abs(clientIdentity.GetHashCode()) % poolLength;
        }
    }
}
