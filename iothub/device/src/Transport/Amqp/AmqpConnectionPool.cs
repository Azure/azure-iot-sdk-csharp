using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
   
    class AmqpConnectionPool : IAmqpUnitCreator
    {
        private const int MaxSpan = int.MaxValue;
        private ISet<IAmqpConnectionHolder> AmqpSasIndividualPool;
        private IDictionary<string, ISet<IAmqpConnectionHolder>> AmqpSasGroupedPool;
        private readonly object Lock;

        internal AmqpConnectionPool()
        {
            AmqpSasIndividualPool = new HashSet<IAmqpConnectionHolder>();
            AmqpSasGroupedPool = new Dictionary<string, ISet<IAmqpConnectionHolder>>();
            Lock = new object();
        }

        public IAmqpUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity, 
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<AmqpMessage> twinMessageListener, 
            Func<string, Message, Task> eventListener)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
            if (deviceIdentity.AuthenticationModel != AuthenticationModel.X509 && (deviceIdentity.AmqpTransportSettings?.AmqpConnectionPoolSettings?.Pooling??false))
            {
                IAmqpConnectionHolder amqpConnectionHolder;
                lock (Lock)
                {
                    ISet<IAmqpConnectionHolder> amqpConnectionHolders = ResolveConnectionGroup(deviceIdentity, true);
                    if (amqpConnectionHolders.Count < deviceIdentity.AmqpTransportSettings.AmqpConnectionPoolSettings.MaxPoolSize)
                    {
                        amqpConnectionHolder = new AmqpConnectionHolder(deviceIdentity);
                        amqpConnectionHolder.OnConnectionDisconnected += (o, args) => {
                            bool removed = amqpConnectionHolders.Remove(o as IAmqpConnectionHolder);
                            if (Logging.IsEnabled) Logging.Info(this, $"Remove ConnectionHolder {o}: {removed}");
                        };
                        amqpConnectionHolders.Add(amqpConnectionHolder);
                        if (Logging.IsEnabled) Logging.Associate(this, amqpConnectionHolder, "amqpConnectionHolders");
                    }
                    else
                    {
                        amqpConnectionHolder = GetLeastUsedConnection(amqpConnectionHolders);
                    }
                }
                if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
                return amqpConnectionHolder.CreateAmqpUnit(deviceIdentity, methodHandler, twinMessageListener, eventListener);
            }
            else
            {
                if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
                return new AmqpConnectionHolder(deviceIdentity)
                    .CreateAmqpUnit(deviceIdentity, methodHandler, twinMessageListener, eventListener);
            }
        }

        private ISet<IAmqpConnectionHolder> ResolveConnectionGroup(DeviceIdentity deviceIdentity, bool create)
        {
            if (deviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
            {
                return AmqpSasIndividualPool;
            }
            else
            {
                string scope = deviceIdentity.IotHubConnectionString.SharedAccessKeyName;
                AmqpSasGroupedPool.TryGetValue(scope, out ISet<IAmqpConnectionHolder>  amqpConnectionHolders);
                if (create && amqpConnectionHolders == null)
                {
                    amqpConnectionHolders = new HashSet<IAmqpConnectionHolder>();
                    AmqpSasGroupedPool.Add(scope, amqpConnectionHolders);
                }
                return amqpConnectionHolders;
            }
        }
        
        private IAmqpConnectionHolder GetLeastUsedConnection(ISet<IAmqpConnectionHolder> amqpConnectionHolders)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(GetLeastUsedConnection)}");

            int count = MaxSpan;

            IAmqpConnectionHolder amqpConnectionHolder = null;

            foreach (IAmqpConnectionHolder value in amqpConnectionHolders)
            {
                int clientCount = value.GetNumberOfUnits();
                if (clientCount < count)
                {
                    amqpConnectionHolder = value;
                    count = clientCount;
                    if (count == 0)
                    {
                        break;
                    }
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(GetLeastUsedConnection)}");
            return amqpConnectionHolder;
        }

    }
}
