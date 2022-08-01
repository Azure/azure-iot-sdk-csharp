// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpTransportHandler : TransportHandler
    {
        private const int ResponseTimeoutInSeconds = 300;
        private readonly TimeSpan _operationTimeout;
        protected AmqpUnit _amqpUnit;
        private readonly Action<TwinCollection> _onDesiredStatePatchListener;
        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Twin>> _twinResponseCompletions = new ConcurrentDictionary<string, TaskCompletionSource<Twin>>();
        private bool _closed;

        static AmqpTransportHandler()
        {
            try
            {
                AmqpTrace.Provider = new AmqpIotTransportLog();
            }
            catch (Exception ex)
            {
                // Do not throw from static ctor.
                if (Logging.IsEnabled)
                    Logging.Error(null, ex, nameof(AmqpTransportHandler));
            }
        }

        internal AmqpTransportHandler(
            PipelineContext context,
            IotHubConnectionInfo connectionInfo,
            IotHubClientAmqpSettings transportSettings,
            Func<MethodRequestInternal, Task> onMethodCallback = null,
            Action<TwinCollection> onDesiredStatePatchReceivedCallback = null,
            Func<string, Message, Task> onModuleMessageReceivedCallback = null,
            Func<Message, Task> onDeviceMessageReceivedCallback = null)
            : base(context, transportSettings)
        {
            _operationTimeout = transportSettings.OperationTimeout;
            _onDesiredStatePatchListener = onDesiredStatePatchReceivedCallback;

            // Set the context information required for setting up the connection into IotHubConnectionInfo
            connectionInfo.AmqpTransportSettings = transportSettings;
            connectionInfo.ProductInfo = context.ProductInfo;
            connectionInfo.ClientOptions = context.ClientOptions;

            _amqpUnit = AmqpUnitManager.GetInstance().CreateAmqpUnit(
                connectionInfo,
                onMethodCallback,
                TwinMessageListener,
                onModuleMessageReceivedCallback,
                onDeviceMessageReceivedCallback,
                OnDisconnected);

            if (Logging.IsEnabled)
                Logging.Associate(this, _amqpUnit, nameof(_amqpUnit));
        }

        private void OnDisconnected()
        {
            if (!_closed)
            {
                lock (_lock)
                {
                    if (!_closed)
                    {
                        OnTransportDisconnected();
                    }
                }
            }
        }

        public override bool IsUsable => !_disposed;

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(OpenAsync));

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
                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);
                await _amqpUnit.OpenAsync(ctb.Token).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(OpenAsync));
            }
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(CloseAsync));

            lock (_lock)
            {
                _closed = true;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);
                await _amqpUnit.CloseAsync(ctb.Token).ConfigureAwait(false);
            }
            finally
            {
                OnTransportClosedGracefully();
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(CloseAsync));
            }
        }

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, cancellationToken, nameof(SendEventAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);
                AmqpIotOutcome amqpIotOutcome = await _amqpUnit.SendEventAsync(message, ctb.Token).ConfigureAwait(false);

                amqpIotOutcome?.ThrowIfNotAccepted();
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message, cancellationToken, nameof(SendEventAsync));
            }
        }

        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, messages, cancellationToken, nameof(SendEventAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);
                await _amqpUnit.SendEventsAsync(messages, ctb.Token).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, messages, cancellationToken, nameof(SendEventAsync));
            }
        }

        public override async Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(ReceiveMessageAsync));

            Message message;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var ctb = new CancellationTokenBundle(_transportSettings.DefaultReceiveTimeout, cancellationToken);
                message = await _amqpUnit.ReceiveMessageAsync(ctb.Token).ConfigureAwait(false);

                if (message != null)
                {
                    break;
                }
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, cancellationToken, cancellationToken, nameof(ReceiveMessageAsync));

            return message;
        }

        public override async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(EnableReceiveMessageAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);
                await _amqpUnit.EnableReceiveMessageAsync(ctb.Token).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(EnableReceiveMessageAsync));
            }
        }

        // This method is added to ensure that over MQTT devices can receive messages that were sent when it was disconnected.
        // This behavior is available by default over AMQP, so no additional implementation is required here.
        public override Task EnsurePendingMessagesAreDeliveredAsync(CancellationToken cancellationToken)
        {
            return TaskHelpers.CompletedTask;
        }

        public override async Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(DisableReceiveMessageAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);
                await _amqpUnit.DisableReceiveMessageAsync(ctb.Token).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(DisableReceiveMessageAsync));
            }
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(EnableMethodsAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);

                await _amqpUnit.EnableMethodsAsync(ctb.Token).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(EnableMethodsAsync));
            }
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(DisableMethodsAsync));

                cancellationToken.ThrowIfCancellationRequested();

                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);
                await _amqpUnit.DisableMethodsAsync(ctb.Token).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, cancellationToken, nameof(DisableMethodsAsync));
            }
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, methodResponse, cancellationToken, nameof(SendMethodResponseAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);
                AmqpIotOutcome amqpIotOutcome = await _amqpUnit
                    .SendMethodResponseAsync(methodResponse, ctb.Token)
                    .ConfigureAwait(false);

                if (amqpIotOutcome != null)
                {
                    amqpIotOutcome.ThrowIfNotAccepted();
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, methodResponse, cancellationToken, nameof(SendMethodResponseAsync));
            }
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(EnableTwinPatchAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                string correlationId = AmqpTwinMessageType.Put + Guid.NewGuid().ToString();

                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);
                await _amqpUnit
                    .SendTwinMessageAsync(AmqpTwinMessageType.Put, correlationId, null, ctb.Token)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(EnableTwinPatchAsync));
            }
        }

        public override async Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, cancellationToken, nameof(DisableTwinPatchAsync));

                cancellationToken.ThrowIfCancellationRequested();

                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);
                await _amqpUnit.DisableTwinLinksAsync(ctb.Token).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(DisableTwinPatchAsync));
            }
        }

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(SendTwinGetAsync));

            try
            {
                await EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);

                Twin twin = await RoundTripTwinMessageAsync(AmqpTwinMessageType.Get, null, cancellationToken)
                    .ConfigureAwait(false);
                return twin ?? throw new InvalidOperationException("Service rejected the message");
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(SendTwinGetAsync));
            }
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, reportedProperties, cancellationToken, nameof(SendTwinPatchAsync));

            try
            {
                await EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                await RoundTripTwinMessageAsync(AmqpTwinMessageType.Patch, reportedProperties, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, reportedProperties, cancellationToken, nameof(SendTwinPatchAsync));
            }
        }

        private async Task<Twin> RoundTripTwinMessageAsync(
            AmqpTwinMessageType amqpTwinMessageType,
            TwinCollection reportedProperties,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(RoundTripTwinMessageAsync));

            string correlationId = amqpTwinMessageType + Guid.NewGuid().ToString();
            Twin response = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var taskCompletionSource = new TaskCompletionSource<Twin>();
                _twinResponseCompletions[correlationId] = taskCompletionSource;

                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);
                await _amqpUnit.SendTwinMessageAsync(amqpTwinMessageType, correlationId, reportedProperties, ctb.Token).ConfigureAwait(false);

                Task<Twin> receivingTask = taskCompletionSource.Task;

                if (await Task
                    .WhenAny(receivingTask, Task.Delay(TimeSpan.FromSeconds(ResponseTimeoutInSeconds), cancellationToken))
                    .ConfigureAwait(false) == receivingTask)
                {
                    if (receivingTask.Exception?.InnerException != null)
                    {
                        throw receivingTask.Exception.InnerException;
                    }

                    // Task completed within timeout.
                    // Consider that the task may have faulted or been canceled.
                    // We re-await the task so that any exceptions/cancellation is re-thrown.
                    response = await receivingTask.ConfigureAwait(false);
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
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(RoundTripTwinMessageAsync));
            }

            return response;
        }

        public override async Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            // If an AMQP transport is opened as a module twin instead of an Edge module we need
            // to enable the deviceBound operations instead of the event receiver link
            if (isAnEdgeModule)
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(EnableEventReceiveAsync));

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);

                    await _amqpUnit.EnableEventReceiveAsync(ctb.Token).ConfigureAwait(false);
                }
                finally
                {
                    if (Logging.IsEnabled)
                        Logging.Exit(this, cancellationToken, nameof(EnableEventReceiveAsync));
                }
            }
            else
            {
                await EnableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
            }

        }

        public override Task CompleteMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, lockToken, cancellationToken, nameof(CompleteMessageAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return DisposeMessageAsync(lockToken, AmqpIotDisposeActions.Accepted, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, lockToken, cancellationToken, nameof(CompleteMessageAsync));
            }
        }

        public override Task AbandonMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, lockToken, cancellationToken, nameof(AbandonMessageAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return DisposeMessageAsync(lockToken, AmqpIotDisposeActions.Released, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, lockToken, cancellationToken, nameof(AbandonMessageAsync));
            }
        }

        public override Task RejectMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, lockToken, cancellationToken, nameof(RejectMessageAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return DisposeMessageAsync(lockToken, AmqpIotDisposeActions.Rejected, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, lockToken, cancellationToken, nameof(RejectMessageAsync));
            }
        }

        private async Task DisposeMessageAsync(string lockToken, AmqpIotDisposeActions outcome, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, outcome, nameof(DisposeMessageAsync));

            try
            {
                // Currently, the same mechanism is used for sending feedback for C2D messages and events received by modules.
                // However, devices only support C2D messages (they cannot receive events), and modules only support receiving events
                // (they cannot receive C2D messages). So we use this to distinguish whether to dispose the message (i.e. send outcome on)
                // the DeviceBoundReceivingLink or the EventsReceivingLink.
                // If this changes (i.e. modules are able to receive C2D messages, or devices are able to receive telemetry), this logic
                // will have to be updated.
                using var ctb = new CancellationTokenBundle(_operationTimeout, cancellationToken);

                AmqpIotOutcome disposeOutcome = await _amqpUnit.DisposeMessageAsync(lockToken, outcome, ctb.Token).ConfigureAwait(false);
                disposeOutcome.ThrowIfError();
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, outcome, nameof(DisposeMessageAsync));
            }
        }

        private void TwinMessageListener(Twin twin, string correlationId, TwinCollection twinCollection, IotHubException ex = default)
        {
            if (correlationId == null)
            {
                // This is desired property updates, so call the callback with TwinCollection.
                _onDesiredStatePatchListener(twinCollection);
            }
            else
            {
                if (correlationId.StartsWith(AmqpTwinMessageType.Get.ToString(), StringComparison.OrdinalIgnoreCase)
                    || correlationId.StartsWith(AmqpTwinMessageType.Patch.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    // For Get and Patch, complete the task.
                    if (_twinResponseCompletions.TryRemove(correlationId, out TaskCompletionSource<Twin> task))
                    {
                        if (ex == default)
                        {
                            task.SetResult(twin);
                        }
                        else
                        {
                            task.SetException(ex);
                        }
                    }
                    else
                    {
                        // This can happen if we received a message from service with correlation Id that was not set by SDK or does not exist in dictionary.
                        if (Logging.IsEnabled)
                            Logging.Info("Could not remove correlation id to complete the task awaiter for a twin operation.", nameof(TwinMessageListener));
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (Logging.IsEnabled)
                {
                    Logging.Enter(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_disposed}; disposing={disposing}", $"{nameof(AmqpTransportHandler)}.{nameof(Dispose)}");
                }

                lock (_lock)
                {
                    if (!_disposed)
                    {
                        base.Dispose(disposing);
                        if (disposing)
                        {
                            _closed = true;
                            AmqpUnitManager.GetInstance()?.RemoveAmqpUnit(_amqpUnit);
                            _disposed = true;
                        }
                    }

                    // the _disposed flag is inherited from the base class DefaultDelegatingHandler and is finally set to null there.
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_disposed}; disposing={disposing}", $"{nameof(AmqpTransportHandler)}.{nameof(Dispose)}");
                }
            }
        }
    }
}
