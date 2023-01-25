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
  ""originalMessageId"": 1,
  ""deviceGenerationId"": 2,
  ""deviceId"": ""testDeviceId"",
  ""enqueuedTimeUtc"": ""1/20/2023 8:06:32 AM +01:00"",
  ""statusCode"": 0,
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
  ""blobSizeInBytes"": 50
}";
            FileUploadNotification fileUploadNotification = JsonConvert.DeserializeObject<FileUploadNotification>(jsonString);
            fileUploadNotification.BlobUriPath.Should().Be(expectedBlobUri);
            fileUploadNotification.BlobName.Should().Be(ExpectedBlobName);
            fileUploadNotification.DeviceId.Should().Be(ExpectedDeviceId);
            fileUploadNotification.BlobSizeInBytes.Should().Be(BlobSizeInBytes);
            fileUploadNotification.EnqueuedOnUtc.Should().Be(enqueuedTimeUtc);
            fileUploadNotification.LastUpdatedOnUtc.Should().Be(lastUpdatedOnUtc);
        }

        [TestMethod]
        public void BasicDigitalTwin_Deserialize_Ok()
        {
            const string ExpectedTwinId = "twinId1234";
            const string ExpectedModelId = "modelId1234";
            var ExpectedMetaData = new DigitalTwinMetadata
            {
                ModelId = "modelId1234"
            };
            const string jsonString = @"
            {
              ""$dtId"": ""twinId1234"",
              ""$metadata"": {
                   ""$model"": ""modelId1234""
                }
            }";
            BasicDigitalTwin basicDigitalTwin = JsonConvert.DeserializeObject<BasicDigitalTwin>(jsonString);
            basicDigitalTwin.Id.Should().Be(ExpectedTwinId);
            basicDigitalTwin.Metadata.Should().BeEquivalentTo(ExpectedMetaData);
            basicDigitalTwin.Metadata.ModelId.Should().Be(ExpectedModelId);
            basicDigitalTwin.Metadata.WritableProperties.Should().NotBeNull();
        }

        [TestMethod]
        public void BasicDigitalTwin_WithCustomProperties_Deserialize_Ok()
        {
            const string ExpectedTwinId = "twinId1234";
            const string ExpectedModelId = "modelId1234";

            const string jsonString = @"
            {
              ""$dtId"": ""twinId1234"",
              ""$metadata"": {
                   ""$model"": ""modelId1234"",
                   ""additionalKey"": ""value""
                },
              ""desiredValue"": ""sampleValue"",
              ""desiredVersion"": 1,
              ""ackVersion"": 1,
              ""ackCode"": 200,
              ""ackDescription"": ""Ack Description"",
            }";

            BasicDigitalTwin basicDigitalTwin = JsonConvert.DeserializeObject<BasicDigitalTwin>(jsonString);
            basicDigitalTwin.Id.Should().Be(ExpectedTwinId);
            basicDigitalTwin.CustomProperties.Should().HaveCount(5);
            basicDigitalTwin.Metadata.WritableProperties.Should().HaveCount(1);
            basicDigitalTwin.Metadata.ModelId.Should().Be(ExpectedModelId);
        }

        [TestMethod]
        public void WritableProperty_Deserialize_Ok()
        {
            const string ExpectedDesiredValue = "sampleValue";
            const int ExpectedDesiredVersion = 1;
            const int ExpectedAckVersion = 1;
            const int ExpectedAckCode = 200;
            const string ExpectedAckDescription = "Ack Description";

            var lastUpdateTime = new DateTimeOffset(2023, 1, 20, 8, 6, 32,
                        new TimeSpan(1, 0, 0));

            const string jsonMetadataString = @"
            {
                 ""$model"": ""modelId1234"",
                 ""desiredValue"": ""sampleValue"",
                 ""desiredVersion"": 1,
                 ""ackVersion"": 1,
                 ""ackCode"": 200,
                 ""ackDescription"": ""Ack Description"",
                 ""lastUpdateTime"": ""1/20/2023 8:06:32 AM +01:00""
            }";
            WritableProperty writableProperty = JsonConvert.DeserializeObject<WritableProperty>(jsonMetadataString);
            writableProperty.DesiredValue.Should().Be(ExpectedDesiredValue);
            writableProperty.DesiredVersion.Should().Be(ExpectedDesiredVersion);
            writableProperty.AckVersion.Should().Be(ExpectedAckVersion);
            writableProperty.AckCode.Should().Be(ExpectedAckCode);
            writableProperty.AckDescription.Should().Be(ExpectedAckDescription);
            writableProperty.LastUpdatedOnUtc.Should().Be(lastUpdateTime);
        }

        [TestMethod]
        public void ComponentMetadata_Deserialize_Ok()
        {
            const string ExpectedKey1Value = "sampleValue";
            const int ExpectedKey2Value = 1;

            const string jsonMetadataString = @"
            {
                 ""key1"": ""sampleValue"",
                 ""key2"": 1
            }";
            ComponentMetadata componentMetadata = JsonConvert.DeserializeObject<ComponentMetadata>(jsonMetadataString);
            componentMetadata.WritableProperties["key1"].Should().Be(ExpectedKey1Value);
            componentMetadata.WritableProperties["key2"].Should().Be(ExpectedKey2Value);
        }

        [TestMethod]
        public void CloudToDeviceMethodScheduledJob_Deserialize_Ok()
        {
            const string ExpectedMethod = "testMethod";
            const string ExpectedPayload = "testPayload";
            const string jsonString = @"
            {
              ""cloudToDeviceMethod"": {
                    ""methodName"": ""testMethod"",
                    ""payload"": ""testPayload""
                }
            }";

            CloudToDeviceMethodScheduledJob job = JsonConvert.DeserializeObject<CloudToDeviceMethodScheduledJob>(jsonString);
            job.DirectMethodRequest.MethodName.Should().Be(ExpectedMethod);
            job.DirectMethodRequest.Payload.Should().Be(ExpectedPayload);
        }

        [TestMethod]
        public void DeviceJobStatistics_Deserialize_Ok()
        {
            const int ExpectedDeviceCount = 100;
            const int ExpectedFailedCount = 50;
            const int ExpectedRunningCount = 20;
            const int ExpectedSucceededCount = 0;
            const int ExpectedPendingCount = 30;
            const string jsonString = @"
            {
              ""deviceCount"": 100,
              ""failedCount"": 50,
              ""SucceededCount"": 0,
              ""runningCount"": 20,
              ""pendingCount"": 30,
            }";

            DeviceJobStatistics statistics = JsonConvert.DeserializeObject<DeviceJobStatistics>(jsonString);
            statistics.DeviceCount.Should().Be(ExpectedDeviceCount);
            statistics.FailedCount.Should().Be(ExpectedFailedCount);
            statistics.SucceededCount.Should().Be(ExpectedSucceededCount);
            statistics.RunningCount.Should().Be(ExpectedRunningCount);
            statistics.PendingCount.Should().Be(ExpectedPendingCount);
        }
    }
}
