// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpAuthenticationRefresher : IAmqpAuthenticationRefresher, IDisposable
    {
        private static readonly string[] s_accessRightsStringArray = new[] { "DeviceConnect" };
        private readonly Uri _amqpEndpoint;
        private readonly AmqpIotCbsLink _amqpIotCbsLink;
        private readonly IConnectionCredentials _connectionCredentials;
        private readonly AmqpIotCbsTokenProvider _amqpIotCbsTokenProvider;
        private readonly string _audience;
        private CancellationTokenSource _refresherCancellationTokenSource;
        private Task _refreshLoop;
        private bool _disposed;

        internal AmqpAuthenticationRefresher(IConnectionCredentials connectionCredentials, AmqpIotCbsLink amqpCbsLink)
        {
            _amqpIotCbsLink = amqpCbsLink;
            _connectionCredentials = connectionCredentials;
            _audience = CreateAmqpCbsAudience(_connectionCredentials);
            _amqpIotCbsTokenProvider = new AmqpIotCbsTokenProvider(_connectionCredentials);
            _amqpEndpoint = new UriBuilder(CommonConstants.AmqpsScheme, _connectionCredentials.HostName, CommonConstants.DefaultAmqpSecurePort).Uri;

            if (Logging.IsEnabled)
            {
                Logging.Associate(this, _connectionCredentials, nameof(_connectionCredentials));
                Logging.Associate(this, amqpCbsLink, nameof(_amqpIotCbsLink));
            }
        }

        async Task IAmqpAuthenticationRefresher.InitLoopAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(IAmqpAuthenticationRefresher.InitLoopAsync));

            DateTime refreshOn = await _amqpIotCbsLink
                .SendTokenAsync(
                    _amqpIotCbsTokenProvider,
                    _amqpEndpoint,
                    _audience,
                    _audience,
                    s_accessRightsStringArray,
                    cancellationToken)
                .ConfigureAwait(false);

            // This cancellation token source is disposed when the authentication refresher is disposed
            // or if this code block is executed more than once per instance of AmqpAuthenticationRefresher (not expected).

            if (_refresherCancellationTokenSource != null)
            {
                if (Logging.IsEnabled)
                    Logging.Info(this, "_refresherCancellationTokenSource was already initialized, whhich was unexpected. Canceling and disposing the previous instance.", nameof(IAmqpAuthenticationRefresher.InitLoopAsync));

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

            // AmqpAuthenticationRefresher.StartLoop 
            // TODO
            if (refreshOn < DateTime.MaxValue
                && this is IAmqpAuthenticationRefresher refresher)
            {
                refresher.StartLoop(refreshOn, _refresherCancellationTokenSource.Token);
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(IAmqpAuthenticationRefresher.InitLoopAsync));
        }

        void IAmqpAuthenticationRefresher.StartLoop(DateTime refreshOn, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, refreshOn, nameof(IAmqpAuthenticationRefresher.StartLoop));

            // This task runs in the background and is unmonitored.
            // When this refresher is disposed it signals this task to be cancelled.
            _refreshLoop = RefreshLoopAsync(refreshOn, cancellationToken);

            if (Logging.IsEnabled)
                Logging.Exit(this, refreshOn, nameof(IAmqpAuthenticationRefresher.StartLoop));
        }

        async Task IAmqpAuthenticationRefresher.StopLoopAsync()
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, nameof(IAmqpAuthenticationRefresher.StopLoopAsync));

                try
                {
                    _refresherCancellationTokenSource?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, "The cancellation token source has already been canceled and disposed", nameof(IAmqpAuthenticationRefresher.StopLoopAsync));
                }

                // Await the completion of _refreshLoop.
                // This will ensure that when StopLoopAsync has been exited then no more token refresh attempts are in-progress.
                await _refreshLoop.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Caught exception when stopping token refresh loop: {ex}");
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(IAmqpAuthenticationRefresher.StopLoopAsync));
            }
        }

        private async Task RefreshLoopAsync(DateTime refreshesOn, CancellationToken cancellationToken)
        {
            TimeSpan waitTime = refreshesOn - DateTime.UtcNow;
            Debug.Assert(_connectionCredentials.SasTokenRefresher != null);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Logging.IsEnabled)
                    Logging.Info(this, refreshesOn, $"{_amqpIotCbsTokenProvider} before {nameof(RefreshLoopAsync)} with wait time {waitTime}.");

                if (waitTime > TimeSpan.Zero)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, refreshesOn, $"{_amqpIotCbsTokenProvider} waiting {waitTime} {nameof(RefreshLoopAsync)}.");

                    await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        refreshesOn = await _amqpIotCbsLink
                            .SendTokenAsync(
                                _amqpIotCbsTokenProvider,
                                _amqpEndpoint,
                                _audience,
                                _audience,
                                s_accessRightsStringArray,
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (IotHubClientException ex) when (ex.ErrorCode is IotHubClientErrorCode.NetworkErrors)
                    {
                        if (Logging.IsEnabled)
                            Logging.Error(this, refreshesOn, $"{_amqpIotCbsTokenProvider} refresh token failed {ex}");
                    }
                    catch (OperationCanceledException)
                    {
                        // close gracefully
                        return;
                    }
                    finally
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, refreshesOn, $"After {nameof(RefreshLoopAsync)}");
                    }

                    waitTime = refreshesOn - DateTime.UtcNow;
                }
            }
        }

        private static string CreateAmqpCbsAudience(IConnectionCredentials connectionCredentials)
        {
            // If the shared access key name is null then this is an individual SAS authenticated client.
            // SAS tokens granted to an individual SAS authenticated client will be scoped to an individual device; for example, myHub.azure-devices.net/devices/device1.
            if (connectionCredentials.SharedAccessKeyName.IsNullOrWhiteSpace())
            {
                string clientAudience = $"{connectionCredentials.HostName}/devices/{WebUtility.UrlEncode(connectionCredentials.DeviceId)}";
                if (!connectionCredentials.ModuleId.IsNullOrWhiteSpace())
                {
                    clientAudience += $"/modules/{WebUtility.UrlEncode(connectionCredentials.ModuleId)}";
                }

                return clientAudience;
            }

            // If the shared access key name is not null then this is a group SAS authenticated client.
            // SAS tokens granted to a group SAS authenticated client will scoped to the IoT hub-level; for example, myHub.azure-devices.net
            return connectionCredentials.HostName;
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
                    Logging.Enter(this, $"Disposed={_disposed}; disposing={disposing}", $"{nameof(AmqpAuthenticationRefresher)}.{nameof(Dispose)}");

                if (!_disposed)
                {
                    if (disposing)
                    {
                        _refresherCancellationTokenSource?.Dispose();
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
