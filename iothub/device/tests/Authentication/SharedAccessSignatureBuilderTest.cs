// Copyright(c) Microsoft.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using System.Globalization;
using System.Net;
using Microsoft.Azure.Devices.Client.HsmAuthentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class SharedAccessSignatureBuilderTest
    {
        [TestMethod]
        public void TestBuildExpiry_ShouldReturnExpireOn()
        {
            var startTime = new DateTime(2018, 1, 1);
            var timeToLive = TimeSpan.FromMinutes(60);
            long seconds = 1514768400;
            string expiresOn = SharedAccessSignatureBuilder.BuildExpiresOn(timeToLive, startTime);

            Assert.AreEqual(seconds.ToString(), expiresOn);
        }

        [TestMethod]
        public void TestBuildAudience_ShouldReturnSasToken()
        {
            string audience = WebUtility.UrlEncode("iothub.test/devices/device1/modules/module1");
            string deviceId = "device1";
            string iotHub = "iothub.test";
            string moduleId = "module1";
            string builtAudience = SharedAccessSignatureBuilder.BuildAudience(iotHub, deviceId, moduleId);

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

            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);

            SharedAccessSignature token = SharedAccessSignatureParser.Parse(sasTokenString);

            Assert.AreEqual(WebUtility.UrlDecode(audience), token.Audience);
            Assert.AreEqual(signature, token.Signature);
            Assert.AreEqual(string.Empty, token.KeyName);
            Assert.AreEqual(SharedAccessSignatureConstants.EpochTime + TimeSpan.FromSeconds(double.Parse(expiry, CultureInfo.InvariantCulture)), token.ExpiresOnUtc);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestBuildSasToken_ShouldReturnSasToken_MalformedSignature()
        {
            string audience = "iothub.test/devices/device1/modules/module1";
            string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);

            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);
            sasTokenString = sasTokenString.Replace(
                SharedAccessSignatureConstants.SignatureFieldName + SharedAccessSignatureConstants.KeyValueSeparator,
                SharedAccessSignatureConstants.SignatureFieldName + "_" + SharedAccessSignatureConstants.KeyValueSeparator);

            SharedAccessSignature _ = SharedAccessSignatureParser.Parse(sasTokenString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestBuildSasToken_ShouldReturnSasToken_MalformedExpiry()
        {
            string audience = "iothub.test/devices/device1/modules/module1";
            string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);

            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);
            sasTokenString = sasTokenString.Replace(
                SharedAccessSignatureConstants.ExpiryFieldName + SharedAccessSignatureConstants.KeyValueSeparator,
                SharedAccessSignatureConstants.ExpiryFieldName + "_" + SharedAccessSignatureConstants.KeyValueSeparator);

            SharedAccessSignature _ = SharedAccessSignatureParser.Parse(sasTokenString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestBuildSasToken_ShouldReturnSasToken_MalformedAudience()
        {
            string audience = "iothub.test/devices/device1/modules/module1";
            string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);

            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);
            sasTokenString = sasTokenString.Replace(
                SharedAccessSignatureConstants.AudienceFieldName + SharedAccessSignatureConstants.KeyValueSeparator,
                SharedAccessSignatureConstants.AudienceFieldName + "_" + SharedAccessSignatureConstants.KeyValueSeparator);

            SharedAccessSignature _ = SharedAccessSignatureParser.Parse(sasTokenString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestBuildSasToken_ShouldReturnSasToken_MissingKeyword()
        {
            string audience = "iothub.test/devices/device1/modules/module1";
            string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);

            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);
            sasTokenString = sasTokenString.Replace(SharedAccessSignatureConstants.SharedAccessSignature, "").TrimStart();
            SharedAccessSignature _ = SharedAccessSignatureParser.Parse(sasTokenString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestBuildSasToken_ShouldReturnSasToken_MalformedKeyword()
        {
            string audience = "iothub.test/devices/device1/modules/module1";
            string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);

            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);
            sasTokenString = sasTokenString.Replace(SharedAccessSignatureConstants.SharedAccessSignature, SharedAccessSignatureConstants.SharedAccessSignature + "_");
            SharedAccessSignature _ = SharedAccessSignatureParser.Parse(sasTokenString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestBuildSasToken_ShouldReturnSasToken_MalformedFields()
        {
            string audience = "iothub.test/devices/device1/modules/module1";
            string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);

            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);
            sasTokenString = sasTokenString.Replace(SharedAccessSignatureConstants.AudienceFieldName + SharedAccessSignatureConstants.KeyValueSeparator, "");
            SharedAccessSignature _ = SharedAccessSignatureParser.Parse(sasTokenString);
        }

        [TestMethod]
        [ExpectedException(typeof(IotHubClientException))]
        public void TestBuildSasToken_ShouldReturnSasToken_expired()
        {
            string audience = "iothub.test/devices/device1/modules/module1";
            string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = TimeSpan.FromSeconds(-1).TotalSeconds.ToString(CultureInfo.InvariantCulture);

            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);

            SharedAccessSignature token = SharedAccessSignatureParser.Parse(sasTokenString);

            Assert.AreEqual(WebUtility.UrlDecode(audience), token.Audience);
            Assert.AreEqual(signature, token.Signature);
            Assert.AreEqual(string.Empty, token.KeyName);
            Assert.AreEqual(SharedAccessSignatureConstants.EpochTime + TimeSpan.FromSeconds(double.Parse(expiry, CultureInfo.InvariantCulture)), token.ExpiresOnUtc);
        }
    }
}
