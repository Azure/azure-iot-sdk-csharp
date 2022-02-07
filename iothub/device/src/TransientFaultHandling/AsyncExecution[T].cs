//Copyright(c) Microsoft.All rights reserved.
//Microsoft would like to thank its contributors, a list
//of whom are at http://aka.ms/entlib-contributors

using Microsoft.Azure.Devices.Client.TransientFaultHandling.Properties;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

//Licensed under the Apache License, Version 2.0 (the "License"); you
//may not use this file except in compliance with the License. You may
//obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
//implied. See the License for the specific language governing permissions
//and limitations under the License.

// THIS FILE HAS BEEN MODIFIED FROM ITS ORIGINAL FORM.
// Change Log:
// 9/1/2017 jasminel Renamed namespace to Microsoft.Azure.Devices.Client.TransientFaultHandling.
// 12/13/2017 crispop Adding minimum time between retries.

namespace Microsoft.Azure.Devices.Client.TransientFaultHandling
{
    /// <summary>
    /// Handles the execution and retries of the user-initiated task.
    /// </summary>
    /// <typeparam name="TResult">The result type of the user-initiated task.</typeparam>
    internal class AsyncExecution<TResult>
    {
        internal const int MinimumTimeBetweenRetriesMiliseconds = 1000;

        private readonly Func<Task<TResult>> taskFunc;

        private readonly ShouldRetry shouldRetry;

        private readonly Func<Exception, bool> isTransient;

        private readonly Action<int, Exception, TimeSpan> onRetrying;

        private readonly bool fastFirstRetry;

        private readonly CancellationToken cancellationToken;

        private readonly Stopwatch stopwatch;

        private Task<TResult> previousTask;

        private int retryCount;

        public AsyncExecution(Func<Task<TResult>> taskFunc, ShouldRetry shouldRetry, Func<Exception, bool> isTransient, Action<int, Exception, TimeSpan> onRetrying, bool fastFirstRetry, CancellationToken cancellationToken)
        {
            this.taskFunc = taskFunc;
            this.shouldRetry = shouldRetry;
            this.isTransient = isTransient;
            this.onRetrying = onRetrying;
            this.fastFirstRetry = fastFirstRetry;
            this.cancellationToken = cancellationToken;
            this.stopwatch = new Stopwatch();
        }

        internal Task<TResult> ExecuteAsync()
        {
            return this.ExecuteAsyncImpl(null);
        }

        private Task<TResult> ExecuteAsyncImpl(Task ignore)
        {
            if (this.cancellationToken.IsCancellationRequested)
            {
                if (this.previousTask != null)
                {
                    return this.previousTask;
                }
                var taskCompletionSource = new TaskCompletionSource<TResult>();
                taskCompletionSource.TrySetCanceled();
                return taskCompletionSource.Task;
            }
            else
            {
                Task<TResult> task;
                try
                {
                    task = this.taskFunc();
                }
                catch (Exception ex)
                {
                    if (!this.isTransient(ex))
                    {
                        throw;
                    }
                    var taskCompletionSource2 = new TaskCompletionSource<TResult>();
                    taskCompletionSource2.TrySetException(ex);
                    task = taskCompletionSource2.Task;
                }
                if (task == null)
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.TaskCannotBeNull, new object[]
                    {
                        "taskFunc"
                    }), "taskFunc");
                }
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    return task;
                }
                if (task.Status == TaskStatus.Created)
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.TaskMustBeScheduled, new object[]
                    {
                        "taskFunc"
                    }), "taskFunc");
                }

                stopwatch.Restart();

                return task.ContinueWith<Task<TResult>>(new Func<Task<TResult>, Task<TResult>>(this.ExecuteAsyncContinueWith), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap<TResult>();
            }
        }

        private Task<TResult> ExecuteAsyncContinueWith(Task<TResult> runningTask)
        {
            if (!runningTask.IsFaulted || this.cancellationToken.IsCancellationRequested)
            {
                return runningTask;
            }
            TimeSpan zero = TimeSpan.Zero;
            Exception innerException = runningTask.Exception.InnerException;

            long executionTime = stopwatch.ElapsedMilliseconds;

            if (innerException is RetryLimitExceededException)
            {
                var taskCompletionSource = new TaskCompletionSource<TResult>();
                if (innerException.InnerException != null)
                {
                    taskCompletionSource.TrySetException(innerException.InnerException);
                }
                else
                {
                    taskCompletionSource.TrySetCanceled();
                }
                return taskCompletionSource.Task;
            }
            if (!this.isTransient(innerException) || !this.shouldRetry(this.retryCount++, innerException, out zero))
            {
                return runningTask;
            }
            if (zero < TimeSpan.Zero)
            {
                zero = TimeSpan.Zero;
            }
            this.onRetrying(this.retryCount, innerException, zero);
            this.previousTask = runningTask;
            if (zero > TimeSpan.Zero && (this.retryCount > 1 || !this.fastFirstRetry))
            {
                if (executionTime + zero.TotalMilliseconds < MinimumTimeBetweenRetriesMiliseconds)
                {
                    double newBackoffTimeMiliseconds = MinimumTimeBetweenRetriesMiliseconds - executionTime;
                    Debug.WriteLine(
                        this.cancellationToken.GetHashCode() + " Last execution time was " + executionTime + ". Adjusting back-off time to " +
                        newBackoffTimeMiliseconds + " to avoid high CPU/Memory spikes.");
                    zero = TimeSpan.FromMilliseconds(newBackoffTimeMiliseconds);
                }

                return Task.Delay(zero).ContinueWith<Task<TResult>>(new Func<Task, Task<TResult>>(this.ExecuteAsyncImpl), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap<TResult>();
            }
            return this.ExecuteAsyncImpl(null);
        }
    }
}
