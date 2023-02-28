// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Amqp
{
    /// <summary>
    /// Handles a single CBS session (for SAS token renewal) including the inital authentication and scheduling all subsequent
    /// authentication attempts.
    /// </summary>
    internal sealed class AmqpCbsSessionHandler : IDisposable
    {
        // There is no AmqpSession object to track here because it is encapsulated by the AmqpCbsLink class.
        private readonly IotHubConnectionProperties _credential;

        private AmqpCbsLink _cbsLink;
        private readonly EventHandler _connectionLossHandler;

        private static readonly TimeSpan s_refreshTokenBuffer = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan s_refreshTokenRetryInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromMinutes(1);
        private readonly IOThreadTimerSlim _refreshTokenTimer;

        protected AmqpCbsSessionHandler() { }

        public AmqpCbsSessionHandler(IotHubConnectionProperties credential, EventHandler connectionLossHandler)
        {
            _credential = credential;
            _connectionLossHandler = connectionLossHandler;
            _refreshTokenTimer = new IOThreadTimerSlim(s => ((AmqpCbsSessionHandler)s).OnRefreshTokenAsync(), this);
        }

        /// <summary>
        /// Opens the session, then opens the CBS links, then sends the initial authentication message.
        /// Marked virtual for unit testing purposes only.
        /// </summary>
        /// <param name="connection">The connection to attach this session to.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public virtual async Task OpenAsync(AmqpConnection connection, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Opening CBS session.");

            try
            {
                // By instantiating a new AmqpCbsLink, a new session is created on the provided connection.
                // Because of that, there is no need for our code here to instantiate a new session for this
                // link to live on.
                _cbsLink = new AmqpCbsLink(connection);
                await SendCbsTokenAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Opening CBS session.");
            }
        }

        /// <summary>
        /// Closes the CBS links and then closes this session.
        /// </summary>
        public void Close()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Closing CBS session.");

            try
            {
                _refreshTokenTimer?.Cancel();
                _cbsLink?.Close();
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Closing CBS session.");
            }
        }

        /// <summary>
        /// Returns true if this session and its CBS link are open. Returns false otherwise.
        /// </summary>
        /// <returns>True if this session and its CBS link are open. False otherwise.</returns>
        public bool IsOpen()
        {
            return _cbsLink != null;
        }

        public void Dispose()
        {
            _refreshTokenTimer?.Dispose();
        }

        private async Task SendCbsTokenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, _cbsLink, cancellationToken, nameof(SendCbsTokenAsync));

            Uri amqpEndpoint = new UriBuilder(AmqpConstants.SchemeAmqps, _credential.HostName, AmqpConstants.DefaultSecurePort).Uri;

            string audience = amqpEndpoint.AbsoluteUri;
            string resource = amqpEndpoint.AbsoluteUri;
            DateTime expiresAtUtc = await _cbsLink
                .SendTokenAsync(
                    _credential,
                    amqpEndpoint,
                    audience,
                    resource,
                    _credential.AmqpAudience.ToArray(),
                    cancellationToken)
                .ConfigureAwait(false);

            ScheduleTokenRefresh(expiresAtUtc);

            if (Logging.IsEnabled)
                Logging.Exit(this, _cbsLink, cancellationToken, nameof(SendCbsTokenAsync));
        }

        private void ScheduleTokenRefresh(DateTime expiresAtUtc)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, expiresAtUtc, nameof(ScheduleTokenRefresh));

            try
            {
                if (expiresAtUtc == DateTime.MaxValue)
                {
                    return;
                }

                TimeSpan timeFromNow = expiresAtUtc.Subtract(s_refreshTokenBuffer).Subtract(DateTime.UtcNow);
                if (timeFromNow > TimeSpan.Zero)
                {
                    _refreshTokenTimer.Set(timeFromNow);
                }

                if (Logging.IsEnabled)
                    Logging.Info(this, timeFromNow, nameof(ScheduleTokenRefresh));
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, expiresAtUtc, nameof(ScheduleTokenRefresh));
            }
        }

        private async void OnRefreshTokenAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(OnRefreshTokenAsync));

            try
            {
                await SendCbsTokenAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, ex, nameof(OnRefreshTokenAsync));

                if (Fx.IsFatal(ex))
                {
                    throw;
                }

                _refreshTokenTimer.Set(s_refreshTokenRetryInterval);
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(OnRefreshTokenAsync));
        }
    }
}
