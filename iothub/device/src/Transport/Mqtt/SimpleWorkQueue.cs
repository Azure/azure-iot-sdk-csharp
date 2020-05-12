// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// Simple work queue with lifecycle state machine.
    /// It is running work items if it is available; otherwise waits for new work item.
    /// Worker will resume work as soon as new work has arrived.
    /// </summary>
    /// <typeparam name="TWork"></typeparam>
    internal class SimpleWorkQueue<TWork>
    {
        private readonly Func<IChannelHandlerContext, TWork, Task> _worker;

        private readonly Queue<TWork> _backlogQueue;
        private readonly TaskCompletionSource _completionSource;
        protected States State { get; set; }

        public SimpleWorkQueue(Func<IChannelHandlerContext, TWork, Task> worker)
        {
            _worker = worker;
            _completionSource = new TaskCompletionSource();
            _backlogQueue = new Queue<TWork>();
        }

        public Task Completion => _completionSource.Task;

        /// <summary>
        /// Current backlog size
        /// </summary>
        public int BacklogSize => _backlogQueue.Count;

        /// <summary>
        /// Puts the new work to backlog queue and resume work if worker is idle.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="workItem"></param>
        public virtual void Post(IChannelHandlerContext context, TWork workItem)
        {
            switch (State)
            {
                case States.Idle:
                    _backlogQueue.Enqueue(workItem);
                    State = States.Processing;
                    StartWorkQueueProcessingAsync(context);
                    break;

                case States.Processing:
                case States.FinalProcessing:
                    _backlogQueue.Enqueue(workItem);
                    break;

                case States.Aborted:
                    ReferenceCountUtil.Release(workItem);
                    break;

                default:
#pragma warning disable CA2208 // Instantiate argument exceptions correctly - should not change exception type now, even though it is the wrong type
                    throw new ArgumentOutOfRangeException(nameof(State), "Unexpected state.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
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
                    _completionSource.TryComplete();
                    break;

                case States.Processing:
                    State = States.FinalProcessing;
                    break;

                case States.FinalProcessing:
                case States.Aborted:
                    break;

                default:
#pragma warning disable CA2208 // Instantiate argument exceptions correctly - should not change exception type now, even though it is the wrong type
                    throw new ArgumentOutOfRangeException(nameof(State), "Unexpected state.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
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

                    Queue<TWork> queue = _backlogQueue;
                    while (queue.Count > 0)
                    {
                        TWork workItem = queue.Dequeue();
                        ReferenceCountUtil.Release(workItem);
                        if (exception == null)
                        {
                            (workItem as ICancellable)?.Cancel();
                        }
                        else
                        {
                            (workItem as ICancellable)?.Abort(exception);
                        }
                    }
                    break;

                case States.Aborted:
                    break;

                default:
#pragma warning disable CA2208 // Instantiate argument exceptions correctly - should not change exception type now, even though it is the wrong type
                    throw new ArgumentOutOfRangeException(nameof(State), "Unexpected state.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }
        }

        protected virtual Task DoWorkAsync(IChannelHandlerContext context, TWork work)
        {
            return _worker(context, work);
        }

        private async void StartWorkQueueProcessingAsync(IChannelHandlerContext context)
        {
            try
            {
                Queue<TWork> queue = _backlogQueue;
                while (queue.Count > 0 && State != States.Aborted)
                {
                    TWork workItem = queue.Dequeue();
                    await DoWorkAsync(context, workItem).ConfigureAwait(false);
                }

                switch (State)
                {
                    case States.Processing:
                        State = States.Idle;
                        break;

                    case States.FinalProcessing:
                    case States.Aborted:
                        _completionSource.TryComplete();
                        break;

                    default:
#pragma warning disable CA2208 // Instantiate argument exceptions correctly - should not change exception type now, even though it is the wrong type
                        throw new ArgumentOutOfRangeException(nameof(State), "Unexpected state.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
                }
            }
            catch (Exception ex)
            {
                Abort();
                _completionSource.TrySetException(new ChannelMessageProcessingException(ex, context));
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
