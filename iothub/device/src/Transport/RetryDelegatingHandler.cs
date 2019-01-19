// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.TransientFaultHandling;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    using ExponentialBackoff = Microsoft.Azure.Devices.Client.ExponentialBackoff;

    internal class RetryDelegatingHandler : DefaultDelegatingHandler
    {
        // RetryCount is used for testing purpose and is equal to MaxValue in prod.
        private static int s_retryMaxCount = int.MaxValue;
        private RetryPolicy _internalRetryPolicy;

        private SemaphoreSlim _handlerLock = new SemaphoreSlim(1, 1);
        private bool _openCalled;
        private bool _opened;
        private bool _methodsEnabled;
        private bool _twinEnabled;
        private bool _eventsEnabled;
        private bool _deviceStreamingEnabled;

        private Task _transportClosedTask;

        private readonly ConnectionStatusChangesHandler _onConnectionStatusChanged;

        public RetryDelegatingHandler(IPipelineContext context, IDelegatingHandler innerHandler)
            : base(context, innerHandler)
        {
            IRetryPolicy defaultRetryStrategy = new ExponentialBackoff(
                retryCount: s_retryMaxCount,
                minBackoff: TimeSpan.FromMilliseconds(100),
                maxBackoff: TimeSpan.FromSeconds(10),
                deltaBackoff: TimeSpan.FromMilliseconds(100));

            _internalRetryPolicy = new RetryPolicy(new TransientErrorStrategy(), new RetryStrategyAdapter(defaultRetryStrategy));
            _onConnectionStatusChanged = context.Get<ConnectionStatusChangesHandler>();
            
            if (Logging.IsEnabled) Logging.Associate(this, _internalRetryPolicy, nameof(SetRetryPolicy));
        }

        public uint RetryTimeoutInMilliseconds { get; set; } = DeviceClient.DefaultOperationTimeoutInMilliseconds;

        private class TransientErrorStrategy : ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex)
            {
                if (ex is IotHubException)
                {
                    return ((IotHubException)ex).IsTransient;
                }

                return false;
            }
        }

        public void SetRetryPolicy(IRetryPolicy retryPolicy)
        {
            _internalRetryPolicy = new RetryPolicy(
                new TransientErrorStrategy(),
                new RetryStrategyAdapter(retryPolicy));

            if (Logging.IsEnabled) Logging.Associate(this, _internalRetryPolicy, nameof(SetRetryPolicy));
        }

        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, message, cancellationToken, nameof(SendEventAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);

                    if (message.IsBodyCalled)
                    {
                        message.ResetBody();
                    }

                    await base.SendEventAsync(message, cancellationToken).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, message, cancellationToken, nameof(SendEventAsync));
            }
        }

        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, messages, cancellationToken, nameof(SendEventAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);

                    foreach (Message m in messages)
                    {
                        if (m.IsBodyCalled)
                        {
                            m.ResetBody();
                        }
                    }

                    await base.SendEventAsync(messages, cancellationToken).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, messages, cancellationToken, nameof(SendEventAsync));
            }
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal method, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, method, cancellationToken, nameof(SendMethodResponseAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);
                    await base.SendMethodResponseAsync(method, cancellationToken).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);

            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, method, cancellationToken, nameof(SendMethodResponseAsync));
            }
        }

        public override async Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(ReceiveAsync));

                return await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);
                    return await base.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(ReceiveAsync));
            }
        }

        public override async Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, timeout, cancellationToken, nameof(ReceiveAsync));

                return await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);
                    return await base.ReceiveAsync(timeout, cancellationToken).ConfigureAwait(false);
                },
            cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, cancellationToken, nameof(ReceiveAsync));
            }
        }

        #region Device Streaming
        public override async Task EnableStreamsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(EnableStreamsAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);

                    try
                    {
                        await _handlerLock.WaitAsync().ConfigureAwait(false);
                        await base.EnableStreamsAsync(cancellationToken).ConfigureAwait(false);
                        Debug.Assert(!_deviceStreamingEnabled);
                        _deviceStreamingEnabled = true;
                    }
                    finally
                    {
                        _handlerLock.Release();
                    }
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(EnableStreamsAsync));
            }
        }

        public override async Task DisableStreamsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(DisableStreamsAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);

                    try
                    {
                        await _handlerLock.WaitAsync().ConfigureAwait(false);
                        await base.DisableStreamsAsync(cancellationToken).ConfigureAwait(false);
                        Debug.Assert(_deviceStreamingEnabled);
                        _deviceStreamingEnabled = false;
                    }
                    finally
                    {
                        _handlerLock.Release();
                    }
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(DisableStreamsAsync));
            }
        }

        public override async Task<DeviceStreamRequest> WaitForDeviceStreamRequestAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(WaitForDeviceStreamRequestAsync));

                return await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);
                    return await base.WaitForDeviceStreamRequestAsync(cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(WaitForDeviceStreamRequestAsync));
            }
        }

        public override async Task AcceptDeviceStreamRequestAsync(DeviceStreamRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, request, cancellationToken, nameof(AcceptDeviceStreamRequestAsync));

                await _internalRetryPolicy.ExecuteAsync(() =>
                {
                    return base.AcceptDeviceStreamRequestAsync(request, cancellationToken);
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, request, cancellationToken, nameof(AcceptDeviceStreamRequestAsync));
            }
        }

        public override async Task RejectDeviceStreamRequestAsync(DeviceStreamRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, request, cancellationToken, nameof(RejectDeviceStreamRequestAsync));

                await _internalRetryPolicy.ExecuteAsync(() =>
                {
                    return base.RejectDeviceStreamRequestAsync(request, cancellationToken);
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, request, cancellationToken, nameof(RejectDeviceStreamRequestAsync));
            }
        }
        #endregion Device Streaming

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(EnableMethodsAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);

                    try
                    {
                        await _handlerLock.WaitAsync().ConfigureAwait(false);
                        Debug.Assert(!_methodsEnabled);
                        await base.EnableMethodsAsync(cancellationToken).ConfigureAwait(false);
                        _methodsEnabled = true;
                    }
                    finally
                    {
                        _handlerLock.Release();
                    }
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(EnableMethodsAsync));
            }
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(DisableMethodsAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);

                    try
                    {
                        await _handlerLock.WaitAsync().ConfigureAwait(false);
                        Debug.Assert(_methodsEnabled);
                        await base.DisableMethodsAsync(cancellationToken).ConfigureAwait(false);
                        _methodsEnabled = false;
                    }
                    finally
                    {
                        _handlerLock.Release();
                    }
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(DisableMethodsAsync));
            }
        }

        public override async Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(EnableEventReceiveAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);

                    try
                    {
                        await _handlerLock.WaitAsync().ConfigureAwait(false);
                        await base.EnableEventReceiveAsync(cancellationToken).ConfigureAwait(false);
                        Debug.Assert(!_eventsEnabled);
                        _eventsEnabled = true;
                    }
                    finally
                    {
                        _handlerLock.Release();
                    }
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(EnableEventReceiveAsync));
            }
        }

        public override async Task DisableEventReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(DisableEventReceiveAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);

                    try
                    {
                        await _handlerLock.WaitAsync().ConfigureAwait(false);
                        Debug.Assert(_eventsEnabled);
                        await base.DisableEventReceiveAsync(cancellationToken).ConfigureAwait(false);
                        _eventsEnabled = false;
                    }
                    finally
                    {
                        _handlerLock.Release();
                    }
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(DisableEventReceiveAsync));
            }
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(EnableTwinPatchAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);

                    try
                    {
                        await _handlerLock.WaitAsync().ConfigureAwait(false);
                        Debug.Assert(!_twinEnabled);
                        await base.EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
                        _twinEnabled = true;
                    }
                    finally
                    {
                        _handlerLock.Release();
                    }
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(EnableTwinPatchAsync));
            }
        }

        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(SendTwinGetAsync));

                return await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);
                    return await base.SendTwinGetAsync(cancellationToken).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(SendTwinGetAsync));
            }
        }

        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, reportedProperties, cancellationToken, nameof(SendTwinPatchAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);
                    await base.SendTwinPatchAsync(reportedProperties, cancellationToken).ConfigureAwait(false);
                },
            cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, reportedProperties, cancellationToken, nameof(SendTwinPatchAsync));
            }
        }

        public override async Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, nameof(CompleteAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);
                    await base.CompleteAsync(lockToken, cancellationToken).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, nameof(CompleteAsync));
            }
        }

        public override async Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, nameof(AbandonAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);
                    await base.AbandonAsync(lockToken, cancellationToken).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, nameof(AbandonAsync));
            }
        }

        public override async Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, nameof(RejectAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await EnsureOpenedAsync(cancellationToken).ConfigureAwait(false);
                    await base.RejectAsync(lockToken, cancellationToken).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, nameof(RejectAsync));
            }
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return EnsureOpenedAsync(cancellationToken);
        }

        public override async Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _handlerLock.WaitAsync().ConfigureAwait(false);
                if (!_openCalled) return;
            }
            finally
            {
                _handlerLock.Release();
            }

            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(CloseAsync));

                await _internalRetryPolicy.ExecuteAsync(async () =>
                {
                    await base.CloseAsync(cancellationToken).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);

                Dispose(true);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(CloseAsync));
            }
        }

        /// <summary>
        /// Implicit open handler.
        /// </summary>
        private async Task EnsureOpenedAsync(CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _opened)) return;

            await _handlerLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_opened)
                {
                    if (Logging.IsEnabled) Logging.Info(this, "Opening connection", nameof(EnsureOpenedAsync));
                    await OpenAsyncInternal(cancellationToken).ConfigureAwait(false);
                    _opened = true;
                    _openCalled = true;

                    // Send the request for transport close notification.
                    _transportClosedTask = HandleDisconnect();
                }
            }
            finally
            {
                _handlerLock.Release();
            }
        }

        private Task OpenAsyncInternal(CancellationToken cancellationToken)
        {
            return _internalRetryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, nameof(OpenAsync));

                    // Will throw on error.
                    await base.OpenAsync(cancellationToken).ConfigureAwait(false);
                    _onConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);
                }
                catch (IotHubException ex)
                {
                    HandleConnectionStatusExceptions(ex);
                    throw;
                }
                finally
                {
                    if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, nameof(OpenAsync));
                }
            },
            cancellationToken);
        }

        private async Task HandleDisconnect()
        {
            try
            {
                // No timeout on connection being established.
                await WaitForTransportClosedAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Canceled when the transport is being closed by the application.
                if (Logging.IsEnabled) Logging.Info(this, "Transport disconnected: closed by application.", nameof(HandleDisconnect));
                _onConnectionStatusChanged(ConnectionStatus.Disabled, ConnectionStatusChangeReason.Client_Close);

                return;
            }

            if (Logging.IsEnabled) Logging.Info(this, "Transport disconnected: unexpected.", nameof(HandleDisconnect));
            await _handlerLock.WaitAsync().ConfigureAwait(false);
            _opened = false;

            try
            {
                // No reason to reconnect at this moment.
                if (!(_methodsEnabled || _twinEnabled || _eventsEnabled || _deviceStreamingEnabled))
                {
                    _onConnectionStatusChanged(ConnectionStatus.Disconnected, ConnectionStatusChangeReason.Communication_Error);
                    return;
                }
                else
                {
                    _onConnectionStatusChanged(ConnectionStatus.Disconnected_Retrying, ConnectionStatusChangeReason.Communication_Error);
                }
                                
                using (var cts = new CancellationTokenSource((int)RetryTimeoutInMilliseconds))
                {
                    CancellationToken cancellationToken = cts.Token;

                    // This will recover to the state before the disconnect.
                    await _internalRetryPolicy.ExecuteAsync(async () =>
                    {
                        if (Logging.IsEnabled) Logging.Info(this, "Attempting to recover subscriptions.", nameof(HandleDisconnect));
                        
                        await base.OpenAsync(cancellationToken).ConfigureAwait(false);

                        List<Task> tasks = new List<Task>(3);

                        if (_methodsEnabled)
                        {
                            tasks.Add(base.EnableMethodsAsync(cancellationToken));
                        }

                        if (_twinEnabled)
                        {
                            tasks.Add(base.EnableTwinPatchAsync(cancellationToken));
                        }

                        if (_eventsEnabled)
                        {
                            tasks.Add(base.EnableEventReceiveAsync(cancellationToken));
                        }

                        if (_deviceStreamingEnabled)
                        {
                            tasks.Add(base.EnableStreamsAsync(cancellationToken));
                        }

                        Debug.Assert(tasks.Count > 0);
                        await Task.WhenAll(tasks).ConfigureAwait(false);

                        // Send the request for transport close notification.
                        _transportClosedTask = HandleDisconnect();

                        _opened = true;
                        _onConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);

                        if (Logging.IsEnabled) Logging.Info(this, "Subscriptions recovered.", nameof(HandleDisconnect));
                    },
                    cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled) Logging.Error(this, ex.ToString(), nameof(HandleDisconnect));

                var hubException = ex as IotHubException;
                if (hubException != null) HandleConnectionStatusExceptions(hubException);
            }
            finally
            {
                _handlerLock.Release();
            }
        }

        private void HandleConnectionStatusExceptions(IotHubException hubException)
        {
            ConnectionStatusChangeReason status = ConnectionStatusChangeReason.Communication_Error;

            if (hubException.IsTransient)
            {
                status = ConnectionStatusChangeReason.Retry_Expired;
            }
            else if (hubException is UnauthorizedException)
            {
                status = ConnectionStatusChangeReason.Bad_Credential;
            }
            else if (hubException is DeviceDisabledException)
            {
                status = ConnectionStatusChangeReason.Device_Disabled;
            }

            _onConnectionStatusChanged(ConnectionStatus.Disconnected, status);
        }
    }
}
