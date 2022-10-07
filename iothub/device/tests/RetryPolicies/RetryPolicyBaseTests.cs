// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("IoTHub")]
    public class RetryPolicyBaseTests
    {
        [TestMethod]
        public void RetryPolicyBase_ObservesMax()
        {
            // arrange
            const uint maxRetries = 2;
            var exponentialBackoff = new TestRetryPolicy(maxRetries);
            var ex = new IotHubClientException("", true);

            // act and assert
            exponentialBackoff.ShouldRetry(maxRetries - 1, ex, out TimeSpan delay).Should().BeTrue();
            exponentialBackoff.ShouldRetry(maxRetries, ex, out delay).Should().BeFalse();
        }

        [TestMethod]
        public void RetryPolicyBase_ObservesInifiniteRetries()
        {
            // arrange
            var exponentialBackoff = new TestRetryPolicy(0);
            var ex = new IotHubClientException("", true);

            // act and assert
            exponentialBackoff.ShouldRetry(uint.MaxValue, ex, out TimeSpan delay).Should().BeTrue();
        }

        [TestMethod]
        public void RetryPolicyBase_ChecksIsTransient()
        {
            // arrange
            var exponentialBackoff = new TestRetryPolicy(0);
            var exTransient = new IotHubClientException("", true);
            var exNotTransient = new IotHubClientException("", false);

            // act and assert
            exponentialBackoff.ShouldRetry(1, exTransient, out TimeSpan delay).Should().BeTrue();
            exponentialBackoff.ShouldRetry(1, exNotTransient, out delay).Should().BeFalse();
        }
    }
}
