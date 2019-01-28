// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Shared;

    sealed class IotHubDeviceMuxConnection : IotHubConnection
    {
        readonly IotHubDeviceScopeConnectionPool deviceScopeConnectionPool;
        readonly ConcurrentDictionary<AmqpObject, IotHubTokenRefresher> iotHubTokenRefreshers;
        readonly long cacheKey;

        public IotHubDeviceMuxConnection(IotHubDeviceScopeConnectionPool deviceScopeConnectionPool, long cacheKey, IotHubConnectionString connectionString, AmqpTransportSettings amqpTransportSettings)
            : base(connectionString.HostName, connectionString.AmqpEndpoint.Port, amqpTransportSettings)
        {
            this.deviceScopeConnectionPool = deviceScopeConnectionPool;
            this.cacheKey = cacheKey;
            this.FaultTolerantSession = new FaultTolerantAmqpObject<AmqpSession>(this.CreateSessionAsync, this.CloseConnection);
            this.iotHubTokenRefreshers = new ConcurrentDictionary<AmqpObject, IotHubTokenRefresher>();
        }

        public override Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(IotHubDeviceMuxConnection)}.{nameof(CloseAsync)}");

                this.FaultTolerantSession.Close();
                return TaskHelpers.CompletedTask;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(IotHubDeviceMuxConnection)}.{nameof(CloseAsync)}");
            }
        }

        public override void SafeClose(Exception exception)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, exception, $"{nameof(IotHubDeviceMuxConnection)}.{nameof(SafeClose)}");
                this.FaultTolerantSession.Close();
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, exception, $"{nameof(IotHubDeviceMuxConnection)}.{nameof(SafeClose)}");
            }
        }

        public override void Release(string deviceId)
        {
            if (this.deviceScopeConnectionPool != null)
            {
                this.deviceScopeConnectionPool.RemoveDeviceFromConnection(this, deviceId);
            }
            else
            {
                this.SafeClose(null);
            }
        }

        public long GetCacheKey()
        {
            return this.cacheKey;
        }

        protected override void OnCreateSession()
        {
            // Cleanup any lingering link refresh token timers
            this.CancelTokenRefreshers();
        }

        /**
          The input connection string can only be a device-scope connection string
         **/
        protected override void OnCreateSendingLink(IotHubConnectionString connectionString)
        {
            if (connectionString.SharedAccessKeyName != null)
            {
                throw new ArgumentException("Must provide a device-scope connection string", "connectionString");
            }
        }

        protected override void OnCreateReceivingLink(IotHubConnectionString connectionString)
        {
            if (connectionString.SharedAccessKeyName != null)
            {
                throw new ArgumentException("Must provide a device-scope connection string", "connectionString");
            }
        }

        protected override Uri BuildLinkAddress(IotHubConnectionString iotHubConnectionString, string path)
        {
            return iotHubConnectionString.BuildLinkAddress(path);
        }

        protected override string BuildAudience(IotHubConnectionString iotHubConnectionString, string path)
        {
            return iotHubConnectionString.Audience + path;
        }

        protected override async Task OpenLinkAsync(AmqpObject link, IotHubConnectionString connectionString, string audience, TimeSpan timeout, CancellationToken token)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, token, $"{nameof(IotHubDeviceMuxConnection)}.{nameof(OpenLinkAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);

            token.ThrowIfCancellationRequested();
            try
            {
                // this is a device-scope connection string. We need to send a CBS token for this specific link before opening it.
                var iotHubLinkTokenRefresher = new IotHubTokenRefresher(this.FaultTolerantSession.Value, connectionString, audience);

                if (this.iotHubTokenRefreshers.TryAdd(link, iotHubLinkTokenRefresher))
                {
                    link.SafeAddClosed((s, e) =>
                    {
                        if (this.iotHubTokenRefreshers.TryRemove(link, out iotHubLinkTokenRefresher))
                        {
                            iotHubLinkTokenRefresher.Cancel();
                        }
                    });

                    // Send Cbs token for new link first
                    // This will throw an exception if the device is not valid or if the token is not valid
                    await iotHubLinkTokenRefresher.SendCbsTokenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                }

                token.ThrowIfCancellationRequested();
                // Open Amqp Link
                await link.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }
            catch (Exception exception) when (!exception.IsFatal())
            {
                link.SafeClose(exception);
                throw;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, token, $"{nameof(IotHubDeviceMuxConnection)}.{nameof(OpenLinkAsync)}");
            }
        }

        void CloseConnection(AmqpSession amqpSession)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, amqpSession.Identifier, $"{nameof(IotHubDeviceMuxConnection)}.{nameof(CloseConnection)}");
                // Closing the connection also closes any sessions.
                amqpSession.Connection.SafeClose();

                this.CancelTokenRefreshers();
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, amqpSession.Identifier, $"{nameof(IotHubDeviceMuxConnection)}.{nameof(CloseConnection)}");
            }
        }

        void CancelTokenRefreshers()
        {
            foreach (var iotHubLinkTokenRefresher in this.iotHubTokenRefreshers.Values)
            {
                iotHubLinkTokenRefresher.Cancel();
            }

            this.iotHubTokenRefreshers.Clear();
        }
    }
}
