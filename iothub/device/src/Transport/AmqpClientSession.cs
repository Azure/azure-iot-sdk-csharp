// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class AmqpClientSession
    {
        #region Members-Constructor
        internal event EventHandler OnAmqpClientSessionClosed;

        internal AmqpConnection AmqpConnection { get; private set; }

        internal AmqpSession AmqpSession { get; private set; }

        internal AmqpSessionSettings AmqpSessionSettings { get; private set; }

        internal AmqpClientCbsLink CbsLink { get; private set; }

        internal AmqpClientLink TelemetrySenderLink { get; private set; }
        internal AmqpClientLink TelemetryReceiverLink { get; private set; }

        internal AmqpClientLink MethodsSenderLink { get; private set; }
        internal AmqpClientLink MethodsReceiverLink { get; private set; }

        internal AmqpClientLink TwinSenderLink { get; private set; }
        internal AmqpClientLink TwinReceiverLink { get; private set; }

        internal AmqpClientLink EventsReceiverLink { get; private set; }

        private AmqpClientLinkFactory AmqpClientLinkFactory;

        Func<MethodRequestInternal, Task> MethodReceivedListener;

        internal AmqpClientSession(AmqpConnection amqpConnection)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}");

            AmqpConnection = amqpConnection;
            AmqpSessionSettings = new AmqpSessionSettings()
            {
                Properties = new Fields()
            };

            TelemetrySenderLink = null;
            TelemetryReceiverLink = null;
            MethodsSenderLink = null;
            MethodsReceiverLink = null;
            TwinSenderLink = null;
            TwinReceiverLink = null;
            EventsReceiverLink = null;

            AmqpClientLinkFactory = new AmqpClientLinkFactory();

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}");
        }
        #endregion

        #region Open-Close
        internal async Task OpenAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OpenAsync)}");

            var amqpLinkFactory = new AmqpLinkFactory();
            amqpLinkFactory.LinkCreated += OnLinkCreated;

            // Create Session
            AmqpSession = new AmqpSession(AmqpConnection, AmqpSessionSettings, amqpLinkFactory);

            // Add Session to the Connection
            AmqpConnection.AddSession(AmqpSession, new ushort?());
            AmqpSession.Closed += OnSessionClosed;

            // Open Session
            await AmqpSession.OpenAsync(timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OpenAsync)}");
        }

        protected virtual void OnLinkCreated(object sender, LinkCreatedEventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OnLinkCreated)}");
        }

        internal async Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(CloseAsync)}");

            if ((AmqpSession != null) && (AmqpSession.State.Equals(AmqpObjectState.Opened)) && (!AmqpSession.IsClosing()))
            {
                await AmqpSession.CloseAsync(timeout).ConfigureAwait(false);

                TelemetrySenderLink = null;
                TelemetryReceiverLink = null;
                MethodsSenderLink = null;
                MethodsReceiverLink = null;
                TwinSenderLink = null;
                TwinReceiverLink = null;
                EventsReceiverLink = null;
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(CloseAsync)}");
        }

        void OnSessionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OnSessionClosed)}");

            OnAmqpClientSessionClosed?.Invoke(o, args);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OnSessionClosed)}");
        }
        #endregion

        #region Authentication
        internal async Task<DateTime> AuthenticateCbs(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(AuthenticateCbs)}");

            DateTime expiresAtUtc;

            if ((AmqpSession != null) && (AmqpSession.State.Equals(AmqpObjectState.Opened)) && (!AmqpSession.IsClosing()))
            {
                if (CbsLink == null)
                {
                    CbsLink = new AmqpClientCbsLink(AmqpConnection);
                }
            }
            else
            {
                throw new InvalidOperationException("Authentication session is not opened");
            }

            expiresAtUtc = await CbsLink.AuthenticateCbsAsync(deviceClientEndpointIdentity, "", timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(AuthenticateCbs)}");

            return expiresAtUtc;
        }
        #endregion

        #region Telemetry
        internal async Task OpenLinkTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout, bool useTokenRefresher, AmqpClientSession amqpAuthenticationSession)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkTelemetryAndC2DAsync)}");

            if ((AmqpSession != null) && (AmqpSession.State.Equals(AmqpObjectState.Opened)) && (!AmqpSession.IsClosing()))
            {
                string correlationId = "";
                if (TelemetrySenderLink == null)
                {
                    TelemetrySenderLink = AmqpClientLinkFactory.Create(AmqpClientLinkType.TelemetrySender, AmqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher, amqpAuthenticationSession);
                    await TelemetrySenderLink.OpenAsync(timeout).ConfigureAwait(false);
                    TelemetrySenderLink.OnAmqpClientLinkClosed += TelemetrySendingLink_OnAmqpClientLinkClosed;
                }

                if (TelemetryReceiverLink == null)
                {
                    TelemetryReceiverLink = AmqpClientLinkFactory.Create(AmqpClientLinkType.C2D, AmqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher, amqpAuthenticationSession);
                    await TelemetryReceiverLink.OpenAsync(timeout).ConfigureAwait(false);
                    TelemetryReceiverLink.OnAmqpClientLinkClosed += TelemetryReceivingLink_OnAmqpClientLinkClosed;
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkTelemetryAndC2DAsync)}");
        }

        internal async Task CloseLinkTelemetryAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkTelemetryAsync)}");

            Task telemetrySenderLinkCloseTask = TelemetrySenderLink.CloseAsync(timeout);
            Task telemetryReceiverLinkCloseTask = TelemetryReceiverLink.CloseAsync(timeout);
            await Task.WhenAll(telemetrySenderLinkCloseTask, telemetryReceiverLinkCloseTask).ConfigureAwait(false);

            TelemetrySenderLink = null;
            TelemetryReceiverLink = null;

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkTelemetryAsync)}");
        }

        private void TelemetrySendingLink_OnAmqpClientLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientSession)}.{nameof(TelemetrySendingLink_OnAmqpClientLinkClosed)}");
            TelemetrySenderLink = null;
        }

        private void TelemetryReceivingLink_OnAmqpClientLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientSession)}.{nameof(TelemetryReceivingLink_OnAmqpClientLinkClosed)}");
            TelemetryReceiverLink = null;
        }

        internal async Task<Outcome> SendTelemetryMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage, TimeSpan operationTimeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(SendTelemetryMessageAsync)}");

            Outcome outcome = null;
            if ((AmqpSession != null) && (AmqpSession.State.Equals(AmqpObjectState.Opened)) && (!AmqpSession.IsClosing()))
            {
                if (TelemetrySenderLink != null)
                {
                    outcome = await TelemetrySenderLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), operationTimeout).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException("TelemetrySendingLink link is null");
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(SendTelemetryMessageAsync)}");

            return outcome;
        }

        #endregion

        #region Methods
        internal async Task OpenLinkMethodsAsync(
            DeviceClientEndpointIdentity deviceClientEndpointIdentity, 
            string correlationId, 
            Func<MethodRequestInternal, Task> methodReceivedListener, 
            TimeSpan timeout, 
            bool useTokenRefresher, 
            AmqpClientSession amqpAuthenticationSession
            )
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkMethodsAsync)}");

            if ((AmqpSession != null) && (AmqpSession.State.Equals(AmqpObjectState.Opened)) && (!AmqpSession.IsClosing()))
            {
                if (MethodsSenderLink == null)
                {
                    MethodsSenderLink = AmqpClientLinkFactory.Create(AmqpClientLinkType.MethodsSender, AmqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher, amqpAuthenticationSession);
                    MethodsSenderLink.OnAmqpClientLinkClosed += MethodsSendingLink_OnAmqpClientLinkClosed;
                    await MethodsSenderLink.OpenAsync(timeout).ConfigureAwait(false);
                }

                if (MethodsReceiverLink == null)
                {
                    MethodsReceiverLink = AmqpClientLinkFactory.Create(AmqpClientLinkType.MethodsReceiver, AmqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher, amqpAuthenticationSession);
                    MethodsReceiverLink.OnAmqpClientLinkClosed += MethodsReceivingLink_OnAmqpClientLinkClosed;
                    await MethodsReceiverLink.OpenAsync(timeout).ConfigureAwait(false);

                    MethodReceivedListener = methodReceivedListener;
                    MethodsReceiverLink.RegisterMessageListener(MethodsRequestReceived);
                }
            }
            
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkMethodsAsync)}");
        }

        internal async Task CloseLinkMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkMethodsAsync)}");

            Task methodsSenderLinkCloseTask = MethodsSenderLink.CloseAsync(timeout);
            Task methodsReceiverLinkCloseTask = MethodsReceiverLink.CloseAsync(timeout);
            await Task.WhenAll(methodsSenderLinkCloseTask, methodsReceiverLinkCloseTask).ConfigureAwait(false);

            MethodsSenderLink = null;
            MethodsReceiverLink = null;

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkMethodsAsync)}");
        }

        internal async Task<Outcome> SendMethodResponseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage, TimeSpan operationTimeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(SendMethodResponseAsync)}");

            Outcome outcome = null;

            if ((AmqpSession != null) && (AmqpSession.State.Equals(AmqpObjectState.Opened)) && (!AmqpSession.IsClosing()))
            {
                if (MethodsSenderLink != null)
                {
                    outcome = await MethodsSenderLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), operationTimeout).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException("MethodsSendingLink link is null");
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }

        private void MethodsSendingLink_OnAmqpClientLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientSession)}.{nameof(MethodsSendingLink_OnAmqpClientLinkClosed)}");
            MethodsSenderLink = null;
            AmqpSession.SafeClose();
        }

        private void MethodsReceivingLink_OnAmqpClientLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientSession)}.{nameof(MethodsReceivingLink_OnAmqpClientLinkClosed)}");
            MethodsReceiverLink = null;
            AmqpSession.SafeClose();
        }

        private void MethodsRequestReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(MethodsRequestReceived)}");

            MethodRequestInternal methodRequestInternal = MethodConverter.ConstructMethodRequestFromAmqpMessage(amqpMessage, new CancellationToken(false));
            MethodsReceiverLink.DisposeDelivery(amqpMessage);
            MethodReceivedListener(methodRequestInternal);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(MethodsRequestReceived)}");
        }
        #endregion

        #region Twin
        internal async Task OpenLinkTwinAsync(
            DeviceClientEndpointIdentity deviceClientEndpointIdentity, 
            string correlationId, 
            Action<AmqpMessage> onTwinPathReceivedListener, 
            TimeSpan timeout,
            bool useTokenRefresher,
            AmqpClientSession amqpAuthenticationSession
            )
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkTwinAsync)}");

            if ((AmqpSession != null) && (AmqpSession.State.Equals(AmqpObjectState.Opened)) && (!AmqpSession.IsClosing()))
            {
                if (TwinSenderLink == null)
                {
                    TwinSenderLink = AmqpClientLinkFactory.Create(AmqpClientLinkType.TwinSender, AmqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher, amqpAuthenticationSession);
                    TwinSenderLink.OnAmqpClientLinkClosed += TwinSendingLink_OnAmqpClientLinkClosed;
                    await TwinSenderLink.OpenAsync(timeout).ConfigureAwait(false);
                }

                if (TwinReceiverLink == null)
                {
                    TwinReceiverLink = AmqpClientLinkFactory.Create(AmqpClientLinkType.TwinReceiver, AmqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher, amqpAuthenticationSession);
                    TwinReceiverLink.OnAmqpClientLinkClosed += TwinReceivingLink_OnAmqpClientLinkClosed;
                    await TwinReceiverLink.OpenAsync(timeout).ConfigureAwait(false);

                    TwinReceiverLink.RegisterMessageListener(onTwinPathReceivedListener);
                }
            }
            else
            {
                throw new InvalidOperationException("OpenLinkTwinAsync: Session is not opened");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkTwinAsync)}");
        }

        private void TwinSendingLink_OnAmqpClientLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientSession)}.{nameof(TwinSendingLink_OnAmqpClientLinkClosed)}");
            TwinSenderLink = null;
            AmqpSession.SafeClose();
        }

        private void TwinReceivingLink_OnAmqpClientLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientSession)}.{nameof(TwinReceivingLink_OnAmqpClientLinkClosed)}");
            TwinReceiverLink = null;
            AmqpSession.SafeClose();
        }

        internal async Task CloseLinkTwinAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkTwinAsync)}");

            Task twinSenderLinkCloseTask = TwinSenderLink.CloseAsync(timeout);
            Task twinReceiverLinkCloseTask = TwinReceiverLink.CloseAsync(timeout);
            await Task.WhenAll(twinSenderLinkCloseTask, twinReceiverLinkCloseTask).ConfigureAwait(false);

            TwinSenderLink = null;
            TwinReceiverLink = null;

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkTwinAsync)}");
        }

        internal async Task<Outcome> SendTwinMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage, TimeSpan operationTimeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(SendTwinMessageAsync)}");

            Outcome outcome = null;

            if ((AmqpSession != null) && (AmqpSession.State.Equals(AmqpObjectState.Opened)) && (!AmqpSession.IsClosing()))
            {
                if (TwinSenderLink != null)
                {
                    outcome = await TwinSenderLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), operationTimeout).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException("MethodsSendingLink link is null");
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(SendTwinMessageAsync)}");

            return outcome;
        }

        internal void DisposeTwinPatchDelivery(AmqpMessage amqpMessage)
        {
            TwinReceiverLink.DisposeDelivery(amqpMessage);
        }
        #endregion

        #region Events
        internal async Task OpenLinkEventsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout, bool useTokenRefresher)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkEventsAsync)}");

            if ((AmqpSession != null) && (AmqpSession.State.Equals(AmqpObjectState.Opened)) && (!AmqpSession.IsClosing()))
            {
                if (EventsReceiverLink == null)
                {
                    string correlationId = "";
                    EventsReceiverLink = AmqpClientLinkFactory.Create(AmqpClientLinkType.EventsReceiver, AmqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher);
                    EventsReceiverLink.OnAmqpClientLinkClosed += EventsReceivingLink_OnAmqpClientLinkClosed;
                }
                await EventsReceiverLink.OpenAsync(timeout).ConfigureAwait(false);

                EventsReceiverLink.RegisterMessageListener(onEventsReceivedListener);
            }
            else
            {
                throw new InvalidOperationException("OpenLinkEventsAsync: Session is not opened");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkEventsAsync)}");
        }

        private void EventsReceivingLink_OnAmqpClientLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientSession)}.{nameof(EventsReceivingLink_OnAmqpClientLinkClosed)}");
        }
        #endregion
    }
}
