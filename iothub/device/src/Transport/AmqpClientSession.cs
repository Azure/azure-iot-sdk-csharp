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

        internal AmqpConnection amqpConnection { get; private set; }

        internal AmqpSession amqpSession { get; private set; }

        internal AmqpSessionSettings amqpSessionSettings { get; private set; }

        internal AmqpClientCbsLink cbsLink { get; private set; }

        internal AmqpClientLink telemetrySenderLink { get; private set; }
        internal AmqpClientLink telemetryReceiverLink { get; private set; }

        internal AmqpClientLink methodsSenderLink { get; private set; }
        internal AmqpClientLink methodsReceiverLink { get; private set; }

        internal AmqpClientLink twinSenderLink { get; private set; }
        internal AmqpClientLink twinReceiverLink { get; private set; }

        internal AmqpClientLink eventsReceiverLink { get; private set; }

        private AmqpClientLinkFactory amqpClientLinkFactory;

        Func<MethodRequestInternal, Task> methodReceivedListener;

        internal AmqpClientSession(AmqpConnection amqpConnection)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}");

            this.amqpConnection = amqpConnection;
            amqpSessionSettings = new AmqpSessionSettings()
            {
                Properties = new Fields()
            };

            telemetrySenderLink = null;
            telemetryReceiverLink = null;
            methodsSenderLink = null;
            methodsReceiverLink = null;
            twinSenderLink = null;
            twinReceiverLink = null;
            eventsReceiverLink = null;

            amqpClientLinkFactory = new AmqpClientLinkFactory();

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
            amqpSession = new AmqpSession(amqpConnection, amqpSessionSettings, amqpLinkFactory);

            // Add Session to the Connection
            amqpConnection.AddSession(amqpSession, new ushort?());
            amqpSession.Closed += OnSessionClosed;

            // Open Session
            await amqpSession.OpenAsync(timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OpenAsync)}");
        }

        protected virtual void OnLinkCreated(object sender, LinkCreatedEventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OnLinkCreated)}");
        }

        internal async Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(CloseAsync)}");

            if ((amqpSession != null) && (amqpSession.State.Equals(AmqpObjectState.Opened)) && (!amqpSession.IsClosing()))
            {
                await amqpSession.CloseAsync(timeout).ConfigureAwait(false);

                telemetrySenderLink = null;
                telemetryReceiverLink = null;
                methodsSenderLink = null;
                methodsReceiverLink = null;
                twinSenderLink = null;
                twinReceiverLink = null;
                eventsReceiverLink = null;
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

            if ((amqpSession != null) && (amqpSession.State.Equals(AmqpObjectState.Opened)) && (!amqpSession.IsClosing()))
            {
                if (cbsLink == null)
                {
                    cbsLink = new AmqpClientCbsLink(amqpConnection);
                }
            }
            else
            {
                throw new InvalidOperationException("Authentication session is not opened");
            }

            expiresAtUtc = await cbsLink.AuthenticateCbsAsync(deviceClientEndpointIdentity, "", timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(AuthenticateCbs)}");

            return expiresAtUtc;
        }
        #endregion

        #region Telemetry
        internal async Task OpenLinkTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout, bool useTokenRefresher, AmqpClientSession amqpAuthenticationSession)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkTelemetryAndC2DAsync)}");

            if ((amqpSession != null) && (amqpSession.State.Equals(AmqpObjectState.Opened)) && (!amqpSession.IsClosing()))
            {
                string correlationId = "";
                if (telemetrySenderLink == null)
                {
                    telemetrySenderLink = amqpClientLinkFactory.Create(AmqpClientLinkType.TelemetrySender, amqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher, amqpAuthenticationSession);
                    await telemetrySenderLink.OpenAsync(timeout).ConfigureAwait(false);
                    telemetrySenderLink.OnAmqpClientLinkClosed += TelemetrySendingLink_OnAmqpClientLinkClosed;
                }

                if (telemetryReceiverLink == null)
                {
                    telemetryReceiverLink = amqpClientLinkFactory.Create(AmqpClientLinkType.C2D, amqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher, amqpAuthenticationSession);
                    await telemetryReceiverLink.OpenAsync(timeout).ConfigureAwait(false);
                    telemetryReceiverLink.OnAmqpClientLinkClosed += TelemetryReceivingLink_OnAmqpClientLinkClosed;
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkTelemetryAndC2DAsync)}");
        }

        internal async Task CloseLinkTelemetryAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkTelemetryAsync)}");

            Task telemetrySenderLinkCloseTask = telemetrySenderLink.CloseAsync(timeout);
            Task telemetryReceiverLinkCloseTask = telemetryReceiverLink.CloseAsync(timeout);
            await Task.WhenAll(telemetrySenderLinkCloseTask, telemetryReceiverLinkCloseTask).ConfigureAwait(false);

            telemetrySenderLink = null;
            telemetryReceiverLink = null;

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkTelemetryAsync)}");
        }

        private void TelemetrySendingLink_OnAmqpClientLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientSession)}.{nameof(TelemetrySendingLink_OnAmqpClientLinkClosed)}");
            telemetrySenderLink = null;
        }

        private void TelemetryReceivingLink_OnAmqpClientLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientSession)}.{nameof(TelemetryReceivingLink_OnAmqpClientLinkClosed)}");
            telemetryReceiverLink = null;
        }

        internal async Task<Outcome> SendTelemetryMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage, TimeSpan operationTimeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(SendTelemetryMessageAsync)}");

            Outcome outcome = null;
            if ((amqpSession != null) && (amqpSession.State.Equals(AmqpObjectState.Opened)) && (!amqpSession.IsClosing()))
            {
                if (telemetrySenderLink != null)
                {
                    outcome = await telemetrySenderLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), operationTimeout).ConfigureAwait(false);
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

            if ((amqpSession != null) && (amqpSession.State.Equals(AmqpObjectState.Opened)) && (!amqpSession.IsClosing()))
            {
                if (methodsSenderLink == null)
                {
                    methodsSenderLink = amqpClientLinkFactory.Create(AmqpClientLinkType.MethodsSender, amqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher, amqpAuthenticationSession);
                    methodsSenderLink.OnAmqpClientLinkClosed += MethodsSendingLink_OnAmqpClientLinkClosed;
                    await methodsSenderLink.OpenAsync(timeout).ConfigureAwait(false);
                }

                if (methodsReceiverLink == null)
                {
                    methodsReceiverLink = amqpClientLinkFactory.Create(AmqpClientLinkType.MethodsReceiver, amqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher, amqpAuthenticationSession);
                    methodsReceiverLink.OnAmqpClientLinkClosed += MethodsReceivingLink_OnAmqpClientLinkClosed;
                    await methodsReceiverLink.OpenAsync(timeout).ConfigureAwait(false);

                    this.methodReceivedListener = methodReceivedListener;
                    methodsReceiverLink.RegisterMessageListener(MethodsRequestReceived);
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkMethodsAsync)}");
        }

        internal async Task CloseLinkMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkMethodsAsync)}");

            Task methodsSenderLinkCloseTask = methodsSenderLink.CloseAsync(timeout);
            Task methodsReceiverLinkCloseTask = methodsReceiverLink.CloseAsync(timeout);
            await Task.WhenAll(methodsSenderLinkCloseTask, methodsReceiverLinkCloseTask).ConfigureAwait(false);

            methodsSenderLink = null;
            methodsReceiverLink = null;

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkMethodsAsync)}");
        }

        internal async Task<Outcome> SendMethodResponseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage, TimeSpan operationTimeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(SendMethodResponseAsync)}");

            Outcome outcome = null;

            if ((amqpSession != null) && (amqpSession.State.Equals(AmqpObjectState.Opened)) && (!amqpSession.IsClosing()))
            {
                if (methodsSenderLink != null)
                {
                    outcome = await methodsSenderLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), operationTimeout).ConfigureAwait(false);
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
            methodsSenderLink = null;
            amqpSession.SafeClose();
        }

        private void MethodsReceivingLink_OnAmqpClientLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientSession)}.{nameof(MethodsReceivingLink_OnAmqpClientLinkClosed)}");
            methodsReceiverLink = null;
            amqpSession.SafeClose();
        }

        private void MethodsRequestReceived(AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(MethodsRequestReceived)}");

            MethodRequestInternal methodRequestInternal = MethodConverter.ConstructMethodRequestFromAmqpMessage(amqpMessage, new CancellationToken(false));
            methodsReceiverLink.DisposeDelivery(amqpMessage);
            methodReceivedListener(methodRequestInternal);

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

            if ((amqpSession != null) && (amqpSession.State.Equals(AmqpObjectState.Opened)) && (!amqpSession.IsClosing()))
            {
                if (twinSenderLink == null)
                {
                    twinSenderLink = amqpClientLinkFactory.Create(AmqpClientLinkType.TwinSender, amqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher, amqpAuthenticationSession);
                    twinSenderLink.OnAmqpClientLinkClosed += TwinSendingLink_OnAmqpClientLinkClosed;
                    await twinSenderLink.OpenAsync(timeout).ConfigureAwait(false);
                }

                if (twinReceiverLink == null)
                {
                    twinReceiverLink = amqpClientLinkFactory.Create(AmqpClientLinkType.TwinReceiver, amqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher, amqpAuthenticationSession);
                    twinReceiverLink.OnAmqpClientLinkClosed += TwinReceivingLink_OnAmqpClientLinkClosed;
                    await twinReceiverLink.OpenAsync(timeout).ConfigureAwait(false);

                    twinReceiverLink.RegisterMessageListener(onTwinPathReceivedListener);
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
            twinSenderLink = null;
            amqpSession.SafeClose();
        }

        private void TwinReceivingLink_OnAmqpClientLinkClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientSession)}.{nameof(TwinReceivingLink_OnAmqpClientLinkClosed)}");
            twinReceiverLink = null;
            amqpSession.SafeClose();
        }

        internal async Task CloseLinkTwinAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkTwinAsync)}");

            Task twinSenderLinkCloseTask = twinSenderLink.CloseAsync(timeout);
            Task twinReceiverLinkCloseTask = twinReceiverLink.CloseAsync(timeout);
            await Task.WhenAll(twinSenderLinkCloseTask, twinReceiverLinkCloseTask).ConfigureAwait(false);

            twinSenderLink = null;
            twinReceiverLink = null;

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkTwinAsync)}");
        }

        internal async Task<Outcome> SendTwinMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage, TimeSpan operationTimeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(SendTwinMessageAsync)}");

            Outcome outcome = null;

            if ((amqpSession != null) && (amqpSession.State.Equals(AmqpObjectState.Opened)) && (!amqpSession.IsClosing()))
            {
                if (twinSenderLink != null)
                {
                    outcome = await twinSenderLink.SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()), operationTimeout).ConfigureAwait(false);
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
            twinReceiverLink.DisposeDelivery(amqpMessage);
        }
        #endregion

        #region Events
        internal async Task OpenLinkEventsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout, bool useTokenRefresher)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkEventsAsync)}");

            if ((amqpSession != null) && (amqpSession.State.Equals(AmqpObjectState.Opened)) && (!amqpSession.IsClosing()))
            {
                if (eventsReceiverLink == null)
                {
                    string correlationId = "";
                    eventsReceiverLink = amqpClientLinkFactory.Create(AmqpClientLinkType.EventsReceiver, amqpSession, deviceClientEndpointIdentity, timeout, correlationId, useTokenRefresher);
                    eventsReceiverLink.OnAmqpClientLinkClosed += EventsReceivingLink_OnAmqpClientLinkClosed;
                }
                await eventsReceiverLink.OpenAsync(timeout).ConfigureAwait(false);

                eventsReceiverLink.RegisterMessageListener(onEventsReceivedListener);
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
