﻿#region license
// ==============================================================================
// Microsoft patterns & practices Enterprise Library
// Transient Fault Handling Application Block
// ==============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ==============================================================================
#endregion

namespace Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling
{
    using System;

    /// <summary>
    /// A retry strategy with backoff parameters for calculating the exponential delay between retries.
    /// </summary>
    public class ExponentialBackoff : RetryStrategy
    {
        private readonly int retryCount;
        private readonly TimeSpan minBackoff;
        private readonly TimeSpan maxBackoff;
        private readonly TimeSpan deltaBackoff;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoff"/> class. 
        /// </summary>
        public ExponentialBackoff()
            : this(DefaultClientRetryCount, DefaultMinBackoff, DefaultMaxBackoff, DefaultClientBackoff)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoff"/> class with the specified retry settings.
        /// </summary>
        /// <param name="retryCount">The maximum number of retry attempts.</param>
        /// <param name="minBackoff">The minimum backoff time</param>
        /// <param name="maxBackoff">The maximum backoff time.</param>
        /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
        public ExponentialBackoff(int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff)
            : this(null, retryCount, minBackoff, maxBackoff, deltaBackoff, DefaultFirstFastRetry)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoff"/> class with the specified name and retry settings.
        /// </summary>
        /// <param name="name">The name of the retry strategy.</param>
        /// <param name="retryCount">The maximum number of retry attempts.</param>
        /// <param name="minBackoff">The minimum backoff time</param>
        /// <param name="maxBackoff">The maximum backoff time.</param>
        /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
        public ExponentialBackoff(string name, int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff)
            : this(name, retryCount, minBackoff, maxBackoff, deltaBackoff, DefaultFirstFastRetry)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoff"/> class with the specified name, retry settings, and fast retry option.
        /// </summary>
        /// <param name="name">The name of the retry strategy.</param>
        /// <param name="retryCount">The maximum number of retry attempts.</param>
        /// <param name="minBackoff">The minimum backoff time</param>
        /// <param name="maxBackoff">The maximum backoff time.</param>
        /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
        /// <param name="firstFastRetry">true to immediately retry in the first attempt; otherwise, false. The subsequent retries will remain subject to the configured retry interval.</param>
        public ExponentialBackoff(string name, int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff, bool firstFastRetry)
            : base(name, firstFastRetry)
        {
            Guard.ArgumentNotNegativeValue(retryCount, "retryCount");
            Guard.ArgumentNotNegativeValue(minBackoff.Ticks, "minBackoff");
            Guard.ArgumentNotNegativeValue(maxBackoff.Ticks, "maxBackoff");
            Guard.ArgumentNotNegativeValue(deltaBackoff.Ticks, "deltaBackoff");
            Guard.ArgumentNotGreaterThan(minBackoff.TotalMilliseconds, maxBackoff.TotalMilliseconds, "minBackoff");

            this.retryCount = retryCount;
            this.minBackoff = minBackoff;
            this.maxBackoff = maxBackoff;
            this.deltaBackoff = deltaBackoff;
        }

        /// <summary>
        /// Returns the corresponding ShouldRetry delegate.
        /// </summary>
        /// <returns>The ShouldRetry delegate.</returns>
        public override ShouldRetry GetShouldRetry()
        {
            return delegate(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
            {
                if (currentRetryCount < this.retryCount)
                {
                    var random = new Random();

                    var delta = (int)((Math.Pow(2.0, currentRetryCount) - 1.0) * random.Next((int)(this.deltaBackoff.TotalMilliseconds * 0.8), (int)(this.deltaBackoff.TotalMilliseconds * 1.2)));
                    var interval = (int)Math.Min(checked(this.minBackoff.TotalMilliseconds + delta), this.maxBackoff.TotalMilliseconds);

                    retryInterval = TimeSpan.FromMilliseconds(interval);

                    return true;
                }

                retryInterval = TimeSpan.Zero;
                return false;
            };
        }
    }
}