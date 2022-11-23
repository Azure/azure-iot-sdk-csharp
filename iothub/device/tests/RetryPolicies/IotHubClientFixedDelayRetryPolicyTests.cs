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
    public class IotHubClientFixedDelayRetryPolicyTests
    {
        [TestMethod]
        [DataRow(1u)]
        [DataRow(10u)]
        [DataRow(100u)]
        [DataRow(1000u)]
        public void FixedDelayRetryPolicy_IsFixedDelay(uint retryCount)
        {
            // arrange
            var expected = TimeSpan.FromSeconds(10);
            var retryPolicy = new IotHubClientFixedDelayRetryPolicy(0, expected, false);
            var exception = new IotHubClientException
            {
                IsTransient = true,
            };

            // act
            retryPolicy.ShouldRetry(retryCount, exception, out TimeSpan retryInterval);

            // assert
            retryInterval.Should().Be(expected);
        }
    }
}
