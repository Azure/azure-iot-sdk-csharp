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
            const string ExpectedDeviceId = "test";
            const string JsonString = @"
{
 ""deviceId"": ""test"",
 ""etag"": ""AAAAAAAAAAM="",
 ""version"": 5,
 ""status"": ""enabled"",
 ""statusUpdateTime"": ""2018-06-29T21:17:08.7759733"",
 ""connectionState"": ""Connected"",
 ""lastActivityTime"": ""2018-06-29T21:17:08.7759733"",
}";

            ClientTwin clientTwin = JsonConvert.DeserializeObject<ClientTwin>(JsonString);
            clientTwin.DeviceId.Should().Be(ExpectedDeviceId);
        }

        [TestMethod]
        public void Configuration_TestWithSchema_Ok()
        {
            const string ExpectedDeviceId = "aa";
            const string ExpectedSchemaVersion = "1.0";
            const string JsonString = @"
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

            Configuration configuration = JsonConvert.DeserializeObject<Configuration>(JsonString);
            configuration.Id.Should().Be(ExpectedDeviceId);
            configuration.SchemaVersion.Should().Be(ExpectedSchemaVersion);
        }

        [TestMethod]
        public void Configuration_TestNullSchema_Ok()
        {
            const string ExpectedDeviceId = "aa";
            const string ExpectedSchemaVersion = "1.0";
            const string JsonString = @"
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
            Configuration configuration = JsonConvert.DeserializeObject<Configuration>(JsonString);
            configuration.Id.Should().Be(ExpectedDeviceId);
            configuration.SchemaVersion.Should().Be(ExpectedSchemaVersion);
        }

        [TestMethod]
        public void ImportConfiguration_Deserialize_Ok()
        {
            const string ExpectedDeviceId = "aa";
            const string ExpectedSchemaVersion = "1.0";
            const string JsonString = @"
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
            ImportConfiguration importConfiguration = JsonConvert.DeserializeObject<ImportConfiguration>(JsonString);
            importConfiguration.Id.Should().Be(ExpectedDeviceId);
            importConfiguration.SchemaVersion.Should().Be(ExpectedSchemaVersion);
            importConfiguration.ImportMode.Should().Be(ConfigurationImportMode.CreateOrUpdateIfMatchETag);
        }

        [TestMethod]
        public void FeedbackRecord_Deserialize_Ok()
        {
            const string ExpectedOriginalMessageId = "1";
            const string ExpectedDeviceMessageId = "2";
            const string ExpectedDeviceId = "testDeviceId";
            const string ExpectedDescription = "Success";
            var expectedEnqueuedTimeUtc = new DateTimeOffset(2023, 1, 20, 8, 6, 32,
                                                new TimeSpan(1, 0, 0));
            const string JsonString = @"
{
  ""originalMessageId"": ""1"",
  ""deviceGenerationId"": ""2"",
  ""deviceId"": ""testDeviceId"",
  ""enqueuedTimeUtc"": ""1/20/2023 8:06:32 AM +01:00"",
  ""statusCode"": ""0"",
  ""description"": ""Success"",
}";
            FeedbackRecord feedbackRecord = JsonConvert.DeserializeObject<FeedbackRecord>(JsonString);
            feedbackRecord.OriginalMessageId.Should().Be(ExpectedOriginalMessageId);
            feedbackRecord.DeviceGenerationId.Should().Be(ExpectedDeviceMessageId);
            feedbackRecord.DeviceId.Should().Be(ExpectedDeviceId);
            feedbackRecord.StatusCode.Should().Be(FeedbackStatusCode.Success);
            feedbackRecord.EnqueuedOnUtc.Should().Be(expectedEnqueuedTimeUtc);
            feedbackRecord.Description.Should().Be(ExpectedDescription);
        }

        [TestMethod]
        public void FileUploadNotification_Deserialize_Ok()
        {
            var expectedBlobUri = new Uri("https://myaccount.blob.core.windows.net");
            const string ExpectedBlobName = "testBlob";
            const string ExpectedDeviceId = "testDeviceId";
            var lastUpdatedOnUtc = new DateTimeOffset(2023, 1, 19, 8, 7, 32,
                                                new TimeSpan(1, 0, 0));
            var enqueuedTimeUtc = new DateTimeOffset(2023, 1, 20, 8, 6, 32,
                                                new TimeSpan(1, 0, 0));
            const long BlobSizeInBytes = 50;
            const string jsonString = @"
{
  ""blobUri"": ""https://myaccount.blob.core.windows.net"",
  ""BlobName"": ""testBlob"",
  ""deviceId"": ""testDeviceId"",
  ""blobName"": ""testBlob"",
  ""lastUpdatedTime"": ""1/19/2023 8:07:32 AM +01:00"", 
  ""enqueuedTimeUtc"": ""1/20/2023 8:06:32 AM +01:00"",
  ""blobSizeInBytes"": ""50""
}";
            FileUploadNotification fileUploadNotification = JsonConvert.DeserializeObject<FileUploadNotification>(jsonString);
            fileUploadNotification.BlobUriPath.Should().Be(expectedBlobUri);
            fileUploadNotification.BlobName.Should().Be(ExpectedBlobName);
            fileUploadNotification.DeviceId.Should().Be(ExpectedDeviceId);
            fileUploadNotification.BlobSizeInBytes.Should().Be(BlobSizeInBytes);
            fileUploadNotification.EnqueuedOnUtc.Should().Be(enqueuedTimeUtc);
            fileUploadNotification.LastUpdatedOnUtc.Should().Be(lastUpdatedOnUtc);
        }

//        [TestMethod]
//        public void BasicDigitalTwin_Deserialize_Ok()
//        {
//            const string ExpectedId = "twinId1234";
//            var ExpectedMetaData = new DigitalTwinMetadata
//            {

//            }
//            const long BlobSizeInBytes = 50;
//            const string jsonString = @"
//{
//  ""$dtId"": ""twinId1234"",
//  ""$metadata"": ""testBlob"",
//  ""lastUpdatedTime"": ""1/19/2023 8:07:32 AM +01:00"", 
//  ""enqueuedTimeUtc"": ""1/20/2023 8:06:32 AM +01:00"",
//  ""blobSizeInBytes"": ""50""
//}";
//            FileUploadNotification fileUploadNotification = JsonConvert.DeserializeObject<FileUploadNotification>(jsonString);
//            fileUploadNotification.BlobUriPath.Should().Be(BlobUri);
//            fileUploadNotification.BlobName.Should().Be(BlobName);
//            fileUploadNotification.DeviceId.Should().Be(DeviceId);
//            fileUploadNotification.BlobSizeInBytes.Should().Be(BlobSizeInBytes);
//            fileUploadNotification.EnqueuedOnUtc.Should().Be(enqueuedTimeUtc);
//            fileUploadNotification.LastUpdatedOnUtc.Should().Be(lastUpdatedOnUtc);
//        }
    }
}
