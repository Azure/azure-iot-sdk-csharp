// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Microsoft would like to thank its contributors, a list of whom are at http://aka.ms/entlib-contributors

using System;

// Source licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License. You may
// obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied. See the License for the specific language governing permissions
// and limitations under the License.

// THIS FILE HAS BEEN MODIFIED FROM ITS ORIGINAL FORM.
// Change Log:
// 9/1/2017 jasminel Renamed namespace to Microsoft.Azure.Devices.Client.TransientFaultHandling and modified access modifier to internal.
// 7/12/2021 drwill Changed property+backing field to auto-property.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a retry strategy that determines the number of retry attempts and the interval between retries.
    /// </summary>
    internal abstract class RetryStrategy
    {
        /// <summary>
        /// Represents the default number of retry attempts.
        /// </summary>
        public const int DefaultClientRetryCount = 10;

        /// <summary>
        /// Represents the default amount of time used when calculating a random delta in the exponential delay between retries.
        /// </summary>
        public static readonly TimeSpan DefaultClientBackoff = TimeSpan.FromSeconds(10.0);

        /// <summary>
        /// Represents the default maximum amount of time used when calculating the exponential delay between retries.
        /// </summary>
        public static readonly TimeSpan DefaultMaxBackoff = TimeSpan.FromSeconds(30.0);

        /// <summary>
        /// Represents the default minimum amount of time used when calculating the exponential delay between retries.
        /// </summary>
        public static readonly TimeSpan DefaultMinBackoff = TimeSpan.FromSeconds(1.0);

        /// <summary>
        /// Represents the default interval between retries.
        /// </summary>
        public static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromSeconds(1.0);

        /// <summary>
        /// Represents the default time increment between retry attempts in the progressive delay policy.
        /// </summary>
        public static readonly TimeSpan DefaultRetryIncrement = TimeSpan.FromSeconds(1.0);

        /// <summary>
        /// Represents the default flag indicating whether the first retry attempt will be made immediately,
        /// whereas subsequent retries will remain subject to the retry interval.
        /// </summary>
        public const bool DefaultFirstFastRetry = true;

        /// <summary>
        /// Creates an instance of the <see cref="RetryStrategy" /> class.
        /// </summary>
        /// <param name="name">The name of the retry strategy.</param>
        /// <param name="firstFastRetry">
        /// True to immediately retry in the first attempt; otherwise, false.
        /// The subsequent retries will remain subject to the configured retry interval.
        /// </param>
        protected RetryStrategy(string name, bool firstFastRetry)
        {
            Name = name;
            FastFirstRetry = firstFastRetry;
        }

        /// <summary>
        /// Returns a default policy that performs no retries, but invokes the action only once.
        /// </summary>
        public static RetryStrategy NoRetry { get; } = new FixedInterval(0, DefaultRetryInterval);

        /// <summary>
        /// Returns a default policy that implements a fixed retry interval configured with the <see cref="DefaultClientRetryCount" />
        /// and <see cref="DefaultRetryInterval" /> parameters. The default retry policy treats all caught exceptions as transient errors.
        /// </summary>
        public static RetryStrategy DefaultFixed { get; } = new FixedInterval(DefaultClientRetryCount, DefaultRetryInterval);

        /// <summary>
        /// Returns a default policy that implements a progressive retry interval configured with the
        /// <see cref="DefaultClientRetryCount" />,
        /// <see cref="DefaultRetryInterval" />,
        /// and <see cref="DefaultRetryIncrement" /> parameters.
        /// The default retry policy treats all caught exceptions as transient errors.
        /// </summary>
        public static RetryStrategy DefaultProgressive { get; } = new Incremental(DefaultClientRetryCount, DefaultRetryInterval, DefaultRetryIncrement);

        /// <summary>
        /// Returns a default policy that implements a random exponential retry interval configured with the <see cref="DefaultClientRetryCount" />,
        /// <see cref="DefaultMinBackoff" />, <see cref="DefaultMaxBackoff" />, and <see cref="DefaultClientBackoff" /> parameters.
        /// The default retry policy treats all caught exceptions as transient errors.
        /// </summary>
        public static RetryStrategy DefaultExponential { get; } = new ExponentialBackoffRetryStrategy(DefaultClientRetryCount, DefaultMinBackoff, DefaultMaxBackoff, DefaultClientBackoff);

        /// <summary>
        /// Gets or sets a value indicating whether the first retry attempt will be made immediately,
        /// whereas subsequent retries will remain subject to the retry interval.
        /// </summary>
        public bool FastFirstRetry { get; set; }

        /// <summary>
        /// Gets the name of the retry strategy.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns the corresponding ShouldRetry delegate.
        /// </summary>
        /// <returns>The ShouldRetry delegate.</returns>
        public abstract ShouldRetry GetShouldRetry();
    }
}
