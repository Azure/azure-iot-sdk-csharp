// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class RetryJitterTests
    {
        [TestMethod]
        public void RetryJitterGeneratedDelay_WithinExpectedRange()
        {
            TimeSpan generatedDelay = RetryJitter.GenerateDelayWithJitterForRetry(TimeSpan.Zero);
            Console.WriteLine($"Result was {generatedDelay}");
            generatedDelay.Should().BeGreaterThanOrEqualTo(generatedDelay);
            generatedDelay.TotalSeconds.Should().BeLessThanOrEqualTo(RetryJitter.MaxJitter);
        }
    }
}
