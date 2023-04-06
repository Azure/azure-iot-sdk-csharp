// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class ConnectionStatusHandler : DefaultDelegatingHandler
    {

        private readonly SemaphoreSlim _clientOpenSemaphore = new(1, 1);

        private readonly CancellationTokenSource _handleDisconnectCts = new();
        private readonly Action<ConnectionStatusInfo> _onConnectionStatusChanged;
        private readonly IIotHubClientRetryPolicy _retryPolicy;

        private Task _transportClosedTask;
        private CancellationTokenSource _cancelPendingOperationsCts;
        private long _clientTransportStatus; // references the current client transport status as the int value of ClientTransportStatus

        internal ConnectionStatusHandler(PipelineContext context, IDelegatingHandler innerHandler)
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
                switch (GetClientTransportStatus())
                {
                    case ClientTransportStatus.Opening:
                    case ClientTransportStatus.Open:
                        return;

                    case ClientTransportStatus.Closing:
                        throw new InvalidOperationException($"The client is currently closing. To reopen the client wait until {nameof(CloseAsync)} completes" +
                            $" and then invoke {nameof(OpenAsync)}.");

                    case ClientTransportStatus.Closed:
                        {
                            SetClientTransportStatus(ClientTransportStatus.Opening);

                            // Create a new cancellation token source that will be signaled by any subsequently invoked CloseAsync() for cancellation.
                            _cancelPendingOperationsCts = new CancellationTokenSource();

                            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                            await _clientOpenSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
                            try
                            {
                                if (GetClientTransportStatus() == ClientTransportStatus.Opening)
                                {
                                    if (Logging.IsEnabled)
                                        Logging.Info(this, "Opening connection", nameof(OpenAsync));

                                    // This is to ensure that if OpenInternalAsync() fails on retry expiration with a custom retry policy,
                                    // we are returning the corresponding connection status change event => disconnected: retry_expired.
                                    try
                                    {
                                        await base.OpenAsync(operationCts.Token).ConfigureAwait(false);

                                        SetClientTransportStatus(ClientTransportStatus.Open);
                                        var connectionStatusInfo = new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk);
                                        _onConnectionStatusChanged(connectionStatusInfo);

                                        // Send the request for transport close notification.
                                        _transportClosedTask = HandleDisconnectAsync();
                                    }
                                    catch (Exception ex) when (!Fx.IsFatal(ex))
                                    {
                                        if (Logging.IsEnabled)
                                            Logging.Error(this, ex, nameof(HandleDisconnectAsync));

                                        HandleConnectionStatusExceptions(ex, true);
                                        SetClientTransportStatus(ClientTransportStatus.Closed);

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

                switch (GetClientTransportStatus())
                {
                    case ClientTransportStatus.Opening:
                        await _clientOpenSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);

                        try
                        {
                            if (GetClientTransportStatus() != ClientTransportStatus.Open)
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

                    case ClientTransportStatus.Open:
                        await base.SendTelemetryAsync(message, operationCts.Token).ConfigureAwait(false);
                        break;

                    case ClientTransportStatus.Closing:
                        throw new InvalidOperationException($"The client is currently closing. To send telemetry wait until {nameof(CloseAsync)} completes" +
                            $" and then invoke {nameof(OpenAsync)} and try again.");

                    case ClientTransportStatus.Closed:
                        throw new InvalidOperationException($"The client is currently closed. To send telemetry reopen the client using {nameof(OpenAsync)} and try again.");
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

            if (GetClientTransportStatus() == ClientTransportStatus.Closed)
            {
                // Already closed so gracefully exit.
                return;
            }

            SetClientTransportStatus(ClientTransportStatus.Closing);

            try
            {
                _cancelPendingOperationsCts?.Cancel();
                _handleDisconnectCts?.Cancel();

                await base.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                SetClientTransportStatus(ClientTransportStatus.Closed);

                _cancelPendingOperationsCts?.Dispose();
                _handleDisconnectCts?.Dispose();
            }
        }

        // Triggered from connection loss event
        // This method is set up for SDK internal retry attempts. We invoke this task as soon as the client is opened.
        // WaitForTransportClosedAsync() waits for the signal that the transport layer has been closed or disconnected.
        // Once disconnected, either gracefully or ungracefully, the SDK begins its reconnection attempt.
        // This reconnection attempt consults the retry policy supplied to infer if a reconnection attempt should be made.
        // These reconnection attempts are monitored through the _handleDisconnectCts cancellation token source.
        // This cancellation token source is set to be canceled when either CloseAsync() or Dispose() is invoked.
        private async Task HandleDisconnectAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(HandleDisconnectAsync));

            try
            {
                var connectionStatusInfo = new ConnectionStatusInfo();

                if (_isDisposed)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Disposed during disconnection.", nameof(HandleDisconnectAsync));

                    _handleDisconnectCts.Cancel();
                }

                try
                {
                    // This waits for a disconnection event to be signaled by the transport layer
                    await WaitForTransportClosedAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Canceled when the transport is being closed by the application.
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Transport disconnected: closed by application.", nameof(HandleDisconnectAsync));

                    SetClientTransportStatus(ClientTransportStatus.Closed);
                    connectionStatusInfo = new ConnectionStatusInfo(ConnectionStatus.Closed, ConnectionStatusChangeReason.ClientClosed);
                    _onConnectionStatusChanged(connectionStatusInfo);

                    return;
                }

                if (Logging.IsEnabled)
                    Logging.Info(this, "Transport disconnected: unexpected.", nameof(HandleDisconnectAsync));

                await _clientOpenSemaphore.WaitAsync().ConfigureAwait(false);
                SetClientTransportStatus(ClientTransportStatus.Closed);

                try
                {
                    // This is used to ensure that when IotHubServiceNoRetry() policy is enabled, we should not be retrying.
                    // This exception is not returned to the user.
                    var networkException = new IotHubClientException("", IotHubClientErrorCode.NetworkErrors);

                    if (!_retryPolicy.ShouldRetry(0, networkException, out TimeSpan delay))
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, "Transport disconnected: closed by application.", nameof(HandleDisconnectAsync));

                        connectionStatusInfo = new ConnectionStatusInfo(ConnectionStatus.Disconnected, ConnectionStatusChangeReason.RetryExpired);
                        _onConnectionStatusChanged(connectionStatusInfo);
                        return;
                    }

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay).ConfigureAwait(false);
                    }

                    // always reconnect.
                    connectionStatusInfo = new ConnectionStatusInfo(ConnectionStatus.DisconnectedRetrying, ConnectionStatusChangeReason.CommunicationError);
                    _onConnectionStatusChanged(connectionStatusInfo);
                    CancellationToken cancellationToken = _handleDisconnectCts.Token;

                    // This will recover to the status before the disconnect.
                    await OpenAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _clientOpenSemaphore?.Release();
                }
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

        private ClientTransportStatus GetClientTransportStatus()
        {
            return (ClientTransportStatus)Interlocked.Read(ref _clientTransportStatus);
        }

        private void SetClientTransportStatus(ClientTransportStatus clientTransportStatus)
        {
            _ = Interlocked.Exchange(ref _clientTransportStatus, (int)clientTransportStatus);
        }

        protected private override void Dispose(bool disposing)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_isDisposed}; disposing={disposing}", $"{nameof(RetryDelegatingHandler)}.{nameof(Dispose)}");

            try
            {
                if (!_isDisposed)
                {
                    if (GetClientTransportStatus() != ClientTransportStatus.Closed)
                    {
                        SetClientTransportStatus(ClientTransportStatus.Closing);
                    }
                    base.Dispose(disposing);

                    if (disposing)
                    {
                        _handleDisconnectCts?.Cancel();
                        _cancelPendingOperationsCts?.Cancel();

                        var disposables = new List<IDisposable>
                        {
                            _handleDisconnectCts,
                            _cancelPendingOperationsCts,
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
                SetClientTransportStatus(ClientTransportStatus.Closed);

                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_isDisposed}; disposing={disposing}", $"{nameof(RetryDelegatingHandler)}.{nameof(Dispose)}");
            }
        }
    }
}
