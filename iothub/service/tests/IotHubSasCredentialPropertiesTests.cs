// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using FluentAssertions;
using Azure;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubSasCredentialPropertiesTests
    {
        private const string _hostName = "myiothub.azure-devices.net";

        [TestMethod]
        public async Task TestCbsTokenGeneration_Succeeds()
        {
            // arrange
            var epochTime = new DateTime(1970, 1, 1);
            DateTime expiresAt = DateTime.UtcNow.Add(TimeSpan.FromHours(1));
            TimeSpan secondsFromEpochTime = expiresAt.Subtract(epochTime);
            long seconds = Convert.ToInt64(secondsFromEpochTime.TotalSeconds, CultureInfo.InvariantCulture);
            string expiry = Convert.ToString(seconds, CultureInfo.InvariantCulture);

            DateTime updatedExpiresAt = DateTime.UtcNow.Add(TimeSpan.FromHours(2));
            TimeSpan updatedSecondsFromEpochTime = updatedExpiresAt.Subtract(epochTime);
            long updatedSeconds = Convert.ToInt64(updatedSecondsFromEpochTime.TotalSeconds, CultureInfo.InvariantCulture);
            string updatedExpiry = Convert.ToString(updatedSeconds, CultureInfo.InvariantCulture);

            string token = string.Format(
                CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}&se={2}",
                WebUtility.UrlEncode(_hostName),
                WebUtility.UrlEncode("signature"),
                expiry);

            string updatedToken = string.Format(
                CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}&se={2}",
                WebUtility.UrlEncode(_hostName),
                WebUtility.UrlEncode("signature"),
                updatedExpiry);

            var azureSasCredential = new AzureSasCredential(token);
            var iotHubSasCredentialProperties = new IotHubSasCredentialProperties(_hostName, azureSasCredential);

            // act

            CbsToken cbsToken = await iotHubSasCredentialProperties.GetTokenAsync(null, null, null).ConfigureAwait(false);
            azureSasCredential.Update(updatedToken);
            CbsToken updatedCbsToken = await iotHubSasCredentialProperties.GetTokenAsync(null, null, null).ConfigureAwait(false);

            // assert
            Math.Abs(expiresAt.Subtract(cbsToken.ExpiresAtUtc).TotalSeconds).Should().BeLessThan(1);
            Math.Abs(updatedExpiresAt.Subtract(updatedCbsToken.ExpiresAtUtc).TotalSeconds).Should().BeLessThan(1);
        }

        [TestMethod]
        public async Task TestCbsTokenGeneration_InvalidExpirationDateTimeFormat_Fails()
        {
            // arrange
            string token = string.Format(
                CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}&se={2}",
                WebUtility.UrlEncode(_hostName),
                WebUtility.UrlEncode("signature"),
                "01:01:2021");

            var azureSasCredential = new AzureSasCredential(token);
            var iotHubSasCredentialProperties = new IotHubSasCredentialProperties(_hostName, azureSasCredential);

            try
            {
                // act
                await iotHubSasCredentialProperties.GetTokenAsync(null, null, null).ConfigureAwait(false);

                Assert.Fail("The parsing of seconds from string to long should have caused an exception.");
            }
            catch (InvalidOperationException ex)
            {
                // assert
                ex.Message.Should().Be($"Invalid seconds from epoch time on {nameof(AzureSasCredential)} signature.");
            }
        }

        [TestMethod]
        public async Task TestCbsTokenGeneration_MissingExpiration_Fails()
        {
            // arrange
            string token = string.Format(
                CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}",
                WebUtility.UrlEncode(_hostName),
                WebUtility.UrlEncode("signature"));

            var azureSasCredential = new AzureSasCredential(token);
            var iotHubSasCredentialProperties = new IotHubSasCredentialProperties(_hostName, azureSasCredential);

            try
            {
                // act
                await iotHubSasCredentialProperties.GetTokenAsync(null, null, null).ConfigureAwait(false);

                Assert.Fail("The missing expiry on the SAS token should have caused an exception.");
            }
            catch (InvalidOperationException ex)
            {
                // assert
                ex.Message.Should().Be($"There is no expiration time on {nameof(AzureSasCredential)} signature.");
            }
        }

        [TestMethod]
        public void TestCbsTokenGeneration_GetAuthorizationHeader()
        {
            // arrange
            string token = string.Format(
                CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}",
                WebUtility.UrlEncode(_hostName),
                WebUtility.UrlEncode("signature"));

            var azureSasCredential = new AzureSasCredential(token);
            var iotHubSasCredentialProperties = new IotHubSasCredentialProperties(_hostName, azureSasCredential);

            iotHubSasCredentialProperties.GetAuthorizationHeader().Should().Be("SharedAccessSignature sr=myiothub.azure-devices.net&sig=signature");
        }
    }
}
