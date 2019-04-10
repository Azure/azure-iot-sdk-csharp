// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpConnectionPool : IAmqpConnectionManager
    {
        private IAmqpConnectionHolder[] _amqpSasIndividualPool;
        private IDictionary<string, IAmqpConnectionHolder[]> _amqpSasGroupedPool;
        private readonly object _lock;

        internal AmqpConnectionPool()
        {
            _lock = new object();
            _amqpSasGroupedPool = new Dictionary<string, IAmqpConnectionHolder[]>();
        }

        private IAmqpConnectionHolder[] ResolveConnectionGroup(DeviceIdentity deviceIdentity)
        {
            if (deviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
            {
                if (_amqpSasIndividualPool == null)
                {
                    _amqpSasIndividualPool = new IAmqpConnectionHolder[deviceIdentity.AmqpTransportSettings.AmqpConnectionPoolSettings.MaxPoolSize];
                }
                return _amqpSasIndividualPool;
            }
            else
            {
                string scope = deviceIdentity.IotHubConnectionString.SharedAccessKeyName;
                _amqpSasGroupedPool.TryGetValue(scope, out IAmqpConnectionHolder[] connectionGroup);
                if (connectionGroup == null)
                {
                    connectionGroup = new IAmqpConnectionHolder[deviceIdentity.AmqpTransportSettings.AmqpConnectionPoolSettings.MaxPoolSize];
                    _amqpSasGroupedPool.Add(scope, connectionGroup);
                }
                return connectionGroup;
            }
        }

        private IAmqpConnectionHolder ResolveConnectionHolder(IAmqpConnectionHolder[] connectionGroup, DeviceIdentity deviceIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(ResolveConnectionHolder)}");
            int index = Math.Abs(deviceIdentity.GetHashCode()) % connectionGroup.Length;
            IAmqpConnectionHolder connectionHolder = connectionGroup[index];

            if (connectionHolder == null)
            {
                connectionHolder = new AmqpConnectionHolder();
                connectionGroup[index] = connectionHolder;
            }
            
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(ResolveConnectionHolder)}");
            return connectionHolder;
        }

        public IAmqpConnectionHolder AllocateAmqpConnectionHolder(DeviceIdentity deviceIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(AllocateAmqpConnectionHolder)}");

            IAmqpConnectionHolder amqpConnectionHolder;
            if (deviceIdentity.AuthenticationModel != AuthenticationModel.X509 && (deviceIdentity.AmqpTransportSettings?.AmqpConnectionPoolSettings?.Pooling ?? false))
            {
                lock (_lock)
                {
                    IAmqpConnectionHolder[] connectionGroup = ResolveConnectionGroup(deviceIdentity);
                    amqpConnectionHolder = amqpConnectionHolder = ResolveConnectionHolder(connectionGroup, deviceIdentity);
                }
            }
            else
            {
                amqpConnectionHolder = new AmqpConnectionHolder();
            }

            if (Logging.IsEnabled) Logging.Exit(deviceIdentity, amqpConnectionHolder, $"{nameof(AllocateAmqpConnectionHolder)}");
            return amqpConnectionHolder;
        }
    }
}
