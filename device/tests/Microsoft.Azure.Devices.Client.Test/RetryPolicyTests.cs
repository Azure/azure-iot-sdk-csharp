// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using Microsoft.Azure.Devices.Client.TransientFaultHandling;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using ExponentialBackoff = Microsoft.Azure.Devices.Client.ExponentialBackoff;
    using NSubstitute;

    [TestClass]
    public class RetryPolicyTests
    {
        [TestMethod]
        [TestCategory("RetryPolicy")]
        public void NoRetryPolicy_VerifyBehavior_Success()
        {
            var noRetryPolicy = new NoRetry();

            TimeSpan retryInterval;
            Assert.IsFalse(noRetryPolicy.ShouldRetry(Arg.Any<int>(), Arg.Any<Exception>(), out retryInterval));
            Assert.AreEqual(TimeSpan.Zero, retryInterval);
        }
    }
}