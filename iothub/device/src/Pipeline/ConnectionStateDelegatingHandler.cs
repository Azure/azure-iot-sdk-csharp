// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Handler to manage the state of the delegating handler pipeline.
    /// This handler also validates if the pipeline is in a state to execute the requested operation.
    /// </summary>
    internal sealed class ConnectionStateDelegatingHandler : DefaultDelegatingHandler
    {
        private readonly SemaphoreSlim _clientOpenSemaphore = new(1, 1);

        private readonly Action<ConnectionStatusInfo> _onConnectionStatusChanged;
        private readonly IIotHubClientRetryPolicy _retryPolicy;

        private readonly ClientTransportStateMachine _clientTransportStateMachine = new();
        private Task _transportClosedTask;
        private CancellationTokenSource _handleDisconnectCts;
        private CancellationTokenSource _cancelPendingOperationsCts;
        private volatile bool _wasOpened;

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
                Logging.Enter(this, _clientTransportStateMachine.GetCurrentState(), nameof(OpenAsync));

            try
            {
                switch (_clientTransportStateMachine.GetCurrentState())
                {
                    // If the pipeline is already open then no action is needed.
                    case ClientTransportState.Open:
                        return;

                    // If the pipeline is currently closing then the user needs to wait until it is closed before reopening it.
                    case ClientTransportState.Closing:
                        throw new InvalidOperationException($"The client is currently closing. To reopen the client wait until {nameof(CloseAsync)} completes" +
                            $" and then invoke {nameof(OpenAsync)}.");

                    // If the pipeline is opening then we continu with the operations to complete the open process. This state is reached when the client was previously connected but then lost connection.
                    // If the pipeline is closed then we proceed with opening the client.
                    case ClientTransportState.Opening:
                    case ClientTransportState.Closed:
                        {
                            if (_clientTransportStateMachine.GetCurrentState() == ClientTransportState.Closed)
                            {
                                _clientTransportStateMachine.MoveNext(ClientStateAction.OpenStart, ClientTransportState.Opening);
                            }

                            // Create a new cancellation token source that will be signaled by any subsequently invoked CloseAsync() for cancellation.
                            _cancelPendingOperationsCts = new CancellationTokenSource();
                            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                            // This semaphore is used to ensure two things - 
                            // 1. Only one thread tries to open the client at any given time.
                            // 2. Before executing an operation the pipeline will verify if it is open.
                            //    If the state is "opening" then it will wait on this semaphore. This will ensure that if any parallel thread is currently trying to open the connection then we wait until
                            //    it completes and then try the requested operation. This is helpful if the client is trying to execute an operation during a disconnection event.
                            //    If there is a parallel thread trying to reopen the client, the client will wait until that thread completes before checking if the requested operation can be carried out.
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

                                        _clientTransportStateMachine.MoveNext(ClientStateAction.OpenSuccess, ClientTransportState.Open);
                                        var connectionStatusInfo = new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk);
                                        _onConnectionStatusChanged(connectionStatusInfo);
                                        _wasOpened = true;

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
                                        if (!_wasOpened && _clientTransportStateMachine.GetCurrentState() == ClientTransportState.Opening)
                                        {
                                            _clientTransportStateMachine.MoveNext(ClientStateAction.OpenFailure, ClientTransportState.Closed);
                                        }

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
                    Logging.Exit(this, _clientTransportStateMachine.GetCurrentState(), nameof(OpenAsync));
            }
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(CloseAsync));

            try
            {
                if (_clientTransportStateMachine.GetCurrentState() == ClientTransportState.Closed
                    || _clientTransportStateMachine.GetCurrentState() == ClientTransportState.Closing)
                {
                    // Already closed or being closed by a parallel call so gracefully exit.
                    return;
                }

                // Transition to "closing" state before starting the work.
                _clientTransportStateMachine.MoveNext(ClientStateAction.CloseStart, ClientTransportState.Closing);

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

                    // Once the work has been completed transition to "closed" state.
                    _clientTransportStateMachine.MoveNext(ClientStateAction.CloseComplete, ClientTransportState.Closed);
                    _wasOpened = false;
                }
            }
            finally
            {
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
                await ValidateStateAndPerformOperationAsync(
                        (ct) => base.SendTelemetryAsync(message, ct),
                        nameof(SendTelemetryAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
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
                await ValidateStateAndPerformOperationAsync(
                        (ct) => base.SendTelemetryAsync(messages, ct),
                        nameof(SendTelemetryAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
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
                await ValidateStateAndPerformOperationAsync(
                        (ct) => base.EnableReceiveMessageAsync(ct),
                        nameof(EnableReceiveMessageAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
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
                await ValidateStateAndPerformOperationAsync(
                        (ct) => base.DisableReceiveMessageAsync(ct),
                        nameof(DisableReceiveMessageAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
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
                await ValidateStateAndPerformOperationAsync(
                        (ct) => base.EnableMethodsAsync(ct),
                        nameof(EnableMethodsAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
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
                await ValidateStateAndPerformOperationAsync(
                        (ct) => base.DisableMethodsAsync(ct),
                        nameof(DisableMethodsAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
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
                await ValidateStateAndPerformOperationAsync(
                        (ct) => base.SendMethodResponseAsync(method, ct),
                        nameof(SendMethodResponseAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
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
                await ValidateStateAndPerformOperationAsync(
                        (ct) => base.EnableTwinPatchAsync(ct),
                        nameof(EnableTwinPatchAsync),
                        cancellationToken)
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
                await ValidateStateAndPerformOperationAsync(
                        (ct) => base.DisableTwinPatchAsync(ct),
                        nameof(DisableTwinPatchAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
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
                return await ValidateStateAndPerformOperationAsync(
                        (ct) => base.GetTwinAsync(ct),
                        nameof(GetTwinAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
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
                return await ValidateStateAndPerformOperationAsync(
                        (ct) => base.UpdateReportedPropertiesAsync(reportedProperties, ct),
                        nameof(UpdateReportedPropertiesAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, reportedProperties, cancellationToken, nameof(UpdateReportedPropertiesAsync));
            }
        }

        public override async Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(FileUploadSasUriRequest request, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, request.BlobName, cancellationToken, nameof(GetFileUploadSasUriAsync));

            try
            {
                return await ValidateStateAndPerformOperationAsync(
                        (ct) => base.GetFileUploadSasUriAsync(request, ct),
                        nameof(GetFileUploadSasUriAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, request.BlobName, cancellationToken, nameof(GetFileUploadSasUriAsync));
            }
        }

        public override async Task CompleteFileUploadAsync(FileUploadCompletionNotification notification, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, notification.CorrelationId, cancellationToken, nameof(CompleteFileUploadAsync));

            try
            {
                await ValidateStateAndPerformOperationAsync(
                        (ct) => base.CompleteFileUploadAsync(notification, ct),
                        nameof(CompleteFileUploadAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, notification.CorrelationId, cancellationToken, nameof(CompleteFileUploadAsync));
            }
        }

        public override async Task<DirectMethodResponse> InvokeMethodAsync(DirectMethodRequest methodInvokeRequest, Uri uri, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, methodInvokeRequest.RequestId, uri, cancellationToken, nameof(InvokeMethodAsync));

            try
            {
                return await ValidateStateAndPerformOperationAsync(
                        (ct) => base.InvokeMethodAsync(methodInvokeRequest, uri, ct),
                        nameof(InvokeMethodAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, methodInvokeRequest.RequestId, uri, cancellationToken, nameof(InvokeMethodAsync));
            }
        }

        public override async Task<DateTime> RefreshSasTokenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(RefreshSasTokenAsync));

            try
            {
                return await ValidateStateAndPerformOperationAsync(
                        (ct) => base.RefreshSasTokenAsync(ct),
                        nameof(RefreshSasTokenAsync),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(RefreshSasTokenAsync));
            }
        }

        private Task ValidateStateAndPerformOperationAsync(Func<CancellationToken, Task> asyncOperation, string operationName, CancellationToken cancellationToken)
        {
            return ValidateStateAndPerformOperationAsync(async (ct) =>
            {
                await asyncOperation(ct).ConfigureAwait(false);
                return false;
            },
            operationName,
            cancellationToken);

        }

        private async Task<T> ValidateStateAndPerformOperationAsync<T>(Func<CancellationToken, Task<T>> asyncOperation, string operationName, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, operationName, cancellationToken, nameof(ValidateStateAndPerformOperationAsync));

            try
            {
                switch (_clientTransportStateMachine.GetCurrentState())
                {
                    case ClientTransportState.Opening:
                        {
                            // If the state is "opening" then we will wait until there is no other parallel thread trying to open the connection.
                            // We will wait on this semaphore using a linked cancellation token that will be canceled if CloseAsync() is called or if the client is disposed.
                            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                            await _clientOpenSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);

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
                            return await asyncOperation(operationCts.Token).ConfigureAwait(false);
                        }

                    case ClientTransportState.Open:
                        {
                            // If the state is "open" then we will create a linked cancellation token that will be canceled if CloseAsync() is called or if the client is disposed.
                            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);
                            return await asyncOperation(operationCts.Token).ConfigureAwait(false);
                        }

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

                    // This transitions the state to "opening" so that a subsequent OpenAsync() call can do the work to open it.
                    _clientTransportStateMachine.MoveNext(ClientStateAction.ConnectionLost, ClientTransportState.Opening);
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

                    // This state transition is to ensure that the pipeline state matches the client's state.
                    _clientTransportStateMachine.MoveNext(ClientStateAction.CloseStart, ClientTransportState.Closing);
                    _clientTransportStateMachine.MoveNext(ClientStateAction.CloseComplete, ClientTransportState.Closed);

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
