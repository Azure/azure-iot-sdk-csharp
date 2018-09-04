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

    internal sealed class IotHubTokenRefresher
    {
        private static readonly string[] AccessRightsStringArray = 
            AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect);

        static readonly TimeSpan BufferPeriod = TimeSpan.FromSeconds(120);

        private readonly AmqpSession amqpSession;
        private readonly IotHubConnectionString connectionString;
        private readonly string audience;
        private readonly CancellationTokenSource cancellationTokenSource;
        private volatile bool taskCancelled;

        public IotHubTokenRefresher(AmqpSession amqpSession, IotHubConnectionString connectionString, string audience)
        {
            this.amqpSession = amqpSession ?? throw new ArgumentNullException("amqpSession");
            this.connectionString = connectionString;
            this.audience = audience;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public void Cancel()
        {
            if (!this.taskCancelled)
            {
                this.taskCancelled = true;
                this.cancellationTokenSource.Cancel();
            }
        }

        public async Task SendCbsTokenAsync(TimeSpan timeout)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(IotHubTokenRefresher)}.{nameof(SendCbsTokenAsync)}");

                // Send a Cbs Token right away and fork off a task to periodically renew it
                var cbsLink = this.amqpSession.Connection.Extensions.Find<AmqpCbsLink>();

                // This can throw PutToken failure in error cases
                var expiresAtUtc = await cbsLink.SendTokenAsync(
                    this.connectionString,
                    this.connectionString.AmqpEndpoint,
                    this.audience,
                    this.connectionString.AmqpEndpoint.AbsoluteUri,
                    AccessRightsStringArray,
                    timeout).ConfigureAwait(false);
                this.SendCbsTokenLoopAsync(expiresAtUtc, timeout).Fork();
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(IotHubTokenRefresher)}.{nameof(SendCbsTokenAsync)}");
            }
        }

        private async Task SendCbsTokenLoopAsync(DateTime expiryTimeUtc, TimeSpan timeout)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, expiryTimeUtc, timeout, $"{nameof(IotHubTokenRefresher)}.{nameof(SendCbsTokenLoopAsync)}");

                bool continueSendingTokens = await WaitUntilNextTokenSendTime(
                    expiryTimeUtc, 
                    this.cancellationTokenSource.Token).ConfigureAwait(false);

                if (!continueSendingTokens)
                {
                    return;
                }

                while (!this.amqpSession.IsClosing())
                {
                    if (this.taskCancelled)
                    {
                        break;
                    }

                    var cbsLink = this.amqpSession.Connection.Extensions.Find<AmqpCbsLink>();

                    if (cbsLink == null)
                    {
                        break;
                    }

                    try
                    {
                        var expiresAtUtc = await cbsLink.SendTokenAsync(
                            this.connectionString,
                            this.connectionString.AmqpEndpoint,
                            this.audience,
                            this.connectionString.AmqpEndpoint.AbsoluteUri,
                            AccessRightsStringArray,
                            timeout).ConfigureAwait(false);

                        continueSendingTokens = await WaitUntilNextTokenSendTime(
                            expiresAtUtc, 
                            this.cancellationTokenSource.Token).ConfigureAwait(false);

                        if (!continueSendingTokens)
                        {
                            break;
                        }
                    }
                    catch (AmqpException amqpException)
                    {
                        if (amqpException.Error.Condition.Equals(AmqpErrorCode.NotFound)) throw;
                    }
                    catch (Exception exception) when (!exception.IsFatal()) { }
                }
            }
            catch (Exception e) when (!e.IsFatal())
            {
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, expiryTimeUtc, timeout, $"{nameof(IotHubTokenRefresher)}.{nameof(SendCbsTokenLoopAsync)}");
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
    }
}
