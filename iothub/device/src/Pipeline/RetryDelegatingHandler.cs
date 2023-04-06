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

        private bool _recoverSubscriptions;

        private volatile bool _isOpen;
        private readonly SemaphoreSlim _clientOpenCloseSemaphore = new(1, 1);
        private readonly SemaphoreSlim _cloudToDeviceMessageSubscriptionSemaphore = new(1, 1);
        private readonly SemaphoreSlim _directMethodSubscriptionSemaphore = new(1, 1);
        private readonly SemaphoreSlim _twinEventsSubscriptionSemaphore = new(1, 1);

        private bool _methodsEnabled;
        private bool _twinEnabled;
        private bool _deviceReceiveMessageEnabled;

        private CancellationTokenSource _loopCancellationTokenSource;
        private Task _refreshLoop;

        internal RetryDelegatingHandler(PipelineContext context, IDelegatingHandler innerHandler)
            : base(context, innerHandler)
        {
            _retryPolicy = context.RetryPolicy;
            _internalRetryHandler = new RetryHandler(_retryPolicy);
        }

        internal void SetRetryPolicy(IIotHubClientRetryPolicy retryPolicy)
        {
            _retryPolicy = retryPolicy;
            _internalRetryHandler.SetRetryPolicy(_retryPolicy);

            if (Logging.IsEnabled)
                Logging.Associate(this, _internalRetryHandler, nameof(SetRetryPolicy));
        }

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(OpenAsync));

            try
            {
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

                                if (_recoverSubscriptions)
                                {

                                    if (Logging.IsEnabled)
                                        Logging.Info(this, "Attempting to recover subscriptions.", nameof(OpenAsync));

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
                                        Logging.Info(this, "Subscriptions recovered.", nameof(OpenAsync));
                                }

                                _recoverSubscriptions = true;
                            }
                            finally
                            {
                                if (Logging.IsEnabled)
                                    Logging.Exit(this, cancellationToken, nameof(OpenAsync));
                            }
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
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
                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
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
                            await _cloudToDeviceMessageSubscriptionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(!_deviceReceiveMessageEnabled);

                                await base.EnableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                                _deviceReceiveMessageEnabled = true;
                            }
                            finally
                            {
                                _cloudToDeviceMessageSubscriptionSemaphore?.Release();
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
                            await _cloudToDeviceMessageSubscriptionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(_deviceReceiveMessageEnabled);

                                await base.DisableReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                                _deviceReceiveMessageEnabled = false;
                            }
                            finally
                            {
                                _cloudToDeviceMessageSubscriptionSemaphore?.Release();
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
                            await _directMethodSubscriptionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(!_methodsEnabled);

                                await base.EnableMethodsAsync(cancellationToken).ConfigureAwait(false);
                                _methodsEnabled = true;
                            }
                            finally
                            {
                                _directMethodSubscriptionSemaphore?.Release();
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
                            await _directMethodSubscriptionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(_methodsEnabled);

                                await base.DisableMethodsAsync(cancellationToken).ConfigureAwait(false);
                                _methodsEnabled = false;
                            }
                            finally
                            {
                                _directMethodSubscriptionSemaphore?.Release();
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
                            await _twinEventsSubscriptionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(!_twinEnabled);

                                await base.EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                                _twinEnabled = true;
                            }
                            finally
                            {
                                _twinEventsSubscriptionSemaphore?.Release();
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
                            await _twinEventsSubscriptionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(_twinEnabled);

                                await base.DisableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                                _twinEnabled = false;
                            }
                            finally
                            {
                                _twinEventsSubscriptionSemaphore?.Release();
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

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, nameof(CloseAsync));

            if (!_isOpen)
            {
                // Already closed so gracefully exit, instead of throw.
                return;
            }

            await _clientOpenCloseSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!_isOpen)
                {
                    // Already closed so gracefully exit, instead of throw.
                    return;
                }

                await base.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _isOpen = false;

                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(CloseAsync));

                _clientOpenCloseSemaphore?.Release();
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
                        _loopCancellationTokenSource?.Dispose();

                        _clientOpenCloseSemaphore?.Dispose();
                        _cloudToDeviceMessageSubscriptionSemaphore?.Dispose();
                        _directMethodSubscriptionSemaphore?.Dispose();
                        _twinEventsSubscriptionSemaphore?.Dispose();
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
