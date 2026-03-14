// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public void DigitalTwinGetResponse_Ctor_Ok()
        {
            // arrange - act
            const string twinId = "twin1234";
            const string modelId = "model1234";

            var simpleBasicDigitalTwin = new BasicDigitalTwin
            {
                Id = twinId,
                Metadata = new DigitalTwinMetadata
                {
                    ModelId = modelId
                }
            };

            var eTag = new ETag("1234");
            var digitalTwinGetResponse = new DigitalTwinGetResponse<BasicDigitalTwin>(simpleBasicDigitalTwin, eTag);

            // assert
            digitalTwinGetResponse.DigitalTwin.Should().Be(simpleBasicDigitalTwin);
            digitalTwinGetResponse.ETag.Should().Be(eTag);
            digitalTwinGetResponse.DigitalTwin.Id.Should().Be(twinId);
            digitalTwinGetResponse.DigitalTwin.Metadata.ModelId.Should().Be(modelId);
            digitalTwinGetResponse.DigitalTwin.CustomProperties.Should().NotBeNull();
        }
    }
}
