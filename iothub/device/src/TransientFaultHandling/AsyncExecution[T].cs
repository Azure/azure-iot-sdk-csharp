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

        protected static readonly TaskContinuationOptions s_taskContinuationOption = 
#if NET451
            TaskContinuationOptions.ExecuteSynchronously;
#else
            TaskContinuationOptions.RunContinuationsAsynchronously;
#endif

        private readonly Func<Task<TResult>> _taskFunc;
        private readonly ShouldRetry _shouldRetry;
        private readonly Func<Exception, bool> _isTransient;
        private readonly Action<int, Exception, TimeSpan> _onRetrying;
        private readonly bool _shouldFastFirstRetry;
        private readonly CancellationToken _cancellationToken;
        private readonly Stopwatch _stopwatch;
        private Task<TResult> _previousTask;
        private int _retryCount;

        public AsyncExecution(
            Func<Task<TResult>> taskFunc,
            ShouldRetry shouldRetry,
            Func<Exception, bool> isTransient,
            Action<int, Exception, TimeSpan> onRetrying,
            bool fastFirstRetry,
            CancellationToken cancellationToken)
        {
            _taskFunc = taskFunc;
            _shouldRetry = shouldRetry;
            _isTransient = isTransient;
            _onRetrying = onRetrying;
            _shouldFastFirstRetry = fastFirstRetry;
            _cancellationToken = cancellationToken;
            _stopwatch = new Stopwatch();
        }

        // This method exists to simply pass null to the first of two functional methods.
        // It takes null and that parameter is ignored because it'll be the result of Task.Delay
        // from the second method (ExecuteAsyncContinueWith).
        internal Task<TResult> ExecuteAsync()
        {
            return ExecuteAsyncImpl(null);
        }

        // This first of two methods kicks off the task specified in _taskFunc and then
        // uses Task.ContinueWith to evaluate the result in the second method.
        private Task<TResult> ExecuteAsyncImpl(Task ignore)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                if (_previousTask != null)
                {
                    return _previousTask;
                }
                var taskCompletionSource = CreateTaskCompletionSource();
                taskCompletionSource.TrySetCanceled();
                return taskCompletionSource.Task;
            }

            Task<TResult> task;
            try
            {
                task = _taskFunc();
            }
            catch (Exception ex) when (!_isTransient(ex))
            {
                var taskCompletionSource2 = CreateTaskCompletionSource();
                taskCompletionSource2.TrySetException(ex);
                task = taskCompletionSource2.Task;
            }

            if (task == null)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.TaskCannotBeNull,
                        new object[]
                        {
                            "taskFunc"
                        }),
                    "taskFunc");
            }

            if (task.Status == TaskStatus.RanToCompletion)
            {
                return task;
            }

            if (task.Status == TaskStatus.Created)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.TaskMustBeScheduled,
                        new object[]
                        {
                            "taskFunc"
                        }),
                    "taskFunc");
            }

            _stopwatch.Restart();

            return task
                .ContinueWith(
                    new Func<Task<TResult>,
                    Task<TResult>>(ExecuteAsyncContinueWith),
                    CancellationToken.None,
                    s_taskContinuationOption,
                    TaskScheduler.Default)
                .Unwrap();
        }

        // This second method evalutes the result of the task after completion
        // and decides if it is done/cancelled/exhausted-retries, or if it should
        // run again. If it should retry, it calls the first method after a possible
        // delay.
        private Task<TResult> ExecuteAsyncContinueWith(Task<TResult> runningTask)
        {
            if (!runningTask.IsFaulted
                || _cancellationToken.IsCancellationRequested)
            {
                return runningTask;
            }
            TimeSpan backoffDelay = TimeSpan.Zero;
            Exception innerException = runningTask.Exception.InnerException;

            long executionTime = _stopwatch.ElapsedMilliseconds;

            if (innerException is RetryLimitExceededException)
            {
                var taskCompletionSource = CreateTaskCompletionSource();
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

            if (!_isTransient(innerException)
                || !_shouldRetry(_retryCount++, innerException, out backoffDelay))
            {
                return runningTask;
            }

            if (backoffDelay < TimeSpan.Zero)
            {
                backoffDelay = TimeSpan.Zero;
            }

            _onRetrying(_retryCount, innerException, backoffDelay);
            _previousTask = runningTask;

            if (backoffDelay > TimeSpan.Zero
                && (_retryCount > 1
                    || !_shouldFastFirstRetry))
            {
                if (executionTime + backoffDelay.TotalMilliseconds < MinimumTimeBetweenRetriesMiliseconds)
                {
                    double newBackoffTimeMiliseconds = MinimumTimeBetweenRetriesMiliseconds - executionTime;
                    Debug.WriteLine($"{_cancellationToken.GetHashCode()} Last execution time was {executionTime}. Adjusting back-off time to {newBackoffTimeMiliseconds} to avoid high CPU/Memory spikes.");
                    backoffDelay = TimeSpan.FromMilliseconds(newBackoffTimeMiliseconds);
                }

                return Task
                    .Delay(backoffDelay)
                    .ContinueWith(new Func<Task, Task<TResult>>(ExecuteAsyncImpl), CancellationToken.None, s_taskContinuationOption, TaskScheduler.Default)
                    .Unwrap();
            }

            return ExecuteAsyncImpl(null);
        }

        protected static TaskCompletionSource<TResult> CreateTaskCompletionSource()
        {
            return new TaskCompletionSource<TResult>(
#if !NET451
                TaskCreationOptions.RunContinuationsAsynchronously
#endif
            );
        }
    }
}
