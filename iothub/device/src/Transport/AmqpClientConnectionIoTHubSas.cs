// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class AmqpClientConnectionIoTHubSas : AmqpClientConnection, IDisposable
    {
        #region Members-Constructor
        private const bool useLinkBasedTokenRefresh = false;

        AmqpClientSession authenticationSession;
        AmqpClientSession workerAmqpClientSession;

        internal override event EventHandler OnAmqpClientConnectionClosed;

        internal bool isConnectionAuthenticated { get; private set; }

        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);
        static readonly TimeSpan RefreshTokenBuffer = TimeSpan.FromMinutes(2);
        static readonly TimeSpan RefreshTokenRetryInterval = TimeSpan.FromSeconds(30);

        private AmqpTokenRefresher amqpTokenRefresher;

        private DeviceClientEndpointIdentity deviceClientEndpointIdentity;
        RemoveClientConnectionFromPool RemoveClientConnectionFromPool;
		
        private static Semaphore openSemaphore = new Semaphore(1,1);
        private static Semaphore closeSemaphore = new Semaphore(1,1);

        internal AmqpClientConnectionIoTHubSas(DeviceClientEndpointIdentity deviceClientEndpointIdentity, RemoveClientConnectionFromPool removeDelegate)
            : base(deviceClientEndpointIdentity.amqpTransportSettings, deviceClientEndpointIdentity.iotHubConnectionString.HostName)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}");

            if (!(deviceClientEndpointIdentity is DeviceClientEndpointIdentityIoTHubSas))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionIoTHubSas)}." + "accepts only IoTHubSas device identities");
            }

            this.deviceClientEndpointIdentity = deviceClientEndpointIdentity;
            RemoveClientConnectionFromPool = removeDelegate;

            authenticationSession = null;
            workerAmqpClientSession = null;
            isConnectionAuthenticated = false;
        }
        #endregion

        #region Open-Close
        internal override async Task OpenAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            openSemaphore.WaitOne();
            
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(OpenAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);
            amqpTokenRefresher?.Cancel();

            // Create transport
            TransportBase transport = await InitializeTransport(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            try
            {
                // Create connection from transport
                amqpConnection = new AmqpConnection(transport, this.amqpSettings, this.amqpConnectionSettings);
                amqpConnection.Closed += OnConnectionClosed;
                await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                // Create Session for Authentication
                authenticationSession = new AmqpClientSession(this);
                await authenticationSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                authenticationSession.OnAmqpClientSessionClosed += AuthenticationSession_OnAmqpClientSessionClosed;

                // Authenticate connection with Cbs
                if ((!useLinkBasedTokenRefresh) && (!isConnectionAuthenticated))
                {
                    if (this.amqpTransportSettings.ClientCertificate == null)
                    {
                        this.amqpTokenRefresher = new AmqpTokenRefresher(
                           this.authenticationSession,
                           deviceClientEndpointIdentity.iotHubConnectionString,
                           deviceClientEndpointIdentity.iotHubConnectionString.AmqpEndpoint.AbsoluteUri
                           );

                        // Send Cbs token for new connection first
                        try
                        {
                            await this.amqpTokenRefresher.RefreshTokenAsync(deviceClientEndpointIdentity, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        }
                        catch (Exception exception) when (!exception.IsFatal())
                        {
                            authenticationSession.amqpSession?.Connection.SafeClose();

                            throw;
                        }
                    }
                    isConnectionAuthenticated = true;
                }
            }
            catch (Exception ex)  when (!ex.IsFatal())
            {
                if (amqpConnection.TerminalException != null)
                {
                    throw AmqpClientHelper.ToIotHubClientContract(amqpConnection.TerminalException);
                }

                amqpConnection.SafeClose(ex);
                throw;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(OpenAsync)}");
            }
            openSemaphore.Release();
        }

        private void AuthenticationSession_OnAmqpClientSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(AuthenticationSession_OnAmqpClientSessionClosed)}");
            amqpConnection.SafeClose();
        }

        private void WorkerAmqpClientSession_OnAmqpClientSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(WorkerAmqpClientSession_OnAmqpClientSessionClosed)}");
            amqpConnection.SafeClose();
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(OnConnectionClosed)}");
            OnAmqpClientConnectionClosed?.Invoke(o, args);
        }

        internal override async Task CloseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            closeSemaphore.WaitOne();

            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(CloseAsync)}");

            if (amqpConnection != null)
            {
                await amqpConnection.CloseAsync(timeout).ConfigureAwait(false);
            }

            RemoveClientConnectionFromPool(deviceClientEndpointIdentity);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(CloseAsync)}");

            closeSemaphore.Release();
        }
        #endregion

        #region Telemetry
        internal override async Task EnableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(EnableTelemetryAndC2DAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                if (workerAmqpClientSession == null)
                {
                    workerAmqpClientSession = new AmqpClientSession(this);
                    await workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                }
                await workerAmqpClientSession.OpenLinkTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh, null).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(EnableTelemetryAndC2DAsync)}");
        }

        internal override async Task DisableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisableTelemetryAndC2DAsync)}");

            await workerAmqpClientSession.CloseLinkTelemetryAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisableTelemetryAndC2DAsync)}");
        }

        internal override async Task<Outcome> SendTelemetrMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(SendTelemetrMessageAsync)}");

            Outcome outcome;

            // Create telemetry links on demand
            await EnableTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            // Send the message
            outcome = await workerAmqpClientSession.SendTelemetryMessageAsync(deviceClientEndpointIdentity, message, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(SendTelemetrMessageAsync)}");

            return outcome;
        }
        #endregion

        #region Methods
        internal override async Task EnableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(EnableMethodsAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                if (workerAmqpClientSession == null)
                {
                    workerAmqpClientSession = new AmqpClientSession(this);
                    await workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                }

                await workerAmqpClientSession.OpenLinkMethodsAsync(deviceClientEndpointIdentity, correlationid, methodReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh, null).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(EnableMethodsAsync)}");
        }

        internal override async Task DisableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisableMethodsAsync)}");

            await workerAmqpClientSession.CloseLinkMethodsAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisableMethodsAsync)}");
        }

        internal override async Task<Outcome> SendMethodResponseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage methodResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(SendMethodResponseAsync)}");

            Outcome outcome;

            outcome = await workerAmqpClientSession.SendMethodResponseAsync(deviceClientEndpointIdentity, methodResponse, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }
        #endregion

        #region Twin
        internal override async Task EnableTwinPatchAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(EnableTwinPatchAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                if (workerAmqpClientSession == null)
                {
                    workerAmqpClientSession = new AmqpClientSession(this);
                    await workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                }

                await workerAmqpClientSession.OpenLinkTwinAsync(deviceClientEndpointIdentity, correlationid, onTwinPathReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh, null).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(EnableTwinPatchAsync)}");
        }

        internal override async Task DisableTwinAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisableTwinAsync)}");

            await workerAmqpClientSession.CloseLinkTwinAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisableTwinAsync)}");
        }

        internal override async Task<Outcome> SendTwinMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage twinMessage, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(SendTwinMessageAsync)}");

            Outcome outcome;

            outcome = await workerAmqpClientSession.SendTwinMessageAsync(deviceClientEndpointIdentity, twinMessage, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }
        #endregion

        #region Events
        internal override async Task EnableEventsReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(EnableEventsReceiveAsync)}");

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                await workerAmqpClientSession.OpenLinkEventsAsync(deviceClientEndpointIdentity, onEventsReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(EnableEventsReceiveAsync)}");
        }
        #endregion

        #region Receive
        internal override async Task<Message> ReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(ReceiveAsync)}");

            Message message;
            AmqpMessage amqpMessage;

            // Create telemetry links on demand
            await EnableTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            amqpMessage = await workerAmqpClientSession.telemetryReceiverLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);

            if (amqpMessage != null)
            {
                message = new Message(amqpMessage)
                {
                    LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
                };
            }
            else
            {
                message = null;
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(ReceiveAsync)}");

            return message;
        }
        #endregion

        #region Accept-Dispose
        internal override async Task<Outcome> DisposeMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisposeMessageAsync)}");

            ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);

            Outcome disposeOutcome = null;

            if (workerAmqpClientSession != null)
            {
                disposeOutcome = await workerAmqpClientSession.telemetryReceiverLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisposeMessageAsync)}");

            return disposeOutcome;
        }

        internal override void DisposeTwinPatchDelivery(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisposeTwinPatchDelivery)}");

            workerAmqpClientSession.DisposeTwinPatchDelivery(amqpMessage);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisposeTwinPatchDelivery)}");
        }

        public void Dispose()
        {
            amqpTokenRefresher.Dispose();
        }

        internal override int GetNumberOfClients()
        {
            return 999;
        }
        #endregion
    }
}
