// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using FluentAssertions;
using Microsoft.Azure.Devices.Common;
using Azure.Core;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class TokenHelperTests
    {
        [TestMethod]
        [DataRow(15, false)] // 15 minutes to expiry
        [DataRow(2, true)] // 2 minutes to expiry
        [DataRow(-2, true)] // Expired 2 minutes ago
        [DataRow(-15, true)] // Expired 15 minutes ago
        public void TestIsTokenCloseToExpiry_Succeeds(int offsetInMinutes, bool expectedIsExpired)
        {
            // arrange
            var expiry = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(offsetInMinutes);
            var tokenCredential = new TestTokenCredential(expiry);

            // act
            AccessToken token = tokenCredential.GetToken(
                default(TokenRequestContext),
                new CancellationToken());
            bool isExpired = TokenHelper.IsCloseToExpiry(token.ExpiresOn);

            // assert
            isExpired.Should().Be(expectedIsExpired);
        }
    }
}
