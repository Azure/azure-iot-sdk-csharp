// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Transport.AmqpIoT;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpConnectionHolder : IAmqpConnectionHolder, IAmqpUnitManager
    {
        private readonly DeviceIdentity _deviceIdentity;
        private readonly AmqpIoTConnector _amqpIoTConnector;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly HashSet<AmqpUnit> _amqpUnits = new HashSet<AmqpUnit>();
        private readonly object _unitsLock = new object();
        private AmqpIoTConnection _amqpIoTConnection;
        private IAmqpAuthenticationRefresher _amqpAuthenticationRefresher;
        private volatile bool _disposed;

        public AmqpConnectionHolder(DeviceIdentity deviceIdentity)
        {
            _deviceIdentity = deviceIdentity;
            _amqpIoTConnector = new AmqpIoTConnector(deviceIdentity.AmqpTransportSettings, deviceIdentity.IotHubConnectionString.HostName);
            if (Logging.IsEnabled)
            {
                Logging.Associate(this, _deviceIdentity, $"{nameof(_deviceIdentity)}");
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
                Logging.Enter(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
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
                Logging.Exit(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
            }

            return amqpUnit;
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, o, $"{nameof(OnConnectionClosed)}");
            }

            if (_amqpIoTConnection != null && ReferenceEquals(_amqpIoTConnection, o))
            {
                _ = _amqpAuthenticationRefresher?.StopLoopAsync().ConfigureAwait(false);
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
                Logging.Exit(this, o, $"{nameof(OnConnectionClosed)}");
            }
        }

        public async Task ShutdownAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, _amqpIotConnection, nameof(ShutdownAsync));

            if (_amqpAuthenticationRefresher != null)
            {
                await _amqpAuthenticationRefresher.StopLoopAsync().ConfigureAwait(false);
            }

            _amqpIotConnection?.SafeClose();

            if (Logging.IsEnabled)
                Logging.Exit(this, _amqpIotConnection, nameof(ShutdownAsync));
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
                Logging.Info(this, disposing, $"{nameof(Dispose)}");
            }

            if (disposing)
            {
                _amqpIoTConnection?.SafeClose();
                _lock?.Dispose();
                _amqpIoTConnector?.Dispose();
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
                Logging.Enter(this, deviceIdentity, timeout, $"{nameof(CreateRefresherAsync)}");
            }

            AmqpIoTConnection amqpIoTConnection = await EnsureConnectionAsync(timeout).ConfigureAwait(false);
            IAmqpAuthenticationRefresher amqpAuthenticator = await amqpIoTConnection.CreateRefresherAsync(deviceIdentity, timeout).ConfigureAwait(false);
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, deviceIdentity, timeout, $"{nameof(CreateRefresherAsync)}");
            }

            return amqpAuthenticator;
        }

        public async Task<AmqpIoTSession> OpenSessionAsync(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, deviceIdentity, timeout, $"{nameof(OpenSessionAsync)}");
            }

            AmqpIoTConnection amqpIoTConnection = await EnsureConnectionAsync(timeout).ConfigureAwait(false);
            AmqpIoTSession amqpIoTSession = await amqpIoTConnection.OpenSessionAsync(timeout).ConfigureAwait(false);
            if (Logging.IsEnabled)
            {
                Logging.Associate(amqpIoTConnection, amqpIoTSession, $"{nameof(OpenSessionAsync)}");
            }

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, deviceIdentity, timeout, $"{nameof(OpenSessionAsync)}");
            }

            return amqpIoTSession;
        }

        public async Task<AmqpIoTConnection> EnsureConnectionAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, timeout, $"{nameof(EnsureConnectionAsync)}");
            }

            AmqpIoTConnection amqpIoTConnection = null;
            IAmqpAuthenticationRefresher amqpAuthenticationRefresher = null;
            bool gain = await _lock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }
            try
            {
                if (_amqpIoTConnection == null || _amqpIoTConnection.IsClosing())
                {
                    if (Logging.IsEnabled)
                    {
                        Logging.Info(this, "Creating new AmqpConnection", $"{nameof(EnsureConnectionAsync)}");
                    }
                    // Create AmqpConnection
                    amqpIoTConnection = await _amqpIoTConnector.OpenConnectionAsync(timeout).ConfigureAwait(false);

                    if (_deviceIdentity.AuthenticationModel != AuthenticationModel.X509)
                    {
                        if (_deviceIdentity.AuthenticationModel == AuthenticationModel.SasGrouped)
                        {
                            if (Logging.IsEnabled)
                            {
                                Logging.Info(this, "Creating connection width AmqpAuthenticationRefresher", $"{nameof(EnsureConnectionAsync)}");
                            }

                            amqpAuthenticationRefresher = new AmqpAuthenticationRefresher(_deviceIdentity, amqpIoTConnection.GetCbsLink());
                            await amqpAuthenticationRefresher.InitLoopAsync(timeout).ConfigureAwait(false);
                        }
                    }
                    _amqpIoTConnection = amqpIoTConnection;
                    _amqpAuthenticationRefresher = amqpAuthenticationRefresher;
                    _amqpIoTConnection.Closed += OnConnectionClosed;
                    if (Logging.IsEnabled)
                    {
                        Logging.Associate(this, _amqpIoTConnection, $"{nameof(_amqpIoTConnection)}");
                    }
                }
                else
                {
                    amqpIoTConnection = _amqpIoTConnection;
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                if (amqpAuthenticationRefresher != null)
                {
                    await amqpAuthenticationRefresher.StopLoopAsync().ConfigureAwait(false);
                }

                amqpIotConnection?.SafeClose();
                throw;
            }
            finally
            {
                _lock.Release();
            }
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, timeout, $"{nameof(EnsureConnectionAsync)}");
            }

            return amqpIoTConnection;
        }

        public void RemoveAmqpUnit(AmqpUnit amqpUnit)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, amqpUnit, $"{nameof(RemoveAmqpUnit)}");
            }

            lock (_unitsLock)
            {
                _amqpUnits.Remove(amqpUnit);
                if (_amqpUnits.Count == 0)
                {
                    // TODO #887: handle gracefulDisconnect
                    _ = ShutdownAsync().ConfigureAwait(false);
                }
            }
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, amqpUnit, $"{nameof(RemoveAmqpUnit)}");
            }
        }
    }
}
