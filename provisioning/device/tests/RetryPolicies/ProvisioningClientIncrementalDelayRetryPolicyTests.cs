// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningClientIncrementalDelayRetryPolicyTests
    {
        [TestMethod]
        public void IncrementalDelayRetryPolicy_IncrementsInSteps()
        {
            // arrange
            var step = TimeSpan.FromSeconds(1);
            var retryPolicy = new ProvisioningClientIncrementalDelayRetryPolicy(0, step, TimeSpan.FromMinutes(100), false);

            // act
            for (uint i = 1; i < 10; ++i)
            {
                retryPolicy.ShouldRetry(i, new ProvisioningClientException("", true), out TimeSpan retryInterval);
                retryInterval.TotalSeconds.Should().Be(step.TotalSeconds * i);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IncrementalDelayRetryPolicy_ProvisioningClientException_ReturnsTrueWhenTransient(bool isTransient)
        {
            // arrange
            var step = TimeSpan.FromSeconds(1);
            var retryPolicy = new ProvisioningClientIncrementalDelayRetryPolicy(0, step, TimeSpan.FromMinutes(100), false);
            var ex = new ProvisioningClientException("", isTransient);

            // act - assert
            retryPolicy.ShouldRetry(1, ex, out _).Should().Be(isTransient);
        }
    }
}
