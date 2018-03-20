// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;

    /// <summary>
    /// Represents a retry policy
    /// </summary>
    public interface IRetryPolicy
    {
    	/// <summary>
        /// Method called by DeviceClient prior to a retry.
        /// </summary>
        /// <param name="currentRetryCount">The number of times the current operation has been attempted.</param>
        /// <param name="lastException">The exception that caused this retry policy check.</param>
        /// <param name="retryInterval">Set this to the desired delay prior to the next attempt.</param>
        /// <returns>True if the operation should be retried. False otherwise.</returns>
        bool ShouldRetry(int currentRetryCount, Exception lastException, out TimeSpan retryInterval);
    }
}
