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
    public class DeserializationTests
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

        [TestMethod]
        public void ImportConfiguration_Deserialize_OK()
        {
            const string expectedDeviceId = "aa";
            const string expectedSchemaVersion = "1.0";
            const string jsonString = @"
{
  ""id"": ""aa"",
  ""schemaVersion"": ""1.0"",
  ""importMode"": ""CreateOrUpdateIfMatchETag"",
  ""content"": {
    ""modulesContent"": {
        ""$edgeAgent"": {
            ""properties.desired"": {
            }
        }
    }
  }
}";
            ImportConfiguration importConfiguration = JsonConvert.DeserializeObject<ImportConfiguration>(jsonString);
            importConfiguration.Id.Should().Be(expectedDeviceId);
            importConfiguration.SchemaVersion.Should().Be(expectedSchemaVersion);
            importConfiguration.ImportMode.Should().Be(ConfigurationImportMode.CreateOrUpdateIfMatchETag);
        }

        [TestMethod]
        public void FeedbackRecord_Deserialize_OK()
        {
            const string originalMessageId = "1";
            const string deviceMessageId = "2";
            const string deviceId = "testDeviceId";
            const string description = "Success";
            DateTimeOffset enqueuedTimeUtc = new DateTimeOffset(2023, 1, 20, 8, 6, 32,
                                                new TimeSpan(1, 0, 0));
            const string jsonString = @"
{
  ""originalMessageId"": ""1"",
  ""deviceGenerationId"": ""2"",
  ""deviceId"": ""testDeviceId"",
  ""enqueuedTimeUtc"": ""1/20/2023 8:06:32 AM +01:00"",
  ""statusCode"": ""0"",
  ""description"": ""Success"",
}";
            FeedbackRecord feedbackRecord = JsonConvert.DeserializeObject<FeedbackRecord>(jsonString);
            feedbackRecord.OriginalMessageId.Should().Be(originalMessageId);
            feedbackRecord.DeviceGenerationId.Should().Be(deviceMessageId);
            feedbackRecord.DeviceId.Should().Be(deviceId);
            feedbackRecord.StatusCode.Should().Be(FeedbackStatusCode.Success);
            feedbackRecord.EnqueuedOnUtc.Should().Be(enqueuedTimeUtc);
            feedbackRecord.Description.Should().Be(description);
        }
    }
}
