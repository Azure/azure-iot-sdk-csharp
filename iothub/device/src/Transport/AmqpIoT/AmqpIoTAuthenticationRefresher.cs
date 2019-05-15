// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTAuthenticationRefresher : IAmqpIoTAuthenticationRefresher, IDisposable
    {
        private static readonly string[] AccessRightsStringArray = AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect);
        private readonly AmqpIoTCbsLink AmqpIoTCbsLink;
        private readonly IotHubConnectionString ConnectionString;
        private readonly AmqpIoTCbsTokenProvider AmqpIoTCbsTokenProvider;
        private readonly string Audience;
        private CancellationTokenSource CancellationTokenSource;
        private TimeSpan OperationTimeout;
        private Task RefreshLoop;
        private bool _disposed;

        internal AmqpIoTAuthenticationRefresher(DeviceIdentity deviceIdentity, AmqpIoTCbsLink amqpCbsLink)
        {
            AmqpIoTCbsLink = amqpCbsLink;
            ConnectionString = deviceIdentity.IotHubConnectionString;
            OperationTimeout = deviceIdentity.AmqpTransportSettings.OperationTimeout;
            Audience = deviceIdentity.Audience;
            AmqpIoTCbsTokenProvider = new AmqpIoTCbsTokenProvider(ConnectionString);

            if (Logging.IsEnabled) Logging.Associate(this, deviceIdentity, $"{nameof(DeviceIdentity)}");
            if (Logging.IsEnabled) Logging.Associate(this, amqpCbsLink, $"{nameof(AmqpIoTCbsLink)}");
        }

        public async Task InitLoopAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(InitLoopAsync)}");
            CancellationTokenSource oldTokenSource = CancellationTokenSource;
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken newToken = CancellationTokenSource.Token;
            oldTokenSource?.Cancel();

            DateTime refreshOn = await AmqpIoTCbsLink.SendTokenAsync(
                    AmqpIoTCbsTokenProvider,
                    ConnectionString.AmqpEndpoint,
                    Audience,
                    Audience,
                    AccessRightsStringArray,
                    timeout
                ).ConfigureAwait(false);

            if (refreshOn < DateTime.MaxValue)
            {
                StartLoop(refreshOn, newToken);
            }

            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(InitLoopAsync)}");
        }

        private void StartLoop(DateTime refreshOn, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, refreshOn, $"{nameof(StartLoop)}");
            RefreshLoop = RefreshLoopAsync(refreshOn, cancellationToken);
            if (Logging.IsEnabled) Logging.Exit(this, refreshOn, $"{nameof(StartLoop)}");
        }

        private async Task RefreshLoopAsync(DateTime refreshesOn, CancellationToken cancellationToken)
        {
            TimeSpan waitTime = refreshesOn - DateTime.UtcNow;
            Debug.Assert(ConnectionString.TokenRefresher != null);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Logging.IsEnabled) Logging.Info(this, refreshesOn, $"Before {nameof(RefreshLoopAsync)}");

                if (waitTime > TimeSpan.Zero)
                {
                    await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        refreshesOn = await AmqpIoTCbsLink.SendTokenAsync(
                            AmqpIoTCbsTokenProvider,
                            ConnectionString.AmqpEndpoint,
                            Audience,
                            Audience,
		                    AccessRightsStringArray,
		                    OperationTimeout
                        ).ConfigureAwait(false);
                    }
                    catch (IotHubCommunicationException ex)
                    {
                        if (Logging.IsEnabled) Logging.Info(this, refreshesOn, $"Refresh token failed {ex}");
                    }
                    finally
                    {
                        if (Logging.IsEnabled) Logging.Info(this, refreshesOn, $"After {nameof(RefreshLoopAsync)}");
                    }

                    waitTime = refreshesOn - DateTime.UtcNow;
                }
            }
        }

        public void StopLoop()
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(StopLoop)}");
            CancellationTokenSource?.Cancel();
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
                StopLoop();
            }

            _disposed = true;
        }
    }
}
