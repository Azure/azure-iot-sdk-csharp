// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests.DigitalTwin
{
    [TestClass]
    [TestCategory("Unit")]
    public class DigitalTwinGetResponseTests
    {
        [TestMethod]
        public void DigitalTwinGetResponse_ctor_ok()
        {
            // arrange - act
            const string TwinId = "twin1234";
            const string ModelId = "model1234";

            var simpleBasicDigitalTwin = new BasicDigitalTwin
            {
                Id = TwinId,
                Metadata = new DigitalTwinMetadata
                {
                    ModelId = ModelId
                }
            };

            var eTag = new ETag("1234");
            var digitalTwinGetResponse = new DigitalTwinGetResponse<BasicDigitalTwin>(simpleBasicDigitalTwin, eTag);

            // assert
            digitalTwinGetResponse.DigitalTwin.Should().Be(simpleBasicDigitalTwin);
            digitalTwinGetResponse.ETag.Should().Be(eTag);
            digitalTwinGetResponse.DigitalTwin.Id.Should().Be(TwinId);
            digitalTwinGetResponse.DigitalTwin.Metadata.ModelId.Should().Be(ModelId);
            digitalTwinGetResponse.DigitalTwin.CustomProperties.Should().NotBeNull();
        }
    }
}
