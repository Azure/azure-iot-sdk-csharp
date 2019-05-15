// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTUnitManager : IAmqpIoTUnitManager, IDisposable
    {
        private static readonly AmqpIoTUnitManager s_instance = new AmqpIoTUnitManager();

        private IDictionary<string, IAmqpIoTUnitManager> _amqpConnectionPools;
        private readonly object _lock = new object();
        private bool _disposed;

        internal AmqpIoTUnitManager()
        {
            _amqpConnectionPools = new Dictionary<string, IAmqpIoTUnitManager>();
        }
        internal static AmqpIoTUnitManager GetInstance()
        {
            return s_instance;
        }

        public AmqpIoTUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity,
            Func<MethodRequestInternal, Task> methodHandler,
            Action<AmqpIoTMessage> twinMessageListener,
            Func<string, Message, Task> eventListener)
        {
            IAmqpIoTUnitManager amqpConnectionPool = ResolveConnectionPool(deviceIdentity.IotHubConnectionString.HostName);
            return amqpConnectionPool.CreateAmqpUnit(
                deviceIdentity,
                methodHandler,
                twinMessageListener,
                eventListener);
        }

        private IAmqpIoTUnitManager ResolveConnectionPool(string host)
        {
            lock (_lock)
            {
                _amqpConnectionPools.TryGetValue(host, out IAmqpIoTUnitManager amqpConnectionPool);
                if (amqpConnectionPool == null)
                {
                    amqpConnectionPool = new AmqpIoTConnectionPool();
                    _amqpConnectionPools.Add(host, amqpConnectionPool);
                }

                if (Logging.IsEnabled) Logging.Associate(this, amqpConnectionPool, $"{nameof(ResolveConnectionPool)}");
                return amqpConnectionPool;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (Logging.IsEnabled) Logging.Info(this, disposing, $"{nameof(Dispose)}");
                _amqpConnectionPools.Clear();
            }

            _disposed = true;
        }
    }
}
