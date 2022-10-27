// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents a retry policy that performs a specified number of retries, using a fixed retry delay with jitter.
    /// </summary>
    /// <remarks>
    /// Jitter can change the delay from 95% to 105% of the calculated time.
    /// </remarks>
    public class DeviceProvisioningClientFixedDelayRetryPolicy : DeviceProvisioningClientRetryPolicyBase
    {
        private readonly TimeSpan _fixedDelay;
        private readonly bool _useJitter;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts; use 0 for infinite retries.</param>
        /// <param name="fixedDelay">The fixed delay to wait between retries.</param>
        /// <param name="useJitter">Whether to add a small, random adjustment to the retry delay to avoid synchronicity in retrying clients.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throw if the value of <paramref name="fixedDelay"/> is negative.</exception>
        public DeviceProvisioningClientFixedDelayRetryPolicy(uint maxRetries, TimeSpan fixedDelay, bool useJitter = true)
            : base(maxRetries)
        {
            AssertNotNegativeValue(fixedDelay.Ticks, nameof(fixedDelay));

            _fixedDelay = fixedDelay;
            _useJitter = useJitter;
        }

        /// <summary>
        /// Returns true if, based on the parameters, the operation should be retried.
        /// </summary>
        /// <param name="currentRetryCount">How many times the operation has been retried.</param>
        /// <param name="lastException">Operation exception.</param>
        /// <param name="retryInterval">Next retry should be performed after this time interval.</param>
        /// <returns>True if the operation should be retried, false otherwise.</returns>
        public override bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            if (!base.ShouldRetry(currentRetryCount, lastException, out retryInterval))
            {
                return false;
            }

            retryInterval = _useJitter
                ? UpdateWithJitter(_fixedDelay.TotalMilliseconds)
                : _fixedDelay;

            return true;
        }

        private static void AssertNotNegativeValue(long argumentValue, string argumentName)
        {
            if (argumentValue < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, string.Format(CultureInfo.CurrentCulture, "ArgumentCannotBeNegative", new object[]
                {
                    argumentName
                }));
            }
        }
    }
}
