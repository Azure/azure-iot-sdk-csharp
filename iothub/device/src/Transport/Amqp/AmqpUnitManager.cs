// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpUnitManager : IAmqpUnitCreator, IDisposable
    {
        private static readonly AmqpUnitManager s_instance = new AmqpUnitManager();

        private IDictionary<string, IAmqpUnitCreator> _amqpConnectionPools;
        private readonly object _lock = new object();
        private bool _disposed;

        internal AmqpUnitManager()
        {
            _amqpConnectionPools = new Dictionary<string, IAmqpUnitCreator>();
        }
        internal static AmqpUnitManager GetInstance()
        {
            return s_instance;
        }

        public AmqpUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity,
            Func<MethodRequestInternal, Task> methodHandler,
            Action<AmqpMessage> twinMessageListener,
            Func<string, Message, Task> eventListener)
        {
            IAmqpUnitCreator amqpConnectionPool = ResolveConnectionPool(deviceIdentity.IotHubConnectionString.HostName);
            return amqpConnectionPool.CreateAmqpUnit(
                deviceIdentity,
                methodHandler,
                twinMessageListener,
                eventListener);
        }

        private IAmqpUnitCreator ResolveConnectionPool(string host)
        {
            lock (_lock)
            {
                _amqpConnectionPools.TryGetValue(host, out IAmqpUnitCreator amqpConnectionPool);
                if (amqpConnectionPool == null)
                {
                    amqpConnectionPool = new AmqpConnectionPool();
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
