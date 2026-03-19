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
        private const int FakeErrorCode = 404;
        private const string FakeTrackingId = "test-tracking-id";
        private const string FakeMessage = "This is a testing provisioning error details message.";

        private static readonly Dictionary<string, string> s_info = new() { { "info-key", "info-value" } };

        [TestMethod]
        public void ProvisioningErrorDetails_Properties()
        {
            // arrange

            var source = new ProvisioningErrorDetails
            {
                ErrorCode = FakeErrorCode,
                TrackingId = FakeTrackingId,
                Message = FakeMessage,
                Info = s_info,
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            ProvisioningErrorDetails provisioningErrorDetails = JsonConvert.DeserializeObject<ProvisioningErrorDetails>(body);

            // assert

            provisioningErrorDetails.ErrorCode.Should().Be(source.ErrorCode);
            provisioningErrorDetails.TrackingId.Should().Be(source.TrackingId);
            provisioningErrorDetails.Message.Should().Be(source.Message);
            provisioningErrorDetails.Info.First().Key.Should().Be(source.Info.First().Key);
            provisioningErrorDetails.Info.First().Value.Should().Be(source.Info.First().Value);
        }
    }
}
