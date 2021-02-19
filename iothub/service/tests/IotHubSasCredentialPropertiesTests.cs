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

#if !NET451

using Azure;

#endif

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubSasCredentialPropertiesTests
    {
        private const string _hostName = "myiothub.azure-devices.net";

#if !NET451

        [TestMethod]
        public async Task TestCbsTokenGeneration_Succeeds()
        {
            // arrange
            DateTime expiresAtUtc = DateTime.UtcNow;
            DateTime updatedExpiresAtUtc = DateTime.UtcNow.AddDays(1);

            string token = string.Format(
                CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}&se={2}",
                WebUtility.UrlEncode(_hostName),
                WebUtility.UrlEncode("signature"),
                expiresAtUtc);

            string updatedToken = string.Format(
                CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}&se={2}",
                WebUtility.UrlEncode(_hostName),
                WebUtility.UrlEncode("signature"),
                updatedExpiresAtUtc);

            var azureSasCredential = new AzureSasCredential(token);
            var iotHubSasCredentialProperties = new IotHubSasCredentialProperties(_hostName, azureSasCredential);

            // act

            CbsToken cbsToken = await iotHubSasCredentialProperties.GetTokenAsync(null, null, null).ConfigureAwait(false);
            azureSasCredential.Update(updatedToken);
            CbsToken updatedCbsToken = await iotHubSasCredentialProperties.GetTokenAsync(null, null, null).ConfigureAwait(false);

            // assert
            cbsToken.ExpiresAtUtc.ToString().Should().Be(expiresAtUtc.ToString());
            updatedCbsToken.ExpiresAtUtc.ToString().Should().Be(updatedExpiresAtUtc.ToString());
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

                Assert.Fail("The parsing of date time in invalid format on the SAS token should have caused an exception.");
            }
            catch (InvalidOperationException ex)
            {
                // assert
                ex.Message.Should().Be($"Invalid expiration time on {nameof(AzureSasCredential)} signature.");
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

#endif
    }
}
