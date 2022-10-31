// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningServiceIncrementalDelayRetryPolicyTests
    {
        [TestMethod]
        public void IncrementalDelayRetryPolicy_IncrementsInSteps()
        {
            // arrange
            var step = TimeSpan.FromSeconds(1);
            var retryPolicy = new ProvisioningServiceIncrementalDelayRetryPolicy(0, step, TimeSpan.FromMinutes(100), false);

            // act
            for (uint i = 1; i < 10; ++i)
            {
                retryPolicy.ShouldRetry(i, new ProvisioningServiceException("", true), out TimeSpan retryInterval);
                retryInterval.TotalSeconds.Should().Be(step.TotalSeconds * i);
            }
        }
    }
}
