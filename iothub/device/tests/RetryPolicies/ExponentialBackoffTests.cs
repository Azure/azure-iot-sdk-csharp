// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.TransientFaultHandling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    public class ExponentialBackoffTests
    {
        private const int MAX_RETRY_ATTEMPTS = 5000;

        [TestMethod]
        [TestCategory("Unit")]
        public void ExponentialBackoffDoesNotUnderflow()
        {
            var exponentialBackoff = new ExponentialBackoffRetryStrategy(
                MAX_RETRY_ATTEMPTS,
                RetryStrategy.DefaultMinBackoff,
                RetryStrategy.DefaultMaxBackoff,
                RetryStrategy.DefaultClientBackoff);
            ShouldRetry shouldRetry = exponentialBackoff.GetShouldRetry();
            for (int i = 1; i < MAX_RETRY_ATTEMPTS; i++)
            {
                shouldRetry(i, new Exception(), out TimeSpan delay);

                if (delay.TotalSeconds <= 0)
                {
                    Assert.Fail("Exponential backoff should never recommend a negative delay");
                }
            }
        }
    }
}
