// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningClientFixedDelayRetryPolicyTests
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
            var retryPolicy = new ProvisioningClientFixedDelayRetryPolicy(0, expected, false);

            // act
            retryPolicy.ShouldRetry(retryCount, new ProvisioningClientException("", true), out TimeSpan retryInterval);

            // assert
            retryInterval.Should().Be(expected);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void FixedDelayRetryPolicy_ProvisioningClientException_ReturnsTrueWhenTransient(bool isTransient)
        {
            // arrange
            var retryPolicy = new ProvisioningClientFixedDelayRetryPolicy(0, TimeSpan.FromMinutes(100), false);
            var ex = new ProvisioningClientException("", isTransient);

            // act - assert
            retryPolicy.ShouldRetry(1, ex, out _).Should().Be(isTransient);
        }
    }
}
