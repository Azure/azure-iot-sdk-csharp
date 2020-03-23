// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
                this.WorkItem = workItem;
                this.Id = id;
            }

            public TWork WorkItem { get; }

            public TWorkId Id { get; }
        }

        private readonly Func<TWork, TWorkId> getWorkId;
        private readonly Func<IChannelHandlerContext, TWork, Task> completeWork;
        private readonly Queue<IncompleteWorkItem> incompleteQueue = new Queue<IncompleteWorkItem>();

        public OrderedTwoPhaseWorkQueue(Func<IChannelHandlerContext, TWork, Task> worker, Func<TWork, TWorkId> getWorkId, Func<IChannelHandlerContext, TWork, Task> completeWork)
            : base(worker)
        {
            this.getWorkId = getWorkId;
            this.completeWork = completeWork;
        }

        public Task CompleteWorkAsync(IChannelHandlerContext context, TWorkId workId)
        {
            if (this.incompleteQueue.Count == 0)
            {
                throw new IotHubException("Nothing to complete.", isTransient: false);
            }
            IncompleteWorkItem incompleteWorkItem = this.incompleteQueue.Peek();
            if (incompleteWorkItem.Id.Equals(workId))
            {
                this.incompleteQueue.Dequeue();
                return this.completeWork(context, incompleteWorkItem.WorkItem);
            }
            throw new IotHubException(
                $"Work must be complete in the same order as it was started. Expected work id: '{incompleteWorkItem.Id}', actual work id: '{workId}'",
                isTransient: false);
        }

        protected override async Task DoWorkAsync(IChannelHandlerContext context, TWork work)
        {
            this.incompleteQueue.Enqueue(new IncompleteWorkItem(this.getWorkId(work), work));
            await base.DoWorkAsync(context, work).ConfigureAwait(false);
        }

        public override void Abort()
        {
            this.Abort(null);
        }

        public override void Abort(Exception exception)
        {
            States stateBefore = this.State;
            base.Abort(exception);
            if (stateBefore != this.State && this.State == States.Aborted)
            {
                Queue<IncompleteWorkItem> queue = this.incompleteQueue;
                while (queue.Count > 0)
                {
                    TWork workItem = queue.Dequeue().WorkItem;
                    if (exception == null)
                    {
                        (workItem as ICancellable)?.Cancel();
                    }
                    else
                    {
                        (workItem as ICancellable)?.Abort(exception);
                    }
                }
            }
        }
    }
}
