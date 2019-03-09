using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
   
    class AmqpConnectionPool : IAmqpConnectionMonitor
    {
        private const int MaxSpan = int.MaxValue;
        private static readonly TimeSpan TimeWait = TimeSpan.FromSeconds(10);
        private static AmqpConnectionPool Instance = new AmqpConnectionPool();
        private ISet<IAmqpConnectionHolder> AmqpSasConnectionHolders;
        private IDictionary<string, ISet<IAmqpConnectionHolder>> AmqpSasGroupConnectionHolders;
        private readonly object Lock;

        internal AmqpConnectionPool()
        {
            AmqpSasConnectionHolders = new HashSet<IAmqpConnectionHolder>();
            AmqpSasGroupConnectionHolders = new Dictionary<string, ISet<IAmqpConnectionHolder>>();
            Lock = new object();
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpConnectionPool)}");
        }

        internal static AmqpConnectionPool GetInstance()
        {
            return Instance;
        }

        internal IAmqpDevice CreateAmqpDevice(
            DeviceIdentity deviceIdentity, 
            Action onAmqpDeviceDisconnected, 
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<AmqpMessage> twinMessageListener, 
            Func<string, Message, Task> eventListener)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(CreateAmqpDevice)}");
            if (deviceIdentity.AuthenticationModel != AuthenticationModel.X509 && (deviceIdentity.AmqpTransportSettings?.AmqpConnectionPoolSettings?.Pooling??false))
            {
                lock (Lock)
                {
                    ISet<IAmqpConnectionHolder> amqpConnectionHolders = ResolveConnectionGroup(deviceIdentity, true);
                    IAmqpConnectionHolder amqpConnectionHolder;
                    if (amqpConnectionHolders.Count < deviceIdentity.AmqpTransportSettings.AmqpConnectionPoolSettings.MaxPoolSize)
                    {
                        amqpConnectionHolder = new AmqpConnectionHolder(deviceIdentity, OnConnectionIdle);
                        amqpConnectionHolders.Add(amqpConnectionHolder);
                    }
                    else
                    {
                        amqpConnectionHolder = GetLeastUsedConnection(amqpConnectionHolders);
                    }
                    if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(CreateAmqpDevice)}");
                    return amqpConnectionHolder.CreateAmqpDevice(deviceIdentity, onAmqpDeviceDisconnected, methodHandler, twinMessageListener, eventListener);
                }
            }
            else
            {
                if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(CreateAmqpDevice)}");
                return new AmqpConnectionHolder(deviceIdentity, OnConnectionIdle)
                    .CreateAmqpDevice(deviceIdentity, onAmqpDeviceDisconnected, methodHandler, twinMessageListener, eventListener);
            }
        }

        private ISet<IAmqpConnectionHolder> ResolveConnectionGroup(DeviceIdentity deviceIdentity, bool create)
        {
            if (deviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
            {
                return AmqpSasConnectionHolders;
            }
            else
            {
                string scope = deviceIdentity.IotHubConnectionString.SharedAccessKeyName;
                AmqpSasGroupConnectionHolders.TryGetValue(scope, out ISet<IAmqpConnectionHolder>  amqpConnectionHolders);
                if (create && amqpConnectionHolders == null)
                {
                    amqpConnectionHolders = new HashSet<IAmqpConnectionHolder>();
                    AmqpSasGroupConnectionHolders.Add(scope, amqpConnectionHolders);
                }
                return amqpConnectionHolders;
            }
        }

        public void OnConnectionIdle(IAmqpConnectionHolder amqpConnectionHolder, DeviceIdentity deviceIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpConnectionHolder, $"{nameof(OnConnectionIdle)}");
            DisposeIdleTransportAsync(amqpConnectionHolder, deviceIdentity).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, amqpConnectionHolder, $"{nameof(OnConnectionIdle)}");
        }

        private async Task DisposeIdleTransportAsync(IAmqpConnectionHolder amqpConnectionHolder, DeviceIdentity deviceIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpConnectionHolder, $"{nameof(DisposeIdleTransportAsync)}");
            ISet<IAmqpConnectionHolder> amqpConnectionHolders = ResolveConnectionGroup(deviceIdentity, false);
            // wait before cleanup to get better performace by avoiding close AMQP connection

            if (amqpConnectionHolders?.Contains(amqpConnectionHolder)??false)
            {
                await Task.Delay(TimeWait).ConfigureAwait(false);
                lock (Lock)
                {
                    if (amqpConnectionHolder.DisposeOnIdle())
                    {
                        if (Logging.IsEnabled) Logging.Info(this, amqpConnectionHolder, $"{nameof(DisposeIdleTransportAsync)}");
                        amqpConnectionHolders.Remove(amqpConnectionHolder);
                    }
                }
            }
            else
            {
                amqpConnectionHolder.DisposeOnIdle();
            }
            if (Logging.IsEnabled) Logging.Exit(this, amqpConnectionHolder, $"{nameof(DisposeIdleTransportAsync)}");
        }

        private IAmqpConnectionHolder GetLeastUsedConnection(ISet<IAmqpConnectionHolder> amqpConnectionHolders)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(GetLeastUsedConnection)}");

            int count = MaxSpan;

            IAmqpConnectionHolder amqpConnectionHolder = null;

            foreach (IAmqpConnectionHolder value in amqpConnectionHolders)
            {
                int clientCount = value.GetNumberOfDevices();
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
            return amqpConnectionHolder?? throw new QuotaExceededException("No more space for device.");
        }

    }
}
