// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class ConnectionStateDelegatingHandler : DefaultDelegatingHandler
    {
        private readonly SemaphoreSlim _clientOpenSemaphore = new(1, 1);

        private readonly Action<ConnectionStatusInfo> _onConnectionStatusChanged;
        private readonly IIotHubClientRetryPolicy _retryPolicy;

        private readonly ClientTransportStateMachine _clientTransportStateMachine = new();
        private Task _transportClosedTask;
        private CancellationTokenSource _handleDisconnectCts;
        private CancellationTokenSource _cancelPendingOperationsCts;

        internal ConnectionStateDelegatingHandler(PipelineContext context, IDelegatingHandler innerHandler)
            : base(context, innerHandler)
        {
            _onConnectionStatusChanged = context.ConnectionStatusChangeHandler;

            // This retry policy is saved only to handle the client state when the retry policy informs that no more retry attempts are to be made
            _retryPolicy = context.RetryPolicy;
        }

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(OpenAsync));

            try
            {
                switch (_clientTransportStateMachine.GetCurrentState())
                {
                    case ClientTransportState.Open:
                        return;

                    case ClientTransportState.Closing:
                        throw new InvalidOperationException($"The client is currently closing. To reopen the client wait until {nameof(CloseAsync)} completes" +
                            $" and then invoke {nameof(OpenAsync)}.");

                    case ClientTransportState.Opening:
                    case ClientTransportState.Closed:
                        {
                            if (_clientTransportStateMachine.GetCurrentState() == ClientTransportState.Closed)
                            {
                                _clientTransportStateMachine.MoveNext(ClientStateAction.OpenStart);
                            }

                            // Create a new cancellation token source that will be signaled by any subsequently invoked CloseAsync() for cancellation.
                            _cancelPendingOperationsCts = new CancellationTokenSource();
                            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                            await _clientOpenSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
                            try
                            {
                                if (_clientTransportStateMachine.GetCurrentState() == ClientTransportState.Opening)
                                {
                                    if (Logging.IsEnabled)
                                        Logging.Info(this, "Opening connection", nameof(OpenAsync));

                                    try
                                    {
                                        await base.OpenAsync(operationCts.Token).ConfigureAwait(false);

                                        _clientTransportStateMachine.MoveNext(ClientStateAction.OpenSuccess);
                                        var connectionStatusInfo = new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk);
                                        _onConnectionStatusChanged(connectionStatusInfo);

                                        // Create a new cancelaltion token source that will be used for reconnection recovery attempts.
                                        _handleDisconnectCts = new CancellationTokenSource();

                                        // Send the request for transport close notification.
                                        _transportClosedTask = HandleDisconnectAsync();
                                    }
                                    catch (Exception ex) when (!Fx.IsFatal(ex))
                                    {
                                        if (Logging.IsEnabled)
                                            Logging.Error(this, ex, nameof(HandleDisconnectAsync));

                                        HandleConnectionStatusExceptions(ex, true);
                                        _clientTransportStateMachine.MoveNext(ClientStateAction.OpenFailure);

                                        throw;
                                    }
                                }
                            }
                            finally
                            {
                                _clientOpenSemaphore?.Release();
                            }

                            break;
                        }
                }
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
                Logging.Enter(this, cancellationToken, nameof(CloseAsync));

            if (_clientTransportStateMachine.GetCurrentState() == ClientTransportState.Closed)
            {
                // Already closed so gracefully exit.
                return;
            }

            _clientTransportStateMachine.MoveNext(ClientStateAction.CloseStart);

            try
            {
                _cancelPendingOperationsCts?.Cancel();
                _handleDisconnectCts?.Cancel();

                await base.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _cancelPendingOperationsCts?.Dispose();
                _cancelPendingOperationsCts = null;

                _handleDisconnectCts?.Dispose();
                _handleDisconnectCts = null;

                _clientTransportStateMachine.MoveNext(ClientStateAction.CloseComplete);
            }
        }

        public override async Task SendTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, cancellationToken, nameof(SendTelemetryAsync));

            try
            {
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                await ValidateStateAndPerformOperationAsync(
                    () => base.SendTelemetryAsync(message, operationCts.Token),
                    nameof(SendTelemetryAsync),
                    operationCts.Token);
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
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                await ValidateStateAndPerformOperationAsync(
                    () => base.SendTelemetryAsync(messages, operationCts.Token),
                    nameof(SendTelemetryAsync),
                    operationCts.Token);
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
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                await ValidateStateAndPerformOperationAsync(
                    () => base.EnableReceiveMessageAsync(operationCts.Token),
                    nameof(EnableReceiveMessageAsync),
                    operationCts.Token);
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
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                await ValidateStateAndPerformOperationAsync(
                    () => base.DisableReceiveMessageAsync(operationCts.Token),
                    nameof(DisableReceiveMessageAsync),
                    operationCts.Token);
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
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                await ValidateStateAndPerformOperationAsync(
                    () => base.EnableMethodsAsync(operationCts.Token),
                    nameof(EnableMethodsAsync),
                    operationCts.Token);
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
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                await ValidateStateAndPerformOperationAsync(
                    () => base.DisableMethodsAsync(operationCts.Token),
                    nameof(DisableMethodsAsync),
                    operationCts.Token);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(DisableMethodsAsync));
            }
        }

        public override async Task SendMethodResponseAsync(DirectMethodResponse method, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, method, cancellationToken, nameof(SendMethodResponseAsync));

            try
            {
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                await ValidateStateAndPerformOperationAsync(
                    () => base.SendMethodResponseAsync(method, operationCts.Token),
                    nameof(SendMethodResponseAsync),
                    operationCts.Token);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, method, cancellationToken, nameof(SendMethodResponseAsync));
            }
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(EnableTwinPatchAsync));

            try
            {
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                await ValidateStateAndPerformOperationAsync(
                    () => base.EnableTwinPatchAsync(operationCts.Token),
                    nameof(EnableTwinPatchAsync),
                    operationCts.Token);
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
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                await ValidateStateAndPerformOperationAsync(
                    () => base.DisableTwinPatchAsync(operationCts.Token),
                    nameof(DisableTwinPatchAsync),
                    operationCts.Token);
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
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                return await ValidateStateAndPerformOperationAsync(
                    () => base.GetTwinAsync(operationCts.Token),
                    nameof(GetTwinAsync),
                    operationCts.Token);
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
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                return await ValidateStateAndPerformOperationAsync(
                    () => base.UpdateReportedPropertiesAsync(reportedProperties, operationCts.Token),
                    nameof(UpdateReportedPropertiesAsync),
                    operationCts.Token);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, reportedProperties, cancellationToken, nameof(UpdateReportedPropertiesAsync));
            }
        }

        public override async Task<DateTime> RefreshSasTokenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(RefreshSasTokenAsync));

            try
            {
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                return await ValidateStateAndPerformOperationAsync(
                    () => base.RefreshSasTokenAsync(operationCts.Token),
                    nameof(RefreshSasTokenAsync),
                    operationCts.Token);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(RefreshSasTokenAsync));
            }
        }

        private Task ValidateStateAndPerformOperationAsync(Func<Task> asyncOperation, string operationName, CancellationToken cancellationToken)
        {
            return ValidateStateAndPerformOperationAsync(async () =>
            {
                await asyncOperation().ConfigureAwait(false);
                return false;
            },
            operationName,
            cancellationToken);

        }

        private async Task<T> ValidateStateAndPerformOperationAsync<T>(Func<Task<T>> asyncOperation, string operationName, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, operationName, cancellationToken, nameof(ValidateStateAndPerformOperationAsync));

            try
            {
                switch (_clientTransportStateMachine.GetCurrentState())
                {
                    case ClientTransportState.Opening:
                        await _clientOpenSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                        try
                        {
                            if (_clientTransportStateMachine.GetCurrentState() != ClientTransportState.Open)
                            {
                                throw new InvalidOperationException($"The client connection must be opened before operations can begin. Call '{nameof(OpenAsync)}' and try again.");
                            }
                        }
                        finally
                        {
                            _clientOpenSemaphore?.Release();
                        }
                        return await asyncOperation().ConfigureAwait(false);

                    case ClientTransportState.Open:
                        return await asyncOperation().ConfigureAwait(false);

                    case ClientTransportState.Closing:
                        throw new InvalidOperationException($"The client is currently closing. Wait until {nameof(CloseAsync)} completes" +
                            $" and then invoke {nameof(OpenAsync)} and try the operation again.");

                    case ClientTransportState.Closed:
                        throw new InvalidOperationException($"The client is currently closed. Reopen the client using {nameof(OpenAsync)} and try the operation again.");

                    default:
                        throw new InvalidOperationException($"Unknown client transport state {_clientTransportStateMachine.GetCurrentState()}." +
                            $"Reach out to the library owners with the logs to determine how this state was reached.");
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, operationName, cancellationToken, nameof(ValidateStateAndPerformOperationAsync));
            }
        }

        // Triggered from connection loss event
        // This method sets up the SDK to try to recover from a connection loss. We start this task as soon as the client is opened.
        // WaitForTransportClosedAsync() waits for the signal that the transport layer has been closed or disconnected.
        // Once disconnected ungracefully, the SDK begins its reconnection attempt.
        // This reconnection attempt consults the retry policy supplied to determine if a reconnection attempt should be made.
        // These reconnection attempts can be canceled through the _handleDisconnectCts cancellation token source.
        // This cancellation token source is set to be canceled when CloseAsync() is invoked.
        private async Task HandleDisconnectAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(HandleDisconnectAsync));

            try
            {
                var connectionStatusInfo = new ConnectionStatusInfo();

                try
                {
                    // This waits for a disconnection event to be signaled by the transport layer
                    await WaitForTransportClosedAsync().ConfigureAwait(false);

                    if (Logging.IsEnabled)
                        Logging.Info(this, "Transport disconnected: unexpected.", nameof(HandleDisconnectAsync));

                    _clientTransportStateMachine.MoveNext(ClientStateAction.ConnectionLost);
                }
                catch (OperationCanceledException)
                {
                    // Canceled when the transport is being closed by the application.
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Transport disconnected: closed by application.", nameof(HandleDisconnectAsync));

                    // The internal transport layer state would have been set by the original CloseAsync() call.
                    connectionStatusInfo = new ConnectionStatusInfo(ConnectionStatus.Closed, ConnectionStatusChangeReason.ClientClosed);
                    _onConnectionStatusChanged(connectionStatusInfo);

                    return;
                }

                // This is used to ensure that when IotHubServiceNoRetry() policy is enabled, we should not be retrying.
                // This exception is not returned to the user.
                var networkException = new IotHubClientException("", IotHubClientErrorCode.NetworkErrors);
                if (!_retryPolicy.ShouldRetry(0, networkException, out TimeSpan delay))
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Transport disconnected: closed by application.", nameof(HandleDisconnectAsync));

                    _clientTransportStateMachine.MoveNext(ClientStateAction.CloseStart);
                    _clientTransportStateMachine.MoveNext(ClientStateAction.CloseComplete);

                    connectionStatusInfo = new ConnectionStatusInfo(ConnectionStatus.Disconnected, ConnectionStatusChangeReason.RetryExpired);
                    _onConnectionStatusChanged(connectionStatusInfo);

                    return;
                }

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                }

                // Reconnect
                connectionStatusInfo = new ConnectionStatusInfo(ConnectionStatus.DisconnectedRetrying, ConnectionStatusChangeReason.CommunicationError);
                _onConnectionStatusChanged(connectionStatusInfo);

                // This will recover to the state before the disconnect.
                await OpenAsync(_handleDisconnectCts.Token).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(HandleDisconnectAsync));
            }
        }

        // The retryAttemptsExhausted flag differentiates between calling this method while still retrying
        // vs calling this when no more retry attempts are being made.
        private void HandleConnectionStatusExceptions(Exception exception, bool retryAttemptsExhausted = false)
        {
            if (Logging.IsEnabled)
                Logging.Info(
                    this,
                    $"Received exception: {exception}, retryAttemptsExhausted={retryAttemptsExhausted}",
                    nameof(HandleConnectionStatusExceptions));

            ConnectionStatus status = ConnectionStatus.Disconnected;
            ConnectionStatusChangeReason reason = ConnectionStatusChangeReason.CommunicationError;

            if (exception is IotHubClientException hubException)
            {
                if (hubException.IsTransient)
                {
                    if (retryAttemptsExhausted)
                    {
                        reason = ConnectionStatusChangeReason.RetryExpired;
                    }
                    else
                    {
                        status = ConnectionStatus.DisconnectedRetrying;
                    }
                }
                else if (hubException.ErrorCode is IotHubClientErrorCode.Unauthorized)
                {
                    reason = ConnectionStatusChangeReason.BadCredential;
                }
                else if (hubException.ErrorCode is IotHubClientErrorCode.DeviceNotFound)
                {
                    // The change reason of DeviceDisabled represents that the device has been deleted or marked
                    // as disabled in the IoT hub instance, which matches the error code of DeviceNotFound.
                    reason = ConnectionStatusChangeReason.DeviceDisabled;
                }
            }

            _onConnectionStatusChanged(new ConnectionStatusInfo(status, reason));
            if (Logging.IsEnabled)
                Logging.Info(
                    this,
                    $"Connection status change: status={status}, reason={reason}",
                    nameof(HandleConnectionStatusExceptions));
        }

        protected private override void Dispose(bool disposing)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_isDisposed}; disposing={disposing}", $"{nameof(RetryDelegatingHandler)}.{nameof(Dispose)}");

            try
            {
                if (!_isDisposed)
                {
                    base.Dispose(disposing);

                    if (disposing)
                    {
                        var cancellationTokenSources = new List<CancellationTokenSource>
                        {
                            _handleDisconnectCts,
                            _cancelPendingOperationsCts,
                        };

                        foreach (CancellationTokenSource cancellationTokenSource in cancellationTokenSources)
                        {
                            try
                            {
                                cancellationTokenSource?.Cancel();
                            }
                            catch (ObjectDisposedException)
                            {
                                if (Logging.IsEnabled)
                                    Logging.Error(this, $"Tried canceling the canccelation token source {cancellationTokenSource} but it has already been disposed by client disposal on a separate thread." +
                                        "Ignoring this exception and continuing with client cleanup.");
                            }
                        }

                        var disposables = new List<IDisposable>
                        {
                            _handleDisconnectCts,
                            _cancelPendingOperationsCts,
                            _clientOpenSemaphore,
                        };

                        foreach (IDisposable disposable in disposables)
                        {
                            try
                            {
                                disposable?.Dispose();
                            }
                            catch (ObjectDisposedException)
                            {
                                if (Logging.IsEnabled)
                                    Logging.Error(this, $"Tried disposing the IDisposable {disposable} but it has already been disposed by client disposal on a separate thread." +
                                        "Ignoring this exception and continuing with client cleanup.");
                            }
                        }
                    }
                    // the _disposed flag is inherited from the base class DefaultDelegatingHandler and is finally set to null there.
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_isDisposed}; disposing={disposing}", $"{nameof(RetryDelegatingHandler)}.{nameof(Dispose)}");
            }
        }
    }
}
