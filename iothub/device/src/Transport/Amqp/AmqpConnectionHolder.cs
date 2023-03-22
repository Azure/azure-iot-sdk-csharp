// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal sealed class AmqpConnectionHolder : IAmqpConnectionHolder, IAmqpUnitManager
    {
        private readonly IConnectionCredentials _connectionCredentials;
        private readonly IotHubClientAmqpSettings _amqpSettings;
        private readonly AmqpIotConnector _amqpIotConnector;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly HashSet<AmqpUnit> _amqpUnits = new();
        private readonly object _unitsLock = new();
        private AmqpIotConnection _amqpIotConnection;
        private IAmqpAuthenticationRefresher _amqpAuthenticationRefresher;
        private volatile bool _disposed;

        public AmqpConnectionHolder(IConnectionCredentials connectionCredentials, IotHubClientAmqpSettings amqpSettings)
        {
            _connectionCredentials = connectionCredentials;
            _amqpSettings = amqpSettings;
            _amqpIotConnector = new AmqpIotConnector(amqpSettings, connectionCredentials.HostName);

            if (Logging.IsEnabled)
                Logging.Associate(this, _connectionCredentials, nameof(_connectionCredentials));
        }

        public AmqpUnit CreateAmqpUnit(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation additionalClientInformation,
            IotHubClientAmqpSettings amqpSettings,
            Func<DirectMethodRequest, Task> onMethodCallback,
            Func<AmqpMessage, string, IotHubClientException, Task> twinMessageListener,
            Func<IncomingMessage, Task<MessageAcknowledgement>> onMessageReceivedCallback,
            Action onUnitDisconnected)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, connectionCredentials, nameof(CreateAmqpUnit));

            var amqpUnit = new AmqpUnit(
                connectionCredentials,
                additionalClientInformation,
                amqpSettings,
                this,
                onMethodCallback,
                twinMessageListener,
                onMessageReceivedCallback,
                onUnitDisconnected);

            // HashSet<T> is not thread safe and requires synchronization for adding/removing elements.
            lock (_unitsLock)
            {
                _amqpUnits.Add(amqpUnit);
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, connectionCredentials, nameof(CreateAmqpUnit));

            return amqpUnit;
        }

        public void Shutdown()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, _amqpIotConnection, nameof(Shutdown));

            if (_amqpIotConnection != null)
            {
                _amqpIotConnection.Closed -= OnConnectionClosed;
                _amqpIotConnection.SafeClose();
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, _amqpIotConnection, nameof(Shutdown));
        }

        public void Dispose()
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"Disposed={_disposed}", $"{nameof(AmqpConnectionHolder)}.{nameof(Dispose)}");

                if (!_disposed)
                {
                    _amqpIotConnection?.SafeClose();
                    _lock?.Dispose();
                    _amqpIotConnector?.Dispose();
                    lock (_unitsLock)
                    {
                        _amqpUnits.Clear();
                    }

                    _disposed = true;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Disposed={_disposed}", $"{nameof(AmqpConnectionHolder)}.{nameof(Dispose)}");
            }
        }

        public async Task<IAmqpAuthenticationRefresher> CreateRefresherAsync(IConnectionCredentials connectionCredentials, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, connectionCredentials, nameof(CreateRefresherAsync));

            AmqpIotConnection amqpIotConnection = await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            IAmqpAuthenticationRefresher amqpAuthenticator = await amqpIotConnection.CreateRefresherAsync(connectionCredentials, cancellationToken);

            if (Logging.IsEnabled)
                Logging.Exit(this, connectionCredentials, nameof(CreateRefresherAsync));

            return amqpAuthenticator;
        }

        public async Task<AmqpIotSession> OpenSessionAsync(IConnectionCredentials connectionCredentials, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, connectionCredentials, nameof(OpenSessionAsync));

            AmqpIotConnection amqpIotConnection = await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
            AmqpIotSession amqpIotSession = await amqpIotConnection.OpenSessionAsync(cancellationToken).ConfigureAwait(false);
            if (Logging.IsEnabled)
            {
                Logging.Associate(amqpIotConnection, amqpIotSession, nameof(OpenSessionAsync));
                Logging.Exit(this, connectionCredentials, nameof(OpenSessionAsync));
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

                if (_amqpIotConnection != null && !_amqpIotConnection.IsClosing())
                {
                    amqpIotConnection = _amqpIotConnection;
                }
                else
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Creating new AmqpConnection", nameof(EnsureConnectionAsync));

                    // Create AmqpConnection
                    amqpIotConnection = await _amqpIotConnector.OpenConnectionAsync(_connectionCredentials, cancellationToken).ConfigureAwait(false);

                    // Group-SAS authenticated clients have a connection-wide token refresh logic.
                    if (_connectionCredentials.AuthenticationModel == AuthenticationModel.SasGrouped)
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, "Creating connection wide AmqpAuthenticationRefresher", nameof(EnsureConnectionAsync));

                        amqpAuthenticationRefresher = new AmqpAuthenticationRefresher(_connectionCredentials, amqpIotConnection.GetCbsLink());
                        await amqpAuthenticationRefresher.RefreshSasTokenAsync(cancellationToken).ConfigureAwait(false);
                    }

                    _amqpIotConnection = amqpIotConnection;
                    _amqpAuthenticationRefresher = amqpAuthenticationRefresher;
                    _amqpIotConnection.Closed += OnConnectionClosed;
                    if (Logging.IsEnabled)
                        Logging.Associate(this, _amqpIotConnection, nameof(_amqpIotConnection));
                }
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
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
                if (IsEmpty())
                {
                    // TODO #887: handle gracefulDisconnect
                    // Currently, when all devices got removed, AmqpConnectionHolder will terminate the TCP connection/websocket instead of graceful disconnect(CloseAsync).
                    // This is tracking work to add a mechanism to gracefully disconnect within the last device's CloseAsync task context.
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

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, o, nameof(OnConnectionClosed));

            if (_amqpIotConnection != null && ReferenceEquals(_amqpIotConnection, o))
            {
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
    }
}
