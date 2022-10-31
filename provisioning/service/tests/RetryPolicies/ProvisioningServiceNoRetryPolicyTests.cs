// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningServiceNoRetryPolicyTests
    {
        [TestMethod]
        public void NoRetryPolicy_RecommendsNo()
        {
            // arrange
            var noRetryPolicy = new ProvisioningServiceNoRetry();

            // act and assert
            noRetryPolicy.ShouldRetry(0, null, out TimeSpan retryInterval).Should().BeFalse();
            retryInterval.Should().Be(TimeSpan.Zero);
        }
    }
}
