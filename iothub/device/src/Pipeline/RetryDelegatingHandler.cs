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
    internal enum ClientTransportStatus
    {
        Closed = 0,
        Open = 1,
    }

    internal sealed class RetryDelegatingHandler : DefaultDelegatingHandler
    {
        // RetryCount is used for testing purpose and is equal to MaxValue in prod.
        private const uint RetryMaxCount = uint.MaxValue;

        private readonly SemaphoreSlim _clientOpenSemaphore = new(1, 1);
        private readonly SemaphoreSlim _cloudToDeviceMessageSubscriptionSemaphore = new(1, 1);
        private readonly SemaphoreSlim _directMethodSubscriptionSemaphore = new(1, 1);
        private readonly SemaphoreSlim _twinEventsSubscriptionSemaphore = new(1, 1);

        private readonly RetryHandler _internalRetryHandler;
        private IIotHubClientRetryPolicy _retryPolicy;

        private bool _methodsEnabled;
        private bool _twinEnabled;
        private bool _deviceReceiveMessageEnabled;
        private bool _isDisposing;
        private long _clientTransportStatus; // references the current client transport status as the int value of ClientTransportStatus

        private Task _transportClosedTask;
        private readonly CancellationTokenSource _handleDisconnectCts = new();
        private readonly Action<ConnectionStatusInfo> _onConnectionStatusChanged;

        private CancellationTokenSource _cancelPendingOperationsCts;
        private CancellationTokenSource _sasRefreshLoopCancellationCts;
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

            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(operationCts.Token).ConfigureAwait(false);
                            await base.SendTelemetryAsync(message, operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message, cancellationToken, nameof(SendTelemetryAsync));
            }
        }

        public override async Task SendTelemetryBatchAsync(IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, messages, cancellationToken, nameof(SendTelemetryAsync));

            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(operationCts.Token).ConfigureAwait(false);
                            await base.SendTelemetryBatchAsync(messages, operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
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

            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(operationCts.Token).ConfigureAwait(false);
                            await base.SendMethodResponseAsync(method, operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
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

            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(operationCts.Token).ConfigureAwait(false);
                            await _cloudToDeviceMessageSubscriptionSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(!_deviceReceiveMessageEnabled);
                                await base.EnableReceiveMessageAsync(operationCts.Token).ConfigureAwait(false);
                                _deviceReceiveMessageEnabled = true;
                            }
                            finally
                            {
                                try
                                {
                                    _cloudToDeviceMessageSubscriptionSemaphore?.Release();
                                }
                                catch (ObjectDisposedException) when (_isDisposing)
                                {
                                    if (Logging.IsEnabled)
                                        Logging.Error(this, "Tried releasing cloud-to-device message subscription semaphore but it has already been disposed by client disposal on a separate thread." +
                                            "Ignoring this exception and continuing with client cleanup.");
                                }
                            }
                        },
                        operationCts.Token)
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

            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(operationCts.Token).ConfigureAwait(false);
                            await _cloudToDeviceMessageSubscriptionSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(_deviceReceiveMessageEnabled);
                                await base.DisableReceiveMessageAsync(operationCts.Token).ConfigureAwait(false);
                                _deviceReceiveMessageEnabled = false;
                            }
                            finally
                            {
                                try
                                {
                                    _cloudToDeviceMessageSubscriptionSemaphore?.Release();
                                }
                                catch (ObjectDisposedException) when (_isDisposing)
                                {
                                    if (Logging.IsEnabled)
                                        Logging.Error(this, "Tried releasing cloud-to-device message subscription semaphore but it has already been disposed by client disposal on a separate thread." +
                                            "Ignoring this exception and continuing with client cleanup.");
                                }
                            }
                        },
                        operationCts.Token)
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

            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(operationCts.Token).ConfigureAwait(false);
                            await _directMethodSubscriptionSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(!_methodsEnabled);
                                await base.EnableMethodsAsync(operationCts.Token).ConfigureAwait(false);
                                _methodsEnabled = true;
                            }
                            finally
                            {
                                try
                                {
                                    _directMethodSubscriptionSemaphore?.Release();
                                }
                                catch (ObjectDisposedException) when (_isDisposing)
                                {
                                    if (Logging.IsEnabled)
                                        Logging.Error(this, "Tried releasing direct method subscription semaphore but it has already been disposed by client disposal on a separate thread." +
                                            "Ignoring this exception and continuing with client cleanup.");
                                }
                            }
                        },
                        operationCts.Token)
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

            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(operationCts.Token).ConfigureAwait(false);
                            await _directMethodSubscriptionSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(_methodsEnabled);
                                await base.DisableMethodsAsync(operationCts.Token).ConfigureAwait(false);
                                _methodsEnabled = false;
                            }
                            finally
                            {
                                try
                                {
                                    _directMethodSubscriptionSemaphore?.Release();
                                }
                                catch (ObjectDisposedException) when (_isDisposing)
                                {
                                    if (Logging.IsEnabled)
                                        Logging.Error(this, "Tried releasing direct method subscription semaphore but it has already been disposed by client disposal on a separate thread." +
                                            "Ignoring this exception and continuing with client cleanup.");
                                }
                            }
                        },
                        operationCts.Token)
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

            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(operationCts.Token).ConfigureAwait(false);
                            await _twinEventsSubscriptionSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(!_twinEnabled);
                                await base.EnableTwinPatchAsync(operationCts.Token).ConfigureAwait(false);
                                _twinEnabled = true;
                            }
                            finally
                            {
                                try
                                {
                                    _twinEventsSubscriptionSemaphore?.Release();
                                }
                                catch (ObjectDisposedException) when (_isDisposing)
                                {
                                    if (Logging.IsEnabled)
                                        Logging.Error(this, "Tried releasing twin event subscription semaphore but it has already been disposed by client disposal on a separate thread." +
                                            "Ignoring this exception and continuing with client cleanup.");
                                }
                            }
                        },
                        operationCts.Token)
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

            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            try
            {
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(operationCts.Token).ConfigureAwait(false);
                            await _twinEventsSubscriptionSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(_twinEnabled);
                                await base.DisableTwinPatchAsync(operationCts.Token).ConfigureAwait(false);
                                _twinEnabled = false;
                            }
                            finally
                            {
                                try
                                {
                                    _twinEventsSubscriptionSemaphore?.Release();
                                }
                                catch (ObjectDisposedException) when (_isDisposing)
                                {
                                    if (Logging.IsEnabled)
                                        Logging.Error(this, "Tried releasing twin event subscription semaphore but it has already been disposed by client disposal on a separate thread." +
                                            "Ignoring this exception and continuing with client cleanup.");
                                }
                            }
                        },
                        operationCts.Token)
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

            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            try
            {
                return await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(operationCts.Token).ConfigureAwait(false);
                            return await base.GetTwinAsync(operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
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

            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            try
            {
                return await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await VerifyIsOpenAsync(operationCts.Token).ConfigureAwait(false);
                            return await base.UpdateReportedPropertiesAsync(reportedProperties, operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
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


            if (GetClientTransportStatus() == ClientTransportStatus.Open)
            {
                return;
            }

            if (GetClientTransportStatus() == ClientTransportStatus.Closed)
            {
                // Create a new cancellation token source that will be signaled by CloseAsync() for cancellation.
                _cancelPendingOperationsCts = new CancellationTokenSource();
            }
            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            await _clientOpenSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
            try
            {
                if (GetClientTransportStatus() == ClientTransportStatus.Closed)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Opening connection", nameof(OpenAsync));

                    // This is to ensure that if OpenInternalAsync() fails on retry expiration with a custom retry policy,
                    // we are returning the corresponding connection status change event => disconnected: retry_expired.
                    try
                    {
                        await OpenInternalAsync(operationCts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!Fx.IsFatal(ex))
                    {
                        HandleConnectionStatusExceptions(ex, true);
                        throw;
                    }

                    if (!_isDisposed)
                    {
                        SetClientTransportStatus(ClientTransportStatus.Open);

                        // Send the request for transport close notification.
                        _transportClosedTask = HandleDisconnectAsync();
                    }
                    else
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, "Race condition: Disposed during opening.", nameof(OpenAsync));

                        _handleDisconnectCts.Cancel();
                    }
                }
            }
            finally
            {
                _clientOpenSemaphore?.Release();
            }
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(CloseAsync));

            if (GetClientTransportStatus() == ClientTransportStatus.Closed)
            {
                // Already closed so gracefully exit, instead of throw.
                return;
            }

            try
            {
                _handleDisconnectCts.Cancel();
                _cancelPendingOperationsCts.Cancel();
                await base.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                SetClientTransportStatus(ClientTransportStatus.Closed);
                _cancelPendingOperationsCts.Dispose();
                _cancelPendingOperationsCts = null;

                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(CloseAsync));
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
                if (_sasRefreshLoopCancellationCts != null)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "_loopCancellationTokenSource was already initialized, which was unexpected. Canceling and disposing the previous instance.", nameof(SetSasTokenRefreshesOn));

                    try
                    {
                        _sasRefreshLoopCancellationCts.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    _sasRefreshLoopCancellationCts.Dispose();
                }
                _sasRefreshLoopCancellationCts = new CancellationTokenSource();

                DateTime refreshesOn = GetSasTokenRefreshesOn();
                if (refreshesOn < DateTime.MaxValue)
                {
                    StartSasTokenLoop(refreshesOn, _sasRefreshLoopCancellationCts.Token);
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
                    _sasRefreshLoopCancellationCts?.Cancel();
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
            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            await _clientOpenSemaphore.WaitAsync(operationCts.Token);

            try
            {
                if (GetClientTransportStatus() == ClientTransportStatus.Closed)
                {
                    throw new InvalidOperationException($"The client connection must be opened before operations can begin. Call '{nameof(OpenAsync)}' and try again.");
                }
            }
            finally
            {
                try
                {
                    _clientOpenSemaphore?.Release();
                }
                catch (ObjectDisposedException) when (_isDisposing)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, "Tried releasing client open semaphore but it has already been disposed by client disposal on a separate thread." +
                            "Ignoring this exception and continuing with client cleanup.");
                }
            }
        }

        private async Task OpenInternalAsync(CancellationToken cancellationToken)
        {
            var connectionStatusInfo = new ConnectionStatusInfo();
            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        if (Logging.IsEnabled)
                            Logging.Enter(this, cancellationToken, nameof(OpenAsync));

                        try
                        {
                            // Will throw on error.
                            await base.OpenAsync(operationCts.Token).ConfigureAwait(false);

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
                    operationCts.Token).ConfigureAwait(false);
        }

        // Triggered from connection loss event
        private async Task HandleDisconnectAsync()
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
                await _internalRetryHandler.RunWithRetryAsync(async () =>
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Attempting to recover subscriptions.", nameof(HandleDisconnectAsync));

                    await base.OpenAsync(cancellationToken).ConfigureAwait(false);

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

                    // Send the request for transport close notification.
                    _transportClosedTask = HandleDisconnectAsync();

                    SetClientTransportStatus(ClientTransportStatus.Open);
                    connectionStatusInfo = new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk);
                    _onConnectionStatusChanged(connectionStatusInfo);

                    if (Logging.IsEnabled)
                        Logging.Info(this, "Subscriptions recovered.", nameof(HandleDisconnectAsync));
                },
                cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, ex.ToString(), nameof(HandleDisconnectAsync));

                HandleConnectionStatusExceptions(ex, true);
            }
            finally
            {
                _clientOpenSemaphore?.Release();
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
                    _isDisposing = true;

                    base.Dispose(disposing);
                    SetClientTransportStatus(ClientTransportStatus.Closed);

                    if (disposing)
                    {
                        _handleDisconnectCts?.Cancel();
                        _cancelPendingOperationsCts?.Cancel();

                        var disposables = new List<IDisposable>
                        {
                            _handleDisconnectCts,
                            _cancelPendingOperationsCts,
                            _sasRefreshLoopCancellationCts,
                            _cloudToDeviceMessageSubscriptionSemaphore,
                            _directMethodSubscriptionSemaphore,
                            _twinEventsSubscriptionSemaphore,
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
