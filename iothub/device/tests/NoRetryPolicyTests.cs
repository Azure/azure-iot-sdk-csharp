// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class NoRetryPolicyTests
    {
        [TestMethod]
        public void NoRetryPolicy_RecommendsNo()
        {
            // arrange
            var noRetryPolicy = new NoRetry();

            // act and assert
            Assert.IsFalse(noRetryPolicy.ShouldRetry(0, null, out TimeSpan retryInterval));
            Assert.AreEqual(TimeSpan.Zero, retryInterval);
        }
    }
}
