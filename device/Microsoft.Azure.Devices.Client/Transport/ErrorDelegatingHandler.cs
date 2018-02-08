// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Shared;
    using System.Diagnostics;

    sealed class ErrorDelegatingHandler : DefaultDelegatingHandler
    {

        internal static readonly HashSet<Type> TransientExceptions = new HashSet<Type>
        {
            typeof(IotHubClientTransientException),
            typeof(IotHubCommunicationException),
            typeof(ServerBusyException),
            typeof(IOException),
            typeof(TimeoutException),
            typeof(ObjectDisposedException),
            typeof(OperationCanceledException),
            typeof(TaskCanceledException),
            typeof(IotHubThrottledException),
#if !PCL && !WINDOWS_UWP
            typeof(System.Net.Sockets.SocketException),
#endif
        };

        internal static readonly HashSet<Type> TransportTransientExceptions = new HashSet<Type>
        {
            typeof(IotHubThrottledException),
            typeof(IotHubClientTransientException),
            typeof(ServerBusyException),
            typeof(OperationCanceledException),
            typeof(TaskCanceledException),
        };

        TaskCompletionSource<int> openCompletion;

        public ErrorDelegatingHandler(IPipelineContext context)
            : base(context)
        {
        }

        public override async Task OpenAsync(bool explicitOpen, CancellationToken cancellationToken)
        {
            Debug.WriteLine(cancellationToken.GetHashCode() + " ErrorDelegatingHandler.OpenAsync()");

            TaskCompletionSource<int> openCompletionBeforeOperationStarted = Volatile.Read(ref this.openCompletion);
            if (openCompletionBeforeOperationStarted == null)
            {
                openCompletionBeforeOperationStarted = new TaskCompletionSource<int>();
                TaskCompletionSource<int> currentOpenPromise;
                if ((currentOpenPromise = Interlocked.CompareExchange(ref this.openCompletion, openCompletionBeforeOperationStarted, null)) == null)
                {
                    IDelegatingHandler handlerBeforeOperationStarted = this.ContinuationFactory(Context);
                    this.InnerHandler = handlerBeforeOperationStarted;
                    try
                    {
                        await this.ExecuteWithErrorHandlingAsync(() => base.OpenAsync(explicitOpen, cancellationToken), false, cancellationToken).ConfigureAwait(false);
                        openCompletionBeforeOperationStarted.TrySetResult(0);
                    }
                    catch (Exception ex) when (IsTransportHandlerStillUsable(ex))
                    {
                        Debug.WriteLine(cancellationToken.GetHashCode() + " ErrorDelegatingHandler.OpenAsync() Reset " + ex.Message);
                        await this.Reset(openCompletionBeforeOperationStarted, handlerBeforeOperationStarted).ConfigureAwait(false);
                        throw;
                    }
                    catch (Exception ex) when (!ex.IsFatal())
                    {
                        Debug.WriteLine(cancellationToken.GetHashCode() + " ErrorDelegatingHandler.OpenAsync() Exception " + ex.Message);
                        throw;
                    }
                }
                else
                {
                    Debug.WriteLine(cancellationToken.GetHashCode() + " ErrorDelegatingHandler.OpenAsync() Awaiting new Open task");
                    await currentOpenPromise.Task.ConfigureAwait(false);
                }
            }
            else
            {
                Debug.WriteLine(cancellationToken.GetHashCode() + " ErrorDelegatingHandler.OpenAsync() Awaiting existing Open task");
                await openCompletionBeforeOperationStarted.Task.ConfigureAwait(false);
            }
        }

        public override Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.ReceiveAsync(cancellationToken), true, cancellationToken);
        }

        public override Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.ReceiveAsync(timeout, cancellationToken), true, cancellationToken);
        }

        public override Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.EnableMethodsAsync(cancellationToken), true, cancellationToken);
        }

        public override Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.DisableMethodsAsync(cancellationToken), true, cancellationToken);
        }

        public override Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.EnableEventReceiveAsync(cancellationToken), true, cancellationToken);
        }

        public override Task DisableEventReceiveAsync(CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.DisableEventReceiveAsync(cancellationToken), true, cancellationToken);
        }

        public override Task RecoverConnections(object o, ConnectionType connectionType, CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.RecoverConnections(o, connectionType, cancellationToken), true, cancellationToken);
        }

        public override Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.EnableTwinPatchAsync(cancellationToken), true, cancellationToken);
        }
        
        public override Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.SendTwinGetAsync(cancellationToken), true, cancellationToken);
        }
        
        public override Task SendTwinPatchAsync(TwinCollection reportedProperties,  CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.SendTwinPatchAsync(reportedProperties, cancellationToken), true, cancellationToken);
        }

        public override Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.AbandonAsync(lockToken, cancellationToken), true, cancellationToken);
        }

        public override Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.CompleteAsync(lockToken, cancellationToken), true, cancellationToken);
        }

        public override Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.RejectAsync(lockToken, cancellationToken), true, cancellationToken);
        }

        public override Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            try
            {
                Debug.WriteLine(cancellationToken.GetHashCode() + " ErrorDelegatingHandler.SendEventAsync() ENTER");
                return this.ExecuteWithErrorHandlingAsync(() => base.SendEventAsync(messages, cancellationToken), true, cancellationToken);
            }
            finally
            {
                Debug.WriteLine(cancellationToken.GetHashCode() + " ErrorDelegatingHandler.SendEventAsync() EXIT");
            }
        }

        public override Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.SendEventAsync(message, cancellationToken), true, cancellationToken);
        }

        public override Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            return this.ExecuteWithErrorHandlingAsync(() => base.SendMethodResponseAsync(methodResponse, cancellationToken), true, cancellationToken);
        }

        Task ExecuteWithErrorHandlingAsync(Func<Task> asyncOperation, bool ensureOpen, CancellationToken cancellationToken)
        {
            return ExecuteWithErrorHandlingAsync(async () => { await asyncOperation().ConfigureAwait(false); return 0; }, ensureOpen, cancellationToken);
        }

        async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> asyncOperation, bool ensureOpen, CancellationToken cancellationToken)
        {
            if (ensureOpen)
            {
                await this.EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
            }

            TaskCompletionSource<int> openCompletionBeforeOperationStarted = Volatile.Read(ref this.openCompletion);
            IDelegatingHandler handlerBeforeOperationStarted = this.InnerHandler;

            try
            {
                return await asyncOperation().ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                if (IsTransient(ex))
                {
                    if (IsTransportHandlerStillUsable(ex))
                    {
                        if (ex is IotHubClientTransientException)
                        {
                            throw;
                        }
                        throw new IotHubClientTransientException("Transient error occurred, please retry.", ex);
                    }
                    await this.Reset(openCompletionBeforeOperationStarted, handlerBeforeOperationStarted).ConfigureAwait(false);
                    if (ex is IotHubClientTransientException)
                    {
                        throw;
                    }
                    throw new IotHubClientTransientException("Transient error occurred, please retry.", ex);
                }
                else
                {
                    await this.Reset(openCompletionBeforeOperationStarted, handlerBeforeOperationStarted).ConfigureAwait(false);
                    throw;
                }
            }
        }

        Task EnsureOpenAsync(CancellationToken cancellationToken)
        {
            return this.OpenAsync(false, cancellationToken);
        }

        static bool IsTransportHandlerStillUsable(Exception exception)
        {
            return exception.Unwind(true).Any(e => TransportTransientExceptions.Contains(e.GetType()));
        }

        static bool IsTransient(Exception exception)
        {
            return exception.Unwind(true).Any(e => TransientExceptions.Contains(e.GetType()));
        }

        async Task Reset(TaskCompletionSource<int> openCompletionBeforeOperationStarted, IDelegatingHandler handlerBeforeOperationStarted)
        {
            if (openCompletionBeforeOperationStarted == Volatile.Read(ref this.openCompletion))
            {
                if (Interlocked.CompareExchange(ref this.openCompletion, null, openCompletionBeforeOperationStarted) == openCompletionBeforeOperationStarted)
                {
                    await Cleanup(handlerBeforeOperationStarted).ConfigureAwait(false);
                }
            }
        }

        static async Task Cleanup(IDelegatingHandler handler)
        {
            try
            {
                if (handler != null)
                {
                    await handler.CloseAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                //unexpected behaviour - ignore. LOG?
            }
        }
    }
}
