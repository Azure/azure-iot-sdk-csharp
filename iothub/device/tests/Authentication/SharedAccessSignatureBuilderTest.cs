// Copyright(c) Microsoft.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using System.Globalization;
using System.Net;
using FluentAssertions;
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
            // arrange and act
            var startTime = new DateTime(2018, 1, 1);
            var timeToLive = TimeSpan.FromMinutes(60);
            long seconds = 1514768400;
            string expiresOn = SharedAccessSignatureBuilder.BuildExpiresOn(timeToLive, startTime);

            // assert
            expiresOn.Should().Be(seconds.ToString());
        }

        [TestMethod]
        public void TestBuildAudience_ShouldReturnSasToken()
        {
            // arrange and act
            string audience = WebUtility.UrlEncode("iothub.test/devices/device1/modules/module1");
            const string deviceId = "device1";
            const string iotHub = "iothub.test";
            const string moduleId = "module1";
            string builtAudience = SharedAccessSignatureBuilder.BuildAudience(iotHub, deviceId, moduleId);

            // assert
            audience.Should().Be(builtAudience);
        }

        [TestMethod]
        public void TestBuildSasToken_ShouldReturnSasToken()
        {
            // arrange
            const string audience = "iothub.test/devices/device1/modules/module1";
            const string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);

            // act
            SharedAccessSignature token = SharedAccessSignatureParser.Parse(sasTokenString);

            // assert
            WebUtility.UrlDecode(audience).Should().Be(token.Audience);
            token.Signature.Should().Be(signature);
            token.KeyName.Should().Be(string.Empty);
            token.ExpiresOnUtc.Should().Be(SharedAccessSignatureConstants.EpochTime + TimeSpan.FromSeconds(double.Parse(expiry, CultureInfo.InvariantCulture)));
        }

        [TestMethod]
        public void TestBuildSasToken_ShouldReturnSasToken_MalformedSignature()
        {
            // arrange
            const string audience = "iothub.test/devices/device1/modules/module1";
            const string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);
            sasTokenString = sasTokenString.Replace(
                SharedAccessSignatureConstants.SignatureFieldName + SharedAccessSignatureConstants.KeyValueSeparator,
                SharedAccessSignatureConstants.SignatureFieldName + "_" + SharedAccessSignatureConstants.KeyValueSeparator);

            // act
            Action act = () => _ = SharedAccessSignatureParser.Parse(sasTokenString);
            
            // assert
            act.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestBuildSasToken_ShouldReturnSasToken_MalformedExpiry()
        {
            // arrange
            const string audience = "iothub.test/devices/device1/modules/module1";
            const string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);
            sasTokenString = sasTokenString.Replace(
                SharedAccessSignatureConstants.ExpiryFieldName + SharedAccessSignatureConstants.KeyValueSeparator,
                SharedAccessSignatureConstants.ExpiryFieldName + "_" + SharedAccessSignatureConstants.KeyValueSeparator);

            // act
            Action act = () => _ = SharedAccessSignatureParser.Parse(sasTokenString);
            
            // assert
            act.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestBuildSasToken_ShouldReturnSasToken_MalformedAudience()
        {
            // arrange
            const string audience = "iothub.test/devices/device1/modules/module1";
            const string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);
            sasTokenString = sasTokenString.Replace(
                SharedAccessSignatureConstants.AudienceFieldName + SharedAccessSignatureConstants.KeyValueSeparator,
                SharedAccessSignatureConstants.AudienceFieldName + "_" + SharedAccessSignatureConstants.KeyValueSeparator);

            // act
            Action act = () => _ = SharedAccessSignatureParser.Parse(sasTokenString);
            
            // assert
            act.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestBuildSasToken_ShouldReturnSasToken_MissingKeyword()
        {
            // arrange
            const string audience = "iothub.test/devices/device1/modules/module1";
            const string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);
            sasTokenString = sasTokenString.Replace(SharedAccessSignatureConstants.SharedAccessSignature, "").TrimStart();
            
            // act
            Action act = () => _ = SharedAccessSignatureParser.Parse(sasTokenString);
            
            // assert
            act.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestBuildSasToken_ShouldReturnSasToken_MalformedKeyword()
        {
            // arrange
            const string audience = "iothub.test/devices/device1/modules/module1";
            const string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);
            sasTokenString = sasTokenString.Replace(SharedAccessSignatureConstants.SharedAccessSignature, SharedAccessSignatureConstants.SharedAccessSignature + "_");
            
            // act
            Action act = () => _ = SharedAccessSignatureParser.Parse(sasTokenString);
            
            // assert
            act.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestBuildSasToken_ShouldReturnSasToken_MalformedFields()
        {
            // arrange
            const string audience = "iothub.test/devices/device1/modules/module1";
            const string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = secondsFromBaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);
            sasTokenString = sasTokenString.Replace(SharedAccessSignatureConstants.AudienceFieldName + SharedAccessSignatureConstants.KeyValueSeparator, "");
            
            // act
            Action act = () => _ = SharedAccessSignatureParser.Parse(sasTokenString);
            
            // assert
            act.Should().Throw<FormatException>();
        }

        [TestMethod]
        [ExpectedException(typeof(IotHubClientException))]
        public void TestBuildSasToken_ShouldReturnSasToken_expired()
        {
            // arrange
            const string audience = "iothub.test/devices/device1/modules/module1";
            const string signature = "signature";

            DateTime expiresOn = DateTime.UtcNow.AddMinutes(10);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            string expiry = TimeSpan.FromSeconds(-1).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            string sasTokenString = SharedAccessSignatureBuilder.BuildSignature(null, null, null, TimeSpan.Zero, audience, signature, expiry);

            // act
            SharedAccessSignature token = SharedAccessSignatureParser.Parse(sasTokenString);

            // assert
            token.Audience.Should().Be(WebUtility.UrlDecode(audience));
            token.Signature.Should().Be(signature);
            token.KeyName.Should().Be(string.Empty);
            token.ExpiresOnUtc.Should().Be(SharedAccessSignatureConstants.EpochTime + TimeSpan.FromSeconds(double.Parse(expiry, CultureInfo.InvariantCulture)));
        }
    }
}
