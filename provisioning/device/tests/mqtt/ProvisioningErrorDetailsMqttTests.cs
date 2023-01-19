// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningErrorDetailsMqttTests
    {
        private const int ThrottledDelay = 32;
        private static readonly string s_validTopicNameThrottled = $"$dps/registrations/res/429/?$rid=9&Retry-After={ThrottledDelay}";

        private const int AcceptedDelay = 23;
        private static readonly string s_validTopicNameAccepted = $"$dps/registrations/res/202/?$rid=9&Retry-After={AcceptedDelay}";

        private static readonly TimeSpan s_defaultInterval = TimeSpan.FromSeconds(2);

        [TestMethod]
        public void RetryAfter_ValidThrottled()
        {
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(s_validTopicNameThrottled, s_defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(ThrottledDelay, actual?.Seconds);
        }

        [TestMethod]
        public void RetryAfter_ValidAccepted()
        {
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(s_validTopicNameAccepted, s_defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(AcceptedDelay, actual?.Seconds);
        }

        [TestMethod]
        public void RetryAfter_WithNoRetryAfterValue()
        {
            const string invalidTopic = "$dps/registrations/res/429/?$rid=9&Retry-After=";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, s_defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void RetryAfter_WithNoRetryAfterQueryKeyOrValue()
        {
            const string invalidTopic = "$dps/registrations/res/429/?$rid=9";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, s_defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void RetryAfter_WithNoQueryString()
        {
            const string invalidTopic = "$dps/registrations/res/429/";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, s_defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void RetryAfter_WithNoTopicString()
        {
            const string invalidTopic = "";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, s_defaultInterval);
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void RetryAfter_WithTooSmallOfDelayChoosesDefault()
        {
            const string invalidTopic = "$dps/registrations/res/429/?$rid=9&Retry-After=0";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, s_defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(s_defaultInterval.Seconds, actual?.Seconds);
        }

        [TestMethod]
        public void RetryAfter_WithNegativeDelayChoosesDefault()
        {
            const string invalidTopic = "$dps/registrations/res/429/?$rid=9&Retry-After=-1";
            TimeSpan? actual = ProvisioningErrorDetailsMqtt.GetRetryAfterFromTopic(invalidTopic, s_defaultInterval);
            Assert.IsNotNull(actual);
            Assert.AreEqual(s_defaultInterval.Seconds, actual?.Seconds);
        }
    }
}
