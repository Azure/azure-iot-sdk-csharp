// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpAuthenticationRefresher : IAmqpAuthenticationRefresher, IDisposable
    {
        private static readonly string[] s_accessRightsStringArray = AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect);
        private readonly AmqpIotCbsLink _amqpIotCbsLink;
        private readonly IotHubConnectionString _connectionString;
        private readonly AmqpIotCbsTokenProvider _amqpIotCbsTokenProvider;
        private readonly string _audience;
        private Task _refreshLoop;
        private bool _disposed;
        private CancellationTokenSource _refresherCancellationTokenSource;

        internal AmqpAuthenticationRefresher(IDeviceIdentity deviceIdentity, AmqpIotCbsLink amqpCbsLink)
        {
            _amqpIotCbsLink = amqpCbsLink;
            _connectionString = deviceIdentity.IotHubConnectionString;
            _audience = deviceIdentity.Audience;
            _amqpIotCbsTokenProvider = new AmqpIotCbsTokenProvider(_connectionString);

            if (Logging.IsEnabled)
            {
                Logging.Associate(this, deviceIdentity, nameof(DeviceIdentity));
                Logging.Associate(this, amqpCbsLink, nameof(_amqpIotCbsLink));
            }
        }

        public async Task InitLoopAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(InitLoopAsync));

            DateTime refreshOn = await _amqpIotCbsLink
                .SendTokenAsync(
                    _amqpIotCbsTokenProvider,
                    _connectionString.AmqpEndpoint,
                    _audience,
                    _audience,
                    s_accessRightsStringArray,
                    cancellationToken)
                .ConfigureAwait(false);

            // InitLoopAsync is not re-entrant, so we can initialize a new cancellation token source for each
            // instance of AmqpAuthenticationRefresher initialized.
            // This cancellation token source is disposed when the authentication refresher is disposed.
            _refresherCancellationTokenSource = new CancellationTokenSource();

            if (refreshOn < DateTime.MaxValue)
            {
                StartLoop(refreshOn, _refresherCancellationTokenSource.Token);
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(InitLoopAsync));
        }

        public void StartLoop(DateTime refreshOn, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, refreshOn, nameof(StartLoop));

            // This task runs in the background and is unmonitored.
            // When this refresher is disposed it signals this task to be cancelled.
            _refreshLoop = RefreshLoopAsync(refreshOn, cancellationToken);

            if (Logging.IsEnabled)
                Logging.Exit(this, refreshOn, nameof(StartLoop));
        }

        private async Task RefreshLoopAsync(DateTime refreshesOn, CancellationToken cancellationToken)
        {
            TimeSpan waitTime = refreshesOn - DateTime.UtcNow;
            Debug.Assert(_connectionString.TokenRefresher != null);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Logging.IsEnabled)
                    Logging.Info(this, refreshesOn, $"{_amqpIotCbsTokenProvider} Before {nameof(RefreshLoopAsync)}");

                if (waitTime > TimeSpan.Zero)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, refreshesOn, $"{_amqpIotCbsTokenProvider} Waiting {waitTime} {nameof(RefreshLoopAsync)}.");

                    await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        refreshesOn = await _amqpIotCbsLink
                            .SendTokenAsync(
                                _amqpIotCbsTokenProvider,
                                _connectionString.AmqpEndpoint,
                                _audience,
                                _audience,
                                s_accessRightsStringArray,
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is IotHubCommunicationException || ex is OperationCanceledException)
                    {
                        // In case the token refresh is not successful either due to a communication exception or cancellation token cancellation
                        // then log the exception and continue.
                        // This task runs on an unmonitored thread so there is no point throwing these exceptions.
                        if (Logging.IsEnabled)
                        {
                            Logging.Error(this, refreshesOn, $"{_amqpIotCbsTokenProvider} Refresh token failed {ex}");
                        }
                    }
                    finally
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, refreshesOn, $"{_amqpIotCbsTokenProvider} After {nameof(RefreshLoopAsync)}");
                    }

                    waitTime = refreshesOn - DateTime.UtcNow;
                }
            }
        }

        public void StopLoop()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(StopLoop));

            _refresherCancellationTokenSource?.Cancel();

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
            try
            {
                if (Logging.IsEnabled)
                {
                    Logging.Enter(this, $"Disposed={_disposed}; disposing={disposing}", $"{nameof(AmqpAuthenticationRefresher)}.{nameof(Dispose)}");
                }

                if (!_disposed)
                {
                    if (disposing)
                    {
                        StopLoop();
                        _refresherCancellationTokenSource?.Dispose();
                        _refresherCancellationTokenSource = null;

                        _amqpIotCbsTokenProvider?.Dispose();
                    }

                    _disposed = true;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"Disposed={_disposed}; disposing={disposing}", $"{nameof(AmqpAuthenticationRefresher)}.{nameof(Dispose)}");
                }
            }
        }
    }
}
