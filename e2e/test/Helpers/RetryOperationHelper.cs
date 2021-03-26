﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    /// <summary>
    /// A helper class with methods that aid in retrying operations.
    /// </summary>
    public class RetryOperationHelper
    {
        /// <summary>
        /// Rety an async operation based on the retry strategy supplied.
        /// </summary>
        /// <param name="asyncOperation">The async operation to be retried.</param>
        /// <param name="retryPolicy">The retry policy to be applied.</param>
        /// <param name="retryableExceptions">The exceptions to be retried on.</param>
        /// <param name="logger">The <see cref="MsTestLogger"/> instance to be used.</param>
        /// <returns></returns>
        public static async Task RetryOperationsAsync(Func<Task> asyncOperation, IRetryPolicy retryPolicy, HashSet<Type> retryableExceptions, MsTestLogger logger)
        {
            int counter = 0;
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
                    logger.Trace($"Attempt {counter}: request got throttled: {ex}");
                }

                if (shouldRetry)
                {
                    logger.Trace($"Will retry operation in {retryInterval}.");
                    await Task.Delay(retryInterval).ConfigureAwait(false);
                }
            }
            while (shouldRetry);
        }
    }
}
