// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class TwinPropertiesTests
    {
        [TestMethod]
        public void TwinProperties_SerializesCorrectly()
        {
            // arrange
            var properties = new Dictionary<string, object>
            {
                { "$version", 1 }
            };

            var desired = new PropertyCollection(properties);
            var reported = new PropertyCollection(properties, false);
            var twinProperties = new TwinProperties(desired, reported);

            // act
            string jsonTwinProperties = twinProperties.ToJson();

            // assert
            jsonTwinProperties.Should().BeEquivalentTo("{\"properties\":{\"desired\":{\"$version\":1},\"reported\":{\"$version\":1}}}");
        }
    }
}
