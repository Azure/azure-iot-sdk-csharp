// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubServiceExponentialBackoffRetryPolicyTests
    {
        [TestMethod]
        public void ExponentialBackoffRetryPolicy_DoesNotUnderflowDelay()
        {
            // arrange
            const uint MaxRetryAttempts = 70;

            var exponentialBackoff = new IotHubServiceExponentialBackoffRetryPolicy(MaxRetryAttempts, TimeSpan.FromDays(365), false);
            TimeSpan previousDelay = TimeSpan.Zero;

            for (uint retryCount = 1; retryCount < MaxRetryAttempts; retryCount++)
            {
                // act
                exponentialBackoff.ShouldRetry(retryCount, new IotHubServiceException("") { IsTransient = true }, out TimeSpan delay).Should().BeTrue();

                // assert
                Console.WriteLine($"{retryCount}: {delay}");
                delay.Should().BeGreaterOrEqualTo(previousDelay, "Exponential backoff should never recommend a negative delay or one less than the previous.");

                previousDelay = delay;
            }
        }

        [TestMethod]
        [DataRow(1u)]
        [DataRow(5u)]
        [DataRow(10u)]
        [DataRow(20u)]
        public void ExponentialBackoffRetryPolicy_IsExponential(uint retryCount)
        {
            // arrange
            var exponentialBackoff = new IotHubServiceExponentialBackoffRetryPolicy(uint.MaxValue, TimeSpan.FromDays(30), false);
            uint exponent = retryCount + 6; // starts at 7

            // act
            exponentialBackoff.ShouldRetry(retryCount, new IotHubServiceException("") { IsTransient = true }, out TimeSpan delay);

            // assert
            delay.TotalMilliseconds.Should().BeApproximately(Math.Pow(2, exponent), 100);
        }

        [TestMethod]
        public void ExponentialBackoffRetryPolicy_ShouldRetry_UseJitter()
        {
            // arrange
            const uint MaxRetryAttempts = 70;
            const uint currentRetryCount = 50;

            var exponentialBackoffJitter = new IotHubServiceExponentialBackoffRetryPolicy(MaxRetryAttempts, TimeSpan.FromDays(365), true);
            var exponentialBackoffStandard = new IotHubServiceExponentialBackoffRetryPolicy(MaxRetryAttempts, TimeSpan.FromDays(365), false);

            // act
            exponentialBackoffJitter.ShouldRetry(currentRetryCount, new IotHubServiceException(""), out TimeSpan jitterDelay);
            exponentialBackoffStandard.ShouldRetry(currentRetryCount, new IotHubServiceException(""), out TimeSpan regularDelay);

            // assert
            double min = regularDelay.TotalMilliseconds * 0.95d;
            double max = regularDelay.TotalMilliseconds * 1.05d;

            jitterDelay.TotalMilliseconds.Should().BeInRange(min, max);
        }
    }
}
