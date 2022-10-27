// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        /// <summary>
        /// Retry an async operation based on the retry strategy supplied.
        /// </summary>
        /// <remarks>
        /// This is for E2E tests of provisioning service clients.
        /// </remarks>
        /// <param name="asyncOperation">The async operation to be retried.</param>
        /// <param name="retryPolicy">The retry policy of hub device/module to be applied.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        public static async Task RunWithHubClientRetryAsync(
            Func<Task> asyncOperation,
            IIotHubClientRetryPolicy retryPolicy,
            CancellationToken cancellationToken = default)
        {
            uint counter = 0;

            while (true)
            {
                TimeSpan retryInterval;
                try
                {
                    counter++;
                    await asyncOperation().ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (!retryPolicy.ShouldRetry(counter, ex, out retryInterval))
                {
                    VerboseTestLogger.WriteLine($"Attempt {counter}: operation did not succeed due to: {ex}");
                }

                if (retryInterval <= TimeSpan.Zero)
                {
                    retryInterval = TimeSpan.FromSeconds(1);
                }

                VerboseTestLogger.WriteLine($"Will retry operation in {retryInterval}.");
                await Task.Delay(retryInterval, cancellationToken);
            }
        }

        /// <summary>
        /// Retry an async operation based on the retry strategy supplied.
        /// </summary>
        /// <remarks>
        /// This is for E2E tests of provisioning service clients.
        /// </remarks>
        /// <param name="asyncOperation">The async operation to be retried.</param>
        /// <param name="retryPolicy">The retry policy of hub service to be applied.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        public static async Task RunWithHubServiceRetryAsync(
            Func<Task> asyncOperation,
            IIotHubServiceRetryPolicy retryPolicy,
            CancellationToken cancellationToken = default)
        {
            uint counter = 0;

            while (true)
            {
                TimeSpan retryInterval;
                try
                {
                    counter++;
                    await asyncOperation().ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (!retryPolicy.ShouldRetry(counter, ex, out retryInterval))
                {
                    VerboseTestLogger.WriteLine($"Attempt {counter}: operation did not succeed due to: {ex}");
                }

                if (retryInterval <= TimeSpan.Zero)
                {
                    retryInterval = TimeSpan.FromSeconds(1);
                }

                VerboseTestLogger.WriteLine($"Will retry operation in {retryInterval}.");
                await Task.Delay(retryInterval, cancellationToken);
            }
        }
    }
}
