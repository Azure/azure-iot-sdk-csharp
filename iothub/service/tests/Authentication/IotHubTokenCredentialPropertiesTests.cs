// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests.Authentication
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubTokenCredentialPropertiesTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private const string TokenType = "Bearer";
        private const string TokenValue = "token";

        [TestMethod]
        public void IotHubTokenCredentialProperties_GetAuthorizationHeader_CloseToExpiry_GeneratesToken()
        {
            // arrange
            var mockCredential = new Mock<TokenCredential>();

            string expectedAuthorizationHeader = $"{TokenType} {TokenValue}";
            DateTime expiryDate = DateTime.UtcNow; // Close to expiry
            var testAccessToken = new AccessToken(TokenValue, expiryDate);

            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);

            mockCredential
                .Setup(c => c.GetToken(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .Returns(new AccessToken(TokenValue, expiryDate));

            // act
            string act() => tokenCredentialProperties.GetAuthorizationHeader();

            // assert
            act().Should().Be(expectedAuthorizationHeader);
            mockCredential.Verify(x => x.GetToken(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void IotHubTokenCredentialProperties_GetAuthorizationHeader_NotCloseToExpiry_DoesNotGeneratesToken()
        {
            // arrange
            var mockCredential = new Mock<TokenCredential>();

            string expectedAuthorizationHeader = $"{TokenType} {TokenValue}";
            DateTime expiryDate = DateTime.UtcNow.AddMinutes(11); // Not Close to expiry
            var testAccessToken = new AccessToken(TokenValue, expiryDate);

            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object, testAccessToken);

            mockCredential
                .Setup(c => c.GetToken(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .Returns(new AccessToken(TokenValue, expiryDate));

            // act
            string act() => tokenCredentialProperties.GetAuthorizationHeader();

            // assert
            act().Should().Be(expectedAuthorizationHeader);
            mockCredential.Verify(x => x.GetToken(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public void IotHubTokenCredentialProperties_GetAuthorizationHeader_NoTokenValue_GeneratesToken()
        {
            // arrange
            var mockCredential = new Mock<TokenCredential>();

            string expectedAuthorizationHeader = $"{TokenType} ";
            DateTime expiryDate = DateTime.UtcNow.AddMinutes(11);

            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object, null); // null cached access token

            mockCredential
                .Setup(c => c.GetToken(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .Returns(new AccessToken(null, expiryDate)); 

            // act
            string act() => tokenCredentialProperties.GetAuthorizationHeader();

            // assert
            act().Should().Be(expectedAuthorizationHeader);
            mockCredential.Verify(x => x.GetToken(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task IotHubTokenCredentialProperties_GetTokenAsync_Ok()
        {
            // arrange
            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);

            string expectedAuthorizationHeader = $"{TokenType}";
            DateTimeOffset expiryDateTimeOffset = DateTimeOffset.UtcNow;

            var accessTokenToReturn = new AccessToken(TokenValue, expiryDateTimeOffset);

            mockCredential
                .Setup(t => t.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(accessTokenToReturn);

            // act
            CbsToken token = await tokenCredentialProperties.GetTokenAsync(null, null, null).ConfigureAwait(false);

            // assert
            token.TokenValue.Should().Be($"{TokenType} {TokenValue}");
            token.TokenType.Should().Be(TokenType);

            int timeDelta = Math.Abs((int)(token.ExpiresAtUtc - expiryDateTimeOffset).TotalSeconds);
            timeDelta.Should().BeLessThan(1);
            mockCredential.Verify(x => x.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
