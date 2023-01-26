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
    public class SerializationTests
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
    }
}
