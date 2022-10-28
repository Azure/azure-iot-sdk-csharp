// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("IoTHub")]
    public class IotHubServiceRetryPolicyBaseTests
    {
        [TestMethod]
        public void RetryPolicyBase_ObservesMax()
        {
            // arrange
            const uint maxRetries = 2;
            var retryPolicy = new IotHubServiceTestRetryPolicy(maxRetries);
            var ex = new IotHubServiceException("") { IsTransient = true };

            // act and assert
            retryPolicy.ShouldRetry(maxRetries - 1, ex, out TimeSpan delay).Should().BeTrue();
            retryPolicy.ShouldRetry(maxRetries, ex, out delay).Should().BeFalse();
        }

        [TestMethod]
        public void RetryPolicyBase_ObservesInifiniteRetries()
        {
            // arrange
            var retryPolicy = new IotHubServiceTestRetryPolicy(0);
            var ex = new IotHubServiceException("") { IsTransient = true };

            // act and assert
            retryPolicy.ShouldRetry(uint.MaxValue, ex, out TimeSpan delay).Should().BeTrue();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RetryPolicyBase_IotHubException_ReturnsTrueWhenTransient(bool isTransient)
        {
            // arrange
            var retryPolicy = new IotHubServiceTestRetryPolicy(0);
            var ex = new IotHubServiceException("") { IsTransient = isTransient };

            // act and assert
            retryPolicy.ShouldRetry(1, ex, out TimeSpan delay).Should().Be(isTransient);
        }

        [TestMethod]
        [DataRow(typeof(Exception))]
        [DataRow(typeof(ArgumentException))]
        [DataRow(typeof(ArgumentNullException))]
        [DataRow(typeof(InvalidOperationException))]
        [DataRow(typeof(TaskCanceledException))]
        [DataRow(typeof(OperationCanceledException))]
        [DataRow(typeof(ObjectDisposedException))]
        [DataRow(typeof(InvalidOperationException))]
        [DataRow(typeof(WebSocketException))]
        [DataRow(typeof(IOException))]
        public void RetryPolicyBase_OtherExceptions_ReturnFalse(Type exceptionType)
        {
            // arrange
            var retryPolicy = new IotHubServiceTestRetryPolicy(0);
            var ex = Activator.CreateInstance(exceptionType, "exParam") as Exception;

            // act and assert
            retryPolicy.ShouldRetry(1, ex, out TimeSpan delay).Should().BeFalse();
        }

        [TestMethod]
        [DataRow(0u)]
        [DataRow(1u)]
        [DataRow(10u)]
        [DataRow(49u)]
        public void RetryPolicyBase_UpdateWithJitter_IgnoresAtThresholdOf50(double baseTimeMs)
        {
            // arrange
            var retryPolicy = new IotHubServiceTestRetryPolicy(0);

            // act
            TimeSpan jitter = retryPolicy.UpdateWithJitter(baseTimeMs);

            // assert
            jitter.Should().Be(TimeSpan.FromMilliseconds(baseTimeMs));
        }

        [TestMethod]
        [DataRow(.1d)]
        [DataRow(.25d)]
        [DataRow(.5d)]
        [DataRow(1d)]
        [DataRow(10d)]
        [DataRow(20d)]
        [DataRow(30d)]
        public void RetryPolicyBase_UpdateWithJitter_NearValue(double seconds)
        {
            // arrange
            var retryPolicy = new IotHubServiceTestRetryPolicy(0);
            var duration = TimeSpan.FromSeconds(seconds);
            double min = duration.TotalMilliseconds * .95d;
            double max = duration.TotalMilliseconds * 1.05d;

            // act
            TimeSpan actual = retryPolicy.UpdateWithJitter(duration.TotalMilliseconds);

            // assert
            actual.TotalMilliseconds.Should().BeInRange(min, max);
        }
    }
}
