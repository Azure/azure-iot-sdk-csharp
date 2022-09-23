// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// A helper class with methods that aid in retrying operations.
    /// </summary>
    internal class RetryOperationHelper
    {
        /// <summary>
        /// Retry an async operation on encountering a transient operation. The retry strategy followed is an exponential backoff strategy.
        /// </summary>
        /// <param name="operationName">An identifier for the async operation to be executed. This is used for debugging purposes.</param>
        /// <param name="asyncOperation">The async operation to be retried.</param>
        /// <param name="shouldExecuteOperation">A function that determines if the operation should be executed.
        /// Eg.: for scenarios when we want to execute the operation only if the client is connected, this would be a function that returns if the client is currently connected.</param>
        /// <param name="logger">The <see cref="ILogger"/> instance to be used.</param>
        /// <param name="exceptionsToBeIgnored">An optional list of exceptions that can be ignored.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        internal static async Task RetryTransientExceptionsAsync(
            string operationName,
            Func<Task> asyncOperation,
            Func<bool> shouldExecuteOperation,
            ILogger logger,
            IDictionary<Type, string> exceptionsToBeIgnored = default,
            CancellationToken cancellationToken = default)
        {
            IRetryPolicy retryPolicy = new ExponentialBackoffTransientExceptionRetryPolicy(maxRetryCount: int.MaxValue, exceptionsToBeIgnored: exceptionsToBeIgnored);

            int attempt = 0;
            bool shouldRetry;

            do
            {
                Exception lastException = new Exception("Client is currently reconnecting internally; attempt the operation after some time.");
                try
                {
                    if (shouldExecuteOperation())
                    {
                        logger.LogInformation(FormatRetryOperationLogMessage(operationName, attempt, "executing."));

                        await asyncOperation();
                        break;
                    }
                    else
                    {
                        logger.LogWarning(FormatRetryOperationLogMessage(operationName, attempt, "operation is not ready to be executed. Attempt discarded."));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(FormatRetryOperationLogMessage(operationName, attempt, $"encountered an exception while processing the request: {ex}"));
                    lastException = ex;
                }

                shouldRetry = retryPolicy.ShouldRetry(++attempt, lastException, out TimeSpan retryInterval);
                if (shouldRetry)
                {
                    logger.LogWarning(FormatRetryOperationLogMessage(operationName, attempt, $"caught a recoverable exception, will retry in {retryInterval}."));
                    await Task.Delay(retryInterval, cancellationToken);
                }
                else
                {
                    logger.LogWarning(FormatRetryOperationLogMessage(operationName, attempt, $"retry policy determined that the operation should no longer be retried, stopping retries."));
                }
            }
            while (shouldRetry && !cancellationToken.IsCancellationRequested);
        }

        private static string FormatRetryOperationLogMessage(string operationName, int attempt, string logMessage)
        {
            return $"Operation name = {operationName}, attempt = {attempt}, status = {logMessage}";
        }
    }
}
