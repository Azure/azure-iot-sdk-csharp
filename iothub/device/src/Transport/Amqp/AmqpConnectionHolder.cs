﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpConnectionHolder : IAmqpConnectionHolder, IAmqpUnitManager
    {
        private readonly DeviceIdentity _deviceIdentity;
        private readonly AmqpIotConnector _amqpIotConnector;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly HashSet<AmqpUnit> _amqpUnits = new HashSet<AmqpUnit>();
        private readonly object _unitsLock = new object();
        private AmqpIotConnection _amqpIotConnection;
        private IAmqpAuthenticationRefresher _amqpAuthenticationRefresher;
        private volatile bool _disposed;

        public AmqpConnectionHolder(DeviceIdentity deviceIdentity)
        {
            _deviceIdentity = deviceIdentity;
            _amqpIotConnector = new AmqpIotConnector(deviceIdentity.AmqpTransportSettings, deviceIdentity.IotHubConnectionString.HostName);
            if (Logging.IsEnabled)
            {
                Logging.Associate(this, _deviceIdentity, nameof(_deviceIdentity));
            }
        }

        public AmqpUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity,
            Func<MethodRequestInternal, Task> onMethodCallback,
            Action<Twin, string, TwinCollection, IotHubException> twinMessageListener,
            Func<string, Message, Task> onModuleMessageReceivedCallback,
            Func<Message, Task> onDeviceMessageReceivedCallback,
            Action onUnitDisconnected)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, deviceIdentity, nameof(CreateAmqpUnit));
            }

            var amqpUnit = new AmqpUnit(
                deviceIdentity,
                this,
                onMethodCallback,
                twinMessageListener,
                onModuleMessageReceivedCallback,
                onDeviceMessageReceivedCallback,
                onUnitDisconnected);
            lock (_unitsLock)
            {
                _amqpUnits.Add(amqpUnit);
            }
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, deviceIdentity, nameof(CreateAmqpUnit));
            }

            return amqpUnit;
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, o, nameof(OnConnectionClosed));
            }

            if (_amqpIotConnection != null && ReferenceEquals(_amqpIotConnection, o))
            {
                _amqpAuthenticationRefresher?.StopLoop();
                HashSet<AmqpUnit> amqpUnits;
                lock (_unitsLock)
                {
                    amqpUnits = new HashSet<AmqpUnit>(_amqpUnits);
                }
                foreach (AmqpUnit unit in amqpUnits)
                {
                    unit.OnConnectionDisconnected();
                }
            }
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, o, nameof(OnConnectionClosed));
            }
        }

        public void Shutdown()
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, _amqpIotConnection, nameof(Shutdown));
            }

            _amqpAuthenticationRefresher?.StopLoop();
            _amqpIotConnection?.SafeClose();
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, _amqpIotConnection, nameof(Shutdown));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (Logging.IsEnabled)
            {
                Logging.Info(this, disposing, nameof(Dispose));
            }

            if (disposing)
            {
                _amqpIotConnection?.SafeClose();
                _lock?.Dispose();
                _amqpIotConnector?.Dispose();
                lock (_unitsLock)
                {
                    _amqpUnits.Clear();
                }
                _amqpAuthenticationRefresher?.Dispose();
            }

            _disposed = true;
        }

        public async Task<IAmqpAuthenticationRefresher> CreateRefresherAsync(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, deviceIdentity, timeout, nameof(CreateRefresherAsync));
            }

            AmqpIotConnection amqpIotConnection = await EnsureConnectionAsync(timeout).ConfigureAwait(false);
            IAmqpAuthenticationRefresher amqpAuthenticator = await amqpIotConnection
                .CreateRefresherAsync(deviceIdentity, timeout)
                .ConfigureAwait(false);
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, deviceIdentity, timeout, nameof(CreateRefresherAsync));
            }

            return amqpAuthenticator;
        }

        public async Task<AmqpIotSession> OpenSessionAsync(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, deviceIdentity, timeout, nameof(OpenSessionAsync));
            }

            AmqpIotConnection amqpIotConnection = await EnsureConnectionAsync(timeout).ConfigureAwait(false);
            AmqpIotSession amqpIotSession = await amqpIotConnection.OpenSessionAsync(timeout).ConfigureAwait(false);
            if (Logging.IsEnabled)
            {
                Logging.Associate(amqpIotConnection, amqpIotSession, nameof(OpenSessionAsync));
            }

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, deviceIdentity, timeout, nameof(OpenSessionAsync));
            }

            return amqpIotSession;
        }

        public async Task<AmqpIotConnection> EnsureConnectionAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, timeout, nameof(EnsureConnectionAsync));
            }

            AmqpIotConnection amqpIotConnection = null;
            IAmqpAuthenticationRefresher amqpAuthenticationRefresher = null;
            bool gain = await _lock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }
            try
            {
                if (_amqpIotConnection == null || _amqpIotConnection.IsClosing())
                {
                    if (Logging.IsEnabled)
                    {
                        Logging.Info(this, "Creating new AmqpConnection", nameof(EnsureConnectionAsync));
                    }
                    // Create AmqpConnection
                    amqpIotConnection = await _amqpIotConnector.OpenConnectionAsync(timeout).ConfigureAwait(false);

                    if (_deviceIdentity.AuthenticationModel == AuthenticationModel.SasGrouped)
                    {
                        if (Logging.IsEnabled)
                        {
                            Logging.Info(this, "Creating connection wide AmqpAuthenticationRefresher", nameof(EnsureConnectionAsync));
                        }

                        amqpAuthenticationRefresher = new AmqpAuthenticationRefresher(_deviceIdentity, amqpIotConnection.GetCbsLink());
                        await amqpAuthenticationRefresher.InitLoopAsync(timeout).ConfigureAwait(false);
                    }

                    _amqpIotConnection = amqpIotConnection;
                    _amqpAuthenticationRefresher = amqpAuthenticationRefresher;
                    _amqpIotConnection.Closed += OnConnectionClosed;
                    if (Logging.IsEnabled)
                    {
                        Logging.Associate(this, _amqpIotConnection, nameof(_amqpIotConnection));
                    }
                }
                else
                {
                    amqpIotConnection = _amqpIotConnection;
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                amqpAuthenticationRefresher?.StopLoop();
                amqpIotConnection?.SafeClose();
                throw;
            }
            finally
            {
                _lock.Release();
            }
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, timeout, nameof(EnsureConnectionAsync));
            }

            return amqpIotConnection;
        }

        public void RemoveAmqpUnit(AmqpUnit amqpUnit)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, amqpUnit, nameof(RemoveAmqpUnit));
            }

            lock (_unitsLock)
            {
                _amqpUnits.Remove(amqpUnit);
                if (_amqpUnits.Count == 0)
                {
                    // TODO #887: handle gracefulDisconnect
                    Shutdown();
                }
            }
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, amqpUnit, nameof(RemoveAmqpUnit));
            }
        }

        internal DeviceIdentity GetDeviceIdentityOfAuthenticationProvider()
        {
            return _deviceIdentity;
        }
    }
}