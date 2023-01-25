// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Azure;
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
        public void ClientTwin_JsonParse_Ok()
        {
            // arrange - act
            var clientTwin = new ClientTwin("test")
            {
                ETag = new ETag("AAAAAAAAAAM="),
                Version = 5,
                Status = ClientStatus.Enabled,
                StatusUpdatedOnUtc = new DateTimeOffset(2023, 1, 20, 8, 6, 32, new TimeSpan(1, 0, 0)),
                ConnectionState = ClientConnectionState.Connected,
                LastActiveOnUtc = new DateTimeOffset(2023, 1, 19, 8, 6, 32, new TimeSpan(1, 0, 0)),
            };

            string clientTwinSerialized = JsonConvert.SerializeObject(clientTwin);

            ClientTwin ct = JsonConvert.DeserializeObject<ClientTwin>(clientTwinSerialized);
            
            // assert
            ct.Should().BeEquivalentTo(clientTwin);
        }

        [TestMethod]
        public void Configuration_JsonParse_Ok()
        {
            // arrange - act
            const string ExpectedSchemaVersion = "1.0";
            var configuration = new Configuration("aa")
            {
                Content = new ConfigurationContent
                {
                    ModulesContent =
                    {
                        {
                            "edgeAgent", new Dictionary<string, object> { { "properties.desired", "test" } }
                        }
                    }
                }
            };
            string configurationSerialized = JsonConvert.SerializeObject(configuration);
            Configuration c = JsonConvert.DeserializeObject<Configuration>(configurationSerialized);

            // assert
            c.SchemaVersion.Should().Be(ExpectedSchemaVersion);
            c.Should().BeEquivalentTo(configuration);
        }

        [TestMethod]
        public void ImportConfiguration_JsonParse_Ok()
        {
            // arrange - act
            var importConfiguration = new ImportConfiguration("aa")
            {
                Id = "aa",
                ImportMode = ConfigurationImportMode.CreateOrUpdateIfMatchETag
            };
            string importConfigurationSerialized = JsonConvert.SerializeObject(importConfiguration);

            ImportConfiguration ic = JsonConvert.DeserializeObject<ImportConfiguration>(importConfigurationSerialized);

            // assert
            ic.Should().BeEquivalentTo(importConfiguration);
        }

        [TestMethod]
        public void FeedbackRecord_JsonParse_Ok()
        {
            // arrange - act
            var feedbackRecord = new FeedbackRecord
            {
                OriginalMessageId = "1",
                DeviceGenerationId = "2",
                DeviceId = "testDeviceId",
                EnqueuedOnUtc = new DateTimeOffset(2023, 1, 20, 8, 6, 32, new TimeSpan(1, 0, 0)),
                StatusCode = FeedbackStatusCode.Success,
                Description = "Success"
            };
            string feedbackRecordSerialized = JsonConvert.SerializeObject(feedbackRecord);

            // assert
            FeedbackRecord fr = JsonConvert.DeserializeObject<FeedbackRecord>(feedbackRecordSerialized);
            fr.Should().BeEquivalentTo(feedbackRecord);
        }

        [TestMethod]
        public void FileUploadNotification_JsonParse_Ok()
        {
            // arrange - act
            var fileUploadNotification = new FileUploadNotification
            {
                DeviceId = "testDeviceId",
                BlobName = "testBlob",
                BlobUriPath = new Uri("https://myaccount.blob.core.windows.net"),
                BlobSizeInBytes = 50,
                LastUpdatedOnUtc = new DateTimeOffset(2023, 1, 19, 8, 7, 32, new TimeSpan(1, 0, 0)),
                EnqueuedOnUtc = new DateTimeOffset(2023, 1, 20, 8, 6, 32, new TimeSpan(1, 0, 0))
            };

            string fileUploadNotificationSerialized = JsonConvert.SerializeObject(fileUploadNotification);

            FileUploadNotification fun = JsonConvert.DeserializeObject<FileUploadNotification>(fileUploadNotificationSerialized);

            // assert
            fun.Should().BeEquivalentTo(fileUploadNotification);
        }

        [TestMethod]
        public void BasicDigitalTwin_JsonParse_Ok()
        {
            // arrange - act
            var basicDigitalTwin = new BasicDigitalTwin
            {
                Id = "twinId1234",
                Metadata = new DigitalTwinMetadata
                {
                    ModelId = "modelId1234"
                }
            };

            string basicDigitalTwinSerialized = JsonConvert.SerializeObject(basicDigitalTwin);
            BasicDigitalTwin bdt = JsonConvert.DeserializeObject<BasicDigitalTwin>(basicDigitalTwinSerialized);

            // assert
            basicDigitalTwin.Should().BeEquivalentTo(bdt);
        }

        [TestMethod]
        public void BasicDigitalTwin_WithCustomProperties_JsonParse_Ok()
        {
            // arrange - act
            var basicDigitalTwin = new BasicDigitalTwin
            {
                Id = "twinId1234",
                Metadata = new DigitalTwinMetadata
                {
                    ModelId = "modelId1234",
                    WritableProperties =
                    {
                        { "additionalKey", "value" }
                    }
                },
                CustomProperties =
                {
                    { "desiredValue", "sampleValue" },
                    { "desiredVersion", 1 },
                    { "ackVersion", 1 },
                    { "ackCode", 200 },
                    { "ackDescription", "Ack Description" }
                }
            };

            string basicDigitalTwinSerialized = JsonConvert.SerializeObject(basicDigitalTwin);
            BasicDigitalTwin bdt = JsonConvert.DeserializeObject<BasicDigitalTwin>(basicDigitalTwinSerialized);

            // assert
            basicDigitalTwin.Should().BeEquivalentTo(bdt);
        }

        [TestMethod]
        public void WritableProperty_JsonParse_Ok()
        {
            // arrange - act
            var writableProperty = new WritableProperty
            {
                DesiredValue = "sampleValue",
                DesiredVersion = 1,
                AckVersion = 1,
                AckCode = 200,
                AckDescription = "Ack Description",
                LastUpdatedOnUtc = new DateTimeOffset(2023, 1, 20, 8, 6, 32, new TimeSpan(1, 0, 0))
            };

            string writablePropertySerialized = JsonConvert.SerializeObject(writableProperty);
            WritableProperty wp = JsonConvert.DeserializeObject<WritableProperty>(writablePropertySerialized);

            // assert
            writableProperty.Should().BeEquivalentTo(wp);
        }

        [TestMethod]
        public void ComponentMetadata_JsonParse_Ok()
        {
            // arrange - act
            var componentMetadata = new ComponentMetadata
            {
                WritableProperties =
                {
                    { "key1", "sampleValue" },
                    { "key2", 1 },
                }
            };
            string componentMetadataSerialized = JsonConvert.SerializeObject(componentMetadata);
            ComponentMetadata metaData = JsonConvert.DeserializeObject<ComponentMetadata>(componentMetadataSerialized);

            // assert
            metaData.Should().BeEquivalentTo(componentMetadata);
        }

        [TestMethod]
        public void CloudToDeviceMethodScheduledJob_JsonParse_Ok()
        {
            // arrange - act
            var cloudToDeviceMethodScheduledJob = new CloudToDeviceMethodScheduledJob(
                new DirectMethodServiceRequest("testMethod")
                {
                    Payload = "testPayload"
                }
            );

            string cloudToDeviceMethodScheduledJobSerialized = JsonConvert.SerializeObject(cloudToDeviceMethodScheduledJob);
            CloudToDeviceMethodScheduledJob job = JsonConvert.DeserializeObject<CloudToDeviceMethodScheduledJob>(cloudToDeviceMethodScheduledJobSerialized);

            // assert
            job.Should().BeEquivalentTo(cloudToDeviceMethodScheduledJob);
        }

        [TestMethod]
        public void DeviceJobStatistics_JsonParse_Ok()
        {
            // arrange - act
            var deviceJobStatistics = new DeviceJobStatistics
            {
                DeviceCount = 100,
                FailedCount = 50,
                SucceededCount = 0,
                RunningCount = 20,
                PendingCount = 30
            };

            string deviceJobStatisticsSerialized = JsonConvert.SerializeObject(deviceJobStatistics);

            DeviceJobStatistics statistics = JsonConvert.DeserializeObject<DeviceJobStatistics>(deviceJobStatisticsSerialized);

            // assert
            statistics.Should().BeEquivalentTo(deviceJobStatistics);
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
