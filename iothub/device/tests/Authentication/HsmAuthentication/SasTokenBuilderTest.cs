// Copyright(c) Microsoft.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using System.Globalization;
using System.Net;
using Microsoft.Azure.Devices.Client.HsmAuthentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.HsmAuthentication
{
    [TestClass]
    [TestCategory("Unit")]
    public class SasTokenBuilderTest
    {
        [TestMethod]
        public void TestBuildExpiry_ShouldReturnExpireOn()
        {
            var startTime = new DateTime(2018, 1, 1);
            TimeSpan timeToLive = TimeSpan.FromMinutes(60);
            long seconds = 1514768400;
            string expiresOn = SasTokenBuilder.BuildExpiresOn(startTime, timeToLive);

            Assert.AreEqual(seconds.ToString(), expiresOn);
        }

        [TestMethod]
        public void TestBuildAudience_ShouldReturnSasToken()
        {
            string audience = WebUtility.UrlEncode("iothub.test/devices/device1/modules/module1");
            string deviceId = "device1";
            string iotHub = "iothub.test";
            string moduleId = "module1";
            string builtAudience = SasTokenBuilder.BuildAudience(iotHub, deviceId, moduleId);

            Assert.AreEqual(audience, builtAudience);
        }

        [TestMethod]
        public void TestBuildSasToken_ShouldReturnSasToken()
        {
            string audience = "iothub.test/devices/device1/modules/module1";
            string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);

            string sasTokenString = SasTokenBuilder.BuildSasToken(audience, signature, expiry);

            SharedAccessSignature token = SharedAccessSignatureParser.Parse(sasTokenString);

            Assert.AreEqual(WebUtility.UrlDecode(audience), token.Audience);
            Assert.AreEqual(signature, token.Signature);
            Assert.AreEqual(string.Empty, token.KeyName);
            Assert.AreEqual(SharedAccessSignatureConstants.EpochTime + TimeSpan.FromSeconds(double.Parse(expiry, CultureInfo.InvariantCulture)), token.ExpiresOnUtc);
        }
    }
}
