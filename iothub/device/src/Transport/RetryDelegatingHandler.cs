// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.TransientFaultHandling;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        private readonly ConnectionStatusChangesHandler _onConnectionStatusChanged;

        public RetryDelegatingHandler(IPipelineContext context, IDelegatingHandler innerHandler)
            : base(context, innerHandler)
        {
            IRetryPolicy defaultRetryStrategy = new ExponentialBackoff(
                retryCount: RetryMaxCount,
                minBackoff: TimeSpan.FromMilliseconds(100),
                maxBackoff: TimeSpan.FromSeconds(10),
                deltaBackoff: TimeSpan.FromMilliseconds(100));

            _internalRetryPolicy = new RetryPolicy(new TransientErrorStrategy(), new RetryStrategyAdapter(defaultRetryStrategy));
            _onConnectionStatusChanged = context.Get<ConnectionStatusChangesHandler>();

            Logging.Associate(this, _internalRetryPolicy, nameof(SetRetryPolicy));
        }

        private class TransientErrorStrategy : ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex)
            {
                return ex is IotHubException
                    ? ((IotHubException)ex).IsTransient
                    : false;
            }
        }

        public void SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            _internalRetryPolicy = new RetryPolicy(
                new TransientErrorStrategy(),
                new RetryStrategyAdapter(retryPolicy));

            Logging.Associate(this, _internalRetryPolicy, nameof(SetRetryPolicy));
        }

        public override async Task SendEventAsync(MessageBase message, CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, message, cancellationToken, nameof(SendEventAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);

                            if (message.IsBodyCalled)
                            {
                                message.ResetBody();
                            }

                            await base.SendEventAsync(message, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, message, cancellationToken, nameof(SendEventAsync));
            }
        }

        public override async Task SendEventAsync(IEnumerable<MessageBase> messages, CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, messages, cancellationToken, nameof(SendEventAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);

                            foreach (Message m in messages)
                            {
                                if (m.IsBodyCalled)
                                {
                                    m.ResetBody();
                                }
                            }

                            await base.SendEventAsync(messages, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, messages, cancellationToken, nameof(SendEventAsync));
            }
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal method, CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, method, cancellationToken, nameof(SendMethodResponseAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
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
                Logging.Exit(this, method, cancellationToken, nameof(SendMethodResponseAsync));
            }
        }

        public override async Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, cancellationToken, nameof(ReceiveAsync));

                return await _internalRetryPolicy
                    .ExecuteAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            return await base.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, cancellationToken, nameof(ReceiveAsync));
            }
        }

        public override async Task<Message> ReceiveAsync(TimeoutHelper timeoutHelper)
        {
            try
            {
                Logging.Enter(this, timeoutHelper, nameof(ReceiveAsync));

                using var cts = new CancellationTokenSource(timeoutHelper.GetRemainingTime());
                return await _internalRetryPolicy
                    .ExecuteAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, timeoutHelper).ConfigureAwait(false);
                            return await base.ReceiveAsync(timeoutHelper).ConfigureAwait(false);
                        },
                        cts.Token)
                    .ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, timeoutHelper, nameof(ReceiveAsync));
            }
        }

        public override async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, cancellationToken, nameof(EnableReceiveMessageAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
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
                Logging.Exit(this, cancellationToken, nameof(EnableReceiveMessageAsync));
            }
        }

        // This is to ensure that if device connects over MQTT with CleanSession flag set to false,
        // then any message sent while the device was disconnected is delivered on the callback.
        public override async Task EnsurePendingMessagesAreDeliveredAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(EnsurePendingMessagesAreDeliveredAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
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
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(EnsurePendingMessagesAreDeliveredAsync));
            }
        }

        public override async Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, cancellationToken, nameof(DisableReceiveMessageAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
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
                Logging.Exit(this, cancellationToken, nameof(DisableReceiveMessageAsync));
            }
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, cancellationToken, nameof(EnableMethodsAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
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
                Logging.Exit(this, cancellationToken, nameof(EnableMethodsAsync));
            }
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, cancellationToken, nameof(DisableMethodsAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
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
                Logging.Exit(this, cancellationToken, nameof(DisableMethodsAsync));
            }
        }

        public override async Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            try
            {
                _isAnEdgeModule = isAnEdgeModule;
                Logging.Enter(this, cancellationToken, nameof(EnableEventReceiveAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
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
                Logging.Exit(this, cancellationToken, nameof(EnableEventReceiveAsync));
            }
        }

        public override async Task DisableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            try
            {
                _isAnEdgeModule = isAnEdgeModule;
                Logging.Enter(this, cancellationToken, nameof(DisableEventReceiveAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
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
                Logging.Exit(this, cancellationToken, nameof(DisableEventReceiveAsync));
            }
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, cancellationToken, nameof(EnableTwinPatchAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
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
                Logging.Exit(this, cancellationToken, nameof(EnableTwinPatchAsync));
            }
        }

        public override async Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, cancellationToken, nameof(DisableTwinPatchAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
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
                Logging.Exit(this, cancellationToken, nameof(DisableTwinPatchAsync));
            }
        }

        public override async Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, lockToken, cancellationToken, nameof(CompleteAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            await base.CompleteAsync(lockToken, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, lockToken, cancellationToken, nameof(CompleteAsync));
            }
        }

        public override async Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, lockToken, cancellationToken, nameof(AbandonAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            await base.AbandonAsync(lockToken, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, lockToken, cancellationToken, nameof(AbandonAsync));
            }
        }

        public override async Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, lockToken, cancellationToken, nameof(RejectAsync));

                await _internalRetryPolicy
                    .ExecuteAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            await base.RejectAsync(lockToken, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, lockToken, cancellationToken, nameof(RejectAsync));
            }
        }

        public override async Task<T> GetClientTwinPropertiesAsync<T>(CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, cancellationToken, nameof(GetClientTwinPropertiesAsync));

                return await _internalRetryPolicy
                    .ExecuteAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            return await base.GetClientTwinPropertiesAsync<T>(cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, cancellationToken, nameof(GetClientTwinPropertiesAsync));
            }
        }

        public override async Task<ClientPropertiesUpdateResponse> SendClientTwinPropertyPatchAsync(Stream reportedProperties, CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, reportedProperties, cancellationToken, nameof(SendClientTwinPropertyPatchAsync));

                return await _internalRetryPolicy
                    .ExecuteAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                            return await base.SendClientTwinPropertyPatchAsync(reportedProperties, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, reportedProperties, cancellationToken, nameof(SendClientTwinPropertyPatchAsync));
            }
        }

        public override async Task<ClientProperties> GetPropertiesAsync(PayloadConvention payloadConvention, CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, payloadConvention, cancellationToken, nameof(SendPropertyPatchAsync));

                return await _internalRetryPolicy
                    .ExecuteAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);
                            return await base.GetPropertiesAsync(payloadConvention, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, payloadConvention, cancellationToken, nameof(SendPropertyPatchAsync));
            }
        }

        public override async Task<ClientPropertiesUpdateResponse> SendPropertyPatchAsync(ClientPropertyCollection reportedProperties, CancellationToken cancellationToken)
        {
            try
            {
                Logging.Enter(this, reportedProperties, cancellationToken, nameof(SendPropertyPatchAsync));

                return await _internalRetryPolicy
                    .ExecuteAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);
                            return await base.SendPropertyPatchAsync(reportedProperties, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Logging.Exit(this, reportedProperties, cancellationToken, nameof(SendPropertyPatchAsync));
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

                Logging.Enter(this, cancellationToken, nameof(CloseAsync));

                _handleDisconnectCts.Cancel();
                await base.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
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
                    Logging.Info(this, "Opening connection", nameof(EnsureOpenedAsync));

                    // This is to ensure that if OpenInternalAsync() fails on retry expiration with a custom retry policy,
                    // we are returning the corresponding connection status change event => disconnected: retry_expired.
                    try
                    {
                        await OpenInternalAsync(withRetry, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!ex.IsFatal())
                    {
                        HandleConnectionStatusExceptions(ex, true);
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

        private async Task EnsureOpenedAsync(bool withRetry, TimeoutHelper timeoutHelper)
        {
            if (Volatile.Read(ref _opened))
            {
                return;
            }

            bool gain = await _handlerSemaphore.WaitAsync(timeoutHelper.GetRemainingTime()).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException("Timed out to acquire handler lock.");
            }

            try
            {
                if (!_opened)
                {
                    Logging.Info(this, "Opening connection", nameof(EnsureOpenedAsync));

                    // This is to ensure that if OpenInternalAsync() fails on retry expiration with a custom retry policy,
                    // we are returning the corresponding connection status change event => disconnected: retry_expired.
                    try
                    {
                        await OpenInternalAsync(withRetry, timeoutHelper).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!ex.IsFatal())
                    {
                        HandleConnectionStatusExceptions(ex, true);
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
            if (withRetry)
            {
                await _internalRetryPolicy
                    .ExecuteAsync(
                        async () =>
                        {
                            try
                            {
                                Logging.Enter(this, cancellationToken, nameof(OpenAsync));

                                // Will throw on error.
                                await base.OpenAsync(cancellationToken).ConfigureAwait(false);
                                _onConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);
                            }
                            catch (Exception ex) when (!ex.IsFatal())
                            {
                                HandleConnectionStatusExceptions(ex);
                                throw;
                            }
                            finally
                            {
                                Logging.Exit(this, cancellationToken, nameof(OpenAsync));
                            }
                        },
                        cancellationToken).ConfigureAwait(false);
            }
            else
            {
                try
                {
                    Logging.Enter(this, cancellationToken, nameof(OpenAsync));

                    // Will throw on error.
                    await base.OpenAsync(cancellationToken).ConfigureAwait(false);
                    _onConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    HandleConnectionStatusExceptions(ex);
                    throw;
                }
                finally
                {
                    Logging.Exit(this, cancellationToken, nameof(OpenAsync));
                }
            }
        }

        private async Task OpenInternalAsync(bool withRetry, TimeoutHelper timeoutHelper)
        {
            using var cts = new CancellationTokenSource(timeoutHelper.GetRemainingTime());

            if (withRetry)
            {
                await _internalRetryPolicy
                .ExecuteAsync(
                    async () =>
                    {
                        try
                        {
                            Logging.Enter(this, timeoutHelper, nameof(OpenAsync));

                            // Will throw on error.
                            await base.OpenAsync(timeoutHelper).ConfigureAwait(false);
                            _onConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);
                        }
                        catch (Exception ex) when (!ex.IsFatal())
                        {
                            HandleConnectionStatusExceptions(ex);
                            throw;
                        }
                        finally
                        {
                            Logging.Exit(this, timeoutHelper, nameof(OpenAsync));
                        }
                    },
                    cts.Token)
                .ConfigureAwait(false);
            }
            else
            {
                try
                {
                    Logging.Enter(this, timeoutHelper, nameof(OpenAsync));

                // Will throw on error.
                await base.OpenAsync(timeoutHelper).ConfigureAwait(false);
                    _onConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    HandleConnectionStatusExceptions(ex);
                    throw;
                }
                finally
                {
                    Logging.Exit(this, timeoutHelper, nameof(OpenAsync));
                }
            }

                
        }

        // Triggered from connection loss event
        private async Task HandleDisconnectAsync()
        {
            if (_disposed)
            {
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
                Logging.Info(this, "Transport disconnected: closed by application.", nameof(HandleDisconnectAsync));

                _onConnectionStatusChanged(ConnectionStatus.Disabled, ConnectionStatusChangeReason.Client_Close);
                return;
            }

            Logging.Info(this, "Transport disconnected: unexpected.", nameof(HandleDisconnectAsync));

            await _handlerSemaphore.WaitAsync().ConfigureAwait(false);
            _opened = false;

            try
            {
                // This is used to ensure that when NoRetry() policy is enabled, we should not be retrying.
                if (!_internalRetryPolicy.RetryStrategy.GetShouldRetry().Invoke(0, new IotHubCommunicationException(), out TimeSpan delay))
                {
                    Logging.Info(this, "Transport disconnected: closed by application.", nameof(HandleDisconnectAsync));

                    _onConnectionStatusChanged(ConnectionStatus.Disconnected, ConnectionStatusChangeReason.Retry_Expired);
                    return;
                }

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                }

                // always reconnect.
                _onConnectionStatusChanged(ConnectionStatus.Disconnected_Retrying, ConnectionStatusChangeReason.Communication_Error);
                CancellationToken cancellationToken = _handleDisconnectCts.Token;

                // This will recover to the state before the disconnect.
                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
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
                    _onConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);

                    Logging.Info(this, "Subscriptions recovered.", nameof(HandleDisconnectAsync));
                },
                cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logging.Error(this, ex.ToString(), nameof(HandleDisconnectAsync));

                HandleConnectionStatusExceptions(ex, true);
            }
            finally
            {
                _handlerSemaphore?.Release();
            }
        }

        // The connectFailed flag differentiates between calling this method while still retrying
        // vs calling this when no more retry attempts are being made.
        private void HandleConnectionStatusExceptions(Exception exception, bool connectFailed = false)
        {
            Logging.Info(this, $"Received exception: {exception}, connectFailed={connectFailed}", nameof(HandleConnectionStatusExceptions));

            ConnectionStatusChangeReason reason = ConnectionStatusChangeReason.Communication_Error;
            ConnectionStatus status = ConnectionStatus.Disconnected;

            if (exception is IotHubException hubException)
            {
                if (hubException.IsTransient)
                {
                    if (!connectFailed)
                    {
                        status = ConnectionStatus.Disconnected_Retrying;
                    }
                    else
                    {
                        reason = ConnectionStatusChangeReason.Retry_Expired;
                    }
                }
                else if (hubException is UnauthorizedException)
                {
                    reason = ConnectionStatusChangeReason.Bad_Credential;
                }
                else if (hubException is DeviceNotFoundException)
                {
                    reason = ConnectionStatusChangeReason.Device_Disabled;
                }
            }

            _onConnectionStatusChanged(status, reason);
            Logging.Info(this, $"Connection status change: status={status}, reason={reason}", nameof(HandleConnectionStatusExceptions));
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

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
        }
    }
}
