// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a retry policy for the hub device/module client.
    /// </summary>
    public interface IIotHubClientRetryPolicy
    {
        /// <summary>
        /// Method called by the client prior to a retry.
        /// </summary>
        /// <param name="currentRetryCount">The number of times the current operation has been attempted.</param>
        /// <param name="lastException">The exception that caused this retry policy check.</param>
        /// <param name="retryDelay">Set this to the desired delay prior to the next attempt.</param>
        /// <returns>True if the operation should be retried,otherwise false.</returns>
        bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryDelay);
    }
}
