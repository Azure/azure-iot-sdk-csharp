// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Amqp;

    sealed class IotHubTokenRefresher
    {
        static readonly TimeSpan RefreshTokenBuffer = TimeSpan.FromMinutes(2);
        static readonly TimeSpan RefreshTokenRetryInterval = TimeSpan.FromSeconds(30);
        static readonly string[] AccessRightsStringArray = AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect);

        readonly AmqpSession amqpSession;
        readonly IotHubConnectionString connectionString;
        readonly string audience;
        readonly CancellationTokenSource cancellationTokenSource;
        volatile bool taskCancelled;

        public IotHubTokenRefresher(AmqpSession amqpSession, IotHubConnectionString connectionString, string audience)
        {
            if (amqpSession == null)
            {
                throw new ArgumentNullException("amqpSession");
            }

            this.amqpSession = amqpSession;
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
            // Send a Cbs Token right away and fork off a task to periodically renew it
            var cbsLink = this.amqpSession.Connection.Extensions.Find<AmqpCbsLink>();

            // This can throw PutToken failure in error cases
            var expiresAtUtc = await cbsLink.SendTokenAsync(
                this.connectionString,
                this.connectionString.AmqpEndpoint,
                this.audience,
                this.connectionString.AmqpEndpoint.AbsoluteUri,
                AccessRightsStringArray,
                timeout);
            this.SendCbsTokenLoopAsync(expiresAtUtc, timeout).Fork();
        }

        async Task SendCbsTokenLoopAsync(DateTime expiryTimeUtc, TimeSpan timeout)
        {
            try
            {
                bool continueSendingTokens = await WaitUntilNextTokenSendTime(expiryTimeUtc, this.cancellationTokenSource.Token);

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
                    if (cbsLink != null)
                    {
                        try
                        {
                            var expiresAtUtc = await cbsLink.SendTokenAsync(
                                this.connectionString,
                                this.connectionString.AmqpEndpoint,
                                this.audience,
                                this.connectionString.AmqpEndpoint.AbsoluteUri,
                                AccessRightsStringArray,
                                timeout);

                            continueSendingTokens = await WaitUntilNextTokenSendTime(expiresAtUtc, this.cancellationTokenSource.Token);
                            if (!continueSendingTokens)
                            {
                                break;
                            }
                        }
                        catch (Exception exception) when (!exception.IsFatal())
                        {
                            var amqpException = exception as AmqpException;
                            if (amqpException != null && amqpException.Error.Condition.Equals(AmqpErrorCode.NotFound))
                            {
                                // no point in continuing CBS token renewal.
                                throw;
                            }

                            await Task.Delay(RefreshTokenRetryInterval, this.cancellationTokenSource.Token);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception e) when (!e.IsFatal())
            {
                // ignore exceptions
            }
        }

        static async Task<bool> WaitUntilNextTokenSendTime(DateTime expiresAtUtc, CancellationToken cancellationToken)
        {
            var waitTime = ComputeTokenRefreshWaitTime(expiresAtUtc);

            if (waitTime == TimeSpan.MaxValue || waitTime == TimeSpan.Zero)
            {
                return false;
            }

            await Task.Delay(waitTime, cancellationToken);
            return true;
        }

        static TimeSpan ComputeTokenRefreshWaitTime(DateTime expiresAtUtc)
        {
            return expiresAtUtc == DateTime.MaxValue ? TimeSpan.MaxValue : expiresAtUtc.Subtract(RefreshTokenBuffer).Subtract(DateTime.UtcNow);
        }
    }
}
