﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a retry policy that performs a specified number of retries, using a exponential back-off scheme, with option jitter,
    /// to determine the interval between retries.
    /// </summary>
    /// <remarks>
    /// Jitter can change the delay from 95% to 105% of the calculated time.
    /// </remarks>
    public class IotHubClientExponentialBackoffRetryPolicy : IotHubClientRetryPolicyBase
    {
        // If we start with an exponent of 1 to calculate the number of millisecond delay, it starts too low and takes too long to get over 1 second.
        // So we always add 6 to the retry count to start at 2^7=128 milliseconds, and exceed 1 second delay on retry #4.
        private const uint MinExponent = 6u;

        // Avoid integer overlow (max of 32) and clamp max delay.
        private const uint MaxExponent = 32u;

        private readonly TimeSpan _maxDelay;
        private readonly bool _useJitter;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts; use 0 for infinite retries.</param>
        /// <param name="maxWait">The maximum amount of time to wait between retries (will not exceed ~12.43 days).</param>
        /// <param name="useJitter">Whether to add a small, random adjustment to the retry delay to avoid synchronicity in clients retrying.</param>
        /// <example>
        /// <code language="csharp">
        /// var exponentialBackoffRetryPolicy = new IotHubClientExponentialBackoffRetryPolicy(maxRetries, maxWait, useJitter);
        /// 
        /// var clientOptions = new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket))
        /// {
        ///     RetryPolicy = exponentialBackoffRetryPolicy,
        /// };
        /// </code>
        /// </example>
        public IotHubClientExponentialBackoffRetryPolicy(uint maxRetries, TimeSpan maxWait, bool useJitter = true)
            : base(maxRetries)
        {
            Argument.AssertNotNegativeValue(maxWait.Ticks, nameof(maxWait));

            _maxDelay = maxWait;
            _useJitter = useJitter;
        }

        /// <inheritdoc/>
        public override bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryDelay)
        {
            if (!base.ShouldRetry(currentRetryCount, lastException, out retryDelay))
            {
                return false;
            }

            // Avoid integer overlow and clamp max delay.
            uint exponent = currentRetryCount + MinExponent;
            exponent = Math.Min(MaxExponent, exponent);

            // 2 to the power of the retry count gives us exponential back-off.
            double exponentialIntervalMs = Math.Pow(2.0, exponent);

            double clampedWaitMs = Math.Min(exponentialIntervalMs, _maxDelay.TotalMilliseconds);

            retryDelay = _useJitter
                ? UpdateWithJitter(clampedWaitMs)
                : TimeSpan.FromMilliseconds(clampedWaitMs);

            return true;
        }
    }
}
