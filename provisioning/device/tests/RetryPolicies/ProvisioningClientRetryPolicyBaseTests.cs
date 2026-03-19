// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningClientRetryPolicyBaseTests
    {
        [TestMethod]
        public void RetryPolicyBase_ObservesMax()
        {
            // arrange
            const uint maxRetries = 2;
            var retryPolicy = new ProvisioningClientTestRetryPolicy(maxRetries);
            var ex = new ProvisioningClientException("", true);

            // act - assert
            retryPolicy.ShouldRetry(maxRetries - 1, ex, out _).Should().BeTrue();
            retryPolicy.ShouldRetry(maxRetries, ex, out _).Should().BeFalse();
        }

        [TestMethod]
        public void RetryPolicyBase_ObservesInifiniteRetries()
        {
            // arrange
            var retryPolicy = new ProvisioningClientTestRetryPolicy(0);
            var ex = new ProvisioningClientException("", true);

            // act - assert
            retryPolicy.ShouldRetry(uint.MaxValue, ex, out _).Should().BeTrue();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RetryPolicyBase_ProvisioningClientException_ReturnsTrueWhenTransient(bool isTransient)
        {
            // arrange
            var retryPolicy = new ProvisioningClientTestRetryPolicy(0);
            var ex = new ProvisioningClientException("", isTransient);

            // act - assert
            retryPolicy.ShouldRetry(1, ex, out _).Should().Be(isTransient);
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
            var retryPolicy = new ProvisioningClientTestRetryPolicy(0);
            var ex = Activator.CreateInstance(exceptionType, "exParam") as Exception;

            // act and assert
            retryPolicy.ShouldRetry(1, ex, out _).Should().BeFalse();
        }

        [TestMethod]
        [DataRow(0u)]
        [DataRow(1u)]
        [DataRow(10u)]
        [DataRow(49u)]
        public void RetryPolicyBase_UpdateWithJitter_IgnoresAtThresholdOf50(double baseTimeMs)
        {
            // arrange
            var retryPolicy = new ProvisioningClientTestRetryPolicy(0);

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
            var retryPolicy = new ProvisioningClientTestRetryPolicy(0);
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
