// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.TransientFaultHandling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    public class ExponentialBackoffTests
    {
        private const int MaxRetryAttempts = 50;

        [TestMethod]
        [TestCategory("Unit")]
        public void ExponentialBackoffDoesNotUnderflow()
        {
            var exponentialBackoff = new ExponentialBackoffRetryStrategy(
                MaxRetryAttempts,
                minBackoff: TimeSpan.FromMilliseconds(50),
                maxBackoff: TimeSpan.FromSeconds(30),
                deltaBackoff: TimeSpan.FromMilliseconds(50));

            TimeSpan previousDelay = TimeSpan.Zero;

            ShouldRetry shouldRetry = exponentialBackoff.GetShouldRetry();
            for (int i = 1; i < MaxRetryAttempts; i++)
            {
                shouldRetry(i, new Exception(), out TimeSpan delay).Should().BeTrue();
                Console.WriteLine($"{i}: {delay}");
                delay.Should().BeGreaterOrEqualTo(TimeSpan.Zero, "Exponential backoff should never recommend a negative delay");
                delay.Should().BeGreaterOrEqualTo(previousDelay, "Each delay should not be smaller than the one previous.");

                previousDelay = delay;
            }
        }
    }
}
