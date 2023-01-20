// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningClientExponentialBackoffRetryPolicyTests
    {
        [TestMethod]
        public void ExponentialBackoffRetryPolicy_DoesNotUnderflowDelay()
        {
            // arrange
            const uint maxRetryAttempts = 70;

            var exponentialBackoff = new ProvisioningClientExponentialBackoffRetryPolicy(maxRetryAttempts, TimeSpan.FromDays(365), false);
            TimeSpan previousDelay = TimeSpan.Zero;

            for (uint retryCount = 1; retryCount < maxRetryAttempts; retryCount++)
            {
                // act
                exponentialBackoff.ShouldRetry(retryCount, new ProvisioningClientException("", true), out TimeSpan delay).Should().BeTrue();

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
            var exponentialBackoff = new ProvisioningClientExponentialBackoffRetryPolicy(uint.MaxValue, TimeSpan.FromDays(30), false);
            uint exponent = retryCount + 6; // starts at 7

            // act
            exponentialBackoff.ShouldRetry(retryCount, new ProvisioningClientException("", true), out TimeSpan delay);

            // assert
            delay.TotalMilliseconds.Should().BeApproximately(Math.Pow(2, exponent), 100);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExponentialBackoffRetryPolicy_ProvisioningClientException_ReturnsTrueWhenTransient(bool isTransient)
        {
            // arrange
            var retryPolicy = new ProvisioningClientExponentialBackoffRetryPolicy(0, TimeSpan.FromMinutes(100), false);
            var ex = new ProvisioningClientException("", isTransient);

            // act - assert
            retryPolicy.ShouldRetry(1, ex, out _).Should().Be(isTransient);
        }
    }
}
