// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpConnectionPool : IAmqpUnitManager
    {
        private AmqpConnectionHolder[] _amqpSasIndividualPool;
        private readonly IDictionary<string, AmqpConnectionHolder[]> _amqpSasGroupedPool = new Dictionary<string, AmqpConnectionHolder[]>();
        private readonly object _lock = new object();

        protected virtual IDictionary<string, AmqpConnectionHolder[]> GetAmqpSasGroupedPoolDictionary()
        {
            return _amqpSasGroupedPool;
        }

        public AmqpUnit CreateAmqpUnit(
            IDeviceIdentity deviceIdentity,
            Func<MethodRequestInternal, Task> onMethodCallback,
            Action<AmqpMessage, string, IotHubException> twinMessageListener,
            Func<string, Message, Task> onModuleMessageReceivedCallback,
            Func<Message, Task> onDeviceMessageReceivedCallback,
            Action onUnitDisconnected)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, deviceIdentity, nameof(CreateAmqpUnit));
            }

            if (deviceIdentity.IsPooling())
            {
                AmqpConnectionHolder amqpConnectionHolder;
                lock (_lock)
                {
                    AmqpConnectionHolder[] amqpConnectionHolders = ResolveConnectionGroup(deviceIdentity);
                    amqpConnectionHolder = ResolveConnectionByHashing(amqpConnectionHolders, deviceIdentity);
                }

                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, deviceIdentity, nameof(CreateAmqpUnit));
                }

                return amqpConnectionHolder.CreateAmqpUnit(
                    deviceIdentity,
                    onMethodCallback,
                    twinMessageListener,
                    onModuleMessageReceivedCallback,
                    onDeviceMessageReceivedCallback,
                    onUnitDisconnected);
            }
            else
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, deviceIdentity, nameof(CreateAmqpUnit));
                }

                return new AmqpConnectionHolder(deviceIdentity)
                    .CreateAmqpUnit(
                        deviceIdentity,
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
            {
                Logging.Enter(this, amqpUnit, nameof(RemoveAmqpUnit));
            }

            IDeviceIdentity deviceIdentity = amqpUnit.GetDeviceIdentity();
            if (deviceIdentity.IsPooling())
            {
                AmqpConnectionHolder amqpConnectionHolder;
                lock (_lock)
                {
                    AmqpConnectionHolder[] amqpConnectionHolders = ResolveConnectionGroup(deviceIdentity);
                    amqpConnectionHolder = ResolveConnectionByHashing(amqpConnectionHolders, deviceIdentity);

                    amqpConnectionHolder.RemoveAmqpUnit(amqpUnit);

                    // If the connection holder does not have any more units, the entry needs to be nullified.
                    if (amqpConnectionHolder.IsEmpty())
                    {
                        int index = GetDeviceIdentityIndex(deviceIdentity, amqpConnectionHolders.Length);
                        amqpConnectionHolders[index] = null;
                        amqpConnectionHolder?.Dispose();
                    }
                }
            }

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, amqpUnit, nameof(RemoveAmqpUnit));
            }
        }

        private AmqpConnectionHolder[] ResolveConnectionGroup(IDeviceIdentity deviceIdentity)
        {
            if (deviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
            {
                if (_amqpSasIndividualPool == null)
                {
                    _amqpSasIndividualPool = new AmqpConnectionHolder[deviceIdentity.AmqpTransportSettings.AmqpConnectionPoolSettings.MaxPoolSize];
                }

                return _amqpSasIndividualPool;
            }
            else
            {
                string scope = deviceIdentity.IotHubConnectionString.SharedAccessKeyName;
                GetAmqpSasGroupedPoolDictionary().TryGetValue(scope, out AmqpConnectionHolder[] amqpConnectionHolders);
                if (amqpConnectionHolders == null)
                {
                    amqpConnectionHolders = new AmqpConnectionHolder[deviceIdentity.AmqpTransportSettings.AmqpConnectionPoolSettings.MaxPoolSize];
                    GetAmqpSasGroupedPoolDictionary().Add(scope, amqpConnectionHolders);
                }

                return amqpConnectionHolders;
            }
        }

        private AmqpConnectionHolder ResolveConnectionByHashing(AmqpConnectionHolder[] pool, IDeviceIdentity deviceIdentity)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, deviceIdentity, nameof(ResolveConnectionByHashing));
            }

            int index = GetDeviceIdentityIndex(deviceIdentity, pool.Length);

            if (pool[index] == null)
            {
                pool[index] = new AmqpConnectionHolder(deviceIdentity);
            }

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, deviceIdentity, nameof(ResolveConnectionByHashing));
            }

            return pool[index];
        }

        private static int GetDeviceIdentityIndex(IDeviceIdentity deviceIdentity, int poolLength)
        {
            return deviceIdentity == null
                ? throw new ArgumentNullException(nameof(deviceIdentity))
                : Math.Abs(deviceIdentity.GetHashCode()) % poolLength;
        }
    }
}
