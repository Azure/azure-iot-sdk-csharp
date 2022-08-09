// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpAuthenticationRefresher : IAmqpAuthenticationRefresher, IDisposable
    {
        private static readonly string[] s_accessRightsStringArray = AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect);
        private readonly Uri _amqpEndpoint;
        private readonly AmqpIotCbsLink _amqpIotCbsLink;
        private readonly IClientConfiguration _clientConfiguration;
        private readonly AmqpIotCbsTokenProvider _amqpIotCbsTokenProvider;
        private readonly string _audience;
        private Task _refreshLoop;
        private bool _disposed;

        internal AmqpAuthenticationRefresher(IClientConfiguration clientConfiguration, AmqpIotCbsLink amqpCbsLink)
        {
            _amqpIotCbsLink = amqpCbsLink;
            _clientConfiguration = clientConfiguration;
            _audience = CreateAmqpCbsAudience(_clientConfiguration);
            _amqpIotCbsTokenProvider = new AmqpIotCbsTokenProvider(_clientConfiguration);
            _amqpEndpoint = new UriBuilder(CommonConstants.AmqpsScheme, clientConfiguration.GatewayHostName, CommonConstants.DefaultAmqpSecurePort).Uri;

            if (Logging.IsEnabled)
            {
                Logging.Associate(this, clientConfiguration, nameof(clientConfiguration));
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
                    _amqpEndpoint,
                    _audience,
                    _audience,
                    s_accessRightsStringArray,
                    cancellationToken)
                .ConfigureAwait(false);

            if (refreshOn < DateTime.MaxValue)
            {
                StartLoop(refreshOn, cancellationToken);
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(InitLoopAsync));
        }

        public void StartLoop(DateTime refreshOn, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, refreshOn, nameof(StartLoop));

            _refreshLoop = RefreshLoopAsync(refreshOn, cancellationToken);

            if (Logging.IsEnabled)
                Logging.Exit(this, refreshOn, nameof(StartLoop));
        }

        private async Task RefreshLoopAsync(DateTime refreshesOn, CancellationToken cancellationToken)
        {
            TimeSpan waitTime = refreshesOn - DateTime.UtcNow;
            Debug.Assert(_clientConfiguration.TokenRefresher != null);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Logging.IsEnabled)
                    Logging.Info(this, refreshesOn, $"Before {nameof(RefreshLoopAsync)}");

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
                    catch (IotHubCommunicationException ex)
                    {
                        if (Logging.IsEnabled)
                        {
                            Logging.Error(this, refreshesOn, $"Refresh token failed {ex}");
                        }
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

        public void StopLoop()
        {
            if (Logging.IsEnabled)
                Logging.Info(this, nameof(StopLoop));
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

        private string CreateAmqpCbsAudience(IClientConfiguration clientConfiguration)
        {
            // If the shared access key name is null then this is an individual SAS authenticated client.
            // SAS tokens granted to an individual SAS authenticated client will be scoped to an individual device; for example, myHub.azure-devices.net/devices/device1.
            if (clientConfiguration.SharedAccessKeyName.IsNullOrWhiteSpace())
            {
                string clientAudience = $"{clientConfiguration.GatewayHostName}/devices/{WebUtility.UrlEncode(clientConfiguration.DeviceId)}";
                if (!clientConfiguration.ModuleId.IsNullOrWhiteSpace())
                {
                    clientAudience += $"/modules/{WebUtility.UrlEncode(clientConfiguration.ModuleId)}";
                }

                return clientAudience;
            }
            else
            {
                // If the shared access key name is not null then this is a group SAS authenticated client.
                // SAS tokens granted to a group SAS authenticated client will scoped to the IoT hub-level; for example, myHub.azure-devices.net
                return clientConfiguration.GatewayHostName;
            }
        }
    }
}
