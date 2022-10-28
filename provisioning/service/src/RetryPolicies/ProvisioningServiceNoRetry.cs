// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Represents a retry policy that performs no retries.
    /// </summary>
    public class ProvisioningServiceNoRetry : IProvisioningServiceRetryPolicy
    {
        /// <summary>
        /// Create an instance of a retry policy that performs no retries.
        /// </summary>
        public ProvisioningServiceNoRetry()
        {
            if (Logging.IsEnabled)
                Logging.Info(
                    this,
                    $"NOTE: A no-retry policy has been enabled; the client will not perform any retries on error.",
                    nameof(ProvisioningServiceNoRetry));
        }

        /// <summary>
        /// A policy to never retry
        /// </summary>
        /// <param name="currentRetryCount">The current retry count.</param>
        /// <param name="lastException">The last exception.</param>
        /// <param name="retryInterval">The retry interval.</param>
        /// <returns>Whether or not to retry</returns>
        public bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            retryInterval = TimeSpan.Zero;
            return false;
        }
    }
}
