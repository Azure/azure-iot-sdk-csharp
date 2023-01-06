// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using FluentAssertions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class SerializationTests
    {
        [TestMethod]
        public void Twin_JsonDateParse_Ok()
        {
            var now = DateTime.Now;
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

            JsonConvert.DeserializeObject<Twin>(jsonString);
        }

        [TestMethod]
        public void Configuration_TestWithSchema_Ok()
        {
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

            JsonConvert.DeserializeObject<Configuration>(jsonString);
        }

        [TestMethod]
        public void Configuration_TestNullSchema_Ok()
        {
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

            JsonConvert.DeserializeObject<Configuration>(jsonString);
        }

        [TestMethod]
        public void Twin_Json_OverrideDefaultJsonSerializer_ExceedMaxDepthThrows()
        {
            // arrange
            string fakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            var ServiceClinet = ServiceClient.CreateFromConnectionString(fakeConnectionString);
            // above arragement is only for setting the defaultJsonSerializerSettings

            const string jsonString = @"
{
 ""deviceId"": ""test"",
 ""etag"": ""AAAAAAAAAAM="",
 ""version"": 5,
 ""status"": ""enabled"",
 ""statusUpdateTime"": ""2018-06-29T21:17:08.7759733"",
 ""connectionState"": ""Connected"",
 ""lastActivityTime"": ""2018-06-29T21:17:08.7759733"",
 ""Capabilities"": {
    ""IotEdge"": false
 }
}";
            var settings = new JsonSerializerSettings { MaxDepth = 1 };

            // act
            Func<Twin> act = () => JsonConvert.DeserializeObject<Twin>(jsonString, settings);

            // assert
            act.Should().Throw<Newtonsoft.Json.JsonReaderException>();
        }
    }
}
