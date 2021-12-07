﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal static class TaskHelpers
    {
        public static IAsyncResult ToAsyncResult<T>(this Task<T> task, AsyncCallback callback, object state)
        {
            if (task.AsyncState == state)
            {
                if (callback != null)
                {
                    task.ContinueWith(
                        (t, st) => ((AsyncCallback)state)(t),
                        callback,
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }

                return task;
            }

            var tcs = new TaskCompletionSource<T>(state);
            task.ContinueWith(
                t =>
                {
                    switch (t.Status)
                    {
                        case TaskStatus.RanToCompletion:
                            tcs.TrySetResult(t.Result);
                            break;
                        case TaskStatus.Canceled:
                            tcs.TrySetCanceled();
                            break;
                        case TaskStatus.Faulted:
                            tcs.TrySetException(t.Exception.InnerExceptions);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(task), "t.Status not defined");
                    }

                    callback?.Invoke(tcs.Task);
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return tcs.Task;
        }

        public static void EndAsyncResult(IAsyncResult asyncResult)
        {
            var task = asyncResult as Task;
            if (task == null)
            {
                throw new ArgumentException($"Given {nameof(asyncResult)} is not subclass of Task.");
            }

            try
            {
                task.Wait();
            }
            catch (AggregateException ae)
            {
                throw ae.GetBaseException();
            }
        }
    }
}
