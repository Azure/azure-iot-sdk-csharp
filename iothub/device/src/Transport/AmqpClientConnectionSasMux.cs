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
    internal class AmqpClientConnectionSasMux : AmqpClientConnection
    {
        #region Members-Constructor
        private const bool useLinkBasedTokenRefresh = true;

        private AmqpClientSession authenticationSession;

        private class MuxWorker
        {
            internal AmqpClientSession workerAmqpClientSession = null;
        }
        private ConcurrentDictionary<DeviceClientEndpointIdentity, MuxWorker> muxedDevices = new ConcurrentDictionary<DeviceClientEndpointIdentity, MuxWorker>();

        internal override event EventHandler OnAmqpClientConnectionClosed;

        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);
        static readonly TimeSpan RefreshTokenBuffer = TimeSpan.FromMinutes(2);
        static readonly TimeSpan RefreshTokenRetryInterval = TimeSpan.FromSeconds(30);

        RemoveClientConnectionFromPool RemoveClientConnectionFromPool;

        private static Semaphore openSemaphore = new Semaphore(1,1);
        private static Semaphore closeSemaphore = new Semaphore(1,1);

        internal AmqpClientConnectionSasMux(DeviceClientEndpointIdentity deviceClientEndpointIdentity, RemoveClientConnectionFromPool removeDelegate)
            : base(deviceClientEndpointIdentity.amqpTransportSettings, deviceClientEndpointIdentity.iotHubConnectionString.HostName)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasMux)}");

            if (!(deviceClientEndpointIdentity is DeviceClientEndpointIdentitySasMux))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}." + "accepts only SasMux device identities" );
            }

            RemoveClientConnectionFromPool = removeDelegate;
            authenticationSession = null;
        }

        internal override bool AddToMux(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(AddToMux)}");

            bool retVal = false;
            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                if (muxedDevices.TryAdd(deviceClientEndpointIdentity, new MuxWorker()))
                {
                    retVal = true;
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(AddToMux)}");

            return retVal;
        }

        private bool RemoveFromMux(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (muxedDevices.ContainsKey(deviceClientEndpointIdentity))
            {
                var worker = new MuxWorker();
                if (muxedDevices.TryRemove(deviceClientEndpointIdentity, out worker))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Open-Close
        internal override async Task OpenAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            openSemaphore.WaitOne();
            
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }
            var timeoutHelper = new TimeoutHelper(timeout);

            if (amqpConnection == null)
            {
                // Create transport
                TransportBase transport = await InitializeTransport(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

                try
                {
                    // Create connection from transport
                    if (amqpConnection == null)
                    {
                        amqpConnection = new AmqpConnection(transport, this.amqpSettings, this.amqpConnectionSettings);
                        amqpConnection.Closed += OnConnectionClosed;
                        await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    }

                    if (!(amqpConnection.IsClosing()))
                    {
                        // Create Session for Authentication
                        if (authenticationSession == null)
                        {
                            authenticationSession = new AmqpClientSession(this);
                            await authenticationSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                            authenticationSession.OnAmqpClientSessionClosed += AuthenticationSession_OnAmqpClientSessionClosed;
                        }
                    }
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
                    if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OpenAsync)}");
                }
            }
            openSemaphore.Release();
        }

        private void AuthenticationSession_OnAmqpClientSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(AuthenticationSession_OnAmqpClientSessionClosed)}");
            amqpConnection.SafeClose();
        }

        private void WorkerAmqpClientSession_OnAmqpClientSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(WorkerAmqpClientSession_OnAmqpClientSessionClosed)}");
            amqpConnection.SafeClose();
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(OnConnectionClosed)}");
            muxedDevices.Clear();
            OnAmqpClientConnectionClosed?.Invoke(o, args);
        }

        internal override async Task CloseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            closeSemaphore.WaitOne();

            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(CloseAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    await muxWorker.workerAmqpClientSession.CloseAsync(timeout).ConfigureAwait(false);
                }
                if (RemoveFromMux(deviceClientEndpointIdentity))
                {
                    if (muxedDevices.IsEmpty)
                    {
                        await amqpConnection.CloseAsync(timeout).ConfigureAwait(false);
                        RemoveClientConnectionFromPool(deviceClientEndpointIdentity);
                    }
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(CloseAsync)}");

            closeSemaphore.Release();
        }
        #endregion

        #region Telemetry
        internal override async Task EnableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableTelemetryAndC2DAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (!(amqpConnection.IsClosing()))
            {
                var timeoutHelper = new TimeoutHelper(timeout);

                if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
                {
                    if (muxWorker.workerAmqpClientSession == null)
                    {
                        muxWorker.workerAmqpClientSession = new AmqpClientSession(this);
                        await muxWorker.workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        muxWorker.workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                    }
                    await muxWorker.workerAmqpClientSession.OpenLinkTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh, authenticationSession).ConfigureAwait(false);

                }
                else
                {
                    throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableTelemetryAndC2DAsync)}" + "TryGetValue failed");
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableTelemetryAndC2DAsync)}");
        }

        internal override async Task DisableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableTelemetryAndC2DAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    await muxWorker.workerAmqpClientSession.CloseLinkTelemetryAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableTelemetryAndC2DAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableTelemetryAndC2DAsync)}");
        }

        internal override async Task<Outcome> SendTelemetrMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendTelemetrMessageAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Outcome outcome = null;

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                // Create telemetry links on demand
                await EnableTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

                if (muxWorker.workerAmqpClientSession != null)
                {
                    // Send the message
                    outcome = await muxWorker.workerAmqpClientSession.SendTelemetryMessageAsync(deviceClientEndpointIdentity, message, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendTelemetrMessageAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendTelemetrMessageAsync)}");

            return outcome;
        }
        #endregion

        #region Methods
        internal override async Task EnableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableMethodsAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (!(amqpConnection.IsClosing()))
            {
                var timeoutHelper = new TimeoutHelper(timeout);

                if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
                {
                    if (muxWorker.workerAmqpClientSession == null)
                    {
                        muxWorker.workerAmqpClientSession = new AmqpClientSession(this);
                        await muxWorker.workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        muxWorker.workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                    }
                    await muxWorker.workerAmqpClientSession.OpenLinkMethodsAsync(deviceClientEndpointIdentity, correlationid, methodReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh, authenticationSession).ConfigureAwait(false);
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableMethodsAsync)}" + "TryGetValue failed");
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableMethodsAsync)}");
        }

        internal override async Task DisableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableMethodsAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    await muxWorker.workerAmqpClientSession.CloseLinkMethodsAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableMethodsAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableMethodsAsync)}");
        }

        internal override async Task<Outcome> SendMethodResponseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage methodResponse, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendMethodResponseAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Outcome outcome = null;

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    outcome = await muxWorker.workerAmqpClientSession.SendMethodResponseAsync(deviceClientEndpointIdentity, methodResponse, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendMethodResponseAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }
        #endregion

        #region Twin
        internal override async Task EnableTwinPatchAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableTwinPatchAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (!(amqpConnection.IsClosing()))
            {
                var timeoutHelper = new TimeoutHelper(timeout);

                if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
                {
                    if (muxWorker.workerAmqpClientSession == null)
                    {
                        muxWorker.workerAmqpClientSession = new AmqpClientSession(this);
                        await muxWorker.workerAmqpClientSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        muxWorker.workerAmqpClientSession.OnAmqpClientSessionClosed += WorkerAmqpClientSession_OnAmqpClientSessionClosed;
                    }
                    await muxWorker.workerAmqpClientSession.OpenLinkTwinAsync(deviceClientEndpointIdentity, correlationid, onTwinPathReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh, authenticationSession).ConfigureAwait(false);
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableTwinPatchAsync)}" + "TryGetValue failed");
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableTwinPatchAsync)}");
        }

        internal override async Task DisableTwinAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableTwinAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    await muxWorker.workerAmqpClientSession.CloseLinkTwinAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisableTwinAsync)}" + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisableTwinAsync)}");
        }

        internal override async Task<Outcome> SendTwinMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage twinMessage, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendTwinMessageAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Outcome outcome = null;

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    outcome = await muxWorker.workerAmqpClientSession.SendTwinMessageAsync(deviceClientEndpointIdentity, twinMessage, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(SendTwinMessageAsync)}" + "TryGetValue failed");
            }


            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(SendMethodResponseAsync)}");

            return outcome;
        }
        #endregion

        #region Events
        internal override async Task EnableEventsReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableEventsReceiveAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (!(amqpConnection.IsClosing()))
            {
                var timeoutHelper = new TimeoutHelper(timeout);

                if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
                {
                    if (muxWorker.workerAmqpClientSession != null)
                    {
                        await muxWorker.workerAmqpClientSession.OpenLinkEventsAsync(deviceClientEndpointIdentity, onEventsReceivedListener, timeoutHelper.RemainingTime(), useLinkBasedTokenRefresh).ConfigureAwait(false);
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(EnableEventsReceiveAsync)}" + "TryGetValue failed");
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(EnableEventsReceiveAsync)}");
        }
        #endregion

        #region Receive
        internal override async Task<Message> ReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(ReceiveAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(OpenAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            Message message;
            AmqpMessage amqpMessage = null;

            // Create telemetry links on demand
            await EnableTelemetryAndC2DAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    amqpMessage = await muxWorker.workerAmqpClientSession.telemetryReceiverLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(ReceiveAsync)}" + "TryGetValue failed");
            }

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

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(ReceiveAsync)}");

            return message;
        }
        #endregion

        #region Accept-Dispose
        internal override async Task<Outcome> DisposeMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisposeMessageAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisposeMessageAsync)}" + "DeviceClientEndpointIdentity crisis");
            }

            ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);

            Outcome disposeOutcome = null;

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    disposeOutcome = await muxWorker.workerAmqpClientSession.telemetryReceiverLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: timeout).ConfigureAwait(false);
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisposeMessageAsync)}");

            return disposeOutcome;
        }

        internal override void DisposeTwinPatchDelivery(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisposeTwinPatchDelivery)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisposeTwinPatchDelivery)}" + "DeviceClientEndpointIdentity crisis");
            }

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    muxWorker.workerAmqpClientSession.DisposeTwinPatchDelivery(amqpMessage);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionSasMux)}.{nameof(DisposeTwinPatchDelivery)}" + "TryGetValue failed");
            }


            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionSasSingle)}.{nameof(DisposeTwinPatchDelivery)}");
        }
        #endregion
    }
}
