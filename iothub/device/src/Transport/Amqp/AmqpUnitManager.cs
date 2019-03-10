using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    class AmqpUnitManager
    {
        private static readonly AmqpUnitManager Instance = new AmqpUnitManager();

        private IDictionary<string, AmqpConnectionPool> AmqpConnectionPools;
        private readonly Semaphore Lock;

        internal AmqpUnitManager()
        {
            AmqpConnectionPools = new Dictionary<string, AmqpConnectionPool>();
            Lock = new Semaphore(1, 1);
        }
        internal static AmqpUnitManager GetInstance()
        {
            return Instance;
        }

        internal IAmqpUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity,
            Func<MethodRequestInternal, Task> methodHandler,
            Action<AmqpMessage> twinMessageListener,
            Func<string, Message, Task> eventListener)
        {
            AmqpConnectionPool amqpConnectionPool = ResolveConnectionPool(deviceIdentity.IotHubConnectionString.HostName);
            return amqpConnectionPool.CreateAmqpUnit(
                deviceIdentity,
                methodHandler,
                twinMessageListener,
                eventListener);
        }

        private AmqpConnectionPool ResolveConnectionPool(string host)
        {
            Lock.WaitOne();
            AmqpConnectionPools.TryGetValue(host, out AmqpConnectionPool amqpConnectionPool);
            if (amqpConnectionPool == null)
            {
                amqpConnectionPool = new AmqpConnectionPool();
                AmqpConnectionPools.Add(host, amqpConnectionPool);
            }
            Lock.Release();
            if (Logging.IsEnabled) Logging.Associate(this, amqpConnectionPool, $"{nameof(ResolveConnectionPool)}");
            return amqpConnectionPool;
        }
    }
}
