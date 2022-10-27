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
    public class IotHubServiceNoRetryPolicyTests
    {
        [TestMethod]
        public void NoRetryPolicy_RecommendsNo()
        {
            // arrange
            var noRetryPolicy = new IotHubServiceNoRetry();

            // act and assert
            noRetryPolicy.ShouldRetry(0, null, out TimeSpan retryInterval).Should().BeFalse();
            retryInterval.Should().Be(TimeSpan.Zero);
        }
    }
}
