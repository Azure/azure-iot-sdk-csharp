// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Transport.Mqtt.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningErrorDetailsMqttTests
    {
        private static double? throttledDelay = 32;
        private static string validTopicNameThrottled = $"$dps/registrations/res/429/?$rid=9&Retry-After={throttledDelay}";

        private static double? acceptedDelay = 23;
        private static string validTopicNameAccepted = $"$dps/registrations/res/202/?$rid=9&Retry-After={acceptedDelay}";

        private TimeSpan defaultInterval = TimeSpan.FromSeconds(2);

        [TestMethod]
        public void testRetryAfterValidThrottled()
        {
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(validTopicNameThrottled, defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(throttledDelay, actual?.Seconds);
        }

        [TestMethod]
        public void testRetryAfterValidAccepted()
        {
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(validTopicNameAccepted, defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(acceptedDelay, actual?.Seconds);
        }

        [TestMethod]
        public void testRetryAfterWithNoRetryAfterValue()
        {
            string invalidTopic = "$dps/registrations/res/429/?$rid=9&Retry-After=";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void testRetryAfterWithNoRetryAfterQueryKeyOrValue()
        {
            string invalidTopic = "$dps/registrations/res/429/?$rid=9";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void testRetryAfterWithNoQueryString()
        {
            string invalidTopic = "$dps/registrations/res/429/";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void testRetryAfterWithNoTopicString()
        {
            string invalidTopic = "";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void testRetryAfterWithTooSmallOfDelayChoosesDefault()
        {
            string invalidTopic = "$dps/registrations/res/429/?$rid=9&Retry-After=0";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(defaultInterval.Seconds, actual?.Seconds);
        }

        [TestMethod]
        public void testRetryAfterWithNegativeDelayChoosesDefault()
        {
            string invalidTopic = "$dps/registrations/res/429/?$rid=9&Retry-After=-1";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(defaultInterval.Seconds, actual?.Seconds);
        }
    }
}
