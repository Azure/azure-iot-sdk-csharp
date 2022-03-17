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
    internal class AsyncExecution<T>
    {
        private static readonly TimeSpan s_minimumTimeBetweenRetries = TimeSpan.FromSeconds(1);

        private readonly Func<Task<T>> _taskFunc;
        private readonly Func<Task> _taskFunc2;
        private readonly ShouldRetry _shouldRetry;
        private readonly Func<Exception, bool> _isTransient;
        private readonly Action<int, Exception, TimeSpan> _onRetrying;
        private readonly bool _fastFirstRetry;
        private readonly CancellationToken _cancellationToken;

        public AsyncExecution(
            Func<Task<T>> taskFunc,
            ShouldRetry shouldRetry,
            Func<Exception, bool> isTransient,
            Action<int, Exception, TimeSpan> onRetrying,
            bool fastFirstRetry,
            CancellationToken cancellationToken)
        {
            _taskFunc = taskFunc ?? throw new InvalidOperationException(Resources.TaskCannotBeNull);
            _shouldRetry = shouldRetry;
            _isTransient = isTransient;
            _onRetrying = onRetrying;
            _fastFirstRetry = fastFirstRetry;
            _cancellationToken = cancellationToken;
        }
        public AsyncExecution(
            Func<Task> taskFunc,
            ShouldRetry shouldRetry,
            Func<Exception, bool> isTransient,
            Action<int, Exception, TimeSpan> onRetrying,
            bool fastFirstRetry,
            CancellationToken cancellationToken)
        {
            _taskFunc2 = taskFunc ?? throw new InvalidOperationException(Resources.TaskCannotBeNull);
            _shouldRetry = shouldRetry;
            _isTransient = isTransient;
            _onRetrying = onRetrying;
            _fastFirstRetry = fastFirstRetry;
            _cancellationToken = cancellationToken;
        }

        public async Task RunAsync()
        {
            await RunAndReportAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Runs the async func specified in the constructor with a retry policy.
        /// </summary>
        public async Task<T> RunAndReportAsync()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = new Stopwatch();
            int retryCount = 0;

            while (!_cancellationToken.IsCancellationRequested)
            {
                TimeSpan retryDelay = TimeSpan.Zero;

                try
                {
                    stopwatch.Restart();
                    if (_taskFunc != null)
                    {
                        return await _taskFunc().ConfigureAwait(false);
                    }
                    else
                    {
                        await _taskFunc2().ConfigureAwait(false);
                        return default(T);
                    }
                }
                catch (Exception ex) when (_isTransient(ex)
                    && ex.InnerException != null
                    && _isTransient(ex.InnerException)
                    && !(ex is RetryLimitExceededException)
                    && _shouldRetry(retryCount++, ex.InnerException, out retryDelay))
                {
                    _onRetrying(retryCount, ex.InnerException, retryDelay);
                }

                retryDelay -= stopwatch.Elapsed;
                if (retryDelay < s_minimumTimeBetweenRetries)
                {
                    retryDelay = s_minimumTimeBetweenRetries;
                    Debug.WriteLine(
                        $"{_cancellationToken.GetHashCode()} Last execution time was {stopwatch.Elapsed}. Adjusting back-off time to {retryDelay} to avoid high CPU/Memory spikes.");

                    if (retryDelay > TimeSpan.Zero
                        && (retryCount > 1
                            || _fastFirstRetry))
                    {
                        await Task.Delay(retryDelay).ConfigureAwait(false);
                    }
                }
            }

            return default(T);
        }
    }
}
