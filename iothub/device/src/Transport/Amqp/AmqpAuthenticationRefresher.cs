using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpAuthenticationRefresher : IAmqpAuthenticationRefresher
    {
        private readonly TimeSpan MinWaitTime = TimeSpan.FromSeconds(1);
        private readonly AmqpCbsLink AmqpCbsLink;
        private readonly IotHubConnectionString ConnectionString;
        private readonly string Audience;
        private CancellationTokenSource CancellationTokenSource;
        private TimeSpan OperationTimeout;
        private Task RefreshLoop;

        internal AmqpAuthenticationRefresher(DeviceIdentity deviceIdentity, AmqpCbsLink amqpCbsLink)
        {
            AmqpCbsLink = amqpCbsLink;
            ConnectionString = deviceIdentity.IotHubConnectionString;
            OperationTimeout = deviceIdentity.AmqpTransportSettings.OperationTimeout;
            Audience = deviceIdentity.Audience;
            if (Logging.IsEnabled) Logging.Associate(this, deviceIdentity, $"{nameof(DeviceIdentity)}");
            if (Logging.IsEnabled) Logging.Associate(this, amqpCbsLink, $"{nameof(AmqpCbsLink)}");
        }

        public async Task InitLoopAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(InitLoopAsync)}");
            CancellationTokenSource oldTokenSource = CancellationTokenSource;
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken newToken = CancellationTokenSource.Token;
            oldTokenSource?.Cancel();
            DateTime expiry = await AmqpCbsLink.SendTokenAsync(
                    ConnectionString,
                    ConnectionString.AmqpEndpoint,
                    Audience,
                    Audience,
                    AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect),
                    timeout
                ).ConfigureAwait(false);
            if (expiry < DateTime.MaxValue)
            {
                StartLoop(expiry, newToken);
            }
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(InitLoopAsync)}");
        }

        private void StartLoop(DateTime expiry, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, expiry, $"{nameof(StartLoop)}");
            RefreshLoop = RefreshLoopAsync(expiry, cancellationToken);
            if (Logging.IsEnabled) Logging.Exit(this, expiry, $"{nameof(StartLoop)}");
        }

        private async Task RefreshLoopAsync(DateTime expiry, CancellationToken cancellationToken)
        {
            TimeSpan waitTime = expiry - DateTime.UtcNow;
            Debug.Assert(ConnectionString.TokenRefresher != null);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Logging.IsEnabled) Logging.Info(this, expiry, $"Before {nameof(RefreshLoopAsync)}");

                if (waitTime < MinWaitTime)
                {
                    await Task.Delay(MinWaitTime, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        expiry = await AmqpCbsLink.SendTokenAsync(
                            ConnectionString,
                            ConnectionString.AmqpEndpoint,
                            Audience,
                            Audience,
                            AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect),
                            OperationTimeout
                        ).ConfigureAwait(false);
                    }
                    catch (AmqpException ex)
                    {
                        if (Logging.IsEnabled) Logging.Info(this, expiry, $"Refresh token failed {ex}");
                    }
                    finally
                    {
                        if (Logging.IsEnabled) Logging.Info(this, expiry, $"After {nameof(RefreshLoopAsync)}");
                    }

                    waitTime = ConnectionString.TokenRefresher.RefreshesOn - DateTime.UtcNow;
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
            if (Logging.IsEnabled) Logging.Info(this, disposing, $"{nameof(Dispose)}");
            if (disposing)
            {
                StopLoop();
                CancellationTokenSource?.Dispose();
            }
        }
    }
}
