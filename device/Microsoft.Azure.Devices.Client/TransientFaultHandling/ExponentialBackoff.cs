//Copyright(c) Microsoft.All rights reserved.
//Microsoft would like to thank its contributors, a list
//of whom are at http://aka.ms/entlib-contributors

//Licensed under the Apache License, Version 2.0 (the "License"); you
//may not use this file except in compliance with the License. You may
//obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
//implied. See the License for the specific language governing permissions
//and limitations under the License.

// THIS FILE HAS BEEN MODIFIED FROM ITS ORIGINAL FORM.
// Change Log:
// 9/1/2017 jasminel Renamed namespace to Microsoft.Azure.Devices.Client.TransientFaultHandling and modified access modifier to internal.

namespace Microsoft.Azure.Devices.Client.TransientFaultHandling
{
    using System;

    /// <summary>
    /// A retry strategy with backoff parameters for calculating the exponential delay between retries.
    /// </summary>
    internal class ExponentialBackoff : RetryStrategy
    {
        private readonly int retryCount;

        private readonly TimeSpan minBackoff;

        private readonly TimeSpan maxBackoff;

        private readonly TimeSpan deltaBackoff;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Azure.Devices.Client.TransientFaultHandling.ExponentialBackoff" /> class. 
        /// </summary>
        public ExponentialBackoff() : this(RetryStrategy.DefaultClientRetryCount, RetryStrategy.DefaultMinBackoff, RetryStrategy.DefaultMaxBackoff, RetryStrategy.DefaultClientBackoff)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Azure.Devices.Client.TransientFaultHandling.ExponentialBackoff" /> class with the specified retry settings.
        /// </summary>
        /// <param name="retryCount">The maximum number of retry attempts.</param>
        /// <param name="minBackoff">The minimum backoff time</param>
        /// <param name="maxBackoff">The maximum backoff time.</param>
        /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
        public ExponentialBackoff(int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff) : this(null, retryCount, minBackoff, maxBackoff, deltaBackoff, RetryStrategy.DefaultFirstFastRetry)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Azure.Devices.Client.TransientFaultHandling.ExponentialBackoff" /> class with the specified name and retry settings.
        /// </summary>
        /// <param name="name">The name of the retry strategy.</param>
        /// <param name="retryCount">The maximum number of retry attempts.</param>
        /// <param name="minBackoff">The minimum backoff time</param>
        /// <param name="maxBackoff">The maximum backoff time.</param>
        /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
        public ExponentialBackoff(string name, int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff) : this(name, retryCount, minBackoff, maxBackoff, deltaBackoff, RetryStrategy.DefaultFirstFastRetry)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Azure.Devices.Client.TransientFaultHandling.ExponentialBackoff" /> class with the specified name, retry settings, and fast retry option.
        /// </summary>
        /// <param name="name">The name of the retry strategy.</param>
        /// <param name="retryCount">The maximum number of retry attempts.</param>
        /// <param name="minBackoff">The minimum backoff time</param>
        /// <param name="maxBackoff">The maximum backoff time.</param>
        /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
        /// <param name="firstFastRetry">true to immediately retry in the first attempt; otherwise, false. The subsequent retries will remain subject to the configured retry interval.</param>
        public ExponentialBackoff(string name, int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff, bool firstFastRetry) : base(name, firstFastRetry)
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
            return delegate (int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
            {
                if (currentRetryCount < this.retryCount)
                {
                    Random random = new Random();
                    int num = (int)((Math.Pow(2.0, (double)currentRetryCount) - 1.0) * (double)random.Next((int)(this.deltaBackoff.TotalMilliseconds * 0.8), (int)(this.deltaBackoff.TotalMilliseconds * 1.2)));
                    int num2 = (int)Math.Min(this.minBackoff.TotalMilliseconds + (double)num, this.maxBackoff.TotalMilliseconds);
                    retryInterval = TimeSpan.FromMilliseconds((double)num2);
                    return true;
                }
                retryInterval = TimeSpan.Zero;
                return false;
            };
        }
    }
}
