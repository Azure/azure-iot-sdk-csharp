// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    /// <summary>
    /// return the provided delay + extra jitter ranging from 0 seconds to 5 seconds
    /// </summary>
    internal class RetryJitter
    {
        private static int jitterMax = 5;
        private static int jitterMin = 0;
		
        public static TimeSpan GenerateDelayWithJitterForRetry(TimeSpan defaultDelay)
        {
            Random random = new Random();
            double jitterSeconds = random.NextDouble() * jitterMax + jitterMin;
            TimeSpan defaultDelayWithJitter = defaultDelay.Add(TimeSpan.FromSeconds(jitterSeconds));
            return defaultDelayWithJitter;
        }
    }
}
