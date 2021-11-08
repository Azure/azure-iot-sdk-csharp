// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

#if NET5_0
using TaskCompletionSource = System.Threading.Tasks.TaskCompletionSource;
#else
using TaskCompletionSource = Microsoft.Azure.Devices.Shared.TaskCompletionSource;
#endif

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// Simple work queue with lifecycle state machine.
    /// It is running work items if it is available; otherwise waits for new work item.
    /// Worker will resume work as soon as new work has arrived.
    /// </summary>
    /// <typeparam name="TWork">The work to perform.</typeparam>
    internal class SimpleWorkQueue<TWork> : IDisposable
    {
        private readonly Func<IChannelHandlerContext, TWork, Task> _workerAsync;
        private readonly Queue<TWork> _backlogQueue;
        private readonly TaskCompletionSource _completionSource;
        private SemaphoreSlim _queueSemaphore = new SemaphoreSlim(1, 1);

        private bool _disposed;

        public SimpleWorkQueue(Func<IChannelHandlerContext, TWork, Task> workerAsync)
        {
            _workerAsync = workerAsync;
            _completionSource = new TaskCompletionSource();
            _backlogQueue = new Queue<TWork>();
        }

        protected States State { get; set; }

        public Task Completion => _completionSource.Task;

        /// <summary>
        /// Current backlog size
        /// </summary>
        public int BacklogSize => _backlogQueue.Count;

        /// <summary>
        /// Puts the new work to backlog queue and resume work if worker is idle.
        /// </summary>
        /// <param name="context">Context of the work for when performing it.</param>
        /// <param name="workItem">The work to perform.</param>
        public virtual void Post(IChannelHandlerContext context, TWork workItem)
        {
            switch (State)
            {
                case States.Idle:
                    Enqueue(workItem);
                    State = States.Processing;
                    StartWorkQueueProcessingAsync(context);
                    break;

                case States.Processing:
                case States.FinalProcessing:
                    Enqueue(workItem);
                    break;

                case States.Aborted:
                    ReferenceCountUtil.Release(workItem);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(State), "Unexpected state.");
            }
        }

        /// <summary>
        /// Stops work gracefully. All backlog will be processed.
        /// </summary>
        public void Complete()
        {
            switch (State)
            {
                case States.Idle:
                    _completionSource.TrySetResult();
                    break;

                case States.Processing:
                    State = States.FinalProcessing;
                    break;

                case States.FinalProcessing:
                case States.Aborted:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(State), "Unexpected state.");
            }
        }

        public virtual void Abort()
        {
            Abort(null);
        }

        public virtual void Abort(Exception exception)
        {
            switch (State)
            {
                case States.Idle:
                case States.Processing:
                case States.FinalProcessing:
                    State = States.Aborted;

                    while (_backlogQueue.Any())
                    {
                        TWork workItem = Dequeue();
                        ReferenceCountUtil.Release(workItem);

                        var cancellableWorkItem = workItem as ICancellable;
                        if (exception == null)
                        {
                            cancellableWorkItem?.Cancel();
                        }
                        else
                        {
                            cancellableWorkItem?.Abort(exception);
                        }
                    }
                    break;

                case States.Aborted:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(State), "Unexpected state.");
            }
        }

        protected virtual Task DoWorkAsync(IChannelHandlerContext context, TWork work)
        {
            return _workerAsync(context, work);
        }

        private async void StartWorkQueueProcessingAsync(IChannelHandlerContext context)
        {
            try
            {
                while (_backlogQueue.Any()
                    && State != States.Aborted)
                {
                    TWork workItem = Dequeue();
                    await DoWorkAsync(context, workItem).ConfigureAwait(false);
                }

                switch (State)
                {
                    case States.Processing:
                        State = States.Idle;
                        break;

                    case States.FinalProcessing:
                    case States.Aborted:
                        _completionSource.TrySetResult();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(State), "Unexpected state.");
                }
            }
            catch (Exception ex)
            {
                Abort();
                _completionSource.TrySetException(new ChannelMessageProcessingException(ex, context));
            }
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                _queueSemaphore?.Dispose();
                _queueSemaphore = null;
                _disposed = true;
            }
        }

        private void Enqueue(TWork workItem)
        {
            try
            {
                _queueSemaphore.Wait();
                _backlogQueue.Enqueue(workItem);
            }
            finally
            {
                _queueSemaphore.Release();
            }
        }

        private TWork Dequeue()
        {
            try
            {
                _queueSemaphore.Wait();
                return _backlogQueue.Dequeue();
            }
            finally
            {
                _queueSemaphore.Release();
            }
        }

        protected enum States
        {
            Idle,
            Processing,
            FinalProcessing,
            Aborted,
        }
    }
}
