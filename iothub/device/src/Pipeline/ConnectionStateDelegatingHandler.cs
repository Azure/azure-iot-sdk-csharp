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
                            //SetClientTransportStatus(ClientTransportState.Opening);

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

                                    // This is to ensure that if OpenInternalAsync() fails on retry expiration with a custom retry policy,
                                    // we are returning the corresponding connection status change event => disconnected: retry_expired.
                                    try
                                    {
                                        await base.OpenAsync(operationCts.Token).ConfigureAwait(false);

                                        _clientTransportStateMachine.MoveNext(ClientStateAction.OpenSuccess);
                                        //SetClientTransportStatus(ClientTransportState.Open);
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
                                        //SetClientTransportStatus(ClientTransportState.Closed);

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

        public override async Task SendTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, cancellationToken, nameof(SendTelemetryAsync));

            try
            {
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                switch (_clientTransportStateMachine.GetCurrentState())
                {
                    case ClientTransportState.Opening:
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
                        await base.SendTelemetryAsync(message, operationCts.Token).ConfigureAwait(false);
                        break;

                    case ClientTransportState.Open:
                        await base.SendTelemetryAsync(message, operationCts.Token).ConfigureAwait(false);
                        break;

                    case ClientTransportState.Closing:
                        throw new InvalidOperationException($"The client is currently closing. Wait until {nameof(CloseAsync)} completes" +
                            $" and then invoke {nameof(OpenAsync)} and try the operation again.");

                    case ClientTransportState.Closed:
                        throw new InvalidOperationException($"The client is currently closed. Reopen the client using {nameof(OpenAsync)} and try the operation again.");
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message, cancellationToken, nameof(SendTelemetryAsync));
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
            //SetClientTransportStatus(ClientTransportState.Closing);

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
                //SetClientTransportStatus(ClientTransportState.Closed);
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
                    //SetClientTransportStatus(ClientTransportState.Closed);
                }
                catch (OperationCanceledException)
                {
                    // Canceled when the transport is being closed by the application.
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Transport disconnected: closed by application.", nameof(HandleDisconnectAsync));

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
