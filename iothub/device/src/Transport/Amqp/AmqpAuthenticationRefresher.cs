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
        private Task _refreshLoop;
        private CancellationTokenSource _loopCancellationTokenSource;

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

            if (refreshOn < DateTime.MaxValue
                && this is IAmqpAuthenticationRefresher refresher)
            {
                refresher.StartLoop(refreshOn);
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(IAmqpAuthenticationRefresher.InitLoopAsync));
        }

        void IAmqpAuthenticationRefresher.StartLoop(DateTime refreshOn)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, refreshOn, nameof(IAmqpAuthenticationRefresher.StartLoop));

            if (_loopCancellationTokenSource == null
                || _refreshLoop == null)
            {
                (this as IAmqpAuthenticationRefresher)?.StopLoop();
            }

            _loopCancellationTokenSource = new CancellationTokenSource();
            _refreshLoop = RefreshLoopAsync(
                refreshOn,
                _loopCancellationTokenSource.Token);

            if (Logging.IsEnabled)
                Logging.Exit(this, refreshOn, nameof(IAmqpAuthenticationRefresher.StartLoop));
        }

        async void IAmqpAuthenticationRefresher.StopLoop()
        {
            _loopCancellationTokenSource?.Cancel();
            if (_refreshLoop != null)
            {
                try
                {
                    await _refreshLoop.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                _refreshLoop = null;
            }

            _loopCancellationTokenSource?.Dispose();
            _loopCancellationTokenSource = null;

            if (Logging.IsEnabled)
                Logging.Info(this, nameof(IAmqpAuthenticationRefresher.StopLoop));
        }

        private async Task RefreshLoopAsync(DateTime refreshesOn, CancellationToken cancellationToken)
        {
            TimeSpan waitTime = refreshesOn - DateTime.UtcNow;
            Debug.Assert(_connectionCredentials.SasTokenRefresher != null);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Logging.IsEnabled)
                    Logging.Info(this, refreshesOn, $"Before {nameof(RefreshLoopAsync)} with wait time {waitTime}.");

                if (waitTime > TimeSpan.Zero)
                {
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
                            Logging.Error(this, refreshesOn, $"Refresh token failed {ex}");
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
            _loopCancellationTokenSource?.Dispose();
        }
    }
}
