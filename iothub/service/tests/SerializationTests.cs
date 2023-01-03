// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class SerializationTests
    {
        [TestMethod]
        public void Twin_JsonDateParse_Ok()
        {
            const string expectedDeviceId = "test";
            const string jsonString = @"
{
 ""deviceId"": ""test"",
 ""etag"": ""AAAAAAAAAAM="",
 ""version"": 5,
 ""status"": ""enabled"",
 ""statusUpdateTime"": ""2018-06-29T21:17:08.7759733"",
 ""connectionState"": ""Connected"",
 ""lastActivityTime"": ""2018-06-29T21:17:08.7759733"",
}";

            ClientTwin clientTwin = JsonConvert.DeserializeObject<ClientTwin>(jsonString);
            clientTwin.DeviceId.Should().Be(expectedDeviceId);
        }

        [TestMethod]
        public void Configuration_TestWithSchema_Ok()
        {
            const string expectedDeviceId = "aa";
            const string expectedSchemaVersion = "1.0";
            const string jsonString = @"
{
  ""id"": ""aa"",
  ""schemaVersion"": ""1.0"",
  ""content"": {
    ""modulesContent"": {
        ""$edgeAgent"": {
            ""properties.desired"": {
                ""schemaVersion"": ""1.0""
            }
        }
    }
  }
}";

            Configuration configuration = JsonConvert.DeserializeObject<Configuration>(jsonString);
            configuration.Id.Should().Be(expectedDeviceId);
            configuration.SchemaVersion.Should().Be(expectedSchemaVersion);
        }

        [TestMethod]
        public void Configuration_TestNullSchema_Ok()
        {
            const string expectedDeviceId = "aa";
            const string expectedSchemaVersion = "1.0";
            const string jsonString = @"
{
  ""id"": ""aa"",
  ""content"": {
    ""modulesContent"": {
        ""$edgeAgent"": {
            ""properties.desired"": {
            }
        }
    }
  }
}";
            Configuration configuration = JsonConvert.DeserializeObject<Configuration>(jsonString);
            configuration.Id.Should().Be(expectedDeviceId);
            configuration.SchemaVersion.Should().Be(expectedSchemaVersion);
        }
    }
}
