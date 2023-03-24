﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    public class IotHubServiceFixedDelayRetryPolicyTests
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
            var retryPolicy = new IotHubServiceFixedDelayRetryPolicy(0, expected, false);

            // act
            retryPolicy.ShouldRetry(retryCount, new IotHubServiceException("") { IsTransient = true }, out TimeSpan retryInterval);

            // assert
            retryInterval.Should().Be(expected);
        }

        [TestMethod]
        public void FixedDelayRetryPolicy_UseJitter()
        {
            // arrange
            uint retryCount = 10;
            var expected = TimeSpan.FromSeconds(10);
            var retryPolicy = new IotHubServiceFixedDelayRetryPolicy(0, expected, true);

            // act
            retryPolicy.ShouldRetry(retryCount, new IotHubServiceException("") { IsTransient = true }, out TimeSpan retryInterval);

            // assert
            retryInterval.Should().BeCloseTo(expected, TimeSpan.FromMilliseconds(500));
        }

        [TestMethod]
        public void FixedDelayRetryPolicy_ShouldRetry_IsFalse()
        {
            // arrange
            uint retryCount = 10;
            var expected = TimeSpan.FromSeconds(10);
            var retryPolicy = new IotHubServiceFixedDelayRetryPolicy(0, expected);

            // act
            // should return false since exception is not transient
            bool shouldRetry = retryPolicy.ShouldRetry(retryCount, new IotHubServiceException("") { IsTransient = false }, out TimeSpan retryDelay);

            // assert
            shouldRetry.Should().BeFalse();
        }
    }
}
