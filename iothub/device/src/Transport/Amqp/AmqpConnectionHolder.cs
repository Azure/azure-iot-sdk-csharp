// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpConnectionHolder : IAmqpConnectionHolder, IDisposable
    {
        public event EventHandler OnConnectionDisconnected;
        private readonly DeviceIdentity DeviceIdentity;
        private readonly IAmqpConnector Connector;
        private readonly SemaphoreSlim Lock;
        private readonly IDictionary<DeviceIdentity, AmqpUnit> AmqpUnits;
        private AmqpConnection AmqpConnection;
        private IAmqpAuthenticationRefresher AmqpAuthenticationRefresher;
        private AmqpCbsLink AmqpCbsLink;
        private bool _disposed;

        public AmqpConnectionHolder(DeviceIdentity deviceIdentity)
        {
            DeviceIdentity = deviceIdentity;
            Connector = new AmqpConnector(deviceIdentity.AmqpTransportSettings, deviceIdentity.IotHubConnectionString.HostName);
            Lock = new SemaphoreSlim(1, 1);
            AmqpUnits = new ConcurrentDictionary<DeviceIdentity, AmqpUnit>();
            if (Logging.IsEnabled) Logging.Associate(this, DeviceIdentity, $"{nameof(DeviceIdentity)}");
        }

        private async Task<IAmqpAuthenticationRefresher> AuthenticationRefresherCreator(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, timeout, $"{nameof(AuthenticationRefresherCreator)}");
            if (AmqpConnection == null)
            {
                throw new IotHubCommunicationException();
            }
            AmqpCbsLink = AmqpCbsLink ?? new AmqpCbsLink(AmqpConnection);
            
            IAmqpAuthenticationRefresher amqpAuthenticator = new AmqpAuthenticationRefresher(deviceIdentity, AmqpCbsLink);
            await amqpAuthenticator.InitLoopAsync(timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, timeout, $"{nameof(AuthenticationRefresherCreator)}");
            return amqpAuthenticator;
        }

        public AmqpUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity, 
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<AmqpMessage> twinMessageListener, 
            Func<string, Message, Task> eventListener)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
            AmqpUnit amqpUnit = new AmqpUnit(
                deviceIdentity, 
                AmqpSessionCreator, 
                AuthenticationRefresherCreator,
                methodHandler,
                twinMessageListener, 
                eventListener);
            amqpUnit.OnUnitDisconnected += (o, args) =>
            {
                bool gracefulDisconnect = (bool)o;
                RemoveDevice(deviceIdentity, gracefulDisconnect);
            };

            AmqpUnits.Remove(deviceIdentity);
            AmqpUnits.Add(deviceIdentity, amqpUnit);
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
            return amqpUnit;
        }

        private async Task<AmqpSession> AmqpSessionCreator(DeviceIdentity deviceIdentity, ILinkFactory linkFactory, AmqpSessionSettings amqpSessionSettings, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, timeout, $"{nameof(AmqpSessionCreator)}");
            AmqpConnection amqpConnection = await EnsureConnection(timeout).ConfigureAwait(false);
            AmqpSession amqpSession = new AmqpSession(amqpConnection, amqpSessionSettings, linkFactory);
            amqpConnection.AddSession(amqpSession, new ushort?());
            if (Logging.IsEnabled) Logging.Associate(amqpConnection, amqpSession, $"{nameof(AmqpSessionCreator)}");
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, timeout, $"{nameof(AmqpSessionCreator)}");
            return amqpSession;
        }

        public int GetNumberOfUnits()
        {
            int count = AmqpUnits.Count;
            if (Logging.IsEnabled) Logging.Info(this, count, $"{nameof(GetNumberOfUnits)}");
            return count;
        }
        private async Task<AmqpConnection> EnsureConnection(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnsureConnection)}");
            AmqpConnection amqpConnection = null;
            IAmqpAuthenticationRefresher amqpAuthenticationRefresher = null;
            AmqpCbsLink amqpCbsLink = null;
            bool gain = await Lock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }
            try
            {
                if (AmqpConnection == null)
                {
                    if (Logging.IsEnabled) Logging.Info(this, "Creating new AmqpConnection", $"{nameof(EnsureConnection)}");
                    // Create AmqpConnection
                    amqpConnection = await Connector.OpenConnectionAsync(timeout).ConfigureAwait(false);

                    if (DeviceIdentity.AuthenticationModel != AuthenticationModel.X509)
                    {
                        if (AmqpCbsLink == null)
                        {
                            if (Logging.IsEnabled) Logging.Info(this, "Creating new AmqpCbsLink", $"{nameof(EnsureConnection)}");
                            amqpCbsLink = new AmqpCbsLink(amqpConnection);
                        }
                        else
                        {
                            amqpCbsLink = AmqpCbsLink;
                        }

                        if (DeviceIdentity.AuthenticationModel == AuthenticationModel.SasGrouped)
                        {
                            if (Logging.IsEnabled) Logging.Info(this, "Creating connection width AmqpAuthenticationRefresher", $"{nameof(EnsureConnection)}");
                            amqpAuthenticationRefresher = new AmqpAuthenticationRefresher(DeviceIdentity, amqpCbsLink);
                            await amqpAuthenticationRefresher.InitLoopAsync(timeout).ConfigureAwait(false);
                        }
                    }
                    AmqpConnection = amqpConnection;
                    AmqpCbsLink = amqpCbsLink;
                    AmqpAuthenticationRefresher = amqpAuthenticationRefresher;
                    AmqpConnection.Closed += OnConnectionClosed;
                    if (Logging.IsEnabled) Logging.Associate(this, AmqpConnection, $"{nameof(AmqpConnection)}");
                    if (Logging.IsEnabled) Logging.Associate(this, AmqpCbsLink, $"{nameof(AmqpCbsLink)}");
                }
                else if (AmqpConnection.IsClosing())
                {
                    throw new IotHubCommunicationException();
                }
                else
                {
                    amqpConnection = AmqpConnection;
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                amqpCbsLink?.Close();
                amqpAuthenticationRefresher?.StopLoop();
                amqpConnection?.SafeClose();
                throw;
            }
            finally
            {
                Lock.Release();
            }
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnsureConnection)}");
            return amqpConnection;
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, o, $"{nameof(OnConnectionClosed)}");
            if (AmqpConnection != null && ReferenceEquals(AmqpConnection, o))
            {
                AmqpAuthenticationRefresher?.StopLoop();
                foreach (AmqpUnit unit in AmqpUnits.Values)
                {
                    unit.OnConnectionDisconnected();
                }
                AmqpUnits.Clear();
                OnConnectionDisconnected?.Invoke(this, EventArgs.Empty);
            }
            if (Logging.IsEnabled) Logging.Exit(this, o, $"{nameof(OnConnectionClosed)}");
        }

        private void RemoveDevice(DeviceIdentity deviceIdentity, bool gracefulDisconnect)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(RemoveDevice)}");
            bool removed = AmqpUnits.Remove(deviceIdentity);
            if (removed && GetNumberOfUnits() == 0)
            {
                // TODO #887: handle gracefulDisconnect
                Shutdown();
            }
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(RemoveDevice)}");
        }

        private void Shutdown()
        {
            if (Logging.IsEnabled) Logging.Enter(this, AmqpConnection, $"{nameof(Shutdown)}");
            AmqpAuthenticationRefresher?.StopLoop();
            AmqpConnection?.Abort();
            OnConnectionDisconnected?.Invoke(this, EventArgs.Empty);
            if (Logging.IsEnabled) Logging.Exit(this, AmqpConnection, $"{nameof(Shutdown)}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (Logging.IsEnabled) Logging.Info(this, disposing, $"{nameof(Dispose)}");
            if (disposing)
            {
                AmqpConnection?.Abort();
                Lock?.Dispose();
                Connector?.Dispose();
                AmqpUnits?.Clear();
                AmqpAuthenticationRefresher?.Dispose();
                OnConnectionDisconnected?.Invoke(this, EventArgs.Empty);
            }

            _disposed = true;
        }
    }
}
