// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests.DigitalTwin
{
    [TestClass]
    [TestCategory("Unit")]
    public class UpdateDigitalTwinOptionsTests
    {
        [TestMethod]
        public void UpdateDigitalTwinOptions_ctor_defaultProperties_Ok() {
            // arrange - act
            var updateDigitalTwinOptions = new UpdateDigitalTwinOptions();

            // assert
            updateDigitalTwinOptions.IfMatch.Should().Be(ETag.All);
        }
    }
}
