﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a retry policy that performs no retries.
    /// </summary>
    public class NoRetry : IRetryPolicy
    {
        /// <summary>
        /// Create an instance of a retry policy that perfrms no retries.
        /// </summary>
        public NoRetry()
        {
            if (Logging.IsEnabled)
                Logging.Info(this, $"NOTE: A no-retry policy has been enabled," +
                    $" the client will not perform any retries on disconnection.", nameof(NoRetry));
        }

        /// <summary>
        /// A policy to never retry
        /// </summary>
        /// <param name="currentRetryCount">The current retry count.</param>
        /// <param name="lastException">The last exception.</param>
        /// <param name="retryInterval">The retry interval.</param>
        /// <returns>Whether or not to retry</returns>
        public bool ShouldRetry(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            retryInterval = TimeSpan.Zero;
            return false;
        }
    }
}
