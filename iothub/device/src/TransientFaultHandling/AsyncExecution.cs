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

        public static async Task RunAsync(
            Func<Task> taskFunc,
            ShouldRetry shouldRetry,
            Func<Exception, bool> isTransient,
            Action<int, Exception, TimeSpan> onRetrying,
            bool fastFirstRetry,
            CancellationToken cancellationToken)
        {
            Func<Task<bool>> taskWrapper = async () =>
            {
                await taskFunc();
                return true;
            };

            await RunAsync(
                    taskWrapper,
                    shouldRetry,
                    isTransient,
                    onRetrying,
                    fastFirstRetry,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public static async Task<T> RunAsync<T>(
            Func<Task<T>> taskFunc,
            ShouldRetry shouldRetry,
            Func<Exception, bool> isTransient,
            Action<int, Exception, TimeSpan> onRetrying,
            bool fastFirstRetry,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = new Stopwatch();
            int retryCount = 0;
            Exception lastException;

            do
            {
                TimeSpan retryDelay;
                try
                {
                    stopwatch.Restart();
                    return await taskFunc().ConfigureAwait(false);
                }
                catch (RetryLimitExceededException ex)
                {
                    if (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }

                    throw new OperationCanceledException();
                }
                catch (Exception ex) when (isTransient(ex)
                    && shouldRetry(retryCount++, ex, out retryDelay))
                {
                    lastException = ex;
                    onRetrying(retryCount, ex, retryDelay);
                }

                stopwatch.Stop();

                if (retryDelay > TimeSpan.Zero
                    && (retryCount > 1 || !fastFirstRetry))
                {
                    TimeSpan expectedDelay = retryDelay + stopwatch.Elapsed;

                    if (expectedDelay < s_minimumTimeBetweenRetries)
                    {
                        retryDelay = s_minimumTimeBetweenRetries - stopwatch.Elapsed;
                        Debug.WriteLine(
                            $"{cancellationToken.GetHashCode()} Last execution time was {stopwatch.Elapsed}. Adjusting back-off time to {retryDelay} to avoid high CPU/Memory spikes.");
                    }

                    await Task.Delay(retryDelay, CancellationToken.None).ConfigureAwait(false);
                }
            } while (!cancellationToken.IsCancellationRequested);

            return lastException == null
                ? (T)default
                : throw lastException;
        }
    }
}
