// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class AmqpClientConnectionMux : AmqpClientConnection
    {
        #region Members-Constructor
        internal readonly bool UseLinkBasedTokenRefresh;
        internal AmqpClientSession AuthenticationSession;
        internal AmqpTokenRefresher AmqpTokenRefresher;

        private class MuxWorker
        {
            internal AmqpClientSession WorkerSession;
            private readonly Semaphore Lock;
            private bool Eslablished;
            internal MuxWorker()
            {
                Lock = new Semaphore(1, 1);
                Eslablished = false;
            }

            internal async Task OpenAsync(AmqpConnection amqpConnection, TimeSpan timeout)
            {
                Lock.WaitOne();
                if (!Eslablished)
                {
                    WorkerSession = new AmqpClientSession(amqpConnection);
                    await WorkerSession.OpenAsync(timeout).ConfigureAwait(false);
                    Eslablished = true;
                }
                Lock.Release();
            }
        }
        private ConcurrentDictionary<DeviceClientEndpointIdentity, MuxWorker> MuxedDevices;

        internal override event EventHandler OnAmqpClientConnectionClosed;

        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);

        OnClientConnectionIdle OnClientConnectionIdle;

        private readonly Semaphore Lock;

        internal AmqpClientConnectionMux(DeviceClientEndpointIdentity deviceClientEndpointIdentity, OnClientConnectionIdle removeDelegate, bool useLinkBasedTokenRefresh)
            : base(deviceClientEndpointIdentity.amqpTransportSettings, deviceClientEndpointIdentity.iotHubConnectionString.HostName)
        {
           if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}");

            MuxedDevices = new ConcurrentDictionary<DeviceClientEndpointIdentity, MuxWorker>();
            Lock = new Semaphore(1, 1);
            OnClientConnectionIdle = removeDelegate;
            UseLinkBasedTokenRefresh = useLinkBasedTokenRefresh;
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}");
        }

        #endregion

        #region Open-Close
        internal override async Task OpenAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(OpenAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            // Make sure connection is ready: AmqpConnection and AuthenticationSession are both established 
            AmqpConnection amqpConnection = await EnsureConnection(deviceClientEndpointIdentity, timeoutHelper.RemainingTime()).ConfigureAwait(false);

            Lock.WaitOne();
            MuxWorker muxWorker;
            // Device should be registerred
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            Lock.Release();
            if (muxWorker == null)
            {
                throw new InvalidOperationException("Worker not found.");
            }
            try
            {
                await muxWorker.OpenAsync(amqpConnection, timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // device failed to establish, remove 
                MuxedDevices.TryRemove(deviceClientEndpointIdentity, out _);
                throw;
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(OpenAsync)}");
        }

        internal async Task<AmqpConnection> EnsureConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnsureConnection)}");

            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

            AmqpConnection amqpConnection = null;
            AmqpClientSession authenticationSession = null;
            AmqpTokenRefresher amqpTokenRefresher = null;
            Lock.WaitOne();
            if (AmqpConnection == null)
            {
                try
                {
                    // Create transport
                    TransportBase transport = await InitializeTransport(deviceClientEndpointIdentity, timeoutHelper.RemainingTime()).ConfigureAwait(false);

                    // Establish connection
                    amqpConnection = new AmqpConnection(transport, amqpSettings, amqpConnectionSettings);
                    await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                    authenticationSession = new AmqpClientSession(amqpConnection);
                    await authenticationSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    if (!UseLinkBasedTokenRefresh && amqpTransportSettings.ClientCertificate == null) 
                    {
                        // Authenticate connection with Cbs
                        amqpTokenRefresher = new AmqpTokenRefresher(
                            authenticationSession,
                            deviceClientEndpointIdentity.iotHubConnectionString,
                            deviceClientEndpointIdentity.iotHubConnectionString.AmqpEndpoint.AbsoluteUri
                        );
                        await amqpTokenRefresher.RefreshTokenAsync(deviceClientEndpointIdentity, timeoutHelper.RemainingTime()).ConfigureAwait(false); ;
                    }

                    // Update status and event handler
                    AmqpConnection = amqpConnection;
                    AuthenticationSession = authenticationSession;
                    AmqpTokenRefresher = amqpTokenRefresher;
                    AmqpConnection.Closed += OnConnectionClosed;
                    AuthenticationSession.OnAmqpClientSessionClosed += OnAmqpAuthenticationSessionClosed;
                }
                catch (Exception ex) // when (!ex.IsFatal())
                {
                    if (amqpConnection == null)
                    {
                        // Create connection failed
                        throw;
                    }
                    else
                    {
                        if (amqpTokenRefresher != null)
                        {
                            // Refresh token failed
                            amqpTokenRefresher.Dispose();
                        }
                        if (amqpConnection.TerminalException == null)
                        {
                            amqpConnection.SafeClose(ex);
                            throw;
                        }
                        else
                        {
                            throw AmqpClientHelper.ToIotHubClientContract(AmqpConnection.TerminalException);
                        }
                    }
                }
                finally
                {
                    Lock.Release();
                }
            }
            else
            {
                // Already connected, do nothing
                amqpConnection = AmqpConnection;
                Lock.Release();
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnsureConnection)}");
            return amqpConnection;
        }

        internal override async Task CloseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(CloseAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

            AmqpConnection amqpConnection = null;

            Lock.WaitOne();
            MuxWorker muxWorker;
            MuxedDevices.TryRemove(deviceClientEndpointIdentity, out muxWorker);
            // Last device is removed
            if (MuxedDevices.Count == 0)
            {
                OnClientConnectionIdle(this);
            }
            Lock.Release();
            if (muxWorker != null)
            {
                await muxWorker.WorkerSession.CloseAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(CloseAsync)}");
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(OnConnectionClosed)}");
            Lock.WaitOne();

            MuxedDevices.Clear();
            AmqpConnection = null;
            AuthenticationSession = null;
            Lock.Release();
            OnAmqpClientConnectionClosed?.Invoke(o, args);
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(OnConnectionClosed)}");
        }

        private void OnAmqpAuthenticationSessionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(OnAmqpAuthenticationSessionClosed)}");
            AmqpConnection connection = AmqpConnection;
            if (connection != null)
            {
                connection.SafeClose();
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(OnAmqpAuthenticationSessionClosed)}");
        }
        #endregion

        #region Telemetry
        internal override async Task EnableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableTelemetryAndC2DAsync)}");
            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(EnableTelemetryAndC2DAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.OpenLinkTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout, UseLinkBasedTokenRefresh, AuthenticationSession).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableTelemetryAndC2DAsync)}");
        }

        internal override async Task DisableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisableTelemetryAndC2DAsync)}");

            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(DisableTelemetryAndC2DAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.CloseLinkTelemetryAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisableTelemetryAndC2DAsync)}");
        }

        internal override async Task<Outcome> SendTelemetrMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(SendTelemetrMessageAsync)}");

            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);

            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(SendTelemetrMessageAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.OpenLinkTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout, UseLinkBasedTokenRefresh, AuthenticationSession).ConfigureAwait(false);
                Outcome outcome = await muxWorker.WorkerSession.SendTelemetryMessageAsync(deviceClientEndpointIdentity, message, timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(SendTelemetrMessageAsync)}");
                return outcome;
            }
        }
        #endregion

        #region Methods
        internal override async Task EnableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableMethodsAsync)}");

            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);

            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(EnableMethodsAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.OpenLinkMethodsAsync(deviceClientEndpointIdentity, correlationid, methodReceivedListener, timeout, UseLinkBasedTokenRefresh, AuthenticationSession).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableMethodsAsync)}");
        }

        internal override async Task DisableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisableMethodsAsync)}");

            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);

            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(DisableMethodsAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.CloseLinkMethodsAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisableMethodsAsync)}");
        }

        internal override async Task<Outcome> SendMethodResponseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage methodResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(SendMethodResponseAsync)}");

            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(SendMethodResponseAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                Outcome outcome = await muxWorker.WorkerSession.SendMethodResponseAsync(deviceClientEndpointIdentity, methodResponse, timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(SendMethodResponseAsync)}");
                return outcome;
            }
        }
        #endregion

        #region Twin
        internal override async Task EnableTwinPatchAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableTwinPatchAsync)}");

            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(EnableTwinPatchAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.OpenLinkTwinAsync(deviceClientEndpointIdentity, correlationid, onTwinPathReceivedListener, timeout, UseLinkBasedTokenRefresh, AuthenticationSession).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableTwinPatchAsync)}");
        }

        internal override async Task DisableTwinAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisableTwinAsync)}");

            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(DisableTwinAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.CloseLinkTwinAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisableTwinAsync)}");
        }

        internal override async Task<Outcome> SendTwinMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage twinMessage, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(SendTwinMessageAsync)}");

            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(SendTwinMessageAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                Outcome outcome = await muxWorker.WorkerSession.SendTwinMessageAsync(deviceClientEndpointIdentity, twinMessage, timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(SendTwinMessageAsync)}");
                return outcome;
            }
        }
        #endregion

        #region Events
        internal override async Task EnableEventsReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableEventsReceiveAsync)}");

            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(EnableEventsReceiveAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.OpenLinkEventsAsync(deviceClientEndpointIdentity, onEventsReceivedListener, timeout, UseLinkBasedTokenRefresh).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableEventsReceiveAsync)}");
        }
        #endregion

        #region Receive
        internal override async Task<Message> ReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(ReceiveAsync)}");

            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(ReceiveAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.OpenLinkTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout, UseLinkBasedTokenRefresh, AuthenticationSession).ConfigureAwait(false);

                AmqpMessage amqpMessage = await muxWorker.WorkerSession.TelemetryReceiverLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
                Message message = null;
                if (amqpMessage != null)
                {
                    message = new Message(amqpMessage)
                    {
                        LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
                    };
                }
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(ReceiveAsync)}");
                return message;
            }

        }
        #endregion

        #region Accept-Dispose
        internal override async Task<Outcome> DisposeMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisposeMessageAsync)}");

            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(DisposeMessageAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);
                Outcome disposeOutcome = await muxWorker.WorkerSession.TelemetryReceiverLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisposeMessageAsync)}");
                return disposeOutcome;
            }
        }

        internal override void DisposeTwinPatchDelivery(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisposeTwinPatchDelivery)}");

            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(DisposeTwinPatchDelivery)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                muxWorker.WorkerSession.DisposeTwinPatchDelivery(amqpMessage);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisposeTwinPatchDelivery)}");
        }

        internal bool DisposeOnIdle()
        {
            bool result = false;
            Lock.WaitOne();
            if (MuxedDevices.Count == 0 && AmqpConnection != null)
            {
                AmqpTokenRefresher amqpTokenRefresher = AmqpTokenRefresher;
                if (amqpTokenRefresher != null)
                {
                    amqpTokenRefresher.Dispose();
                    amqpTokenRefresher = null;
                }
                AmqpConnection.SafeClose();
                AmqpConnection = null;
                result = true;
            }
            Lock.Release();
            return result;
        }
        #endregion

        internal int GetNumberOfClients()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(GetNumberOfClients)}");
            Lock.WaitOne();
            int count = MuxedDevices.Count;
            Lock.Release();
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(GetNumberOfClients)}");
            return count;
        }

        internal AmqpClientConnectionMux AppendMuxWorker(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            Lock.WaitOne();
            // Create new MuxWorker if device is absent
            if (!MuxedDevices.ContainsKey(deviceClientEndpointIdentity))
            {
                MuxedDevices.TryAdd(deviceClientEndpointIdentity, new MuxWorker());
            }
            Lock.Release();
            return this;
        }

    }
}
