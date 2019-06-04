// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Transport.Amqp.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningErrorDetailsAmqpTests
    {
        private static TimeSpan defaultInterval = TimeSpan.FromSeconds(2);

        [TestMethod]
        public void GetRetryFromRejectedSuccess()
        {
            int expectedSeconds = 32;
            Rejected Rejected = new Rejected();
            Rejected.Error = new Error();
            Rejected.Error.Info = new Fields();
            Rejected.Error.Info.Add(new AmqpSymbol("Retry-After"), expectedSeconds);

            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(Rejected, defaultInterval);

            Assert.IsNotNull(actual);
            Assert.AreEqual(actual?.Seconds, expectedSeconds);
        }

        [TestMethod]
        public void GetRetryFromRejectedFallsBackToDefaultIfNegativeRetryAfterProvided()
        {
            int expectedSeconds = -1;
            Rejected rejected = new Rejected();
            rejected.Error = new Error();
            rejected.Error.Info = new Fields();
            rejected.Error.Info.Add(new Azure.Amqp.Encoding.AmqpSymbol("Retry-After"), expectedSeconds);

            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, defaultInterval);

            Assert.IsNotNull(actual);
            Assert.AreEqual(actual?.Seconds, defaultInterval.Seconds);
        }

        [TestMethod]
        public void GetRetryFromRejectedFallsBackToDefaultIfRetryAfterProvidedIs0()
        {
            int expectedSeconds = 0;
            Rejected rejected = new Rejected();
            rejected.Error = new Error();
            rejected.Error.Info = new Fields();
            rejected.Error.Info.Add(new Azure.Amqp.Encoding.AmqpSymbol("Retry-After"), expectedSeconds);

            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, defaultInterval);

            Assert.IsNotNull(actual);
            Assert.AreEqual(actual?.Seconds, defaultInterval.Seconds);
        }

        [TestMethod]
        public void GetRetryFromRejectedReturnsNullIfNoErrorInfoEntries()
        {
            Rejected rejected = new Rejected();
            rejected.Error = new Error();
            rejected.Error.Info = new Fields();

            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, defaultInterval);

            Assert.IsNull(actual);
        }

        [TestMethod]
        public void GetRetryFromRejectedReturnsNullIfNoErrorInfo()
        {
            Rejected rejected = new Rejected();
            rejected.Error = new Error();

            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, defaultInterval);

            Assert.IsNull(actual);
        }

        [TestMethod]
        public void GetRetryFromRejectedReturnsNullIfNoError()
        {
            Rejected rejected = new Rejected();

            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, defaultInterval);

            Assert.IsNull(actual);
        }

        [TestMethod]
        public void GetRetryAfterFromApplicationPropertiesSuccess()
        {
            int expectedRetryAfter = 42;
            AmqpMessage amqpResponse = AmqpMessage.Create();
            amqpResponse.ApplicationProperties = new ApplicationProperties();
            amqpResponse.ApplicationProperties.Map.Add(new MapKey("Retry-After"), expectedRetryAfter);
            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromApplicationProperties(amqpResponse, defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expectedRetryAfter, actual?.Seconds);
        }

        [TestMethod]
        public void GetRetryAfterFromApplicationPropertiesReturnsDefaultIfRetryAfterValueIsNegative()
        {
            int expectedRetryAfter = -1;
            AmqpMessage amqpResponse = AmqpMessage.Create();
            amqpResponse.ApplicationProperties = new ApplicationProperties();
            amqpResponse.ApplicationProperties.Map.Add(new MapKey("Retry-After"), expectedRetryAfter);
            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromApplicationProperties(amqpResponse, defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(defaultInterval.Seconds, actual?.Seconds);
        }

        [TestMethod]
        public void GetRetryAfterFromApplicationPropertiesReturnsDefaultIfRetryAfterValueIsZero()
        {
            int expectedRetryAfter = 0;
            AmqpMessage amqpResponse = AmqpMessage.Create();
            amqpResponse.ApplicationProperties = new ApplicationProperties();
            amqpResponse.ApplicationProperties.Map.Add(new MapKey("Retry-After"), expectedRetryAfter);
            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromApplicationProperties(amqpResponse, defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(defaultInterval.Seconds, actual?.Seconds);
        }

        [TestMethod]
        public void GetRetryAfterFromApplicationPropertiesReturnsNullIfNoRetryAfterApplicationProperty()
        {
            AmqpMessage amqpResponse = AmqpMessage.Create();
            amqpResponse.ApplicationProperties = new ApplicationProperties();
            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromApplicationProperties(amqpResponse, defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void GetRetryAfterFromApplicationPropertiesReturnsNullIfNoApplicationProperties()
        {
            AmqpMessage amqpResponse = AmqpMessage.Create();
            TimeSpan? actual = ProvisioningErrorDetailsAmqp.GetRetryAfterFromApplicationProperties(amqpResponse, defaultInterval);
            Assert.IsNull(actual);
        }
    }
}
