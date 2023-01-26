// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningErrorDetailsTests
    {
        private static readonly int s_errorCode = 404;
        private static readonly string s_trackingId = "test-tracking-id";
        private static readonly string s_message = "This is a testing provisioning error details message.";
        private static readonly Dictionary<string, string> s_info = new() { { "info-key", "info-value" } };

        [TestMethod]
        public void ProvisioningErrorDetails_Properties()
        {
            // arrange

            var source = new ProvisioningErrorDetails
            {
                ErrorCode = s_errorCode,
                TrackingId = s_trackingId,
                Message = s_message,
                Info = s_info,
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            ProvisioningErrorDetails provisioningErrorDetails = JsonConvert.DeserializeObject<ProvisioningErrorDetails>(body);

            // assert

            provisioningErrorDetails.ErrorCode.Should().Be(s_errorCode);
            provisioningErrorDetails.TrackingId.Should().Be(s_trackingId);
            provisioningErrorDetails.Message.Should().Be(s_message);
            provisioningErrorDetails.Info.First().Key.Should().Be("info-key");
            provisioningErrorDetails.Info.First().Value.Should().Be("info-value");
        }
    }
}
