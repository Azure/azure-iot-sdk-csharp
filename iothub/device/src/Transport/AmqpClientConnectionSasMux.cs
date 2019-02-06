// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class AmqpClientConnectionSasMux : AmqpClientConnection
    {
        #region Members-Constructor
        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);
        private const bool useLinkBasedTokenRefresh = true;

        private class MuxWorker
        {
            internal AmqpClientSession WorkerSession;

            internal MuxWorker(AmqpClientConnectionSasMux connection)
            {
                WorkerSession = new AmqpClientSession(connection);
                WorkerSession.OnAmqpClientSessionClosed += connection.WorkerLinkSession_OnAmqpClientSessionClosed;
            }
        }

        private ConcurrentDictionary<DeviceClientEndpointIdentity, MuxWorker> MuxedDevices;

        internal override event EventHandler OnAmqpClientConnectionClosed;

        RemoveClientConnectionFromPool RemoveClientConnectionFromPool;

        private readonly Semaphore DeviceLock;

        private readonly Semaphore StatusLock;

        private DeviceClientEndpointIdentity ConnectionId;

        internal AmqpClientConnectionSasMux(DeviceClientEndpointIdentity deviceClientEndpointIdentity, RemoveClientConnectionFromPool removeDelegate)
            : base(deviceClientEndpointIdentity.amqpTransportSettings, deviceClientEndpointIdentity.iotHubConnectionString.HostName)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}");

            if (!(deviceClientEndpointIdentity is DeviceClientEndpointIdentitySasMux))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)} " + "accepts only SasMux device identities" );
            }

            RemoveClientConnectionFromPool = removeDelegate;
            DeviceLock = new Semaphore(1, 1);
            StatusLock = new Semaphore(1, 1);
            MuxedDevices = new ConcurrentDictionary<DeviceClientEndpointIdentity, MuxWorker>();
        }

        private MuxWorker GetMuxWorker(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            DeviceLock.WaitOne();
            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            DeviceLock.Release();
            return muxWorker;
        }

        private async Task EnsureConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            
            StatusLock.WaitOne();
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnsureConnection)}");
            if (ConnectionId is null)
            {
                try
                {
                    // Create transport
                    TransportBase transport = await InitializeTransport(deviceClientEndpointIdentity, timeoutHelper.RemainingTime()).ConfigureAwait(false);

                    // Establish connection
                    amqpConnection = new AmqpConnection(transport, this.amqpSettings, this.amqpConnectionSettings);
                    await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                    // Update status and event handler
                    ConnectionId = deviceClientEndpointIdentity;
                    amqpConnection.Closed += OnConnectionClosed;

                    // TODO ??? No API found for add connection into Pool, already handled or a gap ???
                }
                catch (Exception ex) // when (!ex.IsFatal())
                {
                    if (amqpConnection == null)
                    {
                        throw;
                    }
                    else
                    {
                        OnConnectionFailure(ex);
                        throw;
                    }
                }
                finally
                {
                    if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnsureConnection)}");
                    StatusLock.Release();
                }
            }
            else
            {
                // Already connected, do nothing
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnsureConnection)}");
                StatusLock.Release();
            }
            
        }

        private void OnConnectionFailure(Exception ex)
        {
            if (amqpConnection != null)
            {
                if (amqpConnection.TerminalException == null)
                {
                    amqpConnection.SafeClose(ex);
                    amqpConnection = null;
                }
                else
                {
                    amqpConnection = null;
                    throw AmqpClientHelper.ToIotHubClientContract(amqpConnection.TerminalException);
                }
            }
        }
        
        #endregion

        #region Open-Close
        internal override async Task OpenAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}");
            await EnsureConnection(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
            DeviceLock.WaitOne();
            if (!MuxedDevices.ContainsKey(deviceClientEndpointIdentity))
            {
                MuxedDevices.TryAdd(deviceClientEndpointIdentity, new MuxWorker(this));
            }
            DeviceLock.Release();
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}");
        }

        internal override async Task CloseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(CloseAsync)}");
            AmqpConnection connection = null;
            DeviceLock.WaitOne();
            MuxWorker muxWorker;
            MuxedDevices.TryRemove(deviceClientEndpointIdentity, out muxWorker);
            if (muxWorker != null && MuxedDevices.Count == 0)
            {
                // Last device is removed
                connection = amqpConnection;
                amqpConnection = null;
                RemoveClientConnectionFromPool(ConnectionId);
            }
            DeviceLock.Release();
            if (muxWorker != null)
            {
                await muxWorker.WorkerSession.CloseAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }
            if (connection != null)
            {
                await connection.CloseAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(CloseAsync)}");
        }

        private void WorkerLinkSession_OnAmqpClientSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(WorkerLinkSession_OnAmqpClientSessionClosed)}");
            amqpConnection.SafeClose();
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(OnConnectionClosed)}");
            

            StatusLock.WaitOne();
            RemoveClientConnectionFromPool(ConnectionId);
            ConnectionId = null;
            amqpConnection = null;
            StatusLock.Release();
            
            DeviceLock.WaitOne();
            ICollection<MuxWorker> workers = MuxedDevices.Values;
            MuxedDevices.Clear();
            DeviceLock.Release();

            foreach(MuxWorker worker in workers)
            {
                worker.WorkerSession.CloseAsync(DefaultOperationTimeout).ConfigureAwait(true);
            }

            OnAmqpClientConnectionClosed?.Invoke(o, args);
        }
        
        
        #endregion

        #region Telemetry
        internal override async Task EnableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableTelemetryAndC2DAsync)}");
            DeviceLock.WaitOne();
            MuxWorker muxWorker;
            MuxedDevices.TryGetValue(deviceClientEndpointIdentity, out muxWorker);
            DeviceLock.Release();

            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableTelemetryAndC2DAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.OpenLinkTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout, useLinkBasedTokenRefresh, null).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableTelemetryAndC2DAsync)}");
        }

        internal override async Task DisableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableTelemetryAndC2DAsync)}");

            MuxWorker muxWorker = GetMuxWorker(deviceClientEndpointIdentity);

            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableTelemetryAndC2DAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.CloseLinkTelemetryAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableTelemetryAndC2DAsync)}");
        }

        internal override async Task<Outcome> SendTelemetrMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendTelemetrMessageAsync)}");

            MuxWorker muxWorker = GetMuxWorker(deviceClientEndpointIdentity);

            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendTelemetrMessageAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await EnableTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
                Outcome outcome = await muxWorker.WorkerSession.SendTelemetryMessageAsync(deviceClientEndpointIdentity, message, timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendTelemetrMessageAsync)}");
                return outcome;
            }
            
        }
        #endregion

        #region Methods
        internal override async Task EnableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableMethodsAsync)}");

            MuxWorker muxWorker = GetMuxWorker(deviceClientEndpointIdentity);

            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableMethodsAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.OpenLinkMethodsAsync(deviceClientEndpointIdentity, correlationid, methodReceivedListener, timeout, useLinkBasedTokenRefresh, null).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableMethodsAsync)}");
        }

        internal override async Task DisableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableMethodsAsync)}");

            MuxWorker muxWorker = GetMuxWorker(deviceClientEndpointIdentity);

            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableMethodsAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.CloseLinkMethodsAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableMethodsAsync)}");
        }

        internal override async Task<Outcome> SendMethodResponseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage methodResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendMethodResponseAsync)}");

            MuxWorker muxWorker = GetMuxWorker(deviceClientEndpointIdentity);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendMethodResponseAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                Outcome outcome = await muxWorker.WorkerSession.SendMethodResponseAsync(deviceClientEndpointIdentity, methodResponse, timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendMethodResponseAsync)}");
                return outcome;
            }
        }
        #endregion

        #region Twin
        internal override async Task EnableTwinPatchAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableTwinPatchAsync)}");

            MuxWorker muxWorker = GetMuxWorker(deviceClientEndpointIdentity);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableTwinPatchAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.OpenLinkTwinAsync(deviceClientEndpointIdentity, correlationid, onTwinPathReceivedListener, timeout, useLinkBasedTokenRefresh, null).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableTwinPatchAsync)}");
        }

        internal override async Task DisableTwinAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableTwinAsync)}");

            MuxWorker muxWorker = GetMuxWorker(deviceClientEndpointIdentity);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableTwinAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else {
                await muxWorker.WorkerSession.CloseLinkTwinAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
            }            
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableTwinAsync)}");
        }

        internal override async Task<Outcome> SendTwinMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage twinMessage, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendTwinMessageAsync)}");

            MuxWorker muxWorker = GetMuxWorker(deviceClientEndpointIdentity);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendTwinMessageAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                Outcome outcome = await muxWorker.WorkerSession.SendTwinMessageAsync(deviceClientEndpointIdentity, twinMessage, timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendTwinMessageAsync)}");
                return outcome;
            }
        }
        #endregion

        #region Events
        internal override async Task EnableEventsReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableEventsReceiveAsync)}");

            MuxWorker muxWorker = GetMuxWorker(deviceClientEndpointIdentity);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableEventsReceiveAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                await muxWorker.WorkerSession.OpenLinkEventsAsync(deviceClientEndpointIdentity, onEventsReceivedListener, timeout, useLinkBasedTokenRefresh).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableEventsReceiveAsync)}");
        }
        #endregion

        #region Receive
        internal override async Task<Message> ReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(ReceiveAsync)}");

            MuxWorker muxWorker = GetMuxWorker(deviceClientEndpointIdentity);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(ReceiveAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                AmqpMessage amqpMessage = await muxWorker.WorkerSession.telemetryReceiverLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
                Message message = null;
                if (amqpMessage != null)
                {
                    message = new Message(amqpMessage)
                    {
                        LockToken = new Guid(amqpMessage.DeliveryTag.Array).ToString()
                    };
                }
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(ReceiveAsync)}");
                return message;
            }
            
        }
        #endregion

        #region Accept-Dispose
        internal override async Task<Outcome> DisposeMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisposeMessageAsync)}");

            MuxWorker muxWorker = GetMuxWorker(deviceClientEndpointIdentity);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisposeMessageAsync)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);
                Outcome disposeOutcome = await muxWorker.WorkerSession.telemetryReceiverLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisposeMessageAsync)}");
                return disposeOutcome;
            }            
        }

        internal override void DisposeTwinPatchDelivery(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisposeTwinPatchDelivery)}");

            MuxWorker muxWorker = GetMuxWorker(deviceClientEndpointIdentity);
            if (muxWorker == null)
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisposeTwinPatchDelivery)}: " + "DeviceClientEndpointIdentity crisis");
            }
            else
            {
                muxWorker.WorkerSession.DisposeTwinPatchDelivery(amqpMessage);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisposeTwinPatchDelivery)}");
        }

        internal override int GetNumberOfClients()
        {
            return MuxedDevices.Count;
        }
        #endregion
    }
}
