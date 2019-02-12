// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class AmqpClientConnectionIoTHubSas : AmqpClientConnection, IDisposable
    {
        #region Members-Constructor
        private const bool useLinkBasedTokenRefresh = false;

        private AmqpClientSession authenticationSession;

        private class MuxWorker
        {
            internal AmqpClientSession workerAmqpClientSession = null;
        }
        private ConcurrentDictionary<DeviceClientEndpointIdentity, MuxWorker> muxedDevices = new ConcurrentDictionary<DeviceClientEndpointIdentity, MuxWorker>();

        internal override event EventHandler OnAmqpClientConnectionClosed;

        internal bool isConnectionAuthenticated { get; private set; }

        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);
        static readonly TimeSpan RefreshTokenBuffer = TimeSpan.FromMinutes(2);
        static readonly TimeSpan RefreshTokenRetryInterval = TimeSpan.FromSeconds(30);

        private AmqpTokenRefresher amqpTokenRefresher;

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

            RemoveClientConnectionFromPool = removeDelegate;
            authenticationSession = null;
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
            
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(OpenAsync)}");

            if (!(muxedDevices.ContainsKey(deviceClientEndpointIdentity)))
            {
                muxedDevices.TryAdd(deviceClientEndpointIdentity, new MuxWorker());
            }

            if (muxedDevices.ContainsKey(deviceClientEndpointIdentity))
            {
                var timeoutHelper = new TimeoutHelper(timeout);

            if (amqpConnection == null)
            {
                // Create transport
                TransportBase transport = await InitializeTransport(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);

                try
                {
                    // Create connection from transport
                    amqpConnection = new AmqpConnection(transport, this.amqpSettings, this.amqpConnectionSettings);
                    amqpConnection.Closed += OnConnectionClosed;
                    await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                    if (!(amqpConnection.IsClosing()))
                    {
                        // Create Session for Authentication
                        if (authenticationSession == null)
                        {
                            authenticationSession = new AmqpClientSession(this);
                            await authenticationSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                            authenticationSession.OnAmqpClientSessionClosed += AuthenticationSession_OnAmqpClientSessionClosed;
                        }

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
                }
                catch (Exception ex) when (!ex.IsFatal())
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
                }
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
            muxedDevices.Clear();
            OnAmqpClientConnectionClosed?.Invoke(o, args);
        }

        internal override async Task CloseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            closeSemaphore.WaitOne();

            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(CloseAsync)}");

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

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(CloseAsync)}");

            closeSemaphore.Release();
        }
        #endregion

        #region Telemetry
        internal override async Task EnableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(EnableTelemetryAndC2DAsync)}");

            if ((amqpConnection != null) & (!(amqpConnection.IsClosing())))
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
                    throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(EnableTelemetryAndC2DAsync)}: " + "TryGetValue failed");
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(EnableTelemetryAndC2DAsync)}");
        }

        internal override async Task DisableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisableTelemetryAndC2DAsync)}");

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    await muxWorker.workerAmqpClientSession.CloseLinkTelemetryAsync(deviceClientEndpointIdentity, timeout).ConfigureAwait(false);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisableTelemetryAndC2DAsync)}: " + "TryGetValue failed");
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisableTelemetryAndC2DAsync)}");
        }

        internal override async Task<Outcome> SendTelemetrMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage message, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(SendTelemetrMessageAsync)}");

            Outcome outcome = null;

            if ((amqpConnection != null) & (!(amqpConnection.IsClosing())))
            {
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
                    throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(SendTelemetrMessageAsync)}: " + "TryGetValue failed");
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(SendTelemetrMessageAsync)}");

            return outcome;
        }
        #endregion

        #region Methods
        internal override async Task EnableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout)
        {
            throw new NotSupportedException("Methods are not upported in IoTHubSas authentication scenario");
        }

        internal override async Task DisableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            throw new NotSupportedException("Methods are not upported in IoTHubSas authentication scenario");
        }

        internal override async Task<Outcome> SendMethodResponseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage methodResponse, TimeSpan timeout)
        {
            throw new NotSupportedException("Methods are not upported in IoTHubSas authentication scenario");
        }
        #endregion

        #region Twin
        internal override async Task EnableTwinPatchAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout)
        {
            throw new NotSupportedException("Twin is not upported in IoTHubSas authentication scenario");
        }

        internal override async Task DisableTwinAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            throw new NotSupportedException("Twin is not upported in IoTHubSas authentication scenario");
        }

        internal override async Task<Outcome> SendTwinMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage twinMessage, TimeSpan timeout)
        {
            throw new NotSupportedException("Twin is not upported in IoTHubSas authentication scenario");
        }
        #endregion

        #region Events
        internal override async Task EnableEventsReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout)
        {
            throw new NotSupportedException("Events are not upported in IoTHubSas authentication scenario");
        }
        #endregion

        #region Receive
        internal override async Task<Message> ReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(ReceiveAsync)}");

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
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(ReceiveAsync)}: " + "TryGetValue failed");
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

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    disposeOutcome = await muxWorker.workerAmqpClientSession.telemetryReceiverLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: timeout).ConfigureAwait(false);
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisposeMessageAsync)}");

            return disposeOutcome;
        }

        internal override void DisposeTwinPatchDelivery(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisposeTwinPatchDelivery)}");

            if (muxedDevices.TryGetValue(deviceClientEndpointIdentity, out MuxWorker muxWorker))
            {
                if (muxWorker.workerAmqpClientSession != null)
                {
                    muxWorker.workerAmqpClientSession.DisposeTwinPatchDelivery(amqpMessage);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisposeTwinPatchDelivery)}: " + "TryGetValue failed");
            }


            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnectionIoTHubSas)}.{nameof(DisposeTwinPatchDelivery)}");
        }

        public void Dispose()
        {
            amqpTokenRefresher.Dispose();
        }

        internal override int GetNumberOfClients()
        {
            return muxedDevices.Count;
        }
        #endregion
    }
}
