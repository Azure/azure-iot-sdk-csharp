// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal class OrderedTwoPhaseWorkQueue<TWorkId, TWork> : SimpleWorkQueue<TWork> where TWorkId : IEquatable<TWorkId>
    {
        private class IncompleteWorkItem
        {
            public IncompleteWorkItem(TWorkId id, TWork workItem)
            {
                WorkItem = workItem ?? throw new ArgumentNullException(nameof(workItem));
                Id = id ?? throw new ArgumentNullException(nameof(id));
            }

            public TWork WorkItem { get; }

            public TWorkId Id { get; }
        }

        private readonly Func<TWork, TWorkId> _getWorkId;
        private readonly Func<IChannelHandlerContext, TWork, Task> _completeWorkAsync;
        private readonly Queue<IncompleteWorkItem> _incompleteQueue = new Queue<IncompleteWorkItem>();
        private SemaphoreSlim _queueSemaphore = new SemaphoreSlim(1, 1);

        private bool _disposed;

        public OrderedTwoPhaseWorkQueue(
            Func<IChannelHandlerContext, TWork, Task> workerAsync,
            Func<TWork, TWorkId> getWorkId,
            Func<IChannelHandlerContext, TWork, Task> completeWorkAsync)
            : base(workerAsync)
        {
            _getWorkId = getWorkId ?? throw new ArgumentNullException(nameof(getWorkId));
            _completeWorkAsync = completeWorkAsync ?? throw new ArgumentNullException(nameof(completeWorkAsync));
        }

        public Task CompleteWorkAsync(IChannelHandlerContext context, TWorkId workId)
        {
            if (!_incompleteQueue.Any())
            {
                throw new IotHubException("Nothing to complete.", isTransient: false);
            }

            IncompleteWorkItem incompleteWorkItem = _incompleteQueue.Peek();
            if (incompleteWorkItem.Id.Equals(workId))
            {
                Dequeue();
                return _completeWorkAsync(context, incompleteWorkItem.WorkItem);
            }

            throw new IotHubException(
                $"Work must be complete in the same order as it was started. Expected work id: '{incompleteWorkItem.Id}', actual work id: '{workId}'",
                isTransient: false);
        }

        protected override async Task DoWorkAsync(IChannelHandlerContext context, TWork work)
        {
            Enqueue(new IncompleteWorkItem(_getWorkId(work), work));
            await base.DoWorkAsync(context, work).ConfigureAwait(false);
        }

        public override void Abort()
        {
            Abort(null);
        }

        public override void Abort(Exception exception)
        {
            States stateBefore = State;
            base.Abort(exception);

            if (stateBefore != State
                && State == States.Aborted)
            {
                while (_incompleteQueue.Any())
                {
                    var workItem = Dequeue().WorkItem as ICancellable;

                    if (exception == null)
                    {
                        workItem?.Cancel();
                    }
                    else
                    {
                        workItem?.Abort(exception);
                    }
                }
            }
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                _queueSemaphore?.Dispose();
                _queueSemaphore = null;
                _disposed = true;
            }
        }

        private void Enqueue(IncompleteWorkItem workItem)
        {
            try
            {
                _queueSemaphore.Wait();
                _incompleteQueue.Enqueue(workItem);
            }
            finally
            {
                _queueSemaphore.Release();
            }
        }

        private IncompleteWorkItem Dequeue()
        {
            try
            {
                _queueSemaphore.Wait();
                return _incompleteQueue.Dequeue();
            }
            finally
            {
                _queueSemaphore.Release();
            }
        }
    }
}
