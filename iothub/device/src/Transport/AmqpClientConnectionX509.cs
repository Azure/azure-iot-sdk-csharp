// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class AmqpClientConnectionX509 : AmqpClientConnection
    {
        #region Members-Constructor
        private const bool useLinkBasedTokenRefresh = false;

        private AmqpClientSession workerAmqpClientSession;

        internal bool isConnectionClosed;
        private ProtocolHeader sentProtocolHeader;
        private TaskCompletionSource<TransportBase> taskCompletionSource;

        internal override event EventHandler OnAmqpClientConnectionClosed;

        internal bool isConnectionAuthenticated { get; private set; }

        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);

        private DeviceClientEndpointIdentity deviceClientEndpointIdentity;

        public AmqpClientConnectionX509(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
            : base(deviceClientEndpointIdentity.amqpTransportSettings, deviceClientEndpointIdentity.iotHubConnectionString.HostName)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}");

            if (!(deviceClientEndpointIdentity is DeviceClientEndpointIdentityX509))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}." + "accepts only X509 device identities");
            }

            this.deviceClientEndpointIdentity = deviceClientEndpointIdentity;

            workerAmqpClientSession = null;
            isConnectionAuthenticated = false;
        }

        internal override bool AddToMux(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            return false;
        }
        #endregion

        #region Open-Close
        internal override async Task OpenAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            var timeoutHelper = new TimeoutHelper(timeout);

            try
            {
                // Create transport
                TransportBase transport = await InitializeTransport(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

                // Create connection from transport
                amqpConnection = new AmqpConnection(transport, this.amqpSettings, this.amqpConnectionSettings);
                amqpConnection.Closed += OnConnectionClosed;
                await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                isConnectionClosed = false;

                isConnectionAuthenticated = true;
            }
            catch (Exception ex) // when (!ex.IsFatal())
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
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}");
            }
        }

        private void AuthenticationSession_OnAmqpClientSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(AuthenticationSession_OnAmqpClientSessionClosed)}");
            amqpConnection.SafeClose();
        }

        private void WorkerAmqpClientSession_OnAmqpClientSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(WorkerAmqpClientSession_OnAmqpClientSessionClosed)}");
            amqpConnection.SafeClose();
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(OnConnectionClosed)}");
            isConnectionClosed = true;
            OnAmqpClientConnectionClosed?.Invoke(o, args);
        }

        internal override async Task CloseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(CloseAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (amqpConnection != null)
            {
                await amqpConnection.CloseAsync(timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(CloseAsync)}");
        }
        #endregion

        #region Telemetry
        internal override async Task EnableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableTelemetryAndC2DAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

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
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableTelemetryAndC2DAsync)}");
        }

        internal override async Task DisableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisableTelemetryAndC2DAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            await workerAmqpClientSession.CloseLinkTelemetryAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisableTelemetryAndC2DAsync)}");
        }

        internal override async Task<Outcome> SendTelemetrMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(SendTelemetrMessageAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Outcome outcome;

            // Create telemetry links on demand
            await EnableTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            // Send the message
            outcome = await workerAmqpClientSession.SendTelemetryMessageAsync(deviceClientEndpointIdentity, message, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(SendTelemetrMessageAsync)}");

            return outcome;
        }
        #endregion

        #region Methods
        internal override async Task EnableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableMethodsAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

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

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableMethodsAsync)}");
        }

        internal override async Task DisableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisableMethodsAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            await workerAmqpClientSession.CloseLinkMethodsAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisableMethodsAsync)}");
        }

        internal override async Task<Outcome> SendMethodResponseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage methodResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(SendMethodResponseAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Outcome outcome;

            outcome = await workerAmqpClientSession.SendMethodResponseAsync(deviceClientEndpointIdentity, methodResponse, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }
        #endregion

        #region Twin
        internal override async Task EnableTwinPatchAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableTwinPatchAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

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

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableTwinPatchAsync)}");
        }

        internal override async Task DisableTwinAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisableTwinAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            await workerAmqpClientSession.CloseLinkTwinAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisableTwinAsync)}");
        }

        internal override async Task<Outcome> SendTwinMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage twinMessage, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(SendTwinMessageAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Outcome outcome;

            outcome = await workerAmqpClientSession.SendTwinMessageAsync(deviceClientEndpointIdentity, twinMessage, timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }
        #endregion

        #region Events
        internal override async Task EnableEventsReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableEventsReceiveAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            var timeoutHelper = new TimeoutHelper(timeout);

            if (isConnectionAuthenticated)
            {
                await workerAmqpClientSession.OpenLinkEventsAsync(deviceClientEndpointIdentity, onEventsReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(EnableEventsReceiveAsync)}");
        }
        #endregion

        #region Receive
        internal override async Task<Message> ReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(ReceiveAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

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

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(ReceiveAsync)}");

            return message;
        }
        #endregion

        #region Accept-Dispose
        internal override async Task<Outcome> DisposeMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisposeMessageAsync)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);

            Outcome disposeOutcome = null;

            if (workerAmqpClientSession != null)
            {
                disposeOutcome = await workerAmqpClientSession.telemetryReceiverLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisposeMessageAsync)}");

            return disposeOutcome;
        }

        internal override void DisposeTwinPatchDelivery(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisposeTwinPatchDelivery)}");

            if (this.deviceClientEndpointIdentity != deviceClientEndpointIdentity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionX509)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            workerAmqpClientSession.DisposeTwinPatchDelivery(amqpMessage);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionX509)}.{nameof(DisposeTwinPatchDelivery)}");
        }
        #endregion
    }
}