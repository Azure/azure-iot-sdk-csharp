// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningErrorDetailsAmqpTests
    {
        private static readonly TimeSpan s_defaultInterval = TimeSpan.FromSeconds(2);

        [TestMethod]
        public void GetRetryFromRejectedSuccess()
        {
            int expectedSeconds = 32;
            var rejected = new Rejected
            {
                Error = new Error()
            };
            rejected.Error.Info = new Fields
            {
                { new AmqpSymbol("Retry-After"), expectedSeconds }
            };

            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, s_defaultInterval);

            Assert.IsNotNull(actual);
            Assert.AreEqual(actual?.Seconds, expectedSeconds);
        }

        [TestMethod]
        public void GetRetryFromRejectedFallsBackToDefaultIfNegativeRetryAfterProvided()
        {
            int expectedSeconds = -1;
            var rejected = new Rejected
            {
                Error = new Error()
            };
            rejected.Error.Info = new Fields
            {
                { new AmqpSymbol("Retry-After"), expectedSeconds }
            };

            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, s_defaultInterval);

            Assert.IsNotNull(actual);
            Assert.AreEqual(actual?.Seconds, s_defaultInterval.Seconds);
        }

        [TestMethod]
        public void GetRetryFromRejectedFallsBackToDefaultIfRetryAfterProvidedIs0()
        {
            int expectedSeconds = 0;
            var rejected = new Rejected
            {
                Error = new Error()
            };
            rejected.Error.Info = new Fields
            {
                { new AmqpSymbol("Retry-After"), expectedSeconds }
            };

            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, s_defaultInterval);

            Assert.IsNotNull(actual);
            Assert.AreEqual(actual?.Seconds, s_defaultInterval.Seconds);
        }

        [TestMethod]
        public void GetRetryFromRejectedReturnsNullIfNoErrorInfoEntries()
        {
            var rejected = new Rejected
            {
                Error = new Error()
            };
            rejected.Error.Info = new Fields();

            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, s_defaultInterval);

            Assert.IsNull(actual);
        }

        [TestMethod]
        public void GetRetryFromRejectedReturnsNullIfNoErrorInfo()
        {
            var rejected = new Rejected
            {
                Error = new Error()
            };

            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, s_defaultInterval);

            Assert.IsNull(actual);
        }

        [TestMethod]
        public void GetRetryFromRejectedReturnsNullIfNoError()
        {
            var rejected = new Rejected();

            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, s_defaultInterval);

            Assert.IsNull(actual);
        }

        [TestMethod]
        public void GetRetryAfterFromApplicationPropertiesSuccess()
        {
            int expectedRetryAfter = 42;
            using var amqpResponse = AmqpMessage.Create();
            amqpResponse.ApplicationProperties = new ApplicationProperties();
            amqpResponse.ApplicationProperties.Map.Add(new MapKey("Retry-After"), expectedRetryAfter);
            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromApplicationProperties(amqpResponse, s_defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expectedRetryAfter, actual?.Seconds);
        }

        [TestMethod]
        public void GetRetryAfterFromApplicationPropertiesReturnsDefaultIfRetryAfterValueIsNegative()
        {
            int expectedRetryAfter = -1;
            using var amqpResponse = AmqpMessage.Create();
            amqpResponse.ApplicationProperties = new ApplicationProperties();
            amqpResponse.ApplicationProperties.Map.Add(new MapKey("Retry-After"), expectedRetryAfter);
            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromApplicationProperties(amqpResponse, s_defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(s_defaultInterval.Seconds, actual?.Seconds);
        }

        [TestMethod]
        public void GetRetryAfterFromApplicationPropertiesReturnsDefaultIfRetryAfterValueIsZero()
        {
            int expectedRetryAfter = 0;
            using var amqpResponse = AmqpMessage.Create();
            amqpResponse.ApplicationProperties = new ApplicationProperties();
            amqpResponse.ApplicationProperties.Map.Add(new MapKey("Retry-After"), expectedRetryAfter);
            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromApplicationProperties(amqpResponse, s_defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(s_defaultInterval.Seconds, actual?.Seconds);
        }

        [TestMethod]
        public void GetRetryAfterFromApplicationPropertiesReturnsNullIfNoRetryAfterApplicationProperty()
        {
            using var amqpResponse = AmqpMessage.Create();
            amqpResponse.ApplicationProperties = new ApplicationProperties();
            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromApplicationProperties(amqpResponse, s_defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void GetRetryAfterFromApplicationPropertiesReturnsNullIfNoApplicationProperties()
        {
            using var amqpResponse = AmqpMessage.Create();
            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromApplicationProperties(amqpResponse, s_defaultInterval);
            Assert.IsNull(actual);
        }
    }
}
