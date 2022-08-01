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
            IIotHubConnectionInfo iotHubConnectionInfo,
            Func<MethodRequestInternal, Task> onMethodCallback,
            Action<Twin, string, TwinCollection, IotHubException> twinMessageListener,
            Func<string, Message, Task> onModuleMessageReceivedCallback,
            Func<Message, Task> onDeviceMessageReceivedCallback,
            Action onUnitDisconnected)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, iotHubConnectionInfo, nameof(CreateAmqpUnit));

            if (iotHubConnectionInfo.IsPooling())
            {
                AmqpConnectionHolder amqpConnectionHolder;
                lock (_lock)
                {
                    AmqpConnectionHolder[] amqpConnectionHolders = ResolveConnectionGroup(iotHubConnectionInfo);
                    amqpConnectionHolder = ResolveConnectionByHashing(amqpConnectionHolders, iotHubConnectionInfo);
                }

                if (Logging.IsEnabled)
                    Logging.Exit(this, iotHubConnectionInfo, nameof(CreateAmqpUnit));

                return amqpConnectionHolder.CreateAmqpUnit(
                    iotHubConnectionInfo,
                    onMethodCallback,
                    twinMessageListener,
                    onModuleMessageReceivedCallback,
                    onDeviceMessageReceivedCallback,
                    onUnitDisconnected);
            }
            else
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, iotHubConnectionInfo, nameof(CreateAmqpUnit));

                return new AmqpConnectionHolder(iotHubConnectionInfo)
                    .CreateAmqpUnit(
                        iotHubConnectionInfo,
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

            IIotHubConnectionInfo iotHubConnectionInfo = amqpUnit.GetIotHubConnectionInfo();
            if (iotHubConnectionInfo.IsPooling())
            {
                AmqpConnectionHolder amqpConnectionHolder;
                lock (_lock)
                {
                    AmqpConnectionHolder[] amqpConnectionHolders = ResolveConnectionGroup(iotHubConnectionInfo);
                    amqpConnectionHolder = ResolveConnectionByHashing(amqpConnectionHolders, iotHubConnectionInfo);

                    amqpConnectionHolder.RemoveAmqpUnit(amqpUnit);

                    // If the connection holder does not have any more units, the entry needs to be nullified.
                    if (amqpConnectionHolder.IsEmpty())
                    {
                        int index = GetDeviceIdentityIndex(iotHubConnectionInfo, amqpConnectionHolders.Length);
                        amqpConnectionHolders[index] = null;
                        amqpConnectionHolder?.Dispose();
                    }
                }
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, amqpUnit, nameof(RemoveAmqpUnit));
        }

        private AmqpConnectionHolder[] ResolveConnectionGroup(IIotHubConnectionInfo iotHubConnectionInfo)
        {
            if (iotHubConnectionInfo.AmqpTransportSettings.ConnectionPoolSettings == null)
            {
                iotHubConnectionInfo.AmqpTransportSettings.ConnectionPoolSettings = new AmqpConnectionPoolSettings();
            }

            if (iotHubConnectionInfo.AuthenticationModel == AuthenticationModel.SasIndividual)
            {
                if (_amqpSasIndividualPool == null)
                {
                    _amqpSasIndividualPool = new AmqpConnectionHolder[iotHubConnectionInfo.AmqpTransportSettings.ConnectionPoolSettings.MaxPoolSize];
                }

                return _amqpSasIndividualPool;
            }

            string scope = iotHubConnectionInfo.SharedAccessKeyName;
            GetAmqpSasGroupedPoolDictionary().TryGetValue(scope, out AmqpConnectionHolder[] amqpConnectionHolders);
            if (amqpConnectionHolders == null)
            {
                amqpConnectionHolders = new AmqpConnectionHolder[iotHubConnectionInfo.AmqpTransportSettings.ConnectionPoolSettings.MaxPoolSize];
                GetAmqpSasGroupedPoolDictionary().Add(scope, amqpConnectionHolders);
            }

            return amqpConnectionHolders;
        }

        private AmqpConnectionHolder ResolveConnectionByHashing(AmqpConnectionHolder[] pool, IIotHubConnectionInfo iotHubConnectionInfo)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, iotHubConnectionInfo, nameof(ResolveConnectionByHashing));

            int index = GetDeviceIdentityIndex(iotHubConnectionInfo, pool.Length);

            if (pool[index] == null)
            {
                pool[index] = new AmqpConnectionHolder(iotHubConnectionInfo);
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, iotHubConnectionInfo, nameof(ResolveConnectionByHashing));

            return pool[index];
        }

        private static int GetDeviceIdentityIndex(IIotHubConnectionInfo iotHubConnectionInfo, int poolLength)
        {
            return iotHubConnectionInfo == null
                ? throw new ArgumentNullException(nameof(iotHubConnectionInfo))
                : Math.Abs(iotHubConnectionInfo.GetHashCode()) % poolLength;
        }
    }
}
