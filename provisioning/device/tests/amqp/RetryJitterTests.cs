﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Transport.Amqp.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class RetryJitterTests
    {
        [TestMethod]
        public void RetryJitterGeneratedDelayLargerOrEqualToDefaultDelay()
        {
            int expectedMinimumDelay = 2;
            var DefaultDelay = TimeSpan.FromSeconds(expectedMinimumDelay);
            TimeSpan GeneratedDelay = RetryJitter.GenerateDelayWithJitterForRetry(DefaultDelay);
            Assert.IsNotNull(GeneratedDelay);
            Assert.IsTrue(GeneratedDelay.Seconds >= DefaultDelay.Seconds);
        }

        [TestMethod]
        public void RetryJitterGeneratedDelayNoLargerThanFiveSeconds()
        {
            //current maximum jitter delay is 5 seconds, may change in the future
            int expectedMinimumDelay = 0;
            var DefaultDelay = TimeSpan.FromSeconds(expectedMinimumDelay);
            TimeSpan GeneratedDelay = RetryJitter.GenerateDelayWithJitterForRetry(DefaultDelay);
            Assert.IsNotNull(GeneratedDelay);
            Assert.IsTrue(GeneratedDelay.Seconds <= 5);
        }
    }
}
