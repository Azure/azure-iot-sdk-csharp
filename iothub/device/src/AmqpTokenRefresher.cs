// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Amqp;
    using System.Diagnostics;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.Devices.Client.Transport;

    internal sealed class AmqpTokenRefresher : IDisposable
    {
        private static readonly string[] AccessRightsStringArray = 
            AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect);

        static readonly TimeSpan BufferPeriod = TimeSpan.FromSeconds(120);

        private readonly AmqpClientSession amqpClientSession;
        private readonly IotHubConnectionString connectionString;
        private readonly string audience;
        private readonly CancellationTokenSource cancellationTokenSource;
        private volatile bool taskCancelled;

        public AmqpTokenRefresher(AmqpClientSession amqpClientSession, IotHubConnectionString connectionString, string audience)
        {
            this.amqpClientSession = amqpClientSession ?? throw new ArgumentNullException("amqpClientSession");
            this.connectionString = connectionString;
            this.audience = audience;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        internal void Cancel()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpTokenRefresher)}.{nameof(Cancel)}");

            if (!this.taskCancelled)
            {
                this.taskCancelled = true;
                this.cancellationTokenSource.Cancel();
            }
        }

        internal async Task RefreshTokenAsync(TimeSpan timeout)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(AmqpTokenRefresher)}.{nameof(RefreshTokenAsync)}");

                // Send a Cbs Token right away and fork off a task to periodically renew it
                var expiresAtUtc = await this.amqpClientSession.AuthenticateCbs(timeout).ConfigureAwait(false);

                this.RefreshTokenLoopAsync(expiresAtUtc, timeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(AmqpTokenRefresher)}.{nameof(RefreshTokenAsync)}");
            }
        }

        private async Task RefreshTokenLoopAsync(DateTime expiryTimeUtc, TimeSpan timeout)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, expiryTimeUtc, timeout, $"{nameof(AmqpTokenRefresher)}.{nameof(RefreshTokenLoopAsync)}");

                bool continueSendingTokens = await WaitUntilNextTokenSendTime(
                    expiryTimeUtc, 
                    this.cancellationTokenSource.Token).ConfigureAwait(false);

                if (!continueSendingTokens)
                {
                    return;
                }

                while (!this.amqpClientSession.amqpSession.IsClosing())
                {
                    if (this.taskCancelled)
                    {
                        break;
                    }

                    var expiresAtUtc = await this.amqpClientSession.AuthenticateCbs(timeout).ConfigureAwait(false);

                    try
                    {
                        continueSendingTokens = await WaitUntilNextTokenSendTime(expiresAtUtc, this.cancellationTokenSource.Token).ConfigureAwait(false);
                        if (!continueSendingTokens)
                        {
                            break;
                        }
                    }
                    catch (AmqpException amqpException)
                    {
                        if (amqpException.Error.Condition.Equals(AmqpErrorCode.NotFound)) throw;
                    }
                }
            }
            catch (Exception e) when (!e.IsFatal())
            {
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, expiryTimeUtc, timeout, $"{nameof(AmqpTokenRefresher)}.{nameof(RefreshTokenLoopAsync)}");
            }
        }

        private async Task<bool> WaitUntilNextTokenSendTime(DateTime expiresAtUtc, CancellationToken cancellationToken)
        {
            if (expiresAtUtc == DateTime.MaxValue)
            {
                return false;
            }

            TimeSpan waitTime = expiresAtUtc - DateTime.UtcNow;
            if (waitTime.TotalSeconds <= 0)
            {
                return false;
            }

            waitTime = waitTime > BufferPeriod ? waitTime - BufferPeriod : TimeSpan.Zero;
            await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
        }
    }
}
