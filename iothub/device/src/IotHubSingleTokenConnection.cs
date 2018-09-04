// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Shared;

    sealed class IotHubSingleTokenConnection : IotHubConnection
    {
        IotHubScopeConnectionPool iotHubScopeConnectionPool;
        IotHubTokenRefresher iotHubTokenRefresher;

        public IotHubSingleTokenConnection(IotHubScopeConnectionPool iotHubScopeConnectionPool, IotHubConnectionString connectionString, AmqpTransportSettings amqpTransportSettings)
            :base(connectionString.HostName, connectionString.AmqpEndpoint.Port, amqpTransportSettings)
        {
            this.iotHubScopeConnectionPool = iotHubScopeConnectionPool;
            this.ConnectionString = connectionString;
            this.FaultTolerantSession = new FaultTolerantAmqpObject<AmqpSession>(this.CreateSessionAsync, this.CloseConnection);
        }

        public IotHubConnectionString ConnectionString { get; }

        public override Task CloseAsync()
        {
            return this.FaultTolerantSession.CloseAsync();
        }

        public override void SafeClose(Exception exception)
        {
            this.FaultTolerantSession.Close();
        }

        public override void Release(string doNotUse)
        {
            if (this.iotHubScopeConnectionPool != null)
            {
                this.iotHubScopeConnectionPool.RemoveRef();
                this.iotHubScopeConnectionPool = null;
            }
            else
            {
                this.FaultTolerantSession.Close();
            }
        }

        protected override async Task<AmqpSession> CreateSessionAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, timeout, cancellationToken, $"{nameof(IotHubSingleTokenConnection)}.{nameof(CreateSessionAsync)}");

                var timeoutHelper = new TimeoutHelper(timeout);

                this.iotHubTokenRefresher?.Cancel();

                AmqpSession amqpSession = await base.CreateSessionAsync(timeoutHelper.RemainingTime(), cancellationToken).ConfigureAwait(false);

                if (this.AmqpTransportSettings.ClientCertificate == null)
                {
                    this.iotHubTokenRefresher = new IotHubTokenRefresher(
                       amqpSession,
                       this.ConnectionString,
                       this.ConnectionString.AmqpEndpoint.AbsoluteUri
                       );

                    // Send Cbs token for new connection first
                    try
                    {
                        await this.iotHubTokenRefresher.SendCbsTokenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    }
                    catch (Exception exception) when (!exception.IsFatal())
                    {
                        amqpSession?.Connection.SafeClose();

                        throw;
                    }

                }

                return amqpSession;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, cancellationToken, $"{nameof(IotHubSingleTokenConnection)}.{nameof(CreateSessionAsync)}");
            }
        }

        protected override Uri BuildLinkAddress(IotHubConnectionString doNotUse, string path)
        {
            return this.ConnectionString.BuildLinkAddress(path);
        }

        protected override string BuildAudience(IotHubConnectionString doNotUse, string donotUse2)
        {
            return string.Empty;
        }

        protected override async Task OpenLinkAsync(AmqpObject link, IotHubConnectionString doNotUse, string doNotUse2, TimeSpan timeout, CancellationToken token)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, token, $"{nameof(IotHubSingleTokenConnection)}.{nameof(OpenLinkAsync)}");

            token.ThrowIfCancellationRequested();
            try
            {
                await link.OpenAsync(timeout).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                {
                    throw;
                }

                link.SafeClose(exception);

                throw;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, token, $"{nameof(IotHubSingleTokenConnection)}.{nameof(OpenLinkAsync)}");
            }
        }

        void CloseConnection(AmqpSession amqpSession)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, amqpSession.Identifier, $"{nameof(IotHubSingleTokenConnection)}.{nameof(CloseConnection)}");
                // Closing the connection also closes any sessions.
                amqpSession?.Connection.SafeClose();
                this.iotHubTokenRefresher?.Cancel();
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, amqpSession.Identifier, $"{nameof(IotHubSingleTokenConnection)}.{nameof(CloseConnection)}");
            }
        }
    }
}