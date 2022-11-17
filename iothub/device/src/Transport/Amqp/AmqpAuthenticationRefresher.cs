// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.AmqpIoT;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpAuthenticationRefresher : IAmqpAuthenticationRefresher, IDisposable
    {
        private static readonly string[] AccessRightsStringArray = AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect);
        private readonly AmqpIoTCbsLink _amqpIoTCbsLink;
        private readonly IotHubConnectionString _connectionString;
        private readonly AmqpIoTCbsTokenProvider _amqpIoTCbsTokenProvider;
        private readonly string _audience;
        private CancellationTokenSource _cancellationTokenSource;
        private TimeSpan _operationTimeout;
        private Task _refreshLoop;
        private bool _disposed;
        private CancellationTokenSource _refresherCancellationTokenSource;

        internal AmqpAuthenticationRefresher(DeviceIdentity deviceIdentity, AmqpIoTCbsLink amqpCbsLink)
        {
            _amqpIoTCbsLink = amqpCbsLink;
            _connectionString = deviceIdentity.IotHubConnectionString;
            _operationTimeout = deviceIdentity.AmqpTransportSettings.OperationTimeout;
            _audience = deviceIdentity.Audience;
            _amqpIoTCbsTokenProvider = new AmqpIoTCbsTokenProvider(_connectionString);

            if (Logging.IsEnabled)
            {
                Logging.Associate(this, deviceIdentity, $"{nameof(DeviceIdentity)}");
            }

            if (Logging.IsEnabled)
            {
                Logging.Associate(this, amqpCbsLink, $"{nameof(_amqpIoTCbsLink)}");
            }
        }

        public async Task InitLoopAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, timeout, $"{nameof(InitLoopAsync)}");
            }

            CancellationTokenSource oldTokenSource = _cancellationTokenSource;
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken newToken = _cancellationTokenSource.Token;
            oldTokenSource?.Cancel();

            DateTime refreshOn = await _amqpIoTCbsLink
                .SendTokenAsync(
                    _amqpIoTCbsTokenProvider,
                    _connectionString.AmqpEndpoint,
                    _audience,
                    _audience,
                    AccessRightsStringArray,
                    timeout)
                .ConfigureAwait(false);

            // This cancellation token source is disposed when the authentication refresher is disposed
            // or if this code block is executed more than once per instance of AmqpAuthenticationRefresher (not expected).

            if (_refresherCancellationTokenSource != null)
            {
                if (Logging.IsEnabled)
                    Logging.Info(this, "_refresherCancellationTokenSource was already initialized, whhich was unexpected. Canceling and disposing the previous instance.", nameof(InitLoopAsync));

                try
                {
                    _refresherCancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                _refresherCancellationTokenSource.Dispose();
            }
            _refresherCancellationTokenSource = new CancellationTokenSource();

            if (refreshOn < DateTime.MaxValue)
            {
                StartLoop(refreshOn, _refresherCancellationTokenSource.Token);
            }

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, timeout, $"{nameof(InitLoopAsync)}");
            }
        }

        public void StartLoop(DateTime refreshOn, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, refreshOn, $"{nameof(StartLoop)}");
            }

            // This task runs in the background and is unmonitored.
            // When this refresher is disposed it signals this task to be cancelled.
            _refreshLoop = RefreshLoopAsync(refreshOn, cancellationToken);
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, refreshOn, $"{nameof(StartLoop)}");
            }
        }

        private async Task RefreshLoopAsync(DateTime refreshesOn, CancellationToken cancellationToken)
        {
            TimeSpan waitTime = refreshesOn - DateTime.UtcNow;
            Debug.Assert(_connectionString.TokenRefresher != null);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Logging.IsEnabled)
                {
                    Logging.Info(this, refreshesOn, $"{_amqpIoTCbsTokenProvider} before {nameof(RefreshLoopAsync)}");
                }

                if (waitTime > TimeSpan.Zero)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, refreshesOn, $"{_amqpIoTCbsTokenProvider} waiting {waitTime} {nameof(RefreshLoopAsync)}.");

                    await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        refreshesOn = await _amqpIoTCbsLink
                            .SendTokenAsync(
                                _amqpIoTCbsTokenProvider,
                                _connectionString.AmqpEndpoint,
                                _audience,
                                _audience,
                                AccessRightsStringArray,
                                _operationTimeout)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is IotHubCommunicationException || ex is OperationCanceledException)
                    {
                        // In case the token refresh is not successful either due to a communication exception or cancellation token cancellation
                        // then log the exception and continue.
                        // This task runs on an unmonitored thread so there is no point throwing these exceptions.
                        if (Logging.IsEnabled)
                        {
                            Logging.Error(this, refreshesOn, $"{_amqpIoTCbsTokenProvider} refresh token failed {ex}");
                        }
                    }
                    finally
                    {
                        if (Logging.IsEnabled)
                        {
                            Logging.Info(this, refreshesOn, $"{_amqpIoTCbsTokenProvider} after {nameof(RefreshLoopAsync)}");
                        }
                    }

                    waitTime = refreshesOn - DateTime.UtcNow;
                }
            }
        }

        public void StopLoop()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(StopLoop));

            try
            {
                _refresherCancellationTokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, "The cancellation token source has already been canceled and disposed", nameof(StopLoop));
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(StopLoop));
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
                StopLoop();
                _refresherCancellationTokenSource?.Dispose();
                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }
    }
}
