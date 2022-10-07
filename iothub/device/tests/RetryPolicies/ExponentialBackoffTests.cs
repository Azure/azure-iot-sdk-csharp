// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("IoTHub")]
    public class ExponentialBackoffTests
    {

        [TestMethod]
        public void ExponentialBackoffDoesNotUnderflow()
        {
            // arrange
            const uint MaxRetryAttempts = 50;

            var exponentialBackoff = new ExponentialBackoffRetryPolicy(MaxRetryAttempts, TimeSpan.FromSeconds(30));

            for (uint i = 0; i < MaxRetryAttempts; i++)
            {
                // act
                exponentialBackoff.ShouldRetry(i, new IotHubClientException("", true), out TimeSpan delay).Should().BeTrue();

                // assert
                Console.WriteLine($"{i}: {delay}");
                delay.Should().BeGreaterOrEqualTo(TimeSpan.Zero, "Exponential backoff should never recommend a negative delay");
            }
        }
    }
}
