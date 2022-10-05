// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpTransportHandler : TransportHandler
    {
        protected AmqpUnit _amqpUnit;
        private readonly Action<TwinCollection> _onDesiredStatePatchListener;
        private readonly object _lock = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Twin>> _twinResponseCompletions = new();
        private readonly ConcurrentDictionary<string, DateTimeOffset> _twinResponseTimeouts = new();

        // Timer to check if any expired messages exist. The timer is executed after each hour of execution.
        private readonly Timer _twinTimeoutTimer;

        internal IotHubConnectionCredentials _connectionCredentials;

        private static readonly TimeSpan s_twinResponseTimeout = TimeSpan.FromMinutes(60);
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
            IotHubClientAmqpSettings transportSettings)
            : base(context, transportSettings)
        {
            _onDesiredStatePatchListener = context.DesiredPropertyUpdateCallback;

            var additionalClientInformation = new AdditionalClientInformation
            {
                ProductInfo = context.ProductInfo,
                ModelId = context.ModelId,
                PayloadConvention = context.PayloadConvention,
            };

            _connectionCredentials = context.IotHubConnectionCredentials;
            _amqpUnit = AmqpUnitManager.GetInstance().CreateAmqpUnit(
                _connectionCredentials,
                additionalClientInformation,
                transportSettings,
                context.MethodCallback,
                TwinMessageListener,
                context.MessageEventCallback,
                OnDisconnected);

            // Create a timer to remove any expired messages.
            _twinTimeoutTimer = new Timer(RemoveOldOperations);

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

        public override bool IsUsable => !_isDisposed;

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(OpenAsync));

            cancellationToken.ThrowIfCancellationRequested();

            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                _closed = false;
            }

            try
            {
                await _amqpUnit.OpenAsync(cancellationToken).ConfigureAwait(false);

                // The timer would invoke callback after every hour.
                _twinTimeoutTimer.Change(s_twinResponseTimeout, s_twinResponseTimeout);
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
                _twinTimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                await _amqpUnit.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                OnTransportClosedGracefully();
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(CloseAsync));
            }
        }

        public override async Task SendEventAsync(OutgoingMessage message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, cancellationToken, nameof(SendEventAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                AmqpIotOutcome amqpIotOutcome = await _amqpUnit.SendEventAsync(message, cancellationToken).ConfigureAwait(false);

                amqpIotOutcome?.ThrowIfNotAccepted();
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message, cancellationToken, nameof(SendEventAsync));
            }
        }

        public override async Task SendEventAsync(IEnumerable<OutgoingMessage> messages, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, messages, cancellationToken, nameof(SendEventAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.SendEventsAsync(messages, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, messages, cancellationToken, nameof(SendEventAsync));
            }
        }

        public override async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(EnableReceiveMessageAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_connectionCredentials.IsEdgeModule)
                {
                    // This method is specifically for receiving module events when connecting to Edgehub
                    await _amqpUnit.EnableEdgeModuleEventReceiveAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // This call determines within it what link address to open,
                    // so there is no need to make a different call for devices vs modules here.
                    await _amqpUnit.EnableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(EnableReceiveMessageAsync));
            }
        }

        public override async Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(DisableReceiveMessageAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.DisableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
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
                await _amqpUnit.EnableMethodsAsync(cancellationToken).ConfigureAwait(false);
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

                await _amqpUnit.DisableMethodsAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, cancellationToken, nameof(DisableMethodsAsync));
            }
        }

        public override async Task SendMethodResponseAsync(DirectMethodResponse methodResponse, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, methodResponse, cancellationToken, nameof(SendMethodResponseAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                AmqpIotOutcome amqpIotOutcome = await _amqpUnit
                    .SendMethodResponseAsync(methodResponse, cancellationToken)
                    .ConfigureAwait(false);

                amqpIotOutcome?.ThrowIfNotAccepted();
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

                await _amqpUnit
                    .SendTwinMessageAsync(AmqpTwinMessageType.Put, correlationId, null, cancellationToken)
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

                await _amqpUnit.DisableTwinLinksAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(DisableTwinPatchAsync));
            }
        }

        public override async Task<Twin> GetTwinAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(GetTwinAsync));

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
                    Logging.Exit(this, cancellationToken, nameof(GetTwinAsync));
            }
        }

        public override async Task<long> UpdateReportedPropertiesAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, reportedProperties, cancellationToken, nameof(UpdateReportedPropertiesAsync));

            try
            {
                await EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                Twin twin = await RoundTripTwinMessageAsync(AmqpTwinMessageType.Patch, reportedProperties, cancellationToken).ConfigureAwait(false);
                return twin.Version ?? 0L;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, reportedProperties, cancellationToken, nameof(UpdateReportedPropertiesAsync));
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
                var taskCompletionSource = new TaskCompletionSource<Twin>(TaskCreationOptions.RunContinuationsAsynchronously);
                _twinResponseCompletions[correlationId] = taskCompletionSource;
                _twinResponseTimeouts[correlationId] = DateTimeOffset.UtcNow;

                await _amqpUnit.SendTwinMessageAsync(amqpTwinMessageType, correlationId, reportedProperties, cancellationToken).ConfigureAwait(false);

                Task<Twin> receivingTask = taskCompletionSource.Task;

                if (receivingTask.Exception?.InnerException != null)
                {
                    throw receivingTask.Exception.InnerException;
                }

                // Consider that the task may have faulted or been canceled.
                // We re-await the task so that any exceptions/cancellation is re-thrown.
                response = await receivingTask.ConfigureAwait(false);
            }
            finally
            {
                _twinResponseCompletions.TryRemove(correlationId, out _);
                _twinResponseTimeouts.TryRemove(correlationId, out _);
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(RoundTripTwinMessageAsync));
            }

            return response;
        }

        private void TwinMessageListener(Twin twin, string correlationId, TwinCollection twinCollection, IotHubClientException ex = default)
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
                            task.TrySetResult(twin);
                        }
                        else
                        {
                            task.TrySetException(ex);
                        }
                    }
                    else
                    {
                        // This can happen if we received a message from service with correlation Id that was not set by SDK or does not exist in dictionary.
                        if (Logging.IsEnabled)
                            Logging.Info("Could not remove correlation Id to complete the task awaiter for a twin operation.", nameof(TwinMessageListener));
                    }
                }
            }
        }

        private void RemoveOldOperations(object _)
        {
            _ = _twinResponseTimeouts
                .Where(x => DateTimeOffset.UtcNow - x.Value > s_twinResponseTimeout)
                .Select(x =>
                    {
                        _twinResponseCompletions.TryRemove(x.Key, out TaskCompletionSource<Twin> _);
                        _twinResponseTimeouts.TryRemove(x.Key, out DateTimeOffset _);
                        return true;
                    });
        }

        protected private override void Dispose(bool disposing)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_isDisposed}; disposing={disposing}", $"{nameof(AmqpTransportHandler)}.{nameof(Dispose)}");

                lock (_lock)
                {
                    if (!_isDisposed)
                    {
                        _twinTimeoutTimer.Dispose();
                        base.Dispose(disposing);
                        if (disposing)
                        {
                            _closed = true;
                            AmqpUnitManager.GetInstance()?.RemoveAmqpUnit(_amqpUnit);
                            _isDisposed = true;
                        }
                    }

                    // the _disposed flag is inherited from the base class DefaultDelegatingHandler and is finally set to null there.
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_isDisposed}; disposing={disposing}", $"{nameof(AmqpTransportHandler)}.{nameof(Dispose)}");
            }
        }
    }
}
