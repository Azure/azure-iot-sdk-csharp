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
        private readonly SemaphoreSlim _lock;
        private readonly ISet<AmqpUnit> _amqpUnits;
        private AmqpIoTConnection _amqpIoTConnection;
        private IAmqpAuthenticationRefresher _amqpAuthenticationRefresher;
        private bool _disposed;

        public AmqpConnectionHolder(DeviceIdentity deviceIdentity)
        {
            _deviceIdentity = deviceIdentity;
            _amqpIoTConnector = new AmqpIoTConnector(deviceIdentity.AmqpTransportSettings, deviceIdentity.IotHubConnectionString.HostName);
            _lock = new SemaphoreSlim(1, 1);
            _amqpUnits = new HashSet<AmqpUnit>();
            if (Logging.IsEnabled) Logging.Associate(this, _deviceIdentity, $"{nameof(_deviceIdentity)}");
        }

        public AmqpUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity, 
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<Twin, string, TwinCollection> twinMessageListener, 
            Func<string, Message, Task> eventListener,
            Action onUnitDisconnected)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");

            AmqpUnit amqpUnit = new AmqpUnit(
                deviceIdentity, 
                this,
                methodHandler,
                twinMessageListener, 
                eventListener,
                onUnitDisconnected);
            _amqpUnits.Add(amqpUnit);
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(CreateAmqpUnit)}");
            return amqpUnit;
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, o, $"{nameof(OnConnectionClosed)}");
            if (_amqpIoTConnection != null && ReferenceEquals(_amqpIoTConnection, o))
            {
                _amqpAuthenticationRefresher?.StopLoop();
                foreach (AmqpUnit unit in _amqpUnits)
                {
                    unit.OnConnectionDisconnected();
                }
            }
            if (Logging.IsEnabled) Logging.Exit(this, o, $"{nameof(OnConnectionClosed)}");
        }

        private void Shutdown()
        {
            if (Logging.IsEnabled) Logging.Enter(this, _amqpIoTConnection, $"{nameof(Shutdown)}");
            _amqpAuthenticationRefresher?.StopLoop();
            _amqpIoTConnection?.SafeClose();
            if (Logging.IsEnabled) Logging.Exit(this, _amqpIoTConnection, $"{nameof(Shutdown)}");
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
                _amqpIoTConnection?.SafeClose();
                _lock?.Dispose();
                _amqpIoTConnector?.Dispose();
                _amqpUnits?.Clear();
                _amqpAuthenticationRefresher?.Dispose();
            }

            _disposed = true;
        }

        public async Task<IAmqpAuthenticationRefresher> CreateRefresher(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, timeout, $"{nameof(CreateRefresher)}");
            AmqpIoTConnection amqpIoTConnection = await EnsureConnection(timeout).ConfigureAwait(false);
            IAmqpAuthenticationRefresher amqpAuthenticator = await amqpIoTConnection.CreateRefresherAsync(deviceIdentity, timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, timeout, $"{nameof(CreateRefresher)}");
            return amqpAuthenticator;
        }

        public async Task<AmqpIoTSession> OpenSessionAsync(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, timeout, $"{nameof(OpenSessionAsync)}");
            AmqpIoTConnection amqpIoTConnection = await EnsureConnection(timeout).ConfigureAwait(false);
            AmqpIoTSession amqpIoTSession = await amqpIoTConnection.OpenSessionAsync(timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Associate(amqpIoTConnection, amqpIoTSession, $"{nameof(OpenSessionAsync)}");
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, timeout, $"{nameof(OpenSessionAsync)}");
            return amqpIoTSession;
        }

        public async Task<AmqpIoTConnection> EnsureConnection(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnsureConnection)}");
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
                    if (Logging.IsEnabled) Logging.Info(this, "Creating new AmqpConnection", $"{nameof(EnsureConnection)}");
                    // Create AmqpConnection
                    amqpIoTConnection = await _amqpIoTConnector.OpenConnectionAsync(timeout).ConfigureAwait(false);

                    if (_deviceIdentity.AuthenticationModel != AuthenticationModel.X509)
                    {
                        if (_deviceIdentity.AuthenticationModel == AuthenticationModel.SasGrouped)
                        {
                            if (Logging.IsEnabled) Logging.Info(this, "Creating connection width AmqpAuthenticationRefresher", $"{nameof(EnsureConnection)}");
                            amqpAuthenticationRefresher = new AmqpAuthenticationRefresher(_deviceIdentity, amqpIoTConnection.GetCbsLink());
                            await amqpAuthenticationRefresher.InitLoopAsync(timeout).ConfigureAwait(false);
                        }
                    }
                    _amqpIoTConnection = amqpIoTConnection;
                    _amqpAuthenticationRefresher = amqpAuthenticationRefresher;
                    _amqpIoTConnection.Closed += OnConnectionClosed;
                    if (Logging.IsEnabled) Logging.Associate(this, _amqpIoTConnection, $"{nameof(_amqpIoTConnection)}");
                }
                else
                {
                    amqpIoTConnection = _amqpIoTConnection;
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                amqpAuthenticationRefresher?.StopLoop();
                amqpIoTConnection?.SafeClose();
                throw;
            }
            finally
            {
                _lock.Release();
            }
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnsureConnection)}");
            return amqpIoTConnection;
        }

        public void RemoveAmqpUnit(AmqpUnit amqpUnit)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpUnit, $"{nameof(RemoveAmqpUnit)}");
            _amqpUnits.Remove(amqpUnit);
            if (_amqpUnits.Count == 0)
            {
                Shutdown();
            }
            if (Logging.IsEnabled) Logging.Exit(this, amqpUnit, $"{nameof(RemoveAmqpUnit)}");
        }
    }
}
