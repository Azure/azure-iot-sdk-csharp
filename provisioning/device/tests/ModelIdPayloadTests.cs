// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ModelIdPayloadTests
    {
        [TestMethod]
        public void ModelIdPayload_ModelId()
        {
            // arrange

            var source = new ModelIdPayload
            {
                ModelId = "test-model-id",
            };
            string body = JsonSerializer.Serialize(source);

            // act
            ModelIdPayload payload = JsonSerializer.Deserialize<ModelIdPayload>(body);

            // assert
            payload.ModelId.Should().Be(source.ModelId);
        }
    }
}
