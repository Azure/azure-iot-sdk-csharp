// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents a retry policy that performs no retries.
    /// </summary>
    public class ProvisioningClientNoRetry : IProvisioningClientRetryPolicy
    {
        /// <summary>
        /// Create an instance of a retry policy that performs no retries.
        /// </summary>
        public ProvisioningClientNoRetry()
        {
            if (Logging.IsEnabled)
                Logging.Info(
                    this,
                    $"NOTE: A no-retry policy has been enabled; the client will not perform any retries on error.",
                    nameof(ProvisioningClientNoRetry));
        }

        /// <inheritdoc/>
        public bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryDelay)
        {
            retryDelay = TimeSpan.Zero;
            return false;
        }
    }
}
