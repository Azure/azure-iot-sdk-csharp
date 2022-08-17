// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.TransientFaultHandling;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class RetryDelegatingHandler : DefaultDelegatingHandler
    {
        // RetryCount is used for testing purpose and is equal to MaxValue in prod.
        private const int RetryMaxCount = int.MaxValue;

        private RetryPolicy _internalRetryPolicy;

        private SemaphoreSlim _handlerSemaphore = new SemaphoreSlim(1, 1);
        private bool _openCalled;
        private bool _opened;
        private bool _methodsEnabled;
        private bool _twinEnabled;
        private bool _eventsEnabled;
        private bool _deviceReceiveMessageEnabled;
        private bool _isAnEdgeModule = true;

        private Task _transportClosedTask;
        private readonly CancellationTokenSource _handleDisconnectCts = new CancellationTokenSource();

        private readonly Action<ConnectionInfo> _onConnectionStateChanged;

        public RetryDelegatingHandler(PipelineContext context, IDelegatingHandler innerHandler)
            : base(context, innerHandler)
        {
            IRetryPolicy defaultRetryStrategy = new ExponentialBackoff(
                retryCount: RetryMaxCount,
                minBackoff: TimeSpan.FromMilliseconds(100),
                maxBackoff: TimeSpan.FromSeconds(10),
                deltaBackoff: TimeSpan.FromMilliseconds(100));

            _internalRetryPolicy = new RetryPolicy(new TransientErrorStrategy(), new RetryStrategyAdapter(defaultRetryStrategy));
            _onConnectionStateChanged = context.ConnectionStateChangeHandler;

            if (Logging.IsEnabled)
                Logging.Associate(this, _internalRetryPolicy, nameof(SetRetryPolicy));
        }

        private class TransientErrorStrategy : ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex)
            {
                return ex is IotHubClientException exception && exception.IsTransient;
            }
        }

        public virtual void SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            _internalRetryPolicy = new RetryPolicy(
                new TransientErrorStrategy(),
                new RetryStrategyAdapter(retryPolicy));

            if (Logging.IsEnabled)
                Logging.Associate(this, _internalRetryPolicy, nameof(SetRetryPolicy));
        }

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, message, cancellationToken, nameof(SendEventAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            await base.SendEventAsync(message, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message, cancellationToken, nameof(SendEventAsync));
            }
        }

        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, messages, cancellationToken, nameof(SendEventAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            await base.SendEventAsync(messages, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, messages, cancellationToken, nameof(SendEventAsync));
            }
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal method, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, method, cancellationToken, nameof(SendMethodResponseAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
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

        public override async Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(ReceiveMessageAsync));

                return await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            return await base.ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(ReceiveMessageAsync));
            }
        }

        public override async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(EnableReceiveMessageAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            // Ensure that the connection has been opened, before enabling the callback for receiving messages.
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);

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

        // This is to ensure that if device connects over MQTT with CleanSession flag set to false,
        // then any message sent while the device was disconnected is delivered on the callback.
        public override async Task EnsurePendingMessagesAreDeliveredAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(EnsurePendingMessagesAreDeliveredAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            // Ensure that the connection has been opened before returning pending messages to the callback.
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);

                            // Wait to acquire the _handlerSemaphore. This ensures that concurrently invoked API calls are invoked in a thread-safe manner.
                            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                            try
                            {
                                // Ensure that a callback for receiving messages has been previously set.
                                Debug.Assert(_deviceReceiveMessageEnabled);
                                await base.EnsurePendingMessagesAreDeliveredAsync(cancellationToken).ConfigureAwait(false);
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
                    Logging.Exit(this, cancellationToken, nameof(EnsurePendingMessagesAreDeliveredAsync));
            }
        }

        public override async Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(DisableReceiveMessageAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            // Ensure that the connection has been opened, before disabling the callback for receiving messages.
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);

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
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(EnableMethodsAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);

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
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(DisableMethodsAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
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

        public override async Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            try
            {
                _isAnEdgeModule = isAnEdgeModule;
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(EnableEventReceiveAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                await base.EnableEventReceiveAsync(isAnEdgeModule, cancellationToken).ConfigureAwait(false);
                                Debug.Assert(!_eventsEnabled);
                                _eventsEnabled = true;
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
                    Logging.Exit(this, cancellationToken, nameof(EnableEventReceiveAsync));
            }
        }

        public override async Task DisableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            try
            {
                _isAnEdgeModule = isAnEdgeModule;
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(DisableEventReceiveAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(_eventsEnabled);
                                await base.DisableEventReceiveAsync(isAnEdgeModule, cancellationToken).ConfigureAwait(false);
                                _eventsEnabled = false;
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
                    Logging.Exit(this, cancellationToken, nameof(DisableEventReceiveAsync));
            }
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(EnableTwinPatchAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
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
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(DisableTwinPatchAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
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

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(SendTwinGetAsync));

                return await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            return await base.SendTwinGetAsync(cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(SendTwinGetAsync));
            }
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, reportedProperties, cancellationToken, nameof(SendTwinPatchAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            await base.SendTwinPatchAsync(reportedProperties, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, reportedProperties, cancellationToken, nameof(SendTwinPatchAsync));
            }
        }

        public override async Task CompleteMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, lockToken, cancellationToken, nameof(CompleteMessageAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            await base.CompleteMessageAsync(lockToken, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, lockToken, cancellationToken, nameof(CompleteMessageAsync));
            }
        }

        public override async Task AbandonMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, lockToken, cancellationToken, nameof(AbandonMessageAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            await base.AbandonMessageAsync(lockToken, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, lockToken, cancellationToken, nameof(AbandonMessageAsync));
            }
        }

        public override async Task RejectMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, lockToken, cancellationToken, nameof(RejectMessageAsync));

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            await base.RejectMessageAsync(lockToken, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, lockToken, cancellationToken, nameof(RejectMessageAsync));
            }
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return EnsureOpenedAsync(true, cancellationToken);
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!_openCalled)
                {
                    return;
                }

                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(CloseAsync));

                _handleDisconnectCts.Cancel();
                await base.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(CloseAsync));

                _handlerSemaphore?.Release();
                Dispose(true);
            }
        }

        /// <summary>
        /// Implicit open handler.
        /// </summary>
        private async Task EnsureOpenedAsync(bool withRetry, CancellationToken cancellationToken)
        {
            // If this object has already been disposed, we will throw an exception indicating that.
            // This is the entry point for interacting with the client and this safety check should be done here.
            // The current behavior does not support open->close->open
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RetryDelegatingHandler));
            }

            if (Volatile.Read(ref _opened))
            {
                return;
            }

            await _handlerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!_opened)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Opening connection", nameof(EnsureOpenedAsync));

                    // This is to ensure that if OpenInternalAsync() fails on retry expiration with a custom retry policy,
                    // we are returning the corresponding connection state change event => disconnected: retry_expired.
                    try
                    {
                        await OpenInternalAsync(withRetry, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!Fx.IsFatal(ex))
                    {
                        HandleConnectionStateExceptions(ex, true);
                        throw;
                    }

                    if (!_disposed)
                    {
                        _opened = true;
                        _openCalled = true;

                        // Send the request for transport close notification.
                        _transportClosedTask = HandleDisconnectAsync();
                    }
                    else
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, "Race condition: Disposed during opening.", nameof(EnsureOpenedAsync));

                        _handleDisconnectCts.Cancel();
                    }
                }
            }
            finally
            {
                _handlerSemaphore?.Release();
            }
        }

        private async Task OpenInternalAsync(bool withRetry, CancellationToken cancellationToken)
        {
            var connectionInfo = new ConnectionInfo();

            if (withRetry)
            {
                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            try
                            {
                                if (Logging.IsEnabled)
                                    Logging.Enter(this, cancellationToken, nameof(OpenAsync));

                                // Will throw on error.
                                await base.OpenAsync(cancellationToken).ConfigureAwait(false);

                                connectionInfo = new ConnectionInfo(ConnectionState.Connected, ConnectionStateChangeReason.ConnectionOk, DateTimeOffset.UtcNow);
                                _onConnectionStateChanged(connectionInfo);
                            }
                            catch (Exception ex) when (!Fx.IsFatal(ex))
                            {
                                HandleConnectionStateExceptions(ex);
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
            else
            {
                try
                {
                    if (Logging.IsEnabled)
                        Logging.Enter(this, cancellationToken, nameof(OpenAsync));

                    // Will throw on error.
                    await base.OpenAsync(cancellationToken).ConfigureAwait(false);

                    connectionInfo = new ConnectionInfo(ConnectionState.Connected, ConnectionStateChangeReason.ConnectionOk, DateTimeOffset.UtcNow);
                    _onConnectionStateChanged(connectionInfo);
                }
                catch (Exception ex) when (!Fx.IsFatal(ex))
                {
                    HandleConnectionStateExceptions(ex);
                    throw;
                }
                finally
                {
                    if (Logging.IsEnabled)
                        Logging.Exit(this, cancellationToken, nameof(OpenAsync));
                }
            }
        }

        // Triggered from connection loss event
        private async Task HandleDisconnectAsync()
        {
            var connectionInfo = new ConnectionInfo();

            if (_disposed)
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

                connectionInfo = new ConnectionInfo(ConnectionState.Disabled, ConnectionStateChangeReason.ClientClose, DateTimeOffset.UtcNow);
                _onConnectionStateChanged(connectionInfo);
                return;
            }

            if (Logging.IsEnabled)
                Logging.Info(this, "Transport disconnected: unexpected.", nameof(HandleDisconnectAsync));

            await _handlerSemaphore.WaitAsync().ConfigureAwait(false);
            _opened = false;

            try
            {
                // This is used to ensure that when NoRetry() policy is enabled, we should not be retrying.
                if (!_internalRetryPolicy.RetryStrategy.GetShouldRetry().Invoke(0, new IotHubClientException(true, IotHubStatusCode.NetworkErrors), out TimeSpan delay))
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Transport disconnected: closed by application.", nameof(HandleDisconnectAsync));

                    connectionInfo = new ConnectionInfo(ConnectionState.Disconnected, ConnectionStateChangeReason.RetryExpired, DateTimeOffset.UtcNow);
                    _onConnectionStateChanged(connectionInfo);
                    return;
                }

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                }

                // always reconnect.
                connectionInfo = new ConnectionInfo(ConnectionState.DisconnectedRetrying, ConnectionStateChangeReason.CommunicationError, DateTimeOffset.UtcNow);
                _onConnectionStateChanged(connectionInfo);
                CancellationToken cancellationToken = _handleDisconnectCts.Token;

                // This will recover to the state before the disconnect.
                await _internalRetryPolicy.RunWithRetryAsync(async () =>
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Attempting to recover subscriptions.", nameof(HandleDisconnectAsync));

                    await base.OpenAsync(cancellationToken).ConfigureAwait(false);

                    var tasks = new List<Task>(4);

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

                    // This is to ensure that, if previously enabled, the callback to receive events for modules is recovered.
                    if (_eventsEnabled)
                    {
                        tasks.Add(base.EnableEventReceiveAsync(_isAnEdgeModule, cancellationToken));
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

                    // Don't check for unhandled C2D messages until the callback (EnableReceiveMessageAsync) is hooked up.
                    if (_deviceReceiveMessageEnabled)
                    {
                        await base.EnsurePendingMessagesAreDeliveredAsync(cancellationToken).ConfigureAwait(false);
                    }

                    // Send the request for transport close notification.
                    _transportClosedTask = HandleDisconnectAsync();

                    _opened = true;
                    connectionInfo = new ConnectionInfo(ConnectionState.Connected, ConnectionStateChangeReason.ConnectionOk, DateTimeOffset.UtcNow);
                    _onConnectionStateChanged(connectionInfo);

                    if (Logging.IsEnabled)
                        Logging.Info(this, "Subscriptions recovered.", nameof(HandleDisconnectAsync));
                },
                cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, ex.ToString(), nameof(HandleDisconnectAsync));

                HandleConnectionStateExceptions(ex, true);
            }
            finally
            {
                _handlerSemaphore?.Release();
            }
        }

        // The retryAttemptsExhausted flag differentiates between calling this method while still retrying
        // vs calling this when no more retry attempts are being made.
        private void HandleConnectionStateExceptions(Exception exception, bool retryAttemptsExhausted = false)
        {
            if (Logging.IsEnabled)
                Logging.Info(
                    this,
                    $"Received exception: {exception}, retryAttemptsExhausted={retryAttemptsExhausted}",
                    nameof(HandleConnectionStateExceptions));

            ConnectionStateChangeReason reason = ConnectionStateChangeReason.CommunicationError;
            ConnectionState state = ConnectionState.Disconnected;

            if (exception is IotHubClientException hubException)
            {
                if (hubException.IsTransient)
                {
                    if (retryAttemptsExhausted)
                    {
                        reason = ConnectionStateChangeReason.RetryExpired;
                    }
                    else
                    {
                        state = ConnectionState.DisconnectedRetrying;
                    }
                }
                else if (hubException is UnauthorizedException)
                {
                    reason = ConnectionStateChangeReason.BadCredential;
                }
                else if (hubException.StatusCode is IotHubStatusCode.DeviceNotFound)
                {
                    reason = ConnectionStateChangeReason.DeviceDisabled;
                }
            }

            _onConnectionStateChanged(new ConnectionInfo(state, reason, DateTimeOffset.UtcNow));
            if (Logging.IsEnabled)
                Logging.Info(
                    this,
                    $"Connection state change: state={state}, reason={reason}",
                    nameof(HandleConnectionStateExceptions));
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (Logging.IsEnabled)
                {
                    Logging.Enter(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_disposed}; disposing={disposing}", $"{nameof(RetryDelegatingHandler)}.{nameof(Dispose)}");
                }

                if (!_disposed)
                {
                    base.Dispose(disposing);
                    if (disposing)
                    {
                        _handleDisconnectCts?.Cancel();
                        _handleDisconnectCts?.Dispose();
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
                {
                    Logging.Exit(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_disposed}; disposing={disposing}", $"{nameof(RetryDelegatingHandler)}.{nameof(Dispose)}");
                }
            }
        }
    }
}
