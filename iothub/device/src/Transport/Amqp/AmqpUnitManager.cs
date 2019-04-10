// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpUnitManager : IAmqpUnitManager
    {
        private static readonly AmqpUnitManager s_instance = new AmqpUnitManager();
        private IDictionary<string, IAmqpConnectionManager> _amqpConnectionPools;
        private readonly object _lock = new object();

        internal AmqpUnitManager()
        {
            _amqpConnectionPools = new Dictionary<string, IAmqpConnectionManager>();
        }

        internal static AmqpUnitManager GetInstance()
        {
            return s_instance;
        }

        public IAmqpUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity,
            Func<MethodRequestInternal, Task> methodHandler,
            Action<AmqpMessage> twinMessageListener,
            Func<string, Message, Task> eventListener,
            Action<bool> onUnitDisconnected)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
            IAmqpConnectionManager amqpConnectionManager = ResolveConnectionPool(deviceIdentity.IotHubConnectionString.HostName);
            if (Logging.IsEnabled) Logging.Associate(deviceIdentity, amqpConnectionManager, $"{nameof(CreateAmqpUnit)}");
            IAmqpConnectionHolder amqpConnectionHolder = amqpConnectionManager.AllocateAmqpConnectionHolder(deviceIdentity);
            if (Logging.IsEnabled) Logging.Associate(deviceIdentity, amqpConnectionHolder, $"{nameof(CreateAmqpUnit)}");
            IAmqpSessionHolder amqpSessionHolder = new AmqpSessionHolder(deviceIdentity, amqpConnectionHolder);
            if (Logging.IsEnabled) Logging.Associate(deviceIdentity, amqpSessionHolder, $"{nameof(CreateAmqpUnit)}");
            bool isPooling = deviceIdentity?.AmqpTransportSettings?.AmqpConnectionPoolSettings?.Pooling ?? false;

            Action<bool> onDeviceDisconnected = gracefulDisconnect =>
            {
                if (Logging.IsEnabled) Logging.Enter(deviceIdentity, $"Notify connection device with pooling {isPooling} is disconnected with graceful {gracefulDisconnect}.", $"{nameof(onDeviceDisconnected)}");
                if (!isPooling && gracefulDisconnect)
                {
                    amqpConnectionHolder.Close();
                }

                if (Logging.IsEnabled) Logging.Exit(deviceIdentity, $"Notify connection device is with pooling {isPooling} disconnected with graceful {gracefulDisconnect}.", $"{nameof(onDeviceDisconnected)}");
            };

            Action<IStatusMonitor> onDeviceDisposed = statusMonitor =>
            {
                if (Logging.IsEnabled) Logging.Enter(deviceIdentity, $"Notify connection device with pooling {isPooling} is disposed.", $"{nameof(onDeviceDisposed)}");

                amqpConnectionHolder.DetachStatusMonitor(statusMonitor);
                if (!isPooling)
                {
                    amqpConnectionHolder.Dispose();
                }


                if (Logging.IsEnabled) Logging.Exit(deviceIdentity, $"Notify connection device with pooling {isPooling} is disposed.", $"{nameof(onDeviceDisposed)}");
            };

            IAmqpUnit amqpUnit = new AmqpUnit(
                deviceIdentity,
                amqpSessionHolder,
                methodHandler,
                twinMessageListener,
                eventListener,
                onDeviceDisconnected,
                onDeviceDisposed
            );

            // Connection directly report to AmqpUnit it's status to trigger pipeline to reconnect
            amqpConnectionHolder.AddStatusMonitor(amqpUnit);
            // Session directly report to AmqpUnit it's status to trigger pipeline to reconnect
            amqpSessionHolder.AddStatusMonitor(amqpUnit);
            
            if (Logging.IsEnabled) Logging.Exit(deviceIdentity, amqpUnit, $"{nameof(CreateAmqpUnit)}");
            return amqpUnit;
            
        }

        private IAmqpConnectionManager ResolveConnectionPool(string host)
        {
            lock (_lock)
            {
                _amqpConnectionPools.TryGetValue(host, out IAmqpConnectionManager amqpConnectionPool);
                if (amqpConnectionPool == null)
                {
                    amqpConnectionPool = new AmqpConnectionPool();
                    _amqpConnectionPools.Add(host, amqpConnectionPool);
                }

                if (Logging.IsEnabled) Logging.Associate(this, amqpConnectionPool, $"{nameof(ResolveConnectionPool)}");
                return amqpConnectionPool;
            }
        }

    }
}
