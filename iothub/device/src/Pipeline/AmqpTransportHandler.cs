// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
#pragma warning disable CA1852 // used in debug for unit test mocking
    internal class AmqpTransportHandler : TransportHandlerBase
#pragma warning restore CA1852
    {
        protected AmqpUnit _amqpUnit;
        private readonly Action<DesiredProperties> _onDesiredStatePatchListener;
        private readonly object _lock = new();
        private readonly ConcurrentDictionary<string, PendingAmqpTwinOperation> _pendingTwinOperations = new();

        // Timer to check if any expired messages exist. The timer is executed after each hour of execution.
        private readonly Timer _twinTimeoutTimer;

        private readonly PayloadConvention _payloadConvention;

        internal IotHubConnectionCredentials _connectionCredentials;

        private static readonly TimeSpan s_twinResponseTimeout = TimeSpan.FromMinutes(2);
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

        internal AmqpTransportHandler(PipelineContext context)
            : base(context)
        {
            _onDesiredStatePatchListener = context.DesiredPropertyUpdateCallback;

            var additionalClientInformation = new AdditionalClientInformation
            {
                ProductInfo = context.ProductInfo,
                ModelId = context.ModelId,
                PayloadConvention = context.PayloadConvention,
            };
            _payloadConvention = context.PayloadConvention;

            _connectionCredentials = context.IotHubConnectionCredentials;
            _amqpUnit = AmqpUnitManager.GetInstance().CreateAmqpUnit(
                _connectionCredentials,
                additionalClientInformation,
                (IotHubClientAmqpSettings)context.IotHubClientTransportSettings,
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
            if (Logging.IsEnabled)
                Logging.Info(this, $"AMQP connection was lost", nameof(OnDisconnected));

            if (!_closed)
            {
                lock (_lock)
                {
                    if (!_closed)
                    {
                        OnTransportDisconnected();

                        // During a disconnection, any pending twin updates won't be received, so we'll preemptively
                        // cancel these operations so the client can retry once reconnected.
                        RemoveOldOperations(TimeSpan.Zero);
                    }
                }
            }
        }

        public override bool IsUsable => !_isDisposed;

        public override DateTime GetSasTokenRefreshesOn()
        {
            return _amqpUnit.GetSasTokenRefreshesOn();
        }

        public override async Task<DateTime> RefreshSasTokenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(RefreshSasTokenAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await _amqpUnit.RefreshSasTokenAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(RefreshSasTokenAsync));
            }
        }

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

                // The timer would invoke callback in the specified time and that duration thereafter.
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

        public override async Task SendTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, cancellationToken, nameof(SendTelemetryAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                AmqpIotOutcome amqpIotOutcome = await _amqpUnit.SendTelemetryAsync(message, cancellationToken).ConfigureAwait(false);

                amqpIotOutcome?.ThrowIfNotAccepted();
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message, cancellationToken, nameof(SendTelemetryAsync));
            }
        }

        public override async Task SendTelemetryAsync(IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, messages, cancellationToken, nameof(SendTelemetryAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.SendEventsAsync(messages, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, messages, cancellationToken, nameof(SendTelemetryAsync));
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
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(DisableMethodsAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.DisableMethodsAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
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
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(DisableTwinPatchAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _amqpUnit.DisableTwinLinksAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(DisableTwinPatchAsync));
            }
        }

        public override async Task<TwinProperties> GetTwinAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(GetTwinAsync));

            try
            {
                await EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);

                AmqpMessage responseFromService = await RoundTripTwinMessageAsync(AmqpTwinMessageType.Get, null, cancellationToken).ConfigureAwait(false);

                if (responseFromService == null)
                {
                    throw new InvalidOperationException("Service rejected the message");
                }

                try
                {
                    // The response here is deserialized into an SDK-defined type based on service-defined NewtonSoft.Json-based json property name.
                    // For this reason, we use NewtonSoft Json serializer for this deserialization.
                    TwinDocument clientTwinProperties = DefaultPayloadConvention.Instance.GetObject<TwinDocument>(responseFromService.BodyStream);

                    var twinDesiredProperties = new DesiredProperties(clientTwinProperties.Desired)
                    {
                        PayloadConvention = _payloadConvention,
                    };

                    var twinReportedProperties = new ReportedProperties(clientTwinProperties.Reported, true)
                    {
                        PayloadConvention = _payloadConvention,
                    };

                    return new TwinProperties(twinDesiredProperties, twinReportedProperties);
                }
                catch (JsonReaderException ex)
                {
                    if (Logging.IsEnabled)
                    {
                        using var reader = new StreamReader(responseFromService.BodyStream);
                        string body = reader.ReadToEnd();
                        Logging.Error(this, $"Failed to parse Twin JSON: {ex}. Message body: '{body}'");
                    }

                    throw;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(GetTwinAsync));
            }
        }

        public override async Task<long> UpdateReportedPropertiesAsync(ReportedProperties reportedProperties, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, reportedProperties, cancellationToken, nameof(UpdateReportedPropertiesAsync));

            try
            {
                await EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                AmqpMessage responseFromService = await RoundTripTwinMessageAsync(AmqpTwinMessageType.Patch, reportedProperties, cancellationToken).ConfigureAwait(false);

                return responseFromService != null
                    && responseFromService.MessageAnnotations.Map.TryGetValue(AmqpIotConstants.ResponseVersionName, out long version)
                    ? version
                    : -1;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, reportedProperties, cancellationToken, nameof(UpdateReportedPropertiesAsync));
            }
        }

        private async Task<AmqpMessage> RoundTripTwinMessageAsync(
            AmqpTwinMessageType amqpTwinMessageType,
            ReportedProperties reportedProperties,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(RoundTripTwinMessageAsync));

            string correlationId = amqpTwinMessageType + Guid.NewGuid().ToString();
            AmqpMessage response = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var taskCompletionSource = new TaskCompletionSource<AmqpMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingTwinOperations[correlationId] = new PendingAmqpTwinOperation(taskCompletionSource);

                await _amqpUnit.SendTwinMessageAsync(amqpTwinMessageType, correlationId, reportedProperties, cancellationToken).ConfigureAwait(false);

                Task<AmqpMessage> receivingTask = taskCompletionSource.Task;

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
                _pendingTwinOperations.TryRemove(correlationId, out _);

                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(RoundTripTwinMessageAsync));
            }

            return response;
        }

        private void TwinMessageListener(AmqpMessage responseFromService, string correlationId, IotHubClientException ex = default)
        {
            if (correlationId == null)
            {
                // This is desired property updates, so call the callback with DesiredPropertyCollection.
                Dictionary<string, object> desiredPropertyPatchDictionary = DefaultPayloadConvention.Instance.GetObject<Dictionary<string, object>>(responseFromService.BodyStream);
                var desiredPropertyPatch = new DesiredProperties(desiredPropertyPatchDictionary)
                {
                    PayloadConvention = _payloadConvention,
                };

                _onDesiredStatePatchListener.Invoke(desiredPropertyPatch);
            }
            else
            {
                if (correlationId.StartsWith(AmqpTwinMessageType.Get.ToString(), StringComparison.OrdinalIgnoreCase)
                    || correlationId.StartsWith(AmqpTwinMessageType.Patch.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Received a response for operation with correlation Id {correlationId}.", nameof(TwinMessageListener));

                    // For Get and Patch, complete the task.
                    if (_pendingTwinOperations.TryRemove(correlationId, out PendingAmqpTwinOperation pendingOperation))
                    {
                        if (ex == default)
                        {
                            pendingOperation.CompletionTask.TrySetResult(responseFromService);
                        }
                        else
                        {
                            pendingOperation.CompletionTask.TrySetException(ex);
                        }
                    }
                    else
                    {
                        // This can happen if we received a message from service with correlation Id that was not set by SDK or does not exist in dictionary.
                        if (Logging.IsEnabled)
                            Logging.Info(this, "Could not remove correlation Id to complete the task awaiter for a twin operation.", nameof(TwinMessageListener));
                    }
                }
                else if (correlationId.StartsWith(AmqpTwinMessageType.Put.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    // This is an acknowledgement received from service for subscribing to desired property updates
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Subscribed for twin desired property updates successfully", nameof(TwinMessageListener));
                }
            }
        }

        private void RemoveOldOperations(object state)
        {
            if (state is not TimeSpan maxAge)
            {
                maxAge = s_twinResponseTimeout;
            }

            if (Logging.IsEnabled)
                Logging.Info(this, $"Removing operations older than {maxAge}", nameof(RemoveOldOperations));

            int canceledOperations = _pendingTwinOperations
                .Where(x => DateTimeOffset.UtcNow - x.Value.RequestSentOnUtc > maxAge)
                .Select(x =>
                    {
                        if (_pendingTwinOperations.TryRemove(x.Key, out PendingAmqpTwinOperation pendingOperation))
                        {
                            if (Logging.IsEnabled)
                                Logging.Error(this, $"Removing twin response for {x.Key}", nameof(RemoveOldOperations));

                            pendingOperation.CompletionTask.TrySetException(new IotHubClientException("Did not receive twin response from service.", IotHubClientErrorCode.NetworkErrors));
                        }
                        return true;
                    })
                .Count();

            if (Logging.IsEnabled)
                Logging.Error(this, $"Removed {canceledOperations} twin responses", nameof(RemoveOldOperations));
        }

        protected private override void Dispose(bool disposing)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_isDisposed}; disposing={disposing}", $"{nameof(AmqpTransportHandler)}.{nameof(Dispose)}");

            try
            {
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
