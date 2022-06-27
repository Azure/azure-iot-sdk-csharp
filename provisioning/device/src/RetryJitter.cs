// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// return the provided delay + extra jitter ranging from 0 seconds to 5 seconds
    /// </summary>
    internal class RetryJitter
    {
        public static TimeSpan GenerateDelayWithJitterForRetry(TimeSpan defaultDelay)
        {
            const int jitterMax = 5;
            const int jitterMin = 0;

            var random = new Random();
            double jitterSeconds = random.NextDouble() * jitterMax + jitterMin;
            TimeSpan defaultDelayWithJitter = defaultDelay.Add(TimeSpan.FromSeconds(jitterSeconds));

            return defaultDelayWithJitter;
        }
    }
}
