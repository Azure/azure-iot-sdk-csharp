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
            IClientConfiguration clientConfiguration,
            Func<MethodRequestInternal, Task> onMethodCallback,
            Action<Twin, string, TwinCollection, IotHubClientException> twinMessageListener,
            Func<string, Message, Task> onModuleMessageReceivedCallback,
            Func<Message, Task> onDeviceMessageReceivedCallback,
            Action onUnitDisconnected)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, clientConfiguration, nameof(CreateAmqpUnit));

            if (clientConfiguration.IsPooling())
            {
                AmqpConnectionHolder amqpConnectionHolder;
                lock (_lock)
                {
                    AmqpConnectionHolder[] amqpConnectionHolders = ResolveConnectionGroup(clientConfiguration);
                    amqpConnectionHolder = ResolveConnectionByHashing(amqpConnectionHolders, clientConfiguration);
                }

                if (Logging.IsEnabled)
                    Logging.Exit(this, clientConfiguration, nameof(CreateAmqpUnit));

                return amqpConnectionHolder.CreateAmqpUnit(
                    clientConfiguration,
                    onMethodCallback,
                    twinMessageListener,
                    onModuleMessageReceivedCallback,
                    onDeviceMessageReceivedCallback,
                    onUnitDisconnected);
            }
            else
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, clientConfiguration, nameof(CreateAmqpUnit));

                return new AmqpConnectionHolder(clientConfiguration)
                    .CreateAmqpUnit(
                        clientConfiguration,
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

            IClientConfiguration clientConfiguration = amqpUnit.GetClientConfiguration();
            if (clientConfiguration.IsPooling())
            {
                AmqpConnectionHolder amqpConnectionHolder;
                lock (_lock)
                {
                    AmqpConnectionHolder[] amqpConnectionHolders = ResolveConnectionGroup(clientConfiguration);
                    amqpConnectionHolder = ResolveConnectionByHashing(amqpConnectionHolders, clientConfiguration);

                    amqpConnectionHolder.RemoveAmqpUnit(amqpUnit);

                    // If the connection holder does not have any more units, the entry needs to be nullified.
                    if (amqpConnectionHolder.IsEmpty())
                    {
                        int index = GetClientConfigurationIndex(clientConfiguration, amqpConnectionHolders.Length);
                        amqpConnectionHolders[index] = null;
                        amqpConnectionHolder?.Dispose();
                    }
                }
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, amqpUnit, nameof(RemoveAmqpUnit));
        }

        private AmqpConnectionHolder[] ResolveConnectionGroup(IClientConfiguration clientConfiguration)
        {
            var amqpSettings = clientConfiguration.ClientOptions.TransportSettings as IotHubClientAmqpSettings;
            if (amqpSettings.ConnectionPoolSettings == null)
            {
                amqpSettings.ConnectionPoolSettings = new AmqpConnectionPoolSettings();
            }

            if (clientConfiguration.AuthenticationModel == AuthenticationModel.SasIndividual)
            {
                if (_amqpSasIndividualPool == null)
                {
                    _amqpSasIndividualPool = new AmqpConnectionHolder[amqpSettings.ConnectionPoolSettings.MaxPoolSize];
                }

                return _amqpSasIndividualPool;
            }

            string scope = clientConfiguration.SharedAccessKeyName;
            GetAmqpSasGroupedPoolDictionary().TryGetValue(scope, out AmqpConnectionHolder[] amqpConnectionHolders);
            if (amqpConnectionHolders == null)
            {
                amqpConnectionHolders = new AmqpConnectionHolder[amqpSettings.ConnectionPoolSettings.MaxPoolSize];
                GetAmqpSasGroupedPoolDictionary().Add(scope, amqpConnectionHolders);
            }

            return amqpConnectionHolders;
        }

        private AmqpConnectionHolder ResolveConnectionByHashing(AmqpConnectionHolder[] pool, IClientConfiguration clientConfiguration)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, clientConfiguration, nameof(ResolveConnectionByHashing));

            int index = GetClientConfigurationIndex(clientConfiguration, pool.Length);

            if (pool[index] == null)
            {
                pool[index] = new AmqpConnectionHolder(clientConfiguration);
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, clientConfiguration, nameof(ResolveConnectionByHashing));

            return pool[index];
        }

        private static int GetClientConfigurationIndex(IClientConfiguration clientConfiguration, int poolLength)
        {
            return clientConfiguration == null
                ? throw new ArgumentNullException(nameof(clientConfiguration))
                : Math.Abs(clientConfiguration.GetHashCode()) % poolLength;
        }
    }
}
