// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.TransientFaultHandling.Properties;

namespace Microsoft.Azure.Devices.Client.TransientFaultHandling
{
    /// <summary>
    /// Runs an async task with retry policy.
    /// </summary>
    internal static class AsyncExecution
    {
        private static readonly TimeSpan s_minimumTimeBetweenRetries = TimeSpan.FromSeconds(1);

        public static async Task<T> RunAsync<T>(
            Func<Task<T>> taskFunc,
            ShouldRetry shouldRetry,
            Func<Exception, bool> isTransient,
            Action<int, Exception, TimeSpan> onRetrying,
            bool fastFirstRetry,
            CancellationToken cancellationToken)
        {
            if (taskFunc == null)
            {
                throw new ArgumentNullException(nameof(taskFunc), Resources.TaskCannotBeNull);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = new Stopwatch();
            int retryCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                TimeSpan retryDelay;
                try
                {
                    stopwatch.Restart();
                    return await taskFunc().ConfigureAwait(false);
                }
                catch (Exception ex) when (isTransient(ex)
                    && ex.InnerException != null
                    && isTransient(ex.InnerException)
                    && !(ex is RetryLimitExceededException)
                    && shouldRetry(retryCount++, ex.InnerException, out retryDelay))
                {
                    onRetrying(retryCount, ex.InnerException, retryDelay);
                }

                retryDelay -= stopwatch.Elapsed;
                if (retryDelay < s_minimumTimeBetweenRetries)
                {
                    retryDelay = s_minimumTimeBetweenRetries;
                    Debug.WriteLine(
                        $"{cancellationToken.GetHashCode()} Last execution time was {stopwatch.Elapsed}. Adjusting back-off time to {retryDelay} to avoid high CPU/Memory spikes.");

                    if (retryDelay > TimeSpan.Zero
                        && (retryCount > 1
                            || fastFirstRetry))
                    {
                        await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return default;
        }
        public static async Task RunAsync(
            Func<Task> taskFunc,
            ShouldRetry shouldRetry,
            Func<Exception, bool> isTransient,
            Action<int, Exception, TimeSpan> onRetrying,
            bool fastFirstRetry,
            CancellationToken cancellationToken)
        {
            Func<Task<bool>> wrapperTask = (Func<Task<bool>>)taskFunc;
            await RunAsync(
                    wrapperTask,
                    shouldRetry,
                    isTransient,
                    onRetrying,
                    fastFirstRetry,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
