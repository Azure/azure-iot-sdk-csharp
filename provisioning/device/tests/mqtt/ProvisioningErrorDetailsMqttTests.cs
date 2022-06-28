// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Azure.Devices.Provisioning.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningErrorDetailsMqttTests
    {
        private const double ThrottledDelay = 32;
        private static readonly string s_validTopicNameThrottled = $"$dps/registrations/res/429/?$rid=9&Retry-After={ThrottledDelay}";

        private const double AcceptedDelay = 23;
        private static readonly string s_validTopicNameAccepted = $"$dps/registrations/res/202/?$rid=9&Retry-After={AcceptedDelay}";

        private static readonly TimeSpan s_defaultInterval = TimeSpan.FromSeconds(2);

        [TestMethod]
        public void TestRetryAfterValidThrottled()
        {
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(s_validTopicNameThrottled, s_defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(ThrottledDelay, actual?.Seconds);
        }

        [TestMethod]
        public void TestRetryAfterValidAccepted()
        {
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(s_validTopicNameAccepted, s_defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(AcceptedDelay, actual?.Seconds);
        }

        [TestMethod]
        public void TestRetryAfterWithNoRetryAfterValue()
        {
            const string invalidTopic = "$dps/registrations/res/429/?$rid=9&Retry-After=";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, s_defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void TestRetryAfterWithNoRetryAfterQueryKeyOrValue()
        {
            const string invalidTopic = "$dps/registrations/res/429/?$rid=9";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, s_defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void TestRetryAfterWithNoQueryString()
        {
            const string invalidTopic = "$dps/registrations/res/429/";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, s_defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void TestRetryAfterWithNoTopicString()
        {
            const string invalidTopic = "";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, s_defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void TestRetryAfterWithTooSmallOfDelayChoosesDefault()
        {
            const string invalidTopic = "$dps/registrations/res/429/?$rid=9&Retry-After=0";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, s_defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(s_defaultInterval.Seconds, actual?.Seconds);
        }

        [TestMethod]
        public void TestRetryAfterWithNegativeDelayChoosesDefault()
        {
            const string invalidTopic = "$dps/registrations/res/429/?$rid=9&Retry-After=-1";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, s_defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(s_defaultInterval.Seconds, actual?.Seconds);
        }
    }
}
