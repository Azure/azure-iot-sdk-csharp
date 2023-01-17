// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("IoTHub")]
    public class IotHubServiceIncrementalDelayRetryPolicyTests
    {
        [TestMethod]
        public void IncrementalDelayRetryPolicy_IncrementsInSteps()
        {
            // arrange
            var step = TimeSpan.FromSeconds(1);
            var retryPolicy = new IotHubServiceIncrementalDelayRetryPolicy(0, step, TimeSpan.FromMinutes(100), false);

            // act
            for (uint i = 1; i < 10; ++i)
            {
                retryPolicy.ShouldRetry(i, new IotHubServiceException("") { IsTransient = true }, out TimeSpan retryInterval);
                retryInterval.TotalSeconds.Should().Be(step.TotalSeconds * i);
            }
        }

        [TestMethod]
        public void IncrementalRetryPolicy_UseJitter()
        {
            // arrange
            var step = TimeSpan.FromSeconds(1);
            var retryPolicy = new IotHubServiceIncrementalDelayRetryPolicy(0, step, TimeSpan.FromMinutes(100), true);

            // act
            for (uint i = 1; i < 10; ++i)
            {
                // assert
                // note -- provide range of 0.06 instead of 0.05 to account for precision loss
                retryPolicy.ShouldRetry(i, new IotHubServiceException("") { IsTransient = true }, out TimeSpan retryInterval);
                retryInterval.TotalSeconds.Should().BeApproximately(step.TotalSeconds * i, step.TotalSeconds * i * 0.06);
            }
        }

        [TestMethod]
        public void IncrementalRetryPolicy_ShouldRetry_IsFalse() 
        {
            // arrange
            var step = TimeSpan.FromSeconds(1);
            var retryPolicy = new IotHubServiceIncrementalDelayRetryPolicy(0, step, TimeSpan.FromMinutes(100), true);

            // act
            bool shouldRetry = retryPolicy.ShouldRetry(0, new Exception(), out TimeSpan retryInterval);

            // assert
            shouldRetry.Should().BeFalse();
        }
    }
}
