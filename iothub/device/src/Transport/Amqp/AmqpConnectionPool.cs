// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Transport.AmqpIoT;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpConnectionPool : IAmqpUnitManager
    {
        private AmqpConnectionHolder[] _amqpSasIndividualPool;
        private IDictionary<string, AmqpConnectionHolder[]> _amqpSasGroupedPool = new Dictionary<string, AmqpConnectionHolder[]>();
        private readonly object _lock = new object();

        public AmqpUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity,
            Func<MethodRequestInternal, Task> methodHandler,
            Action<Twin, string, TwinCollection> twinMessageListener,
            Func<string, Message, Task> eventListener,
            Action onUnitDisconnected)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
            if (deviceIdentity.IsPooling())
            {
                AmqpConnectionHolder amqpConnectionHolder;
                lock (_lock)
                {
                    AmqpConnectionHolder[] amqpConnectionHolders = ResolveConnectionGroup(deviceIdentity);
                    amqpConnectionHolder = ResolveConnectionByHashing(amqpConnectionHolders, deviceIdentity);
                }

                if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
                return amqpConnectionHolder.CreateAmqpUnit(deviceIdentity, methodHandler, twinMessageListener, eventListener, onUnitDisconnected);
            }
            else
            {
                if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
                return new AmqpConnectionHolder(deviceIdentity).CreateAmqpUnit(deviceIdentity, methodHandler, twinMessageListener, eventListener, onUnitDisconnected);
            }
        }

        public void RemoveAmqpUnit(AmqpUnit amqpUnit)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpUnit, $"{nameof(RemoveAmqpUnit)}");
            DeviceIdentity deviceIdentity = amqpUnit.GetDeviceIdentity();
            if (deviceIdentity.IsPooling())
            {
                AmqpConnectionHolder amqpConnectionHolder;
                lock (_lock)
                {
                    AmqpConnectionHolder[] amqpConnectionHolders = ResolveConnectionGroup(deviceIdentity);
                    amqpConnectionHolder = ResolveConnectionByHashing(amqpConnectionHolders, deviceIdentity);
                }
                amqpConnectionHolder.RemoveAmqpUnit(amqpUnit);
            }

            if (Logging.IsEnabled) Logging.Exit(this, amqpUnit, $"{nameof(RemoveAmqpUnit)}");
        }

        private AmqpConnectionHolder[] ResolveConnectionGroup(DeviceIdentity deviceIdentity)
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
                _amqpSasGroupedPool.TryGetValue(scope, out AmqpConnectionHolder[] amqpConnectionHolders);
                if (amqpConnectionHolders == null)
                {
                    amqpConnectionHolders = new AmqpConnectionHolder[deviceIdentity.AmqpTransportSettings.AmqpConnectionPoolSettings.MaxPoolSize];
                    _amqpSasGroupedPool.Add(scope, amqpConnectionHolders);
                }

                return amqpConnectionHolders;
            }
        }

        private AmqpConnectionHolder ResolveConnectionByHashing(AmqpConnectionHolder[] pool, DeviceIdentity deviceIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(ResolveConnectionByHashing)}");
            int index = Math.Abs(deviceIdentity.GetHashCode()) % pool.Length;
            if (pool[index] == null)
            {
                pool[index] = new AmqpConnectionHolder(deviceIdentity);
            }

            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(ResolveConnectionByHashing)}");
            return pool[index];
        }
    }
}
