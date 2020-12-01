// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    /// <summary>
    /// return the provided delay + extra jitter ranging from 0 seconds to 5 seconds
    /// </summary>
    internal class RetryJitter
    {
        private const int JitterMax = 5;
        private const int JitterMin = 0;

        public static TimeSpan GenerateDelayWithJitterForRetry(TimeSpan defaultDelay)
        {
            var random = new Random();
            double jitterSeconds = random.NextDouble() * JitterMax + JitterMin;
            TimeSpan defaultDelayWithJitter = defaultDelay.Add(TimeSpan.FromSeconds(jitterSeconds));
            return defaultDelayWithJitter;
        }
    }
}
