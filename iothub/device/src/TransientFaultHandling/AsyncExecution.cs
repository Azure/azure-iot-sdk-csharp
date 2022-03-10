// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//Copyright(c) Microsoft.All rights reserved.
//Microsoft would like to thank its contributors, a list
//of whom are at http://aka.ms/entlib-contributors

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.TransientFaultHandling.Properties;

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
// 3/11/2020 drwill Misc code cleanup for code style and fxcopy warnings

namespace Microsoft.Azure.Devices.Client.TransientFaultHandling
{
    /// <summary>
    /// Provides a wrapper for a non-generic <see cref="Task" /> and calls into the pipeline
    /// to retry only the generic version of the <see cref="Task" />.
    /// </summary>
    internal class AsyncExecution : AsyncExecution<bool>
    {
        private static Task<bool> s_cachedBoolTask;

        public AsyncExecution(
            Func<Task> taskAction,
            ShouldRetry shouldRetry,
            Func<Exception, bool> isTransient,
            Action<int, Exception, TimeSpan> onRetrying,
            bool fastFirstRetry,
            CancellationToken cancellationToken)
            : base(() => StartAsGenericTask(taskAction), shouldRetry, isTransient, onRetrying, fastFirstRetry, cancellationToken)
        {
        }

        /// <summary>
        /// Wraps the non-generic <see cref="Task" /> into a generic <see cref="Task" />.
        /// </summary>
        /// <param name="taskAction">The task to wrap.</param>
        /// <returns>A <see cref="Task" /> that wraps the non-generic <see cref="Task" />.</returns>
        private static Task<bool> StartAsGenericTask(Func<Task> taskAction)
        {
            Task task = taskAction();

            if (task == null)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.TaskCannotBeNull,
                        new object[]
                        {
                            "taskAction"
                        }),
                    nameof(taskAction));
            }

            if (task.Status == TaskStatus.RanToCompletion)
            {
                return GetCachedTask();
            }

            if (task.Status == TaskStatus.Created)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.TaskMustBeScheduled,
                        new object[]
                        {
                            "taskAction"
                        }),
                    nameof(taskAction));
            }

            TaskCompletionSource<bool> tcs = CreateTaskCompletionSource();
            task.ContinueWith(
                delegate (Task t)
                  {
                      if (t.IsFaulted)
                      {
                          tcs.TrySetException(t.Exception.InnerExceptions);
                          return;
                      }
                      if (t.IsCanceled)
                      {
                          tcs.TrySetCanceled();
                          return;
                      }
                      tcs.TrySetResult(true);
                  },
                CancellationToken.None,
                s_taskContinuationOption,
                TaskScheduler.Default);

            return tcs.Task;
        }

        private static Task<bool> GetCachedTask()
        {
            if (s_cachedBoolTask == null)
            {
                TaskCompletionSource<bool> taskCompletionSource = CreateTaskCompletionSource();
                taskCompletionSource.TrySetResult(true);
                s_cachedBoolTask = taskCompletionSource.Task;
            }

            return s_cachedBoolTask;
        }
    }
}
