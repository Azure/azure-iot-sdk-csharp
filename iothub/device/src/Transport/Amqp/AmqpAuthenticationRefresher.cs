using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpAuthenticationRefresher : IAmqpAuthenticationRefresher
    {
        static readonly TimeSpan BufferPeriod = TimeSpan.FromSeconds(120);
        private readonly AmqpCbsLink AmqpCbsLink;
        private readonly IotHubConnectionString ConnectionString;
        private readonly string Audience;
        private CancellationTokenSource CancellationTokenSource;

        internal AmqpAuthenticationRefresher(DeviceIdentity deviceIdentity, AmqpCbsLink amqpCbsLink)
        {
            AmqpCbsLink = amqpCbsLink;
            ConnectionString = deviceIdentity.IotHubConnectionString;
            if (ConnectionString.SharedAccessKeyName == null)
            {
                Audience = $"{ConnectionString.AmqpEndpoint.AbsoluteUri}{ConnectionString.DeviceId}";
            }
            else
            {
                Audience = $"{ConnectionString.AmqpEndpoint.AbsoluteUri}";
            }
            if (Logging.IsEnabled) Logging.Associate(this, deviceIdentity, $"{nameof(DeviceIdentity)}");
            if (Logging.IsEnabled) Logging.Associate(this, amqpCbsLink, $"{nameof(AmqpCbsLink)}");
        }

        public async Task InitLoopAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(InitLoopAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            CancellationTokenSource oldTokenSource = CancellationTokenSource;
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken newToken = CancellationTokenSource.Token;
            oldTokenSource?.Cancel();
            DateTime expiry = await AmqpCbsLink.SendTokenAsync(
                    ConnectionString,
                    ConnectionString.AmqpEndpoint,
                    Audience,
                    ConnectionString.AmqpEndpoint.AbsoluteUri,
                    AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect),
                    timeoutHelper.RemainingTime()
                ).ConfigureAwait(false);
            if (expiry < DateTime.UtcNow)
            {
                throw new DeviceDisabledException();
            }
            StartLoop(expiry, newToken);
            if (Logging.IsEnabled) Logging.Exit(this, timeoutHelper.RemainingTime(), $"{nameof(InitLoopAsync)}");
        }

        private void StartLoop(DateTime expiry, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, expiry, $"{nameof(StartLoop)}");
            RefreshLoopAsync(expiry, cancellationToken).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, expiry, $"{nameof(StartLoop)}");
        }

        private async Task RefreshLoopAsync(DateTime expiry, CancellationToken cancellationToken)
        {
            TimeSpan waitTime = expiry - DateTime.UtcNow;
            while (expiry < DateTime.MaxValue && !cancellationToken.IsCancellationRequested)
            {
                if (Logging.IsEnabled) Logging.Info(this, expiry, $"Before {nameof(RefreshLoopAsync)}");
                if (waitTime.Milliseconds > 0)
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
                            ConnectionString.AmqpEndpoint.AbsoluteUri,
                            AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect),
                            BufferPeriod
                        ).ConfigureAwait(false);
                    }
                    catch (AmqpException amqpException)
                    {
                        if (amqpException.Error.Condition.Equals(AmqpErrorCode.NotFound))
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        if (Logging.IsEnabled) Logging.Info(this, expiry, $"After {nameof(RefreshLoopAsync)}");
                    }
                    if (expiry < DateTime.UtcNow)
                    {
                        throw new DeviceDisabledException();
                    }
                    else
                    {
                        waitTime = expiry - DateTime.UtcNow - BufferPeriod;
                        if (waitTime < BufferPeriod)
                        {
                            waitTime = BufferPeriod;
                        }
                    }
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
