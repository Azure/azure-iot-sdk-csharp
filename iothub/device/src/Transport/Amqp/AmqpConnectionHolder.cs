// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    class AmqpResource : IDisposable
    {
        private readonly Action<AmqpResource> _onDiconnected;
        private readonly AmqpConnection _amqpConnection;
        private readonly AmqpCbsLink _amqpCbsLink;
        private readonly IAmqpAuthenticationRefresher _amqpAuthenticationRefresher;
        private readonly IAmqpConnector _amqpConnector;
        private bool _disposed;

        AmqpResource(AmqpConnection amqpConnection, AmqpCbsLink amqpCbsLink, IAmqpAuthenticationRefresher amqpAuthenticationRefresher, Action<AmqpResource> onDiconnected)
        {
            _amqpConnection = amqpConnection;
            _amqpCbsLink = amqpCbsLink;
            _amqpAuthenticationRefresher = amqpAuthenticationRefresher;
            _onDiconnected = onDiconnected;
            _amqpConnection.Closed += OnDisconnected;
        }

        internal static async Task<AmqpResource> AllocateAsync(DeviceIdentity deviceIdentity, Action<AmqpResource> diconnectionNotification, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(typeof(AmqpResource), deviceIdentity, timeout, $"{nameof(AllocateAsync)}");
            IAmqpConnector amqpConnector = null;
            AmqpConnection amqpConnection = null;
            IAmqpAuthenticationRefresher amqpAuthenticationRefresher = null;
            try
            {
                amqpConnector = new AmqpConnector(deviceIdentity.AmqpTransportSettings, deviceIdentity.IotHubConnectionString.HostName);
                amqpConnection = await amqpConnector.OpenConnectionAsync(timeout).ConfigureAwait(false);
                AmqpCbsLink amqpCbsLink = new AmqpCbsLink(amqpConnection);
                if (deviceIdentity.AuthenticationModel != AuthenticationModel.X509 && deviceIdentity.AuthenticationModel == AuthenticationModel.SasGrouped)
                {
                    amqpAuthenticationRefresher = await StartAmqpAuthenticationRefresherAsync(deviceIdentity, amqpCbsLink, timeout).ConfigureAwait(false);
                }
                AmqpResource amqpResource = new AmqpResource(amqpConnection, amqpCbsLink, amqpAuthenticationRefresher, diconnectionNotification);
                if (Logging.IsEnabled) Logging.Associate(amqpResource, amqpConnection, $"{nameof(AllocateAsync)}");
                if (Logging.IsEnabled) Logging.Associate(amqpResource, amqpCbsLink, $"{nameof(AllocateAsync)}");
                if (Logging.IsEnabled)Logging.Associate(amqpResource, amqpAuthenticationRefresher, $"{nameof(AllocateAsync)}");
                if (Logging.IsEnabled) Logging.Exit(typeof(AmqpResource), timeout, $"{nameof(AllocateAsync)}");
                return amqpResource;
            }
            catch (Exception ex) when(!ex.IsFatal())
            {
                amqpConnector?.Dispose();
                amqpAuthenticationRefresher?.StopLoop();
                if (amqpConnection?.IsClosing() ?? false && ex is InvalidOperationException)
                {
                    throw new IotHubCommunicationException("Amqp connection is closed.");
                }
                AmqpHelper.AbortAmqpObject(amqpConnection);
                throw;
            }
        }

        internal static async Task<IAmqpAuthenticationRefresher> StartAmqpAuthenticationRefresherAsync(DeviceIdentity deviceIdentity, AmqpCbsLink amqpCbsLink, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(typeof(AmqpResource), deviceIdentity, timeout, $"{nameof(StartAmqpAuthenticationRefresherAsync)}");
            AmqpAuthenticationRefresher amqpAuthenticationRefresher = new AmqpAuthenticationRefresher(deviceIdentity, amqpCbsLink);
            await amqpAuthenticationRefresher.InitLoopAsync(timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(typeof(AmqpResource), deviceIdentity, timeout, $"{nameof(StartAmqpAuthenticationRefresherAsync)}");
            return amqpAuthenticationRefresher;
        }

        internal async Task<IAmqpAuthenticationRefresher> StartAmqpAuthenticationRefresherAsync(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, timeout, $"{nameof(StartAmqpAuthenticationRefresherAsync)}");
            try
            {
                IAmqpAuthenticationRefresher amqpAuthenticationRefresher = await StartAmqpAuthenticationRefresherAsync(deviceIdentity, _amqpCbsLink, timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Associate(deviceIdentity, amqpAuthenticationRefresher, $"{nameof(StartAmqpAuthenticationRefresherAsync)}");
                if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, timeout, $"{nameof(StartAmqpAuthenticationRefresherAsync)}");
                return amqpAuthenticationRefresher;

            }
            catch(Exception ex) when(!ex.IsFatal())
            {
                if (_amqpConnection.IsClosing())
                {
                    throw new IotHubCommunicationException("Amqp connection is closed."); 
                }
                throw;
            }
        }

        public async Task<AmqpSession> CreateAmqpSessionAsync(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, timeout, $"{nameof(CreateAmqpSessionAsync)}");
            try
            {
                AmqpSession amqpSession = new AmqpSession(
                    _amqpConnection, 
                    new AmqpSessionSettings()
                     {
                         Properties = new Fields()
                     }, 
                    AmqpLinkFactory.GetInstance()
                );
                _amqpConnection.AddSession(amqpSession, new ushort?());
                await amqpSession.OpenAsync(timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Associate(this, amqpSession, $"{nameof(CreateAmqpSessionAsync)}");
                if (Logging.IsEnabled) Logging.Associate(deviceIdentity, amqpSession, $"{nameof(CreateAmqpSessionAsync)}");
                if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, timeout, $"{nameof(CreateAmqpSessionAsync)}");
                return amqpSession;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                if (_amqpConnection.IsClosing() && ex is InvalidOperationException)
                {
                    // AMQP will throw InvalidOperationException if amqpConnection is closed anyhow, retry in this case.
                    throw new IotHubCommunicationException("Amqp connection is closed.");
                }
                else
                {
                    throw;
                }
            }
        }

        internal bool IsInvalid()
        {
            return _amqpConnection?.IsClosing() ?? true;
        }

        internal bool IsValid()
        {
            return !IsInvalid();
        }

        internal bool IsDisposed()
        {
            return _disposed;
        }

        private void OnDisconnected(object o, EventArgs args)
        {
            _amqpAuthenticationRefresher?.StopLoop();
            _onDiconnected?.Invoke(this);
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
                _amqpConnector?.Dispose();
                _amqpAuthenticationRefresher?.Dispose();
                _amqpConnection?.Abort();
            }

            _disposed = true;
            
        }
    }

    internal class AmqpConnectionHolder : AbstractStatusWatcher, IAmqpConnectionHolder
    {
        private readonly SemaphoreSlim _connectionLock;

        private AmqpResource _amqpResource;

        public AmqpConnectionHolder() : base()
        {
            _connectionLock = new SemaphoreSlim(1, 1);
        }

        private async Task<AmqpResource> EnsureAmqpResource(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {

            bool gain = await _connectionLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }

            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, timeout, $"{nameof(EnsureAmqpResource)}");
            AmqpResource amqpResource = GetAmqpResource();
            try
            {
                if (amqpResource?.IsInvalid() ?? true)
                {
                    amqpResource = await AmqpResource.AllocateAsync(deviceIdentity, OnDisconnected, timeout).ConfigureAwait(false);
                    lock (_stateLock)
                    {
                        if (_disposed)
                        {
                            // release resource
                            amqpResource?.Dispose();
                            throw new ObjectDisposedException("AmqpConnectionHolder is disposed.");
                        }
                        else
                        {
                            if (Logging.IsEnabled) Logging.Associate(deviceIdentity, _amqpResource, $"{nameof(EnsureAmqpResource)}");
                            _amqpResource = amqpResource;
                        }

                    }
                }
            }
            finally
            {
                _connectionLock.Release();
            }

            ThrowExceptionIfClosedOrDisposed();
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, timeout, $"{nameof(EnsureAmqpResource)}");
            return amqpResource;
        }

        public async Task<IAmqpAuthenticationRefresher> AllocateAuthenticationRefresher(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, timeout, $"{nameof(AllocateAuthenticationRefresher)}");
            AmqpResource amqpResource = await EnsureAmqpResource(deviceIdentity, timeout).ConfigureAwait(false);
            IAmqpAuthenticationRefresher amqpAuthenticator = await amqpResource.StartAmqpAuthenticationRefresherAsync(deviceIdentity, timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, timeout, $"{nameof(AllocateAuthenticationRefresher)}");
            return amqpAuthenticator;
        }

        private void OnDisconnected(AmqpResource amqpResource)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpResource, $"{nameof(OnDisconnected)}");
            ChangeStatus(Status.Closed);
            if (Logging.IsEnabled) Logging.Exit(this, amqpResource, $"{nameof(OnDisconnected)}");
        }

        private AmqpResource GetAmqpResource()
        {
            lock (_stateLock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("AmqpConnectionHolder is disposed.");
                }
                return _amqpResource;
            }
        }

        public async Task<IAmqpAuthenticationRefresher> StartAmqpAuthenticationRefresherAsync(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(StartAmqpAuthenticationRefresherAsync)}");
            AmqpResource amqpResource = await EnsureAmqpResource(deviceIdentity, timeout).ConfigureAwait(false);
            IAmqpAuthenticationRefresher amqpAuthenticationRefresher = await amqpResource.StartAmqpAuthenticationRefresherAsync(deviceIdentity, timeout).ConfigureAwait(false);
            ThrowExceptionIfClosedOrDisposed();
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(StartAmqpAuthenticationRefresherAsync)}");
            return amqpAuthenticationRefresher;
        }

        public async Task<AmqpSession> CreateAmqpSessionAsync(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(CreateAmqpSessionAsync)}");
            AmqpResource amqpResource = await EnsureAmqpResource(deviceIdentity, timeout).ConfigureAwait(false);
            AmqpSession amqpSession = await amqpResource.CreateAmqpSessionAsync(deviceIdentity, timeout).ConfigureAwait(false);
            ThrowExceptionIfClosedOrDisposed();
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(CreateAmqpSessionAsync)}");
            return amqpSession;
        }

        public void Close()
        {
            if (Logging.IsEnabled) Logging.Enter(this, "Close Amqp connection.", $"{nameof(Close)}");
            _connectionLock.Wait();
            try
            {
                ChangeStatus(Status.Closed);
            }
            finally
            {
                _connectionLock.Release();
                if (Logging.IsEnabled) Logging.Exit(this, "Close Amqp connection.", $"{nameof(Close)}");
            }
        }

        protected override void CleanupResource()
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(CleanupResource)}");
            _amqpResource?.Dispose();
        }

        protected override void DisposeResource()
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(DisposeResource)}");
            CleanupResource();
        }
    }
}
