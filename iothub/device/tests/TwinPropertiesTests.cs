// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

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

            var desired = new DesiredProperties(properties);
            var reported = new ReportedProperties(properties, false);
            var twinProperties = new TwinProperties(desired, reported);

            // act
            string jsonTwinProperties = twinProperties.ToJson();

            // assert
            jsonTwinProperties.Should().BeEquivalentTo("{\"properties\":{\"desired\":{\"$version\":1},\"reported\":{\"$version\":1}}}");
        }
    }
}
