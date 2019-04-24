// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Logger;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpAuthenticationRefresher : IAmqpAuthenticationRefresher
    {
        private static readonly string[] AccessRightsStringArray = AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect);
        private readonly AmqpCbsLink AmqpCbsLink;
        private readonly IotHubConnectionString ConnectionString;
        private readonly string Audience;
        private CancellationTokenSource CancellationTokenSource;
        private TimeSpan OperationTimeout;
        private Task RefreshLoop;
        private bool _disposed;

        internal AmqpAuthenticationRefresher(DeviceIdentity deviceIdentity, AmqpCbsLink amqpCbsLink)
        {
            AmqpCbsLink = amqpCbsLink;
            ConnectionString = deviceIdentity.IotHubConnectionString;
            OperationTimeout = deviceIdentity.AmqpTransportSettings.OperationTimeout;
            Audience = deviceIdentity.Audience;
            if (Logging.IsEnabled) Logging.Associate(this, deviceIdentity, $"{nameof(DeviceIdentity)}");
            if (Logging.IsEnabled) Logging.Associate(this, amqpCbsLink, $"{nameof(AmqpCbsLink)}");

            EventCounterLogger.GetInstance().OnAmqpTokenRefresherCreated();
        }

        public async Task InitLoopAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(InitLoopAsync)}");
            CancellationTokenSource oldTokenSource = CancellationTokenSource;
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken newToken = CancellationTokenSource.Token;
            oldTokenSource?.Cancel();
            DateTime refreshOn = await AmqpCbsLink.SendTokenAsync(
                    ConnectionString,
                    ConnectionString.AmqpEndpoint,
                    Audience,
                    Audience,
                    AccessRightsStringArray,
                    timeout
                ).ConfigureAwait(false);

            EventCounterLogger.GetInstance().OnAmqpTokenRefreshed();

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
                    try
                    {
                        await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
                    }
                    catch(TaskCanceledException)
                    {
                        if (Logging.IsEnabled) Logging.Info(this, "Refresher task is cancelled.", $"{nameof(StartLoop)}");
                    }
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        refreshesOn = await AmqpCbsLink.SendTokenAsync(
                            ConnectionString,
                            ConnectionString.AmqpEndpoint,
                            Audience,
                            Audience,
		                    AccessRightsStringArray,
		                    OperationTimeout
                        ).ConfigureAwait(false);
                    }
                    catch (AmqpException ex)
                    {
                        if (Logging.IsEnabled) Logging.Info(this, refreshesOn, $"Refresh token failed {ex}");
                    }
                    finally
                    {
                        EventCounterLogger.GetInstance().OnAmqpTokenRefreshed();
                        if (Logging.IsEnabled) Logging.Info(this, refreshesOn, $"After {nameof(RefreshLoopAsync)}");
                    }

                    waitTime = refreshesOn - DateTime.UtcNow;
                }
            }

            EventCounterLogger.GetInstance().OnAmqpTokenRefresherDisposed();
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
