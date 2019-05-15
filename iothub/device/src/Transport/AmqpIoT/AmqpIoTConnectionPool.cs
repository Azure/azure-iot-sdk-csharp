// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTConnectionPool : IAmqpIoTUnitManager
    {
        private const int MaxSpan = int.MaxValue;
        private ISet<IAmqpIoTConnectionHolder> AmqpSasIndividualPool;
        private IDictionary<string, ISet<IAmqpIoTConnectionHolder>> AmqpSasGroupedPool;
        private readonly object Lock;

        internal AmqpIoTConnectionPool()
        {
            AmqpSasIndividualPool = new HashSet<IAmqpIoTConnectionHolder>();
            AmqpSasGroupedPool = new Dictionary<string, ISet<IAmqpIoTConnectionHolder>>();
            Lock = new object();
        }

        public AmqpIoTUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity, 
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<AmqpIoTMessage> twinMessageListener, 
            Func<string, Message, Task> eventListener)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
            if (deviceIdentity.AuthenticationModel != AuthenticationModel.X509 && (deviceIdentity.AmqpTransportSettings?.AmqpConnectionPoolSettings?.Pooling??false))
            {
                IAmqpIoTConnectionHolder amqpConnectionHolder;
                lock (Lock)
                {
                    ISet<IAmqpIoTConnectionHolder> amqpConnectionHolders = ResolveConnectionGroup(deviceIdentity, true);
                    if (amqpConnectionHolders.Count < deviceIdentity.AmqpTransportSettings.AmqpConnectionPoolSettings.MaxPoolSize)
                    {
                        amqpConnectionHolder = new AmqpIoTConnectionHolder(deviceIdentity);
                        amqpConnectionHolder.OnConnectionDisconnected += (o, args) => RemoveConnection(amqpConnectionHolders, o as IAmqpIoTConnectionHolder);
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
                return new AmqpIoTConnectionHolder(deviceIdentity)
                    .CreateAmqpUnit(deviceIdentity, methodHandler, twinMessageListener, eventListener);
            }
        }

        private void RemoveConnection(ISet<IAmqpIoTConnectionHolder> amqpConnectionHolders, IAmqpIoTConnectionHolder amqpConnectionHolder)
        {
            lock (Lock)
            {
                
                bool removed = amqpConnectionHolder.GetNumberOfUnits() == 0 && amqpConnectionHolders.Remove(amqpConnectionHolder);
                if (Logging.IsEnabled) Logging.Info(this, $"Remove ConnectionHolder {amqpConnectionHolder}: {removed}");
            }
        }

        private ISet<IAmqpIoTConnectionHolder> ResolveConnectionGroup(DeviceIdentity deviceIdentity, bool create)
        {
            if (deviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
            {
                return AmqpSasIndividualPool;
            }
            else
            {
                string scope = deviceIdentity.IotHubConnectionString.SharedAccessKeyName;
                AmqpSasGroupedPool.TryGetValue(scope, out ISet<IAmqpIoTConnectionHolder>  amqpConnectionHolders);
                if (create && amqpConnectionHolders == null)
                {
                    amqpConnectionHolders = new HashSet<IAmqpIoTConnectionHolder>();
                    AmqpSasGroupedPool.Add(scope, amqpConnectionHolders);
                }
                return amqpConnectionHolders;
            }
        }
        
        private IAmqpIoTConnectionHolder GetLeastUsedConnection(ISet<IAmqpIoTConnectionHolder> amqpConnectionHolders)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(GetLeastUsedConnection)}");

            int count = MaxSpan;

            IAmqpIoTConnectionHolder amqpConnectionHolder = null;

            foreach (IAmqpIoTConnectionHolder value in amqpConnectionHolders)
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
