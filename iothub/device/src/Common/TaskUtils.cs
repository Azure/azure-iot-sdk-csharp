// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Common
{
    /// <summary>
    /// Stephen Cleary approved
    /// http://stackoverflow.com/questions/15428604/how-to-run-a-task-on-a-custom-taskscheduler-using-await
    /// </summary>
    internal static class TaskUtils
    {
        private static readonly TaskFactory s_myTaskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, TaskScheduler.Default);

        internal static Task RunOnDefaultScheduler(this Func<Task> func)
        {
            return s_myTaskFactory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).Unwrap();
        }

        internal static Task<T> RunOnDefaultScheduler<T>(this Func<Task<T>> func)
        {
            return s_myTaskFactory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).Unwrap();
        }

        internal static Task RunOnDefaultScheduler(this Action func)
        {
            return s_myTaskFactory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        internal static Task<T> RunOnDefaultScheduler<T>(Func<T> func) => s_myTaskFactory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
    }
}
