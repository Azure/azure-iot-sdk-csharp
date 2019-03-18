using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    class AmqpUnitManager : IAmqpUnitCreator, IDisposable
    {
        private static readonly AmqpUnitManager Instance = new AmqpUnitManager();

        private IDictionary<string, IAmqpUnitCreator> AmqpConnectionPools;
        private readonly Semaphore Lock;

        internal AmqpUnitManager()
        {
            AmqpConnectionPools = new Dictionary<string, IAmqpUnitCreator>();
            Lock = new Semaphore(1, 1);
        }
        internal static AmqpUnitManager GetInstance()
        {
            return Instance;
        }

        public IAmqpUnit CreateAmqpUnit(
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
            Lock.WaitOne();
            AmqpConnectionPools.TryGetValue(host, out IAmqpUnitCreator amqpConnectionPool);
            if (amqpConnectionPool == null)
            {
                amqpConnectionPool = new AmqpConnectionPool();
                AmqpConnectionPools.Add(host, amqpConnectionPool);
            }
            Lock.Release();
            if (Logging.IsEnabled) Logging.Associate(this, amqpConnectionPool, $"{nameof(ResolveConnectionPool)}");
            return amqpConnectionPool;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Logging.IsEnabled) Logging.Info(this, disposing, $"{nameof(Dispose)}");
            if (disposing)
            {
                Lock?.Dispose();
                AmqpConnectionPools.Clear();
            }
        }
    }
}
