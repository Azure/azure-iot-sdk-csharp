// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpConnectionHolder : IAmqpConnectionHolder, IAmqpUnitManager
    {
        private readonly IIotHubConnectionInfo _iotHubConnectionInfo;
        private readonly AmqpIotConnector _amqpIotConnector;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly HashSet<AmqpUnit> _amqpUnits = new HashSet<AmqpUnit>();
        private readonly object _unitsLock = new object();
        private AmqpIotConnection _amqpIotConnection;
        private IAmqpAuthenticationRefresher _amqpAuthenticationRefresher;
        private volatile bool _disposed;

        public AmqpConnectionHolder(IIotHubConnectionInfo iotHubConnectionInfo)
        {
            _iotHubConnectionInfo = iotHubConnectionInfo;
            _amqpIotConnector = new AmqpIotConnector(iotHubConnectionInfo.AmqpTransportSettings, iotHubConnectionInfo.HostName);
            if (Logging.IsEnabled)
                Logging.Associate(this, _iotHubConnectionInfo, nameof(_iotHubConnectionInfo));
        }

        public AmqpUnit CreateAmqpUnit(
            IIotHubConnectionInfo iotHubConnectionInfo,
            Func<MethodRequestInternal, Task> onMethodCallback,
            Action<Twin, string, TwinCollection, IotHubException> twinMessageListener,
            Func<string, Message, Task> onModuleMessageReceivedCallback,
            Func<Message, Task> onDeviceMessageReceivedCallback,
            Action onUnitDisconnected)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, iotHubConnectionInfo, nameof(CreateAmqpUnit));

            var amqpUnit = new AmqpUnit(
                iotHubConnectionInfo,
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
                Logging.Exit(this, iotHubConnectionInfo, nameof(CreateAmqpUnit));

            return amqpUnit;
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, o, nameof(OnConnectionClosed));

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
                Logging.Exit(this, o, nameof(OnConnectionClosed));
        }

        public void Shutdown()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, _amqpIotConnection, nameof(Shutdown));

            _amqpAuthenticationRefresher?.StopLoop();
            _amqpIotConnection?.SafeClose();

            if (Logging.IsEnabled)
                Logging.Exit(this, _amqpIotConnection, nameof(Shutdown));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            try
            {
                if (Logging.IsEnabled)
                {
                    Logging.Enter(this, $"Disposed={_disposed}; disposing={disposing}", $"{nameof(AmqpConnectionHolder)}.{nameof(Dispose)}");
                }

                if (!_disposed)
                {
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
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"Disposed={_disposed}; disposing={disposing}", $"{nameof(AmqpConnectionHolder)}.{nameof(Dispose)}");
                }
            }
        }

        public async Task<IAmqpAuthenticationRefresher> CreateRefresherAsync(IIotHubConnectionInfo iotHubConnectionInfo, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, iotHubConnectionInfo, nameof(CreateRefresherAsync));

            AmqpIotConnection amqpIotConnection = await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            IAmqpAuthenticationRefresher amqpAuthenticator = await amqpIotConnection
                .CreateRefresherAsync(iotHubConnectionInfo, cancellationToken)
                .ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Exit(this, iotHubConnectionInfo, nameof(CreateRefresherAsync));

            return amqpAuthenticator;
        }

        public async Task<AmqpIotSession> OpenSessionAsync(IIotHubConnectionInfo iotHubConnectionInfo, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, iotHubConnectionInfo, nameof(OpenSessionAsync));

            AmqpIotConnection amqpIotConnection = await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            AmqpIotSession amqpIotSession = await amqpIotConnection.OpenSessionAsync(cancellationToken).ConfigureAwait(false);
            if (Logging.IsEnabled)
            {
                Logging.Associate(amqpIotConnection, amqpIotSession, nameof(OpenSessionAsync));
                Logging.Exit(this, iotHubConnectionInfo, nameof(OpenSessionAsync));
            }

            return amqpIotSession;
        }

        public async Task<AmqpIotConnection> EnsureConnectionAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(EnsureConnectionAsync));

            AmqpIotConnection amqpIotConnection = null;
            IAmqpAuthenticationRefresher amqpAuthenticationRefresher = null;
            try
            {
                await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException();
            }

            try
            {
                if (_amqpIotConnection == null || _amqpIotConnection.IsClosing())
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Creating new AmqpConnection", nameof(EnsureConnectionAsync));

                    // Create AmqpConnection
                    amqpIotConnection = await _amqpIotConnector.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

                    if (_iotHubConnectionInfo.AuthenticationModel == AuthenticationModel.SasGrouped)
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, "Creating connection wide AmqpAuthenticationRefresher", nameof(EnsureConnectionAsync));

                        amqpAuthenticationRefresher = new AmqpAuthenticationRefresher(_iotHubConnectionInfo, amqpIotConnection.GetCbsLink());
                        await amqpAuthenticationRefresher.InitLoopAsync(cancellationToken).ConfigureAwait(false);
                    }

                    _amqpIotConnection = amqpIotConnection;
                    _amqpAuthenticationRefresher = amqpAuthenticationRefresher;
                    _amqpIotConnection.Closed += OnConnectionClosed;
                    if (Logging.IsEnabled)
                        Logging.Associate(this, _amqpIotConnection, nameof(_amqpIotConnection));
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
                Logging.Exit(this, nameof(EnsureConnectionAsync));

            return amqpIotConnection;
        }

        public void RemoveAmqpUnit(AmqpUnit amqpUnit)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, amqpUnit, nameof(RemoveAmqpUnit));

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
                Logging.Exit(this, amqpUnit, nameof(RemoveAmqpUnit));
        }

        internal bool IsEmpty()
        {
            return !_amqpUnits.Any();
        }
    }
}
