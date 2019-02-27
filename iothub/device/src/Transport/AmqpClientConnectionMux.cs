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
        internal readonly bool useLinkBasedTokenRefresh;
        internal AmqpClientSession authenticationSession;
        internal AmqpTokenRefresher amqpTokenRefresher;

        private class MuxedDevice
        {
            internal AmqpClientSession DeviceSession;
            private readonly Semaphore Lock;
            private bool Eslablished;
            internal MuxedDevice()
            {
                Lock = new Semaphore(1, 1);
                Eslablished = false;
            }

            internal async Task OpenAsync(AmqpConnection amqpConnection, TimeSpan timeout)
            {
                Lock.WaitOne();
                if (!Eslablished)
                {
                    DeviceSession = new AmqpClientSession(amqpConnection);
                    await DeviceSession.OpenAsync(timeout).ConfigureAwait(false);
                    Eslablished = true;
                }
                Lock.Release();
            }
        }
        private ConcurrentDictionary<DeviceClientEndpointIdentity, MuxedDevice> MuxedDevices;

        internal override event EventHandler OnAmqpClientConnectionClosed;

        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);

        OnClientConnectionIdle OnClientConnectionIdle;

        private readonly Semaphore Lock;

        internal AmqpClientConnectionMux(DeviceClientEndpointIdentity deviceClientEndpointIdentity, OnClientConnectionIdle removeDelegate, bool useLinkBasedTokenRefresh)
            : base(deviceClientEndpointIdentity.amqpTransportSettings, deviceClientEndpointIdentity.iotHubConnectionString.HostName)
        {
           if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}");

            MuxedDevices = new ConcurrentDictionary<DeviceClientEndpointIdentity, MuxedDevice>();
            Lock = new Semaphore(1, 1);
            OnClientConnectionIdle = removeDelegate;
            this.useLinkBasedTokenRefresh = useLinkBasedTokenRefresh;
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
            MuxedDevice muxedDevice;
            // Device should be registerred
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);
            Lock.Release();
            if (muxedDevice == null)
            {
                throw new InvalidOperationException("Device not found.");
            }
            try
            {
                await muxedDevice.OpenAsync(amqpConnection, timeoutHelper.RemainingTime()).ConfigureAwait(false);
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
            if (this.amqpConnection == null)
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
                    if (!useLinkBasedTokenRefresh && amqpTransportSettings.ClientCertificate == null) 
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
                    this.amqpConnection = amqpConnection;
                    this.authenticationSession = authenticationSession;
                    this.amqpTokenRefresher = amqpTokenRefresher;
                    this.amqpConnection.Closed += OnConnectionClosed;
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
                            throw AmqpClientHelper.ToIotHubClientContract(amqpConnection.TerminalException);
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
                amqpConnection = this.amqpConnection;
                Lock.Release();
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnsureConnection)}");
            return amqpConnection;
        }

        internal override async Task CloseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(CloseAsync)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            Lock.WaitOne();
            MuxedDevice muxedDevice;
            MuxedDevices.TryRemove(deviceClientEndpointIdentity, out muxedDevice);
            // Last device is removed
            if (MuxedDevices.Count == 0)
            {
                OnClientConnectionIdle(this);
            }
            Lock.Release();
            if (muxedDevice != null)
            {
                await muxedDevice.DeviceSession.CloseAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(CloseAsync)}");
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(OnConnectionClosed)}");
            Lock.WaitOne();
            if (this.amqpConnection != null && ReferenceEquals(amqpConnection, o))
            {
                MuxedDevices.Clear();
                amqpConnection = null;
                authenticationSession = null;
            }
            Lock.Release();
            OnAmqpClientConnectionClosed?.Invoke(o, args);
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(OnConnectionClosed)}");
        }
        #endregion

        #region Telemetry
        internal override async Task EnableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableTelemetryAndC2DAsync)}");
            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);
            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(EnableTelemetryAndC2DAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxedDevice.DeviceSession.OpenLinkTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout, useLinkBasedTokenRefresh, authenticationSession).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableTelemetryAndC2DAsync)}");
        }

        internal override async Task DisableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisableTelemetryAndC2DAsync)}");

            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);
            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(DisableTelemetryAndC2DAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxedDevice.DeviceSession.CloseLinkTelemetryAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisableTelemetryAndC2DAsync)}");
        }

        internal override async Task<Outcome> SendTelemetrMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(SendTelemetrMessageAsync)}");

            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);

            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(SendTelemetrMessageAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxedDevice.DeviceSession.OpenLinkTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout, useLinkBasedTokenRefresh, authenticationSession).ConfigureAwait(false);
                Outcome outcome = await muxedDevice.DeviceSession.SendTelemetryMessageAsync(deviceClientEndpointIdentity, message, timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(SendTelemetrMessageAsync)}");
                return outcome;
            }
        }
        #endregion

        #region Methods
        internal override async Task EnableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableMethodsAsync)}");

            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);

            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(EnableMethodsAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxedDevice.DeviceSession.OpenLinkMethodsAsync(deviceClientEndpointIdentity, correlationid, methodReceivedListener, timeout, useLinkBasedTokenRefresh, authenticationSession).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableMethodsAsync)}");
        }

        internal override async Task DisableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisableMethodsAsync)}");

            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);

            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(DisableMethodsAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxedDevice.DeviceSession.CloseLinkMethodsAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisableMethodsAsync)}");
        }

        internal override async Task<Outcome> SendMethodResponseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage methodResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(SendMethodResponseAsync)}");

            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);
            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(SendMethodResponseAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                Outcome outcome = await muxedDevice.DeviceSession.SendMethodResponseAsync(deviceClientEndpointIdentity, methodResponse, timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(SendMethodResponseAsync)}");
                return outcome;
            }
        }
        #endregion

        #region Twin
        internal override async Task EnableTwinPatchAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableTwinPatchAsync)}");

            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);
            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(EnableTwinPatchAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxedDevice.DeviceSession.OpenLinkTwinAsync(deviceClientEndpointIdentity, correlationid, onTwinPathReceivedListener, timeout, useLinkBasedTokenRefresh, authenticationSession).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableTwinPatchAsync)}");
        }

        internal override async Task DisableTwinAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisableTwinAsync)}");

            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);
            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(DisableTwinAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxedDevice.DeviceSession.CloseLinkTwinAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisableTwinAsync)}");
        }

        internal override async Task<Outcome> SendTwinMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage twinMessage, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(SendTwinMessageAsync)}");

            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);
            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(SendTwinMessageAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                Outcome outcome = await muxedDevice.DeviceSession.SendTwinMessageAsync(deviceClientEndpointIdentity, twinMessage, timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(SendTwinMessageAsync)}");
                return outcome;
            }
        }
        #endregion

        #region Events
        internal override async Task EnableEventsReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableEventsReceiveAsync)}");

            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);
            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(EnableEventsReceiveAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxedDevice.DeviceSession.OpenLinkEventsAsync(deviceClientEndpointIdentity, onEventsReceivedListener, timeout, useLinkBasedTokenRefresh).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(EnableEventsReceiveAsync)}");
        }
        #endregion

        #region Receive
        internal override async Task<Message> ReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(ReceiveAsync)}");

            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);
            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(ReceiveAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxedDevice.DeviceSession.OpenLinkTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout, useLinkBasedTokenRefresh, authenticationSession).ConfigureAwait(false);

                AmqpMessage amqpMessage = await muxedDevice.DeviceSession.telemetryReceiverLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
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

            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);
            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(DisposeMessageAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);
                Outcome disposeOutcome = await muxedDevice.DeviceSession.telemetryReceiverLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisposeMessageAsync)}");
                return disposeOutcome;
            }
        }

        internal override void DisposeTwinPatchDelivery(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisposeTwinPatchDelivery)}");

            MuxedDevice muxedDevice;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxedDevice);
            if (muxedDevice == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionMux)}.{nameof(DisposeTwinPatchDelivery)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                muxedDevice.DeviceSession.DisposeTwinPatchDelivery(amqpMessage);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionMux)}.{nameof(DisposeTwinPatchDelivery)}");
        }

        internal bool DisposeOnIdle()
        {
            bool result = false;
            AmqpConnection amqpConnection = null;
            AmqpTokenRefresher amqpTokenRefresher = null;
            Lock.WaitOne();
            if (MuxedDevices.Count == 0 && this.amqpConnection != null)
            {
                amqpConnection = this.amqpConnection;
                amqpTokenRefresher = this.amqpTokenRefresher;
                this.amqpConnection = null;
                this.amqpTokenRefresher = null;
                result = true;
            }
            Lock.Release();
            if (amqpTokenRefresher != null)
            {
                amqpTokenRefresher.Dispose();
            }
            if (amqpConnection != null)
            {
                amqpConnection.SafeClose();
            }
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
                MuxedDevices.TryAdd(deviceClientEndpointIdentity, new MuxedDevice());
            }
            Lock.Release();
            return this;
        }

    }
}
