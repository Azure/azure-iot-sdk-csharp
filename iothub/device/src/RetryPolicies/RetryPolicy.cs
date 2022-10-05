// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
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
// 9/1/2017 jasminel Renamed namespace to Microsoft.Azure.Devices.Client.TransientFaultHandling and modified access modifier to internal.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Provides the base implementation of the retry mechanism for unreliable actions and transient conditions.
    /// </summary>
    internal class RetryPolicy
    {
        /// <summary>
        /// Implements a strategy that ignores any transient errors.
        /// </summary>
        private sealed class TransientErrorIgnoreStrategy : ITransientErrorDetectionStrategy
        {
            /// <summary>
            /// Always returns false.
            /// </summary>
            /// <param name="ex">The exception.</param>
            /// <returns>Always false.</returns>
            public bool IsTransient(Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Implements a strategy that treats all exceptions as transient errors.
        /// </summary>
        private sealed class TransientErrorCatchAllStrategy : ITransientErrorDetectionStrategy
        {
            /// <summary>
            /// Always returns true.
            /// </summary>
            /// <param name="ex">The exception.</param>
            /// <returns>Always true.</returns>
            public bool IsTransient(Exception ex)
            {
                return true;
            }
        }

        /// <summary>
        /// An instance of a callback delegate that will be invoked whenever a retry condition is encountered.
        /// </summary>
        public event EventHandler<RetryingEventArgs> Retrying;

        /// <summary>
        /// Returns a default policy that performs no retries, but invokes the action only once.
        /// </summary>
        public static RetryPolicy NoRetry { get; } = new RetryPolicy(new TransientErrorIgnoreStrategy(), RetryStrategy.NoRetry);

        /// <summary>
        /// Returns a default policy that implements a fixed retry interval configured with the default <see cref="FixedInterval" /> retry strategy.
        /// The default retry policy treats all caught exceptions as transient errors.
        /// </summary>
        public static RetryPolicy DefaultFixed { get; } = new RetryPolicy(new TransientErrorCatchAllStrategy(), RetryStrategy.DefaultFixed);

        /// <summary>
        /// Returns a default policy that implements a progressive retry interval configured with the default <see cref="Incremental" /> retry strategy.
        /// The default retry policy treats all caught exceptions as transient errors.
        /// </summary>
        public static RetryPolicy DefaultProgressive { get; } = new RetryPolicy(new TransientErrorCatchAllStrategy(), RetryStrategy.DefaultProgressive);

        /// <summary>
        /// Returns a default policy that implements a random exponential retry interval configured with the default <see cref="FixedInterval" /> retry strategy.
        /// The default retry policy treats all caught exceptions as transient errors.
        /// </summary>
        public static RetryPolicy DefaultExponential { get; } = new RetryPolicy(new TransientErrorCatchAllStrategy(), RetryStrategy.DefaultExponential);

        /// <summary>
        /// Gets the retry strategy.
        /// </summary>
        public RetryStrategy RetryStrategy { get; private set; }

        /// <summary>
        /// Gets the instance of the error detection strategy.
        /// </summary>
        public ITransientErrorDetectionStrategy ErrorDetectionStrategy { get; private set; }

        /// <summary>
        /// Creates an instance of this class with the specified number of retry attempts and parameters defining the progressive delay between retries.
        /// </summary>
        /// <param name="errorDetectionStrategy">The error detection strategy that is responsible for detecting transient conditions.</param>
        /// <param name="retryStrategy">The strategy to use for this retry policy.</param>
        public RetryPolicy(ITransientErrorDetectionStrategy errorDetectionStrategy, RetryStrategy retryStrategy)
        {
            Argument.AssertNotNull(errorDetectionStrategy, "errorDetectionStrategy");
            Argument.AssertNotNull(retryStrategy, "retryPolicy");
            ErrorDetectionStrategy = errorDetectionStrategy;
            if (errorDetectionStrategy == null)
            {
                throw new InvalidOperationException("The error detection strategy type must implement the ITransientErrorDetectionStrategy interface.");
            }
            RetryStrategy = retryStrategy;
        }

        /// <summary>
        /// Creates an instance of this class with the specified number of retry attempts
        /// and default fixed time interval between retries.
        /// </summary>
        /// <param name="errorDetectionStrategy">The <see cref="ITransientErrorDetectionStrategy" />
        /// that is responsible for detecting transient conditions.</param>
        /// <param name="retryCount">The number of retry attempts.</param>
        public RetryPolicy(ITransientErrorDetectionStrategy errorDetectionStrategy, int retryCount)
            : this(errorDetectionStrategy, new FixedInterval(retryCount))
        {
        }

        /// <summary>
        /// Creates an instance of this class with the specified number of retry attempts
        /// and fixed time interval between retries.
        /// </summary>
        /// <param name="errorDetectionStrategy">The error detection strategy that is responsible
        /// for detecting transient conditions.</param>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <param name="retryInterval">The interval between retries.</param>
        public RetryPolicy(ITransientErrorDetectionStrategy errorDetectionStrategy, int retryCount, TimeSpan retryInterval)
            : this(errorDetectionStrategy, new FixedInterval(retryCount, retryInterval))
        {
        }

        /// <summary>
        /// Creates an instance of this class with the specified number of retry attempts
        /// and back-off parameters for calculating the exponential delay between retries.
        /// </summary>
        /// <param name="errorDetectionStrategy">The error detection strategy that is responsible
        /// for detecting transient conditions.</param>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <param name="minBackoff">The minimum back-off time.</param>
        /// <param name="maxBackoff">The maximum back-off time.</param>
        /// <param name="deltaBackoff">The time value that will be used to calculate a random delta in the exponential delay
        /// between retries.</param>
        public RetryPolicy(
            ITransientErrorDetectionStrategy errorDetectionStrategy,
            int retryCount,
            TimeSpan minBackoff,
            TimeSpan maxBackoff,
            TimeSpan deltaBackoff)
            : this(errorDetectionStrategy, new ExponentialBackoffRetryStrategy(retryCount, minBackoff, maxBackoff, deltaBackoff))
        {
        }

        /// <summary>
        /// Creates an instance of this class with the specified number of retry attempts and
        /// parameters defining the progressive delay between retries.
        /// </summary>
        /// <param name="errorDetectionStrategy">The error detection strategy that is responsible for
        /// detecting transient conditions.</param>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <param name="initialInterval">The initial interval that will apply for the first retry.</param>
        /// <param name="increment">The incremental time value that will be used to calculate the progressive delay between retries.</param>
        public RetryPolicy(ITransientErrorDetectionStrategy errorDetectionStrategy, int retryCount, TimeSpan initialInterval, TimeSpan increment)
            : this(errorDetectionStrategy, new Incremental(retryCount, initialInterval, increment))
        {
        }

        /// <summary>
        /// Repetitively executes the specified asynchronous task while it satisfies the current retry policy.
        /// </summary>
        /// <param name="taskAction">A function that returns a started task (also known as "hot" task).</param>
        /// <param name="cancellationToken">The token used to cancel the retry operation. This token does not cancel the execution
        /// of the asynchronous task.</param>
        /// <returns>
        /// Returns a task that will run to completion if the original task completes successfully (either the
        /// first time or after retrying transient failures). If the task fails with a non-transient error or
        /// the retry limit is reached, the returned task will transition to a faulted state and the exception must be observed.
        /// </returns>
        public Task RunWithRetryAsync(Func<Task> taskAction, CancellationToken cancellationToken = default)
        {
            return taskAction == null
                ? throw new ArgumentNullException(nameof(taskAction))
                : RunWithRetryAsync(
                    taskAction,
                    RetryStrategy.GetShouldRetry(),
                    new Func<Exception, bool>(ErrorDetectionStrategy.IsTransient),
                    new Action<int, Exception, TimeSpan>(OnRetrying),
                    RetryStrategy.FastFirstRetry,
                    cancellationToken);
        }

        /// <summary>
        /// Repeatedly executes the specified asynchronous task while it satisfies the current retry policy.
        /// </summary>
        /// <param name="taskFunc">A function that returns a started task (also known as "hot" task).</param>
        /// <returns>
        /// Returns a task that will run to completion if the original task completes successfully (either the
        /// first time or after retrying transient failures). If the task fails with a non-transient error or
        /// the retry limit is reached, the returned task will transition to a faulted state and the exception must be observed.
        /// </returns>
        public Task<TResult> RunWithRetryAsync<TResult>(Func<Task<TResult>> taskFunc)
        {
            return RunWithRetryAsync(taskFunc, default);
        }

        /// <summary>
        /// Repeatedly executes the specified asynchronous task while it satisfies the current retry policy.
        /// </summary>
        /// <param name="taskFunc">A function that returns a started task (also known as "hot" task).</param>
        /// <param name="cancellationToken">The token used to cancel the retry operation. This token does not cancel the execution of the asynchronous task.</param>
        /// <returns>
        /// Returns a task that will run to completion if the original task completes successfully (either the
        /// first time or after retrying transient failures). If the task fails with a non-transient error or
        /// the retry limit is reached, the returned task will transition to a faulted state and the exception must be observed.
        /// </returns>
        public Task<TResult> RunWithRetryAsync<TResult>(Func<Task<TResult>> taskFunc, CancellationToken cancellationToken)
        {
            if (taskFunc == null)
            {
                throw new ArgumentNullException(nameof(taskFunc));
            }

            return RunWithRetryAsync(
                taskFunc,
                RetryStrategy.GetShouldRetry(),
                new Func<Exception, bool>(ErrorDetectionStrategy.IsTransient),
                new Action<int, Exception, TimeSpan>(OnRetrying),
                RetryStrategy.FastFirstRetry,
                cancellationToken);
        }

        /// <summary>
        /// Notifies the subscribers whenever a retry condition is encountered.
        /// </summary>
        /// <param name="retryCount">The current retry attempt count.</param>
        /// <param name="lastError">The exception that caused the retry conditions to occur.</param>
        /// <param name="delay">The delay that indicates how long the current thread will be suspended before the next iteration is invoked.</param>
        protected virtual void OnRetrying(int retryCount, Exception lastError, TimeSpan delay)
        {
            Retrying?.Invoke(this, new RetryingEventArgs(retryCount, delay, lastError));
        }

        private static async Task RunWithRetryAsync(
            Func<Task> taskFunc,
            ShouldRetry shouldRetry,
            Func<Exception, bool> isTransient,
            Action<int, Exception, TimeSpan> onRetrying,
            bool fastFirstRetry,
            CancellationToken cancellationToken)
        {
            async Task<bool> TaskWrapper()
            {
                // There are two typews of tasks: return nothing and return a specific type.
                // We use this to proxy to the generics implementation.
                await taskFunc();
                return true;
            }

            await RunWithRetryAsync(
                    TaskWrapper,
                    shouldRetry,
                    isTransient,
                    onRetrying,
                    fastFirstRetry,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private static async Task<T> RunWithRetryAsync<T>(
            Func<Task<T>> taskFunc,
            ShouldRetry shouldRetry,
            Func<Exception, bool> isTransient,
            Action<int, Exception, TimeSpan> onRetrying,
            bool fastFirstRetry,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // We really should be calling cancellationToken.ThrowIfCancellationRequested()
                // but to retain previous behavior, we'll throw this specific exception type.
                throw new TaskCanceledException();
            }

            var minimumTimeBetweenRetries = TimeSpan.FromSeconds(1);
            var stopwatch = new Stopwatch();
            int retryCount = 0;
            Exception lastException = null;

            do
            {
                TimeSpan retryDelay;
                try
                {
                    // Measure how long it takes until the call fails, so we can determine how long until we retry again.
                    stopwatch.Restart();
                    return await taskFunc().ConfigureAwait(false);
                }
                catch (RetryLimitExceededException ex)
                {
                    if (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }

                    if (cancellationToken.IsCancellationRequested
                        && lastException != null)
                    {
                        throw lastException;
                    }

                    throw new OperationCanceledException();
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested
                    && isTransient(ex)
                    && shouldRetry(retryCount++, ex, out retryDelay))
                {
                    lastException = ex;
                    onRetrying(retryCount, ex, retryDelay);

                    if (retryDelay < TimeSpan.Zero)
                    {
                        retryDelay = TimeSpan.Zero;
                    }
                }

                stopwatch.Stop();

                // If we expect to wait until retry, calculate the remaining wait time, considering how much time the operation already
                // took, and the minimum delay.
                if (retryDelay > TimeSpan.Zero
                    && (retryCount > 1 || !fastFirstRetry))
                {
                    TimeSpan calculatedDelay = retryDelay + stopwatch.Elapsed;

                    // Don't let it retry more often than the minimum.
                    if (calculatedDelay < minimumTimeBetweenRetries)
                    {
                        retryDelay = minimumTimeBetweenRetries - stopwatch.Elapsed;
                        Console.WriteLine(
                            $"{cancellationToken.GetHashCode()} Last execution time was {stopwatch.Elapsed}. Adjusting back-off time to {retryDelay} to avoid high CPU/Memory spikes.");
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // Don't pass in the cancellation token, because we'll handle that
                        // condition specially in the catch blocks above.
                        await Task.Delay(retryDelay, CancellationToken.None).ConfigureAwait(false);
                    }
                }
            } while (!cancellationToken.IsCancellationRequested);

            // On cancellation, we'll rethrow the last exception we've seen if available.
            return lastException == null
                ? (T)default
                : throw lastException;
        }
    }
}
