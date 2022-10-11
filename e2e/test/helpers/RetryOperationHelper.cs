// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    /// <summary>
    /// A helper class with methods that aid in retrying operations.
    /// </summary>
    public class RetryOperationHelper
    {
        // A conservative, basic retry policy that should be fine for most scenarios.
        public static IRetryPolicy DefaultRetryPolicy = new ExponentialBackoffRetryPolicy(5, TimeSpan.FromSeconds(10));

        /// <summary>
        /// Retry an async operation based on the retry strategy supplied.
        /// </summary>
        /// <remarks>
        /// This is for E2E tests of provisioning service clients .
        /// </remarks>
        /// <param name="asyncOperation">The async operation to be retried.</param>
        /// <param name="retryPolicy">The retry policy to be applied.</param>
        /// <param name="retryableExceptions">The exceptions to be retried on.</param>
        /// <param name="logger">The <see cref="MsTestLogger"/> instance to be used.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        public static async Task RetryOperationsAsync(
            Func<Task> asyncOperation,
            IRetryPolicy retryPolicy,
            HashSet<Type> retryableExceptions,
            MsTestLogger logger,
            CancellationToken cancellationToken = default)
        {
            uint counter = 0;
            bool shouldRetry;
            do
            {
                TimeSpan retryInterval;
                try
                {
                    await asyncOperation().ConfigureAwait(false);
                    break;
                }
                catch (Exception ex) when (retryableExceptions.Any(e => e.IsInstanceOfType(ex)))
                {
                    shouldRetry = retryPolicy.ShouldRetry(++counter, ex, out retryInterval);
                    logger.Trace($"Attempt {counter}: operation did not succeed: {ex}");

                    if (!shouldRetry)
                    {
                        logger.Trace($"Encountered an exception that will not be retried - attempt: {counter}; exception: {ex}");
                        throw;
                    }
                }

                logger.Trace($"Will retry operation in {retryInterval}.");
                await Task.Delay(retryInterval, cancellationToken);
            }
            while (shouldRetry && !cancellationToken.IsCancellationRequested);
        }

        /// <summary>
        /// Retry an async operation based on the retry strategy supplied.
        /// </summary>
        /// <remarks>
        /// This is for E2E tests of hub service clients.
        /// </remarks>
        /// <param name="asyncOperation">The async operation to be retried.</param>
        /// <param name="retryPolicy">The retry policy to be applied.</param>
        /// <param name="retryableStatusCodes">The errors to be retried on.</param>
        /// <param name="logger">The <see cref="MsTestLogger"/> instance to be used.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        public static async Task RetryOperationsAsync(
            Func<Task> asyncOperation,
            IRetryPolicy retryPolicy,
            HashSet<IotHubServiceErrorCode> retryableStatusCodes,
            MsTestLogger logger,
            CancellationToken cancellationToken = default)
        {
            uint counter = 0;
            bool shouldRetry;
            do
            {
                TimeSpan retryInterval;
                try
                {
                    await asyncOperation().ConfigureAwait(false);
                    break;
                }
                catch (Exception ex) when (ex is IotHubServiceException serviceEx && retryableStatusCodes.Contains(serviceEx.ErrorCode))
                {
                    shouldRetry = retryPolicy.ShouldRetry(++counter, ex, out retryInterval);
                    logger.Trace($"Attempt {counter}: operation did not succeed: {ex}");

                    if (!shouldRetry)
                    {
                        logger.Trace($"Encountered an exception that will not be retried - attempt: {counter}; exception: {ex}");
                        throw;
                    }
                }

                logger.Trace($"Will retry operation in {retryInterval}.");
                await Task.Delay(retryInterval, cancellationToken);
            }
            while (shouldRetry && !cancellationToken.IsCancellationRequested);
        }
    }
}
