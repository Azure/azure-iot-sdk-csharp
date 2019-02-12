// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
#if !NETSTANDARD1_3
    using System.Transactions;
#endif
    static class TaskHelpers
    {
#if NET451
        public static readonly Task CompletedTask = Task.FromResult<bool>(false);
#else
        public static readonly Task CompletedTask = Task.CompletedTask;
#endif

        public static IAsyncResult ToAsyncResult(this Task task, AsyncCallback callback, object state)
        {
            if (task.AsyncState == state)
            {
                if (callback != null)
                {
                    task.ContinueWith(
                        t => callback(task),
                        TaskContinuationOptions.ExecuteSynchronously);
                }

                return task;
            }

            var tcs = new TaskCompletionSource<object>(state);
            task.ContinueWith(
                _ =>
                {
                    if (task.IsFaulted)
                    {
                        tcs.TrySetException(task.Exception.InnerExceptions);
                    }
                    else if (task.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        tcs.TrySetResult(null);
                    }

                    if (callback != null)
                    {
                        callback(tcs.Task);
                    }
                },
                TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        public static IAsyncResult ToAsyncResult<TResult>(this Task<TResult> task, AsyncCallback callback, object state)
        {
            if (task.AsyncState == state)
            {
                if (callback != null)
                {
                    task.ContinueWith(
                        t => callback(task),
                        TaskContinuationOptions.ExecuteSynchronously);
                }

                return task;
            }

            var tcs = new TaskCompletionSource<TResult>(state);
            task.ContinueWith(
                _ =>
                {
                    if (task.IsFaulted)
                    {
                        tcs.TrySetException(task.Exception.InnerExceptions);
                    }
                    else if (task.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        tcs.TrySetResult(task.Result);
                    }

                    if (callback != null)
                    {
                        callback(tcs.Task);
                    }
                },
                TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        internal static void MarshalTaskResults<TResult>(Task source, TaskCompletionSource<TResult> proxy)
        {
            Fx.Assert(source != null, "source Task is required!");
            Fx.Assert(proxy != null, "proxy TaskCompletionSource is required!");

            switch (source.Status)
            {
                case TaskStatus.Faulted:
                    var exception = source.Exception.GetBaseException();
                    proxy.TrySetException(exception);
                    break;
                case TaskStatus.Canceled:
                    proxy.TrySetCanceled();
                    break;
                case TaskStatus.RanToCompletion:
                    Task<TResult> castedSource = source as Task<TResult>;
                    proxy.TrySetResult(
                        castedSource == null ? default(TResult) : // source is a Task
                            castedSource.Result); // source is a Task<TResult>
                    break;
            }
        }

        public static async Task WithTimeout(this Task task, TimeSpan timeout, Func<string> errorMessage, CancellationToken token)
        {
            if (timeout == TimeSpan.MaxValue)
            {
                timeout = Timeout.InfiniteTimeSpan;
            }

            if (task.IsCanceled || token.IsCancellationRequested)
            {
                Debug.WriteLine(token.GetHashCode() + " WithTimeout: task canceled before adding delay task.");
                throw new TimeoutException(errorMessage());
            }

            if (task.IsCompleted || (timeout == Timeout.InfiniteTimeSpan && token == CancellationToken.None))
            {
                Debug.WriteLine(token.GetHashCode() + " WithTimeout: task completed before adding delay task.");
                await task.ConfigureAwait(false);
                return;
            }

            using (var delayCts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                if (task == await Task.WhenAny(task, Task.Delay(timeout, delayCts.Token)).ConfigureAwait(false))
                {
                    Debug.WriteLine(token.GetHashCode() + " WithTimeout: task completed.");

                    delayCts.Cancel();
                    await task.ConfigureAwait(false);
                    return;
                }
            }

            Debug.WriteLine(token.GetHashCode() + " WithTimeout: task timed-out.");

            throw new TimeoutException(errorMessage());
        }
    }

}
