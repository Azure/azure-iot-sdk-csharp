// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class RetryDelegatingHandler : DefaultDelegatingHandler
    {
        // RetryCount is used for testing purpose and is equal to MaxValue in prod.
        private const uint RetryMaxCount = uint.MaxValue;

        private readonly RetryHandler _internalRetryHandler;
        private IIotHubClientRetryPolicy _retryPolicy;

        private bool _isOpen;
        private SemaphoreSlim _handlerSemaphore = new(1, 1);
        private bool _methodsEnabled;
        private bool _twinEnabled;
        private bool _deviceReceiveMessageEnabled;

        private Task _transportClosedTask;
        private CancellationTokenSource _handleDisconnectCts;

        private readonly Action<ConnectionStatusInfo> _onConnectionStatusChanged;

        private CancellationTokenSource _loopCancellationTokenSource;
        private Task _refreshLoop;

        internal RetryDelegatingHandler(PipelineContext context, IDelegatingHandler innerHandler)
            : base(context, innerHandler)
        {
            _retryPolicy = context.RetryPolicy;
            _internalRetryHandler = new RetryHandler(_retryPolicy);

            _onConnectionStatusChanged = context.ConnectionStatusChangeHandler;

            if (Logging.IsEnabled)
                Logging.Associate(this, _internalRetryHandler, nameof(RetryDelegatingHandler));
        }

        internal void SetRetryPolicy(IIotHubClientRetryPolicy retryPolicy)
        {
            _retryPolicy = retryPolicy;
            _internalRetryHandler.SetRetryPolicy(_retryPolicy);

            if (Logging.IsEnabled)
                Logging.Associate(this, _internalRetryHandler, nameof(SetRetryPolicy));
        }

        public override async Task SendTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, cancellationToken, nameof(SendTelemetryAsync));

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(cancellationToken).ConfigureAwait(false);
                            await base.SendTelemetryAsync(message, cancellationToken).ConfigureAwait(false);
                        },
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
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(cancellationToken).ConfigureAwait(false);
                            await base.SendTelemetryAsync(messages, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, messages, cancellationToken, nameof(SendTelemetryAsync));
            }
        }

        public override async Task SendMethodResponseAsync(DirectMethodResponse method, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, method, cancellationToken, nameof(SendMethodResponseAsync));

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(cancellationToken).ConfigureAwait(false);
                            await base.SendMethodResponseAsync(method, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, method, cancellationToken, nameof(SendMethodResponseAsync));
            }
        }

        public override async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(EnableReceiveMessageAsync));

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(cancellationToken).ConfigureAwait(false);
                            // Wait to acquire the _handlerSemaphore. This ensures that concurrently invoked API calls are invoked in a thread-safe manner.
                            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                // The telemetry downlink needs to be enabled only for the first time that the callback is set.
                                Debug.Assert(!_deviceReceiveMessageEnabled);
                                await base.EnableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                                _deviceReceiveMessageEnabled = true;
                            }
                            finally
                            {
                                _handlerSemaphore?.Release();
                            }
                        },
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
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(cancellationToken).ConfigureAwait(false);
                            // Wait to acquire the _handlerSemaphore. This ensures that concurrently invoked API calls are invoked in a thread-safe manner.
                            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                // Ensure that a callback for receiving messages has been previously set.
                                Debug.Assert(_deviceReceiveMessageEnabled);
                                await base.DisableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                                _deviceReceiveMessageEnabled = false;
                            }
                            finally
                            {
                                _handlerSemaphore?.Release();
                            }
                        },
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
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(cancellationToken).ConfigureAwait(false);
                            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(!_methodsEnabled);
                                await base.EnableMethodsAsync(cancellationToken).ConfigureAwait(false);
                                _methodsEnabled = true;
                            }
                            finally
                            {
                                _handlerSemaphore?.Release();
                            }
                        },
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
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(cancellationToken).ConfigureAwait(false);
                            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(_methodsEnabled);
                                await base.DisableMethodsAsync(cancellationToken).ConfigureAwait(false);
                                _methodsEnabled = false;
                            }
                            finally
                            {
                                _handlerSemaphore?.Release();
                            }
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(DisableMethodsAsync));
            }
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(EnableTwinPatchAsync));

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(cancellationToken).ConfigureAwait(false);
                            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(!_twinEnabled);
                                await base.EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                                _twinEnabled = true;
                            }
                            finally
                            {
                                _handlerSemaphore?.Release();
                            }
                        },
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
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(cancellationToken).ConfigureAwait(false);
                            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(_twinEnabled);
                                await base.DisableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                                _twinEnabled = false;
                            }
                            finally
                            {
                                _handlerSemaphore?.Release();
                            }
                        },
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
                return await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(cancellationToken).ConfigureAwait(false);
                            return await base.GetTwinAsync(cancellationToken).ConfigureAwait(false);
                        },
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
                return await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(cancellationToken).ConfigureAwait(false);
                            return await base.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, reportedProperties, cancellationToken, nameof(UpdateReportedPropertiesAsync));
            }
        }

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            // If this object has already been disposed, we will throw an exception indicating that.
            // This is the entry point for interacting with the client and this safety check should be done here.
            // The current behavior does not support open->close->open
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(RetryDelegatingHandler));
            }

            if (_isOpen)
            {
                return;
            }

            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!_isOpen)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Opening connection", nameof(OpenAsync));

                    // This is to ensure that if OpenInternalAsync() fails on retry expiration with a custom retry policy,
                    // we are returning the corresponding connection status change event => disconnected: retry_expired.
                    try
                    {
                        await OpenInternalAsync(true, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!Fx.IsFatal(ex))
                    {
                        HandleConnectionStatusExceptions(ex, true);
                        throw;
                    }

                    if (!_isDisposed)
                    {
                        _isOpen = true;

                        // Send up the handler for handling transport disconnection event.
                        _transportClosedTask = HandleDisconnectAsync();
                    }
                    else
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, "Race condition: Disposed during opening.", nameof(OpenAsync));

                        _handleDisconnectCts?.Cancel();
                        _handleDisconnectCts?.Dispose();
                        _handleDisconnectCts = null;
                    }
                }
            }
            finally
            {
                _handlerSemaphore?.Release();
            }
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(CloseAsync));

            if (!_isOpen)
            {
                // Already closed so gracefully exit, instead of throw.
                return;
            }

            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!_isOpen)
                {
                    // Already closed so gracefully exit, instead of throw.
                    return;
                }

                _handleDisconnectCts?.Cancel();
                _handleDisconnectCts?.Dispose();
                _handleDisconnectCts = null;

                await base.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _isOpen = false;

                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(CloseAsync));

                _handlerSemaphore?.Release();
            }
        }

        public override async Task<DateTime> RefreshSasTokenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(RefreshSasTokenAsync));

            try
            {
                return await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(cancellationToken).ConfigureAwait(false);
                            return await base.RefreshSasTokenAsync(cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(RefreshSasTokenAsync));
            }
        }

        public override void SetSasTokenRefreshesOn()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(SetSasTokenRefreshesOn));

            if (_refreshLoop == null)
            {
                if (_loopCancellationTokenSource != null)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "_loopCancellationTokenSource was already initialized, which was unexpected. Canceling and disposing the previous instance.", nameof(SetSasTokenRefreshesOn));

                    try
                    {
                        _loopCancellationTokenSource.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    _loopCancellationTokenSource.Dispose();
                }
                _loopCancellationTokenSource = new CancellationTokenSource();

                DateTime refreshesOn = GetSasTokenRefreshesOn();
                if (refreshesOn < DateTime.MaxValue)
                {
                    StartSasTokenLoop(refreshesOn, _loopCancellationTokenSource.Token);
                }
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(SetSasTokenRefreshesOn));
        }

        public override async Task StopSasTokenLoopAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(StopSasTokenLoopAsync));

            try
            {
                try
                {
                    _loopCancellationTokenSource?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, "The cancellation token source has already been canceled and disposed", nameof(StopSasTokenLoopAsync));
                }

                // Await the completion of _refreshLoop.
                // This will ensure that when StopLoopAsync has been exited then no more token refresh attempts are in-progress.
                if (_refreshLoop != null)
                {
                    await _refreshLoop.ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Caught exception when stopping token refresh loop: {ex}");
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(StopSasTokenLoopAsync));
            }
        }

        private void StartSasTokenLoop(DateTime refreshesOn, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, refreshesOn, nameof(StartSasTokenLoop));

            // This task runs in the background and is unmonitored.
            // When this refresher is disposed it signals this task to be cancelled.
            _refreshLoop = RefreshSasTokenLoopAsync(refreshesOn, cancellationToken);

            if (Logging.IsEnabled)
                Logging.Exit(this, refreshesOn, nameof(StartSasTokenLoop));
        }

        private async Task RefreshSasTokenLoopAsync(DateTime refreshesOn, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, refreshesOn, nameof(RefreshSasTokenLoopAsync));

            try
            {
                TimeSpan waitTime = refreshesOn - DateTime.UtcNow;

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, refreshesOn, $"Before {nameof(RefreshSasTokenLoopAsync)} with wait time {waitTime}.");

                    if (waitTime > TimeSpan.Zero)
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, refreshesOn, $"Token refreshes after {waitTime} {nameof(RefreshSasTokenLoopAsync)}.");

                        await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
                    }

                    refreshesOn = await RefreshSasTokenAsync(cancellationToken).ConfigureAwait(false);

                    waitTime = refreshesOn - DateTime.UtcNow;

                    if (Logging.IsEnabled)
                        Logging.Info(this, refreshesOn, $"Token has been refreshed; valid until {refreshesOn}.");
                }
            }
            // OperationCanceledException can be thrown when the connection is closing or the cancellationToken is signaled
            catch (OperationCanceledException) { return; }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, refreshesOn, nameof(RefreshSasTokenLoopAsync));
            }
        }

        private async Task VerifyIsOpenAsync(CancellationToken cancellationToken)
        {
            await _handlerSemaphore.WaitAsync(cancellationToken);

            try
            {
                if (!_isOpen)
                {
                    throw new InvalidOperationException($"The client connection must be opened before operations can begin. Call '{nameof(OpenAsync)}' and try again.");
                }
            }
            finally
            {
                _handlerSemaphore.Release();
            }
        }

        private async Task OpenInternalAsync(bool recoverSubscriptions, CancellationToken cancellationToken)
        {
            var connectionStatusInfo = new ConnectionStatusInfo();

            _handleDisconnectCts ??= new CancellationTokenSource();

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        if (Logging.IsEnabled)
                            Logging.Enter(this, cancellationToken, nameof(OpenAsync));

                        try
                        {
                            // Will throw on error.
                            await base.OpenAsync(cancellationToken).ConfigureAwait(false);

                            if (recoverSubscriptions)
                            {

                                if (Logging.IsEnabled)
                                    Logging.Info(this, "Attempting to recover subscriptions.", nameof(HandleDisconnectAsync));

                                var tasks = new List<Task>(3);

                                // This is to ensure that, if previously enabled, the callback to receive direct methods is recovered.
                                if (_methodsEnabled)
                                {
                                    tasks.Add(base.EnableMethodsAsync(cancellationToken));
                                }

                                // This is to ensure that, if previously enabled, the callback to receive twin properties is recovered.
                                if (_twinEnabled)
                                {
                                    tasks.Add(base.EnableTwinPatchAsync(cancellationToken));
                                }

                                // This is to ensure that, if previously enabled, the callback to receive C2D messages is recovered.
                                if (_deviceReceiveMessageEnabled)
                                {
                                    tasks.Add(base.EnableReceiveMessageAsync(cancellationToken));
                                }

                                if (tasks.Any())
                                {
                                    await Task.WhenAll(tasks).ConfigureAwait(false);
                                }

                                if (Logging.IsEnabled)
                                    Logging.Info(this, "Subscriptions recovered.", nameof(HandleDisconnectAsync));
                            }

                            _isOpen = true;
                            connectionStatusInfo = new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk);
                            _onConnectionStatusChanged(connectionStatusInfo);
                        }
                        catch (Exception ex) when (!Fx.IsFatal(ex))
                        {
                            HandleConnectionStatusExceptions(ex);
                            throw;
                        }
                        finally
                        {
                            if (Logging.IsEnabled)
                                Logging.Exit(this, cancellationToken, nameof(OpenAsync));
                        }
                    },
                    cancellationToken).ConfigureAwait(false);
        }

        // Triggered from connection loss event
        private async Task HandleDisconnectAsync()
        {
            var connectionStatusInfo = new ConnectionStatusInfo();

            if (_isDisposed)
            {
                if (Logging.IsEnabled)
                    Logging.Info(this, "Disposed during disconnection.", nameof(HandleDisconnectAsync));

                _handleDisconnectCts?.Cancel();
                _handleDisconnectCts?.Dispose();
                _handleDisconnectCts = null;
            }

            try
            {
                // No timeout on connection being established.
                await WaitForTransportClosedAsync().ConfigureAwait(false);
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

            if (Logging.IsEnabled)
                Logging.Info(this, "Transport disconnected: unexpected.", nameof(HandleDisconnectAsync));

            await _handlerSemaphore.WaitAsync().ConfigureAwait(false);
            _isOpen = false;

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
                await OpenInternalAsync(true, cancellationToken).ConfigureAwait(false);

                if (!_isDisposed)
                {
                    _isOpen = true;

                    // Send up the handler for handling transport disconnection event.
                    _transportClosedTask = HandleDisconnectAsync();
                }
                else
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Race condition: Disposed during opening.", nameof(OpenAsync));

                    _handleDisconnectCts?.Cancel();
                    _handleDisconnectCts?.Dispose();
                    _handleDisconnectCts = null;
                }
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, ex.ToString(), nameof(HandleDisconnectAsync));

                HandleConnectionStatusExceptions(ex, true);
            }
            finally
            {
                _handlerSemaphore?.Release();
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

            ConnectionStatusChangeReason reason = ConnectionStatusChangeReason.CommunicationError;
            ConnectionStatus status = ConnectionStatus.Disconnected;

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

                    _isOpen = false;

                    if (disposing)
                    {
                        _handleDisconnectCts?.Cancel();
                        _handleDisconnectCts?.Dispose();
                        _handleDisconnectCts = null;

                        _loopCancellationTokenSource?.Dispose();
                        if (_handlerSemaphore != null && _handlerSemaphore.CurrentCount == 0)
                        {
                            _handlerSemaphore.Release();
                        }
                        _handlerSemaphore?.Dispose();
                        _handlerSemaphore = null;
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
