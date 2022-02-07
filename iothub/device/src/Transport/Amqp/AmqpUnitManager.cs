﻿// Copyright (c) Microsoft. All rights reserved.
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
    internal class AmqpUnitManager : IAmqpUnitManager
    {
        private static readonly AmqpUnitManager s_instance = new AmqpUnitManager();

        private readonly IDictionary<string, IAmqpUnitManager> _amqpConnectionPools;
        private readonly object _connectionPoolLock = new object();

        internal AmqpUnitManager()
        {
            _amqpConnectionPools = new Dictionary<string, IAmqpUnitManager>();
        }

        internal static AmqpUnitManager GetInstance()
        {
            return s_instance;
        }

        public AmqpUnit CreateAmqpUnit(
            IDeviceIdentity deviceIdentity,
            Func<MethodRequestInternal, Task> onMethodCallback,
            Action<AmqpMessage, string, IotHubException> twinMessageListener,
            Func<string, Message, Task> onModuleMessageReceivedCallback,
            Func<Message, Task> onDeviceMessageReceivedCallback,
            Action onUnitDisconnected)
        {
            IAmqpUnitManager amqpConnectionPool = ResolveConnectionPool(deviceIdentity.IotHubConnectionString.HostName);
            return amqpConnectionPool.CreateAmqpUnit(
                deviceIdentity,
                onMethodCallback,
                twinMessageListener,
                onModuleMessageReceivedCallback,
                onDeviceMessageReceivedCallback,
                onUnitDisconnected);
        }

        public void RemoveAmqpUnit(AmqpUnit amqpUnit)
        {
            IAmqpUnitManager amqpConnectionPool = ResolveConnectionPool(amqpUnit.GetDeviceIdentity().IotHubConnectionString.HostName);
            amqpConnectionPool.RemoveAmqpUnit(amqpUnit);
            amqpUnit.Dispose();
        }

        private IAmqpUnitManager ResolveConnectionPool(string host)
        {
            lock (_connectionPoolLock)
            {
                _amqpConnectionPools.TryGetValue(host, out IAmqpUnitManager amqpConnectionPool);
                if (amqpConnectionPool == null)
                {
                    amqpConnectionPool = new AmqpConnectionPool();
                    _amqpConnectionPools.Add(host, amqpConnectionPool);
                }

                if (Logging.IsEnabled)
                {
                    Logging.Associate(this, amqpConnectionPool, $"{nameof(ResolveConnectionPool)}");
                }

                return amqpConnectionPool;
            }
        }
    }
}
