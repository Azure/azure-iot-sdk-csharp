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
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal enum ClientTransportStatus
    {
        Closed = 0,
        Open = 1,
    }

    internal class RetryDelegatingHandler : DefaultDelegatingHandler
    {
        // RetryCount is used for testing purpose and is equal to MaxValue in prod.
        private const int RetryMaxCount = int.MaxValue;

        private RetryPolicy _internalRetryPolicy;

        private readonly SemaphoreSlim _clientOpenSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _cloudToDeviceMessageSubscriptionSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _cloudToDeviceEventSubscriptionSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _directMethodSubscriptionSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _twinEventsSubscriptionSemaphore = new SemaphoreSlim(1, 1);
        private bool _openCalled;
        private bool _methodsEnabled;
        private bool _twinEnabled;
        private bool _eventsEnabled;
        private bool _deviceReceiveMessageEnabled;
        private bool _isDisposing;
        private bool _isAnEdgeModule = true;
        private long _clientTransportState; // references the current client transport status as the int value of ClientTransportStatus

        private Task _transportClosedTask;
        private readonly CancellationTokenSource _handleDisconnectCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _cancelPendingOperationsCts = new CancellationTokenSource();

        private readonly ConnectionStatusChangesHandler _onConnectionStatusChanged;

        public RetryDelegatingHandler(PipelineContext context, IDelegatingHandler innerHandler)
            : base(context, innerHandler)
        {
            IRetryPolicy defaultRetryStrategy = new ExponentialBackoff(
                retryCount: RetryMaxCount,
                minBackoff: TimeSpan.FromMilliseconds(100),
                maxBackoff: TimeSpan.FromSeconds(10),
                deltaBackoff: TimeSpan.FromMilliseconds(100));

            _internalRetryPolicy = new RetryPolicy(new TransientErrorStrategy(), new RetryStrategyAdapter(defaultRetryStrategy));
            _onConnectionStatusChanged = context.ConnectionStatusChangesHandler;

            if (Logging.IsEnabled)
                Logging.Associate(this, _internalRetryPolicy, nameof(SetRetryPolicy));
        }

        private class TransientErrorStrategy : ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex)
            {
                return ex is IotHubException exception && exception.IsTransient;
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

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);

                            if (message.IsBodyCalled)
                            {
                                message.ResetBody();
                            }

                            await base.SendEventAsync(message, operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
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

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);

                            foreach (Message m in messages)
                            {
                                if (m.IsBodyCalled)
                                {
                                    m.ResetBody();
                                }
                            }

                            await base.SendEventAsync(messages, operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
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

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
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

        public override async Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(ReceiveAsync));

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                return await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
                            return await base.ReceiveAsync(operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(ReceiveAsync));
            }
        }

        public override async Task<Message> ReceiveAsync(TimeoutHelper timeoutHelper)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, timeoutHelper, nameof(ReceiveAsync));

                using var cts = new CancellationTokenSource(timeoutHelper.GetRemainingTime());
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, _cancelPendingOperationsCts.Token);

                return await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, timeoutHelper).ConfigureAwait(false);
                            return await base.ReceiveAsync(timeoutHelper).ConfigureAwait(false);
                        },
                        operationCts.Token)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, timeoutHelper, nameof(ReceiveAsync));
            }
        }

        public override async Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(EnableReceiveMessageAsync));

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);

                            // Wait to acquire the _cloudToDeviceSubscriptionSemaphore. This ensures that concurrently invoked API calls are invoked in a thread-safe manner.
                            await _cloudToDeviceMessageSubscriptionSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
                            try
                            {
                                // The telemetry downlink needs to be enabled only for the first time that the callback is set.
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

        // This is to ensure that if device connects over MQTT with CleanSession flag set to false,
        // then any message sent while the device was disconnected is delivered on the callback.
        public override async Task EnsurePendingMessagesAreDeliveredAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(EnsurePendingMessagesAreDeliveredAsync));

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);

                            // Wait to acquire the _cloudToDeviceMessageSubscriptionSemaphore. This ensures that concurrently invoked API calls are invoked in a thread-safe manner.
                            await _cloudToDeviceMessageSubscriptionSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);

                            try
                            {
                                // Ensure that a callback for receiving messages has been previously set.
                                Debug.Assert(_deviceReceiveMessageEnabled);
                                await base.EnsurePendingMessagesAreDeliveredAsync(operationCts.Token).ConfigureAwait(false);
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
                    Logging.Exit(this, cancellationToken, nameof(EnsurePendingMessagesAreDeliveredAsync));
            }
        }

        public override async Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(DisableReceiveMessageAsync));

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);

                            // Wait to acquire the _cloudToDeviceMessageSubscriptionSemaphore. This ensures that concurrently invoked API calls are invoked in a thread-safe manner.
                            await _cloudToDeviceMessageSubscriptionSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
                            try
                            {
                                // Ensure that a callback for receiving messages has been previously set.
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
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(EnableMethodsAsync));

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
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
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(DisableMethodsAsync));

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
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

        public override async Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(EnableEventReceiveAsync));

                _isAnEdgeModule = isAnEdgeModule;
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
                            await _cloudToDeviceEventSubscriptionSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(!_eventsEnabled);
                                await base.EnableEventReceiveAsync(isAnEdgeModule, operationCts.Token).ConfigureAwait(false);
                                _eventsEnabled = true;
                            }
                            finally
                            {
                                try
                                {
                                    _cloudToDeviceEventSubscriptionSemaphore?.Release();
                                }
                                catch (ObjectDisposedException) when (_isDisposing)
                                {
                                    if (Logging.IsEnabled)
                                        Logging.Error(this, "Tried releasing cloud-to-device event subscription semaphore but it has already been disposed by client disposal on a separate thread." +
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
                    Logging.Exit(this, cancellationToken, nameof(EnableEventReceiveAsync));
            }
        }

        public override async Task DisableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(DisableEventReceiveAsync));

                _isAnEdgeModule = isAnEdgeModule;
                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
                            await _cloudToDeviceEventSubscriptionSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
                            try
                            {
                                Debug.Assert(_eventsEnabled);
                                await base.DisableEventReceiveAsync(isAnEdgeModule, operationCts.Token).ConfigureAwait(false);
                                _eventsEnabled = false;
                            }
                            finally
                            {
                                try
                                {
                                    _cloudToDeviceEventSubscriptionSemaphore?.Release();
                                }
                                catch (ObjectDisposedException) when (_isDisposing)
                                {
                                    if (Logging.IsEnabled)
                                        Logging.Error(this, "Tried releasing cloud-to-device event subscription semaphore but it has already been disposed by client disposal on a separate thread." +
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
                    Logging.Exit(this, cancellationToken, nameof(DisableEventReceiveAsync));
            }
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(EnableTwinPatchAsync));

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
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
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(DisableTwinPatchAsync));

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
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

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(SendTwinGetAsync));

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                return await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
                            return await base.SendTwinGetAsync(operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
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

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
                            await base.SendTwinPatchAsync(reportedProperties, operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, reportedProperties, cancellationToken, nameof(SendTwinPatchAsync));
            }
        }

        public override async Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, lockToken, cancellationToken, nameof(CompleteAsync));

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
                            await base.CompleteAsync(lockToken, operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, lockToken, cancellationToken, nameof(CompleteAsync));
            }
        }

        public override async Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, lockToken, cancellationToken, nameof(AbandonAsync));

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
                            await base.AbandonAsync(lockToken, operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, lockToken, cancellationToken, nameof(AbandonAsync));
            }
        }

        public override async Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, lockToken, cancellationToken, nameof(RejectAsync));

                using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

                await _internalRetryPolicy
                    .RunWithRetryAsync(
                        async () =>
                        {
                            await EnsureOpenedAsync(false, operationCts.Token).ConfigureAwait(false);
                            await base.RejectAsync(lockToken, operationCts.Token).ConfigureAwait(false);
                        },
                        operationCts.Token)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, lockToken, cancellationToken, nameof(RejectAsync));
            }
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return EnsureOpenedAsync(true, cancellationToken);
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!_openCalled
                    || GetClientTransportStatus() == ClientTransportStatus.Closed)
                {
                    return;
                }

                if (Logging.IsEnabled)
                    Logging.Enter(this, cancellationToken, nameof(CloseAsync));

                _handleDisconnectCts?.Cancel();
                _cancelPendingOperationsCts?.Cancel();
                await base.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                SetClientTransportStatus(ClientTransportStatus.Closed);
                Dispose(true);

                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, nameof(CloseAsync));
            }
        }

        /// <summary>
        /// Implicit open handler.
        /// </summary>
        private async Task EnsureOpenedAsync(bool withRetry, CancellationToken cancellationToken)
        {
            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

            // If this object has already been disposed, we will throw an exception indicating that.
            // This is the entry point for interacting with the client and this safety check should be done here.
            // The current behavior does not support open->close->open
            if (_isDisposed)
            {
                throw new ObjectDisposedException("IoT client", ClientDisposedMessage);
            }

            if (GetClientTransportStatus() == ClientTransportStatus.Open)
            {
                return;
            }

            await _clientOpenSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);
            try
            {
                if (GetClientTransportStatus() == ClientTransportStatus.Closed)
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, "Opening connection", nameof(EnsureOpenedAsync));

                    // This is to ensure that if OpenInternalAsync() fails on retry expiration with a custom retry policy,
                    // we are returning the corresponding connection status change event => disconnected: retry_expired.
                    try
                    {
                        await OpenInternalAsync(withRetry, operationCts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!ex.IsFatal())
                    {
                        HandleConnectionStatusExceptions(ex, true);
                        throw;
                    }

                    if (!_isDisposed)
                    {
                        SetClientTransportStatus(ClientTransportStatus.Open);
                        _openCalled = true;

                        // Send the request for transport close notification.
                        _transportClosedTask = HandleDisconnectAsync();
                    }
                    else
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, "Race condition: Disposed during opening.", nameof(EnsureOpenedAsync));

                        _handleDisconnectCts?.Cancel();
                    }
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
                        Logging.Error(this, "Tried releasing twin event subscription semaphore but it has already been disposed by client disposal on a separate thread." +
                            "Ignoring this exception and continuing with client cleanup.");
                }
            }
        }

        private async Task EnsureOpenedAsync(bool withRetry, TimeoutHelper timeoutHelper)
        {
            using var cts = new CancellationTokenSource(timeoutHelper.GetRemainingTime());
            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, _cancelPendingOperationsCts.Token);

            // If this object has already been disposed, we will throw an exception indicating that.
            // This is the entry point for interacting with the client and this safety check should be done here.
            // The current behavior does not support open->close->open
            if (_isDisposed)
            {
                throw new ObjectDisposedException("IoT client", ClientDisposedMessage);
            }

            if (GetClientTransportStatus() == ClientTransportStatus.Open)
            {
                return;
            }

            await _clientOpenSemaphore.WaitAsync(operationCts.Token).ConfigureAwait(false);

            try
            {
                if (GetClientTransportStatus() == ClientTransportStatus.Closed)
                {
                    if (Logging.IsEnabled)
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

                    if (!_isDisposed)
                    {
                        SetClientTransportStatus(ClientTransportStatus.Open);
                        _openCalled = true;

                        // Send the request for transport close notification.
                        _transportClosedTask = HandleDisconnectAsync();
                    }
                    else
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, "Race condition: Disposed during opening.", nameof(EnsureOpenedAsync));

                        _handleDisconnectCts?.Cancel();
                    }
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
                        Logging.Error(this, "Tried releasing twin event subscription semaphore but it has already been disposed by client disposal on a separate thread." +
                            "Ignoring this exception and continuing with client cleanup.");
                }
            }
        }

        private async Task OpenInternalAsync(bool withRetry, CancellationToken cancellationToken)
        {
            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancelPendingOperationsCts.Token);

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
                                await base.OpenAsync(operationCts.Token).ConfigureAwait(false);
                                _onConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);
                            }
                            catch (Exception ex) when (!ex.IsFatal())
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
            else
            {
                try
                {
                    if (Logging.IsEnabled)
                        Logging.Enter(this, cancellationToken, nameof(OpenAsync));

                    // Will throw on error.
                    await base.OpenAsync(operationCts.Token).ConfigureAwait(false);
                    _onConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    HandleConnectionStatusExceptions(ex);
                    throw;
                }
                finally
                {
                    if (Logging.IsEnabled)
                        Logging.Exit(this, cancellationToken, nameof(OpenAsync));
                }
            }
        }

        private async Task OpenInternalAsync(bool withRetry, TimeoutHelper timeoutHelper)
        {
            using var cts = new CancellationTokenSource(timeoutHelper.GetRemainingTime());
            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, _cancelPendingOperationsCts.Token);

            if (withRetry)
            {
                await _internalRetryPolicy
                .RunWithRetryAsync(
                    async () =>
                    {
                        try
                        {
                            if (Logging.IsEnabled)
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
                            if (Logging.IsEnabled)
                                Logging.Exit(this, timeoutHelper, nameof(OpenAsync));
                        }
                    },
                    operationCts.Token)
                .ConfigureAwait(false);
            }
            else
            {
                try
                {
                    if (Logging.IsEnabled)
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
                    if (Logging.IsEnabled)
                        Logging.Exit(this, timeoutHelper, nameof(OpenAsync));
                }
            }
        }

        // Triggered from connection loss event
        private async Task HandleDisconnectAsync()
        {
            if (_isDisposed)
            {
                if (Logging.IsEnabled)
                    Logging.Info(this, "Disposed during disconnection.", nameof(HandleDisconnectAsync));

                _handleDisconnectCts?.Cancel();
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

                _onConnectionStatusChanged(ConnectionStatus.Disabled, ConnectionStatusChangeReason.Client_Close);
                return;
            }

            if (Logging.IsEnabled)
                Logging.Info(this, "Transport disconnected: unexpected.", nameof(HandleDisconnectAsync));

            await _clientOpenSemaphore.WaitAsync().ConfigureAwait(false);
            SetClientTransportStatus(ClientTransportStatus.Closed);

            try
            {
                // This is used to ensure that when NoRetry() policy is enabled, we should not be retrying.
                if (!_internalRetryPolicy.RetryStrategy.GetShouldRetry().Invoke(0, new IotHubCommunicationException(), out TimeSpan delay))
                {
                    if (Logging.IsEnabled)
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
                CancellationToken cancellationToken = _handleDisconnectCts?.Token ?? CancellationToken.None;

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

                    SetClientTransportStatus(ClientTransportStatus.Open);
                    _onConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);

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
                try
                {
                    _clientOpenSemaphore?.Release();
                }
                catch (ObjectDisposedException) when (_isDisposing)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, "Tried releasing twin event subscription semaphore but it has already been disposed by client disposal on a separate thread." +
                            "Ignoring this exception and continuing with client cleanup.");
                }
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

            ConnectionStatusChangeReason reason = ConnectionStatusChangeReason.Communication_Error;
            ConnectionStatus status = ConnectionStatus.Disconnected;

            if (exception is IotHubException hubException)
            {
                if (hubException.IsTransient)
                {
                    if (retryAttemptsExhausted)
                    {
                        reason = ConnectionStatusChangeReason.Retry_Expired;
                    }
                    else
                    {
                        status = ConnectionStatus.Disconnected_Retrying;
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
            if (Logging.IsEnabled)
                Logging.Info(
                    this,
                    $"Connection status change: status={status}, reason={reason}",
                    nameof(HandleConnectionStatusExceptions));
        }

        private ClientTransportStatus GetClientTransportStatus()
        {
            return (ClientTransportStatus)Interlocked.Read(ref _clientTransportState);
        }

        private void SetClientTransportStatus(ClientTransportStatus clientTransportStatus)
        {
            _ = Interlocked.Exchange(ref _clientTransportState, (int)clientTransportStatus);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_isDisposed}; disposing={disposing}", $"{nameof(RetryDelegatingHandler)}.{nameof(Dispose)}");

                if (!_isDisposed)
                {
                    _isDisposing = true;
                    SetClientTransportStatus(ClientTransportStatus.Closed);

                    base.Dispose(disposing);

                    if (disposing)
                    {
                        _handleDisconnectCts?.Cancel();
                        _cancelPendingOperationsCts?.Cancel();

                        var disposables = new List<IDisposable>
                        {
                            _handleDisconnectCts,
                            _cancelPendingOperationsCts,
                            _clientOpenSemaphore,
                            _cloudToDeviceMessageSubscriptionSemaphore,
                            _cloudToDeviceEventSubscriptionSemaphore,
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

                    // the _disposed flag is inherited from the base class DefaultDelegatingHandler and is finally set to true there.
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_isDisposed}; disposing={disposing}", $"{nameof(RetryDelegatingHandler)}.{nameof(Dispose)}");
                }
            }
        }
    }
}
