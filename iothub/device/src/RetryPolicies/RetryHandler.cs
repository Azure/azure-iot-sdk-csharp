// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Provides the base implementation of the retry mechanism for unreliable actions and transient conditions.
    /// </summary>
    internal sealed class RetryHandler
    {
        private IIotHubClientRetryPolicy _retryPolicy;

        /// <summary>
        /// Creates an instance of this class with the specified number of retry attempts and parameters defining the progressive delay between retries.
        /// </summary>
        /// <param name="retryPolicy">The retry policy to use for operations.</param>
        internal RetryHandler(IIotHubClientRetryPolicy retryPolicy)
        {
            Debug.Assert(retryPolicy != null);
            _retryPolicy = retryPolicy;
        }

        internal void SetRetryPolicy(IIotHubClientRetryPolicy retryPolicy)
        {
            Debug.Assert(retryPolicy != null);
            _retryPolicy = retryPolicy;
        }

        /// <summary>
        /// Repetitively executes the specified asynchronous task while it satisfies the current retry policy.
        /// </summary>
        /// <param name="taskFunc">A function that returns a started task (also known as "hot" task).</param>
        /// <param name="cancellationToken">The token used to cancel the retry operation. This token does not cancel the execution
        /// of the asynchronous task.</param>
        /// <returns>
        /// Returns a task that will run to completion if the original task completes successfully (either the
        /// first time or after retrying transient failures). If the task fails with a non-transient error or
        /// the retry limit is reached, the returned task will transition to a faulted state and the exception must be observed.
        /// </returns>
        internal async Task RunWithRetryAsync(Func<Task> taskFunc, CancellationToken cancellationToken = default)
        {
            async Task<bool> TaskWrapper()
            {
                // There are two types of tasks: return nothing and return a specific type.
                // We use this to proxy to the generics implementation.
                await taskFunc();
                return true;
            }

            await RunWithRetryAsync(TaskWrapper, (ex) => false, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Repetitively executes the specified asynchronous task with a specific return value while it satisfies the current retry policy.
        /// </summary>
        /// <param name="taskFunc">A function that returns a started task (also known as "hot" task).</param>
        /// <param name="cancellationToken">The token used to cancel the retry operation. This token does not cancel the execution
        /// of the asynchronous task.</param>
        /// <returns>
        /// Returns a task that will run to completion if the original task completes successfully (either the
        /// first time or after retrying transient failures). If the task fails with a non-transient error or
        /// the retry limit is reached, the returned task will transition to a faulted state and the exception must be observed.
        /// </returns>
        internal async Task<T> RunWithRetryAsync<T>(Func<Task<T>> taskFunc, CancellationToken cancellationToken = default)
        {
            return await RunWithRetryAsync(taskFunc, (ex) => false, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Repetitively executes the specified asynchronous task with a specific return value while it satisfies the current retry policy.
        /// </summary>
        /// <param name="taskFunc">A function that returns a started task (also known as "hot" task).</param>
        /// <param name="transientErrorCheck">Additional check for transient exceptions.</param>
        /// <param name="cancellationToken">The token used to cancel the retry operation. This token does not cancel the execution
        /// of the asynchronous task.</param>
        /// <returns>
        /// Returns a task that will run to completion if the original task completes successfully (either the
        /// first time or after retrying transient failures). If the task fails with a non-transient error or
        /// the retry limit is reached, the returned task will transition to a faulted state and the exception must be observed.
        /// </returns>
        internal async Task<T> RunWithRetryAsync<T>(Func<Task<T>> taskFunc, Func<Exception, bool> transientErrorCheck, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Debug.Assert(taskFunc != null);
            Debug.Assert(transientErrorCheck != null);

            uint retryCount = 0;
            TimeSpan retryDelay;

            while (true)
            {
                try
                {
                    return await taskFunc().ConfigureAwait(false);
                }
                catch (Exception ex) when (transientErrorCheck.Invoke(ex)
                    || _retryPolicy.ShouldRetry(++retryCount, ex, out retryDelay))
                {
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"Retry handler observed an exception approved for retry {retryCount} with delay {retryDelay}: {ex}");
                }

                if (retryDelay > TimeSpan.Zero)
                {
                    // If cancellation is requested, we don't want to wait the full duration.
                    await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
