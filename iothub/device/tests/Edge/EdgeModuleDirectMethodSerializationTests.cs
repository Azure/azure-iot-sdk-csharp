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

namespace Microsoft.Azure.Devices.Client.Tests.Edge
{
    [TestClass]
    [TestCategory("Unit")]
    public class EdgeModuleDirectMethodSerializationTests
    {
        [TestMethod]
        public void EdgeModuleDirectMethodSerialization_SerializesCorrectly()
        {
            // arrange
            byte[] payload = Encoding.Unicode.GetBytes("testPayload");
            var edgeDirectMethod = new EdgeModuleDirectMethodRequest("testMethod", payload);

            // act
            string edgeDirectMethodJson = JsonConvert.SerializeObject(edgeDirectMethod, Formatting.None);

            // assert
            edgeDirectMethodJson.Should().Be("test");
        }

        [TestMethod]
        public void EdgeModuleDirectMethodSerialization_DesrializesCorrectly()
        {
        }
    }
}
