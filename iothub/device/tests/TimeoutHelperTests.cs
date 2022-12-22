// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    /// <summary>
    /// The timeout helper is a way of keeping track of how much time remains against a specified deadline.
    /// The user specifies a time span, and whether or not to set the deadline immediately or upon the first call to GetRemainingTime().
    /// This is useful with the AMQP library, as it does not offer cancellation tokens, rather time spans for timeout.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class TimeoutHelperTests
    {
        [TestMethod]
        public async Task TimeoutHelper_Ctor_DoesNotStartTimeout()
        {
            // arrange
            var timeout = TimeSpan.FromSeconds(1);

            // act

            var toh = new TimeoutHelper(timeout);
            await Task.Delay(timeout).ConfigureAwait(false);

            // assert

            // As timeout did not start, asking for it now should return the original time, and then set the deadline.
            TimeSpan remainingTime = toh.GetRemainingTime();
            remainingTime.Should().Be(timeout);

            // Waiting a bit should show the
            await Task.Delay(1).ConfigureAwait(false);
            remainingTime = toh.GetRemainingTime();
            remainingTime.Should().BeLessThan(timeout);
        }

        [TestMethod]
        public async Task TimeoutHelper_Ctor_StartsTimeout()
        {
            // arrange

            var timeout = TimeSpan.FromSeconds(1);

            // act

            var toh = new TimeoutHelper(timeout, startTimeout: true);
            await Task.Delay(1).ConfigureAwait(false);

            // assert

            // As timeout did start, asking for it now should return something less than the original time
            var remainingTime = toh.GetRemainingTime();
            remainingTime.Should().BeLessThan(timeout);
        }

        [TestMethod]
        public void Timeouthelper_Ctor_NoTimeOut()
        {
            // arrange

            var timeout = TimeSpan.MaxValue;

            // act

            var toh = new TimeoutHelper(timeout, true);

            // assert

            TimeSpan remainingTime = toh.GetRemainingTime();
            remainingTime.Should().Be(timeout);
        }

        [TestMethod]
        public async Task TimeoutHelper_RemainingTime_TimesOut()
        {
            // arrange
            var timeout = TimeSpan.FromSeconds(1);

            // act
            var toh = new TimeoutHelper(timeout, true);

            // assert

            TimeSpan remainingTime = toh.GetRemainingTime();
            remainingTime.Should().NotBe(TimeSpan.Zero);

            // ensure we're over the time, because the test will sometimes fail with some microseconds remaining
            TimeSpan delay = timeout.Add(TimeSpan.FromMilliseconds(50));
            await Task.Delay(delay).ConfigureAwait(false);

            remainingTime = toh.GetRemainingTime();
            remainingTime.Should().Be(TimeSpan.Zero);
        }
    }
}
