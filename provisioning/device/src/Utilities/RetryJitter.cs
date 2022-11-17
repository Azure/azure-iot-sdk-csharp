// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Return the provided delay + extra jitter ranging from 0 seconds to 5 seconds.
    /// </summary>
    internal static class RetryJitter
    {
        internal const double MaxJitter = 5D;

        private static readonly Random s_random = new();

        internal static TimeSpan GenerateDelayWithJitterForRetry(TimeSpan defaultDelay)
        {
            Debug.Assert(defaultDelay!= null);

            double jitterSeconds = Math.Min(s_random.NextDouble() * MaxJitter, MaxJitter);
            return defaultDelay.Add(TimeSpan.FromSeconds(jitterSeconds));
        }
    }
}
