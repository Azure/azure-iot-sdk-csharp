// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport.AmqpIoT;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpTransportHandler : TransportHandler
    {
        #region Members-Constructor

        private const int ResponseTimeoutInSeconds = 300;
        private readonly TimeSpan _operationTimeout;
        private readonly AmqpUnit _amqpUnit;
        private readonly Action<TwinCollection> _onDesiredStatePatchListener;
        private readonly object _lock = new object();
        private ConcurrentDictionary<string, TaskCompletionSource<Twin>> _twinResponseCompletions = new ConcurrentDictionary<string, TaskCompletionSource<Twin>>();
        private bool _closed;
#pragma warning disable CA1810 // Initialize reference type static fields inline: We use the static ctor to have init-once semantics.

        static AmqpTransportHandler()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            try
            {
                AmqpIoTTrace.SetProvider(new AmqpIoTTransportLog());
            }
            catch (Exception ex)
            {
                // Do not throw from static ctor.
                Logging.Error(null, ex, nameof(AmqpTransportHandler));
            }
        }

        internal AmqpTransportHandler(
            IPipelineContext context,
            IotHubConnectionString connectionString,
            AmqpTransportSettings transportSettings,
            Func<MethodRequestInternal, Task> onMethodCallback = null,
            Action<TwinCollection> onDesiredStatePatchReceivedCallback = null,
            Func<string, Message, Task> onModuleMessageReceivedCallback = null,
            Func<Message, Task> onDeviceMessageReceivedCallback = null)
            : base(context, transportSettings)
        {
            _operationTimeout = transportSettings.OperationTimeout;
            _onDesiredStatePatchListener = onDesiredStatePatchReceivedCallback;
            var deviceIdentity = new DeviceIdentity(connectionString, transportSettings, context.Get<ProductInfo>(), context.Get<ClientOptions>());
            _amqpUnit = AmqpUnitManager.GetInstance().CreateAmqpUnit(
                deviceIdentity,
                onMethodCallback,
                TwinMessageListener,
                onModuleMessageReceivedCallback,
                onDeviceMessageReceivedCallback,
                OnDisconnected
            );

            Logging.Associate(this, _amqpUnit, $"{nameof(_amqpUnit)}");
        }

        private void OnDisconnected()
        {
            lock (_lock)
            {
                if (!_closed)
                {
                    OnTransportDisconnected();
                }
            }
        }

        #endregion Members-Constructor

        public override bool IsUsable => !_disposed;

        #region Open-Close

        public override async Task OpenAsync(TimeoutHelper timeoutHelper)
        {
            Logging.Enter(this, timeoutHelper, $"{nameof(OpenAsync)}");

            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _closed = false;
            }

            try
            {
                await _amqpUnit.OpenAsync(timeoutHelper.GetRemainingTime()).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, timeoutHelper, $"{nameof(OpenAsync)}");
            }
        }

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, cancellationToken, $"{nameof(OpenAsync)}");

            cancellationToken.ThrowIfCancellationRequested();
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _closed = false;
            }

            try
            {
                await _amqpUnit.OpenAsync(_operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, cancellationToken, $"{nameof(OpenAsync)}");
            }
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"{nameof(CloseAsync)}");

            lock (_lock)
            {
                _closed = true;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _amqpUnit.CloseAsync(_operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                OnTransportClosedGracefully();
                Logging.Exit(this, $"{nameof(CloseAsync)}");
            }
        }

        #endregion Open-Close

        #region Telemetry

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            Logging.Enter(this, message, cancellationToken, $"{nameof(SendEventAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                AmqpIoTOutcome amqpIoTOutcome = await _amqpUnit.SendEventAsync(message, _operationTimeout).ConfigureAwait(false);

                if (amqpIoTOutcome != null)
                {
                    amqpIoTOutcome.ThrowIfNotAccepted();
                }
            }
            finally
            {
                Logging.Exit(this, message, cancellationToken, $"{nameof(SendEventAsync)}");
            }
        }

        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            Logging.Enter(this, messages, cancellationToken, $"{nameof(SendEventAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.SendEventsAsync(messages, _operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, messages, cancellationToken, $"{nameof(SendEventAsync)}");
            }
        }

        public override async Task<Message> ReceiveAsync(TimeoutHelper timeoutHelper)
        {
            Logging.Enter(this, timeoutHelper, timeoutHelper.GetRemainingTime(), $"{nameof(ReceiveAsync)}");

            Message message = await _amqpUnit.ReceiveMessageAsync(timeoutHelper.GetRemainingTime()).ConfigureAwait(false);

            Logging.Exit(this, timeoutHelper, timeoutHelper.GetRemainingTime(), $"{nameof(ReceiveAsync)}");

            return message;
        }

        public override async Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, cancellationToken, $"{nameof(ReceiveAsync)}");

            Message message;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                message = await _amqpUnit.ReceiveMessageAsync(TransportSettings.DefaultReceiveTimeout).ConfigureAwait(false);
                if (message != null)
                {
                    break;
                }
            }

            Logging.Exit(this, cancellationToken, cancellationToken, $"{nameof(ReceiveAsync)}");

            return message;
        }

        public override async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, cancellationToken, $"{nameof(EnableReceiveMessageAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _amqpUnit.EnableReceiveMessageAsync(_operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, cancellationToken, $"{nameof(EnableReceiveMessageAsync)}");
            }
        }

        public override async Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, cancellationToken, $"{nameof(DisableReceiveMessageAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _amqpUnit.DisableReceiveMessageAsync(_operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, cancellationToken, $"{nameof(DisableReceiveMessageAsync)}");
            }
        }

        #endregion Telemetry

        #region Methods

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, cancellationToken, $"{nameof(EnableMethodsAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.EnableMethodsAsync(_operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, cancellationToken, $"{nameof(EnableMethodsAsync)}");
            }
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, cancellationToken, $"{nameof(DisableMethodsAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.DisableMethodsAsync(_operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, cancellationToken, $"{nameof(DisableMethodsAsync)}");
            }
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            Logging.Enter(this, methodResponse, cancellationToken, $"{nameof(SendMethodResponseAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                AmqpIoTOutcome amqpIoTOutcome = await _amqpUnit.SendMethodResponseAsync(methodResponse, _operationTimeout).ConfigureAwait(false);
                if (amqpIoTOutcome != null)
                {
                    amqpIoTOutcome.ThrowIfNotAccepted();
                }
            }
            finally
            {
                Logging.Exit(this, methodResponse, cancellationToken, $"{nameof(SendMethodResponseAsync)}");
            }
        }

        #endregion Methods

        #region Twin

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, cancellationToken, $"{nameof(EnableTwinPatchAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _amqpUnit.EnableTwinLinksAsync(_operationTimeout).ConfigureAwait(false);
                await _amqpUnit.SendTwinMessageAsync(AmqpTwinMessageType.Put, Guid.NewGuid().ToString(), null, _operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, cancellationToken, $"{nameof(EnableTwinPatchAsync)}");
            }
        }

        public override async Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, cancellationToken, $"{nameof(DisableTwinPatchAsync)}");

                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.DisableTwinLinksAsync(_operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, cancellationToken, $"{nameof(DisableTwinPatchAsync)}");
            }
        }

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, cancellationToken, $"{nameof(SendTwinGetAsync)}");

            try
            {
                await EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                Twin twin = await RoundTripTwinMessage(AmqpTwinMessageType.Get, null, cancellationToken).ConfigureAwait(false);
                if (twin == null)
                {
                    throw new InvalidOperationException("Service rejected the message");
                }
                return twin;
            }
            finally
            {
                Logging.Exit(this, cancellationToken, $"{nameof(SendTwinGetAsync)}");
            }
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            Logging.Enter(this, reportedProperties, cancellationToken, $"{nameof(SendTwinPatchAsync)}");

            try
            {
                await EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                Twin twin = await RoundTripTwinMessage(AmqpTwinMessageType.Patch, reportedProperties, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, reportedProperties, cancellationToken, $"{nameof(SendTwinPatchAsync)}");
            }
        }

        private async Task<Twin> RoundTripTwinMessage(AmqpTwinMessageType amqpTwinMessageType, TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            Logging.Enter(this, cancellationToken, $"{nameof(RoundTripTwinMessage)}");

            string correlationId = Guid.NewGuid().ToString();
            Twin response = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var taskCompletionSource = new TaskCompletionSource<Twin>();
                _twinResponseCompletions[correlationId] = taskCompletionSource;

                await _amqpUnit.SendTwinMessageAsync(amqpTwinMessageType, correlationId, reportedProperties, _operationTimeout).ConfigureAwait(false);

                var receivingTask = taskCompletionSource.Task;
                if (await Task.WhenAny(receivingTask, Task.Delay(TimeSpan.FromSeconds(ResponseTimeoutInSeconds), cancellationToken)).ConfigureAwait(false) == receivingTask)
                {
                    // Task completed within timeout.
                    // Consider that the task may have faulted or been canceled.
                    // We re-await the task so that any exceptions/cancellation is rethrown.
                    response = await receivingTask.ConfigureAwait(false);
                    if (response == null)
                    {
                        throw new InvalidOperationException("Service response is null");
                    }
                }
                else
                {
                    // Timeout happen
                    throw new TimeoutException();
                }
            }
            finally
            {
                _twinResponseCompletions.TryRemove(correlationId, out _);
                Logging.Exit(this, cancellationToken, $"{nameof(RoundTripTwinMessage)}");
            }

            return response;
        }

        #endregion Twin

        #region Events

        public override async Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, cancellationToken, $"{nameof(EnableEventReceiveAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.EnableEventReceiveAsync(_operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, cancellationToken, $"{nameof(EnableEventReceiveAsync)}");
            }
        }

        #endregion Events

        #region Device streaming

        public override async Task EnableStreamsAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(EnableStreamsAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _amqpUnit.EnableStreamsAsync(_operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(EnableStreamsAsync)}");
            }
        }

        public override async Task DisableStreamsAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(DisableStreamsAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _amqpUnit.DisableStreamsAsync(_operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(DisableStreamsAsync)}");
            }
        }

        public override async Task<DeviceStreamRequest> WaitForDeviceStreamRequestAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(WaitForDeviceStreamRequestAsync)}");

            try
            {
                DeviceStreamRequest deviceStreamRequest;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    deviceStreamRequest = await _amqpUnit.WaitForDeviceStreamRequestAsync(_operationTimeout).ConfigureAwait(false);
                    if (deviceStreamRequest != null)
                    {
                        break;
                    }
                }

                return deviceStreamRequest;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(WaitForDeviceStreamRequestAsync)}");
            }
        }

        public override async Task AcceptDeviceStreamRequestAsync(DeviceStreamRequest request, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, request, cancellationToken, $"{nameof(AcceptDeviceStreamRequestAsync)}");
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _amqpUnit.AcceptDeviceStreamRequestAsync(request, _operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, request, cancellationToken, $"{nameof(AcceptDeviceStreamRequestAsync)}");
            }
        }

        public override async Task RejectDeviceStreamRequestAsync(DeviceStreamRequest request, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, request, cancellationToken, $"{nameof(RejectDeviceStreamRequestAsync)}");
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _amqpUnit.RejectDeviceStreamRequestAsync(request, _operationTimeout).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, request, cancellationToken, $"{nameof(RejectDeviceStreamRequestAsync)}");
            }
        }

        #endregion Device streaming

        #region Accept-Dispose

        public override Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            Logging.Enter(this, lockToken, cancellationToken, $"{nameof(CompleteAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return DisposeMessageAsync(lockToken, AmqpIoTDisposeActions.Accepted);
            }
            finally
            {
                Logging.Exit(this, lockToken, cancellationToken, $"{nameof(CompleteAsync)}");
            }
        }

        public override Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            Logging.Enter(this, lockToken, cancellationToken, $"{nameof(AbandonAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return DisposeMessageAsync(lockToken, AmqpIoTDisposeActions.Released);
            }
            finally
            {
                Logging.Exit(this, lockToken, cancellationToken, $"{nameof(AbandonAsync)}");
            }
        }

        public override Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            Logging.Enter(this, lockToken, cancellationToken, $"{nameof(RejectAsync)}");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return DisposeMessageAsync(lockToken, AmqpIoTDisposeActions.Rejected);
            }
            finally
            {
                Logging.Exit(this, lockToken, cancellationToken, $"{nameof(RejectAsync)}");
            }
        }

        private async Task DisposeMessageAsync(string lockToken, AmqpIoTDisposeActions outcome)
        {
            Logging.Enter(this, outcome, $"{nameof(DisposeMessageAsync)}");

            AmqpIoTOutcome disposeOutcome;
            try
            {
                // Currently, the same mechanism is used for sending feedback for C2D messages and events received by modules.
                // However, devices only support C2D messages (they cannot receive events), and modules only support receiving events
                // (they cannot receive C2D messages). So we use this to distinguish whether to dispose the message (i.e. send outcome on)
                // the DeviceBoundReceivingLink or the EventsReceivingLink.
                // If this changes (i.e. modules are able to receive C2D messages, or devices are able to receive telemetry), this logic
                // will have to be updated.
                disposeOutcome = await _amqpUnit.DisposeMessageAsync(lockToken, outcome, _operationTimeout).ConfigureAwait(false);
                disposeOutcome.ThrowIfError();
            }
            finally
            {
                Logging.Exit(this, outcome, $"{nameof(DisposeMessageAsync)}");
            }
        }

        #endregion Accept-Dispose

        #region Helpers

        private void TwinMessageListener(Twin twin, string correlationId, TwinCollection twinCollection)
        {
            if (correlationId != null)
            {
                // It is a GET, just complete the task.
                TaskCompletionSource<Twin> task;
                if (_twinResponseCompletions.TryRemove(correlationId, out task))
                {
                    task.SetResult(twin);
                }
            }
            else
            {
                // It is a PATCH, just call the callback with the TwinCollection
                _onDesiredStatePatchListener(twinCollection);
            }
        }

        #endregion Helpers

        #region IDispose

        protected override void Dispose(bool disposing)
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                Logging.Info(this, $"{nameof(disposing)}");

                if (disposing)
                {
                    _closed = true;
                    OnTransportClosedGracefully();
                    AmqpUnitManager.GetInstance().RemoveAmqpUnit(_amqpUnit);
                    _disposed = true;
                }
            }
        }

        #endregion IDispose
    }
}
