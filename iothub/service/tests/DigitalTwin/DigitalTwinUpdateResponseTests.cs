// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Azure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests.DigitalTwin
{
    [TestClass]
    [TestCategory("Unit")]
    public class DigitalTwinUpdateResponseTests
    {
        [TestMethod]
        public void DigitalTwinUpdateResponse_ctor_defaultProperties()
        {
            // arrange - act
            var digitalTwinUpdateResponse = new DigitalTwinUpdateResponse();

            // assert
            digitalTwinUpdateResponse.ETag.Should().Be(default(ETag));
            digitalTwinUpdateResponse.Location.Should().BeNull();
        }

        [TestMethod]
        public void DigitalTwinUpdateResponse_ctor_ok()
        {
            // arrange - act
            var eTag = new ETag("1234");
            string location = "https://contoso.azure-devices.net/digitaltwins/sampleDevice?api-version=2021-04-12";
            var digitalTwinUpdateResponse = new DigitalTwinUpdateResponse(eTag, location);

            // assert
            digitalTwinUpdateResponse.ETag.Should().Be(eTag);
            digitalTwinUpdateResponse.Location.Should().Be(location);
        }
    }
}
