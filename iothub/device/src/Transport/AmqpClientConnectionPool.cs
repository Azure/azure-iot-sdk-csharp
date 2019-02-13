// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Devices.Shared;
    using System.Threading;
    using System.Threading.Tasks;

    delegate void OnClientConnectionIdle(AmqpClientConnection amqpClientConnection);

    internal class AmqpClientConnectionPool
    {
        private static readonly TimeSpan TimeWait = TimeSpan.FromSeconds(10);
        private ISet<AmqpClientConnectionMux> AmqpClientSasConnections;
        private ISet<AmqpClientConnectionMux> AmqpClientIoTHubSasConnections;
        private readonly Semaphore Lock;

        internal AmqpClientConnectionPool()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}");

            AmqpClientSasConnections = new HashSet<AmqpClientConnectionMux>();
            AmqpClientIoTHubSasConnections = new HashSet<AmqpClientConnectionMux>();
            Lock = new Semaphore(1, 1);
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionPool)}");
        }

        internal AmqpClientConnection CreateClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(CreateClientConnection)}");
            AmqpClientConnection amqpClientConnection;
            if (deviceClientEndpointIdentity is DeviceClientEndpointIdentitySasSingle)
            {
                amqpClientConnection = new AmqpClientConnectionSasSingle(deviceClientEndpointIdentity);
            }
            else if (deviceClientEndpointIdentity is DeviceClientEndpointIdentityX509)
            {
                amqpClientConnection = new AmqpClientConnectionX509(deviceClientEndpointIdentity);
            }
            else if (deviceClientEndpointIdentity is DeviceClientEndpointIdentitySasMux)
            {
                amqpClientConnection = GetOrAllocateAmqpClientConnectionMux(AmqpClientSasConnections, deviceClientEndpointIdentity, true);
            }
            else if (deviceClientEndpointIdentity is DeviceClientEndpointIdentityIoTHubSas)
            {
                amqpClientConnection = GetOrAllocateAmqpClientConnectionMux(AmqpClientIoTHubSasConnections, deviceClientEndpointIdentity, false);
            }
            else
            {
                throw new NotImplementedException();
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(CreateClientConnection)}");
            return amqpClientConnection;
        }

        internal void OnClientConnectionIdle(AmqpClientConnection amqpClientConnection)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(OnClientConnectionIdle)}");
            DisposeEmptyClientConnectionAsync(amqpClientConnection).ConfigureAwait(true);
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(OnClientConnectionIdle)}");
        }

        private AmqpClientConnectionMux GetOrAllocateAmqpClientConnectionMux(ISet<AmqpClientConnectionMux> amqpClientMuxConnections, DeviceClientEndpointIdentity deviceClientEndpointIdentity, bool useLinkBasedTokenRefresh)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(GetOrAllocateAmqpClientConnectionMux)}");

            Lock.WaitOne();
            AmqpClientConnectionMux amqpClientConnection;
            if (amqpClientMuxConnections.Count < deviceClientEndpointIdentity.amqpTransportSettings.AmqpConnectionPoolSettings.MaxPoolSize)
            {
                amqpClientConnection = new AmqpClientConnectionMux(deviceClientEndpointIdentity, OnClientConnectionIdle, useLinkBasedTokenRefresh);
                amqpClientMuxConnections.Add(amqpClientConnection);
            }
            else
            {
                amqpClientConnection = GetLeastUsedConnection(amqpClientMuxConnections);
            }
            amqpClientConnection.AppendMuxWorker(deviceClientEndpointIdentity);
            Lock.Release();
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(GetOrAllocateAmqpClientConnectionMux)}");
            return amqpClientConnection;
        }
        
        private async Task DisposeEmptyClientConnectionAsync(AmqpClientConnection amqpClientConnection)
        {
            // wait before cleanup to get better performace by avoiding close AMQP connection
            await Task.Delay(TimeWait).ConfigureAwait(false);
            Lock.WaitOne();
            AmqpClientConnectionMux amqpClientConnectionMux = amqpClientConnection as AmqpClientConnectionMux;
            if (amqpClientConnectionMux != null)
            {
                if (amqpClientConnectionMux.DisposeOnIdle())
                {
                    if (amqpClientConnectionMux.UseLinkBasedTokenRefresh)
                    {
                         AmqpClientSasConnections.Remove(amqpClientConnectionMux);
                    }
                    else
                    {
                       AmqpClientIoTHubSasConnections.Remove(amqpClientConnectionMux);
                    }
                }
            }
            Lock.Release();
        }

        private AmqpClientConnectionMux GetLeastUsedConnection(ISet<AmqpClientConnectionMux> amqpClientConnectionMuxes)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(GetLeastUsedConnection)}");
            
            int count = int.MaxValue;

            AmqpClientConnectionMux amqpClientConnection = null;

            foreach (AmqpClientConnectionMux value in amqpClientConnectionMuxes)
            {
                int clientCount = value.GetNumberOfClients();
                if (clientCount < count)
                {
                    amqpClientConnection = value;
                    count = clientCount;
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionPool)}.{nameof(GetLeastUsedConnection)}");

            return amqpClientConnection;
        }
    }
}
