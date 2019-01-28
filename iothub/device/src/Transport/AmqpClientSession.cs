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

        internal AmqpClientConnection amqpClientConnection { get; private set; }

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

        internal bool isSessionOpened { get; private set; }

        private AmqpClientLinkFactory amqpClientLinkFactory;

        Func<MethodRequestInternal, Task> methodReceivedListener;

        internal AmqpClientSession(AmqpClientConnection amqpClientConnection)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}");

            this.amqpClientConnection = amqpClientConnection;
            amqpSessionSettings = new AmqpSessionSettings()
            {
                Properties = new Fields()
            };
            isSessionOpened = false;

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
            amqpSession = new AmqpSession(amqpClientConnection.amqpConnection, amqpSessionSettings, amqpLinkFactory);

            // Add Session to the Connection
            amqpClientConnection.amqpConnection.AddSession(amqpSession, new ushort?());
            amqpSession.Closed += OnSessionClosed;

            // Open Session
            await amqpSession.OpenAsync(timeout).ConfigureAwait(false);
            isSessionOpened = true;

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OpenAsync)}");
        }

        protected virtual void OnLinkCreated(object sender, LinkCreatedEventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OnLinkCreated)}");
        }

        internal async Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(CloseAsync)}");

            if ((amqpSession != null) && (isSessionOpened))
            {
                await amqpSession.CloseAsync(timeout).ConfigureAwait(false);

                isSessionOpened = false;

                telemetrySenderLink = null;
                telemetryReceiverLink = null;
                methodsSenderLink = null;
                methodsReceiverLink = null;
                twinSenderLink = null;
                twinReceiverLink = null;
                eventsReceiverLink = null;
            }
            else
            {
                throw new InvalidOperationException("Session is not authenticated");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(CloseAsync)}");
        }

        void OnSessionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OnSessionClosed)}");

            isSessionOpened = false;
            OnAmqpClientSessionClosed?.Invoke(o, args);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OnSessionClosed)}");
        }
        #endregion

        #region Authentication
        internal async Task<DateTime> AuthenticateCbs(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(AuthenticateCbs)}");

            DateTime expiresAtUtc;

            if (isSessionOpened)
            {
                if (cbsLink == null)
                {
                    cbsLink = new AmqpClientCbsLink(this, amqpClientConnection.deviceClientEndpointIdentity);
                }
            }
            else
            {
                throw new InvalidOperationException("Session is not opened");
            }

            expiresAtUtc = await cbsLink.AuthenticateCbsAsync(timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(AuthenticateCbs)}");

            return expiresAtUtc;
        }
        #endregion

        #region Telemetry
        internal async Task OpenLinkTelemetryAndC2DAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkTelemetryAndC2DAsync)}");

            if (isSessionOpened)
            {
                if (telemetrySenderLink == null)
                {
                    telemetrySenderLink = amqpClientLinkFactory.Create(AmqpClientLinkType.TelemetrySender, this, amqpClientConnection.deviceClientEndpointIdentity, timeout);
                    await telemetrySenderLink.OpenAsync(timeout).ConfigureAwait(false);
                    telemetrySenderLink.OnAmqpClientLinkClosed += TelemetrySendingLink_OnAmqpClientLinkClosed;
                }

                if (telemetryReceiverLink == null)
                {
                    telemetryReceiverLink = amqpClientLinkFactory.Create(AmqpClientLinkType.C2D, this, amqpClientConnection.deviceClientEndpointIdentity, timeout);
                    await telemetryReceiverLink.OpenAsync(timeout).ConfigureAwait(false);
                    telemetryReceiverLink.OnAmqpClientLinkClosed += TelemetryReceivingLink_OnAmqpClientLinkClosed;
                }
            }
            else
            {
                throw new InvalidOperationException("Session is not opened and authenticated");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkTelemetryAndC2DAsync)}");
        }

        internal async Task CloseLinkTelemetryAsync(TimeSpan timeout)
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

        internal async Task<Outcome> SendTelemetryMessageAsync(AmqpMessage amqpMessage, TimeSpan operationTimeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(SendTelemetryMessageAsync)}");

            Outcome outcome = null;
            if (isSessionOpened)
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
            else
            {
                throw new InvalidOperationException("Session is not opened and authenticated");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(SendTelemetryMessageAsync)}");

            return outcome;
        }

        #endregion

        #region Methods
        internal async Task OpenLinkMethodsAsync(string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkMethodsAsync)}");

            if (isSessionOpened)
            {
                if (methodsSenderLink == null)
                {
                    methodsSenderLink = amqpClientLinkFactory.Create(AmqpClientLinkType.MethodsSender, this, amqpClientConnection.deviceClientEndpointIdentity, timeout, correlationid);
                    methodsSenderLink.OnAmqpClientLinkClosed += MethodsSendingLink_OnAmqpClientLinkClosed;
                    await methodsSenderLink.OpenAsync(timeout).ConfigureAwait(false);
                }

                if (methodsReceiverLink == null)
                {
                    methodsReceiverLink = amqpClientLinkFactory.Create(AmqpClientLinkType.MethodsReceiver, this, amqpClientConnection.deviceClientEndpointIdentity, timeout, correlationid);
                    methodsReceiverLink.OnAmqpClientLinkClosed += MethodsReceivingLink_OnAmqpClientLinkClosed;
                    await methodsReceiverLink.OpenAsync(timeout).ConfigureAwait(false);

                    this.methodReceivedListener = methodReceivedListener;
                    methodsReceiverLink.RegisterMessageListener(MethodsRequestReceived);
                }
            }
            else
            {
                throw new InvalidOperationException("Session is not opened and authenticated");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkMethodsAsync)}");
        }

        internal async Task CloseLinkMethodsAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkMethodsAsync)}");

            Task methodsSenderLinkCloseTask = methodsSenderLink.CloseAsync(timeout);
            Task methodsReceiverLinkCloseTask = methodsReceiverLink.CloseAsync(timeout);
            await Task.WhenAll(methodsSenderLinkCloseTask, methodsReceiverLinkCloseTask).ConfigureAwait(false);

            methodsSenderLink = null;
            methodsReceiverLink = null;

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkMethodsAsync)}");
        }

        internal async Task<Outcome> SendMethodResponseAsync(AmqpMessage amqpMessage, TimeSpan operationTimeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(SendMethodResponseAsync)}");

            Outcome outcome = null;
            if (isSessionOpened)
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
            else
            {
                throw new InvalidOperationException("Session is not opened and authenticated");
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
        internal async Task OpenLinkTwinAsync(string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkTwinAsync)}");

            if (isSessionOpened)
            {
                if (twinSenderLink == null)
                {
                    twinSenderLink = amqpClientLinkFactory.Create(AmqpClientLinkType.TwinSender, this, amqpClientConnection.deviceClientEndpointIdentity, timeout);
                    twinSenderLink.OnAmqpClientLinkClosed += TwinSendingLink_OnAmqpClientLinkClosed;
                    await twinSenderLink.OpenAsync(timeout).ConfigureAwait(false);
                }

                if (twinReceiverLink == null)
                {
                    twinReceiverLink = amqpClientLinkFactory.Create(AmqpClientLinkType.TwinReceiver, this, amqpClientConnection.deviceClientEndpointIdentity, timeout);
                    twinReceiverLink.OnAmqpClientLinkClosed += TwinReceivingLink_OnAmqpClientLinkClosed;
                    await twinReceiverLink.OpenAsync(timeout).ConfigureAwait(false);

                    twinReceiverLink.RegisterMessageListener(onTwinPathReceivedListener);
                }
            }
            else
            {
                throw new InvalidOperationException("Session is not authenticated");
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

        internal async Task CloseLinkTwinAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkTwinAsync)}");

            Task twinSenderLinkCloseTask = twinSenderLink.CloseAsync(timeout);
            Task twinReceiverLinkCloseTask = twinReceiverLink.CloseAsync(timeout);
            await Task.WhenAll(twinSenderLinkCloseTask, twinReceiverLinkCloseTask).ConfigureAwait(false);

            twinSenderLink = null;
            twinReceiverLink = null;

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientSession)}.{nameof(CloseLinkTwinAsync)}");
        }

        internal async Task<Outcome> SendTwinMessageAsync(AmqpMessage amqpMessage, TimeSpan operationTimeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(SendTwinMessageAsync)}");

            Outcome outcome = null;
            if (isSessionOpened)
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
            else
            {
                throw new InvalidOperationException("Session is not opened and authenticated");
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
        internal async Task OpenLinkEventsAsync(Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientSession)}.{nameof(OpenLinkEventsAsync)}");

            if (isSessionOpened)
            {
                if (eventsReceiverLink == null)
                {
                    eventsReceiverLink = amqpClientLinkFactory.Create(AmqpClientLinkType.EventsReceiver, this, amqpClientConnection.deviceClientEndpointIdentity, timeout);
                    eventsReceiverLink.OnAmqpClientLinkClosed += EventsReceivingLink_OnAmqpClientLinkClosed;
                }
                await eventsReceiverLink.OpenAsync(timeout).ConfigureAwait(false);

                eventsReceiverLink.RegisterMessageListener(onEventsReceivedListener);
            }
            else
            {
                throw new InvalidOperationException("Session is not authenticated");
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
