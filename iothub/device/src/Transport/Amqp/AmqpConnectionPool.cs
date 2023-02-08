// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal sealed class AmqpConnectionPool : IAmqpUnitManager
    {
        private AmqpConnectionHolder[] _amqpSasIndividualPool;
        private readonly Dictionary<string, AmqpConnectionHolder[]> _amqpSasGroupedPool = new();
        private readonly object _lock = new();

        public AmqpUnit CreateAmqpUnit(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation additionalClientInformation,
            IotHubClientAmqpSettings amqpSettings,
            Func<DirectMethodRequest, Task> onMethodCallback,
            Action<AmqpMessage, string, IotHubClientException> twinMessageListener,
            Func<IncomingMessage, Task<MessageAcknowledgement>> onMessageReceivedCallback,
            Action onUnitDisconnected)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, connectionCredentials, nameof(CreateAmqpUnit));

            if (connectionCredentials.ClientCertificate == null
                && amqpSettings.ConnectionPoolSettings != null
                && amqpSettings.ConnectionPoolSettings.UsePooling)
            {
                AmqpConnectionHolder amqpConnectionHolder;
                lock (_lock)
                {
                    AmqpConnectionHolder[] amqpConnectionHolders = ResolveConnectionGroup(connectionCredentials, amqpSettings);
                    amqpConnectionHolder = ResolveConnectionByHashing(amqpConnectionHolders, connectionCredentials, amqpSettings);
                }

                if (Logging.IsEnabled)
                    Logging.Exit(this, connectionCredentials, nameof(CreateAmqpUnit));

                return amqpConnectionHolder.CreateAmqpUnit(
                    connectionCredentials,
                    additionalClientInformation,
                    amqpSettings,
                    onMethodCallback,
                    twinMessageListener,
                    onMessageReceivedCallback,
                    onUnitDisconnected);
            }
            else
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, connectionCredentials, nameof(CreateAmqpUnit));

                return new AmqpConnectionHolder(connectionCredentials, amqpSettings)
                    .CreateAmqpUnit(
                        connectionCredentials,
                        additionalClientInformation,
                        amqpSettings,
                        onMethodCallback,
                        twinMessageListener,
                        onMessageReceivedCallback,
                        onUnitDisconnected);
            }
        }

        public void RemoveAmqpUnit(AmqpUnit amqpUnit)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, amqpUnit, nameof(RemoveAmqpUnit));

            (IConnectionCredentials connectionCredentials, IotHubClientAmqpSettings amqpSettings) = amqpUnit.GetConnectionCredentialsAndAmqpSettings();
            if (connectionCredentials.ClientCertificate == null
                && amqpSettings.ConnectionPoolSettings != null
                && amqpSettings.ConnectionPoolSettings.UsePooling)
            {
                AmqpConnectionHolder amqpConnectionHolder;
                lock (_lock)
                {
                    AmqpConnectionHolder[] amqpConnectionHolders = ResolveConnectionGroup(connectionCredentials, amqpSettings);
                    amqpConnectionHolder = ResolveConnectionByHashing(amqpConnectionHolders, connectionCredentials, amqpSettings);

                    amqpConnectionHolder.RemoveAmqpUnit(amqpUnit);

                    // If the connection holder does not have any more units, the entry needs to be nullified.
                    if (amqpConnectionHolder.IsEmpty())
                    {
                        int index = GetClientIndex(connectionCredentials, amqpConnectionHolders.Length);
                        amqpConnectionHolders[index] = null;
                        amqpConnectionHolder?.Dispose();
                    }
                }
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, amqpUnit, nameof(RemoveAmqpUnit));
        }

        private AmqpConnectionHolder[] ResolveConnectionGroup(IConnectionCredentials connectionCredentials, IotHubClientAmqpSettings amqpSettings)
        {
            amqpSettings.ConnectionPoolSettings ??= new AmqpConnectionPoolSettings();

            if (connectionCredentials.AuthenticationModel == AuthenticationModel.SasIndividual)
            {
                _amqpSasIndividualPool ??= new AmqpConnectionHolder[amqpSettings.ConnectionPoolSettings.MaxPoolSize];

                return _amqpSasIndividualPool;
            }

            string scope = connectionCredentials.SharedAccessKeyName;
            _amqpSasGroupedPool.TryGetValue(scope, out AmqpConnectionHolder[] amqpConnectionHolders);
            if (amqpConnectionHolders == null)
            {
                amqpConnectionHolders = new AmqpConnectionHolder[amqpSettings.ConnectionPoolSettings.MaxPoolSize];
                _amqpSasGroupedPool.Add(scope, amqpConnectionHolders);
            }

            return amqpConnectionHolders;
        }

        private AmqpConnectionHolder ResolveConnectionByHashing(AmqpConnectionHolder[] pool, IConnectionCredentials connectionCredentials, IotHubClientAmqpSettings amqpSettings)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, connectionCredentials, nameof(ResolveConnectionByHashing));

            int index = GetClientIndex(connectionCredentials, pool.Length);

            pool[index] ??= new AmqpConnectionHolder(connectionCredentials, amqpSettings);

            if (Logging.IsEnabled)
                Logging.Exit(this, connectionCredentials, nameof(ResolveConnectionByHashing));

            return pool[index];
        }

        private static int GetClientIndex(IConnectionCredentials connectionCredentials, int poolLength)
        {
            return connectionCredentials == null
                ? throw new ArgumentNullException(nameof(connectionCredentials))
                : Math.Abs(connectionCredentials.GetHashCode()) % poolLength;
        }
    }
}
