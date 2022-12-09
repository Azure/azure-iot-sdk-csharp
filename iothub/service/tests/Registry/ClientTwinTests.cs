// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Azure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ClientTwinTests
    {
        [TestMethod]
        public void ClientTwin_ParentScopes_NotNull()
        {
            // arrange
            var twin = new ClientTwin();

            // assert
            twin.ParentScopes.Should().NotBeNull("To prevent NREs because a property was unexecptedly null, it should have a default list instance assigned.");
            twin.ParentScopes.Should().BeEmpty("The default list instance should be empty.");
        }

        [TestMethod]
        public void ClientTwin_Tags_NotNull()
        {
            // arrange
            var twin = new ClientTwin();

            // assert
            twin.Tags.Should().NotBeNull("To prevent NREs because a property was unexecptedly null, it should have a default list instance assigned.");
        }

        [TestMethod]
        public void ClientTwin_Properties_NotNull()
        {
            // arrange
            var twin = new ClientTwin();

            // assert
            twin.Properties.Should().NotBeNull("To prevent NREs because a property was unexecptedly null, it should have a default list instance assigned.");
        }

        [TestMethod]
        public void ClientTwin_Configurations_NotNull()
        {
            // arrange
            var twin = new ClientTwin();

            // assert
            twin.Configurations.Should().NotBeNull("To prevent NREs because a property was unexecptedly null, it should have a default list instance assigned.");
        }

        [TestMethod]
        public void ClientTwin_Capabilities_NotNull()
        {
            // arrange
            var twin = new ClientTwin();

            // assert
            twin.Capabilities.Should().NotBeNull("To prevent NREs because a property was unexecptedly null, it should have a default list instance assigned.");
        }

        [TestMethod]
        public void ClientTwin_BasicPropertiesSerialize()
        {
            // arrange
            const string deviceId = nameof(deviceId);
            const string modelId = nameof(modelId);
            const string moduleId = nameof(moduleId);
            var tags = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 2 },
            };
            ETag eTag = new($"\"{nameof(eTag)}\"");
            ETag deviceETag = new($"\"{nameof(deviceETag)}\"");
            long version = 1;
            var status = ClientStatus.Disabled;
            const string statusReason = nameof(statusReason);
            var statusUpdateOnUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(1));
            var connectionState = ClientConnectionState.Connected;
            var lastActiveOnUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(5));
            int cloudToDeviceMessageCount = 2;
            var authenticationType = ClientAuthenticationType.Sas;
            const string deviceScope = nameof(deviceScope);
            var parentScopes = new List<string> { nameof(deviceScope) };

            var twin = new ClientTwin(deviceId)
            {
                ModelId = modelId,
                ModuleId = moduleId,
                Tags = tags,
                ETag = eTag,
                DeviceETag = deviceETag,
                Version = version,
                Status = status,
                StatusReason = statusReason,
                StatusUpdatedOnUtc = statusUpdateOnUtc,
                ConnectionState = connectionState,
                LastActiveOnUtc = lastActiveOnUtc,
                CloudToDeviceMessageCount = cloudToDeviceMessageCount,
                AuthenticationType = authenticationType,
                DeviceScope = deviceScope,
                ParentScopes = parentScopes,
            };

            string twinJson = JsonConvert.SerializeObject(twin);
            twinJson.Should().NotBeNull();

            ClientTwin actual = JsonConvert.DeserializeObject<ClientTwin>(twinJson);
            actual.Should().NotBeNull();
            actual.DeviceId.Should().Be(deviceId);
            actual.ModelId.Should().Be(modelId);
            actual.ModuleId.Should().Be(moduleId);
            actual.Tags.Should().BeEquivalentTo(tags);
            actual.ETag.Should().Be(eTag);
            actual.DeviceETag.Should().Be(deviceETag);
            actual.Version.Should().Be(version);
            actual.Status.Should().Be(status);
            actual.StatusUpdatedOnUtc.Should().Be(statusUpdateOnUtc);
            actual.ConnectionState.Should().Be(connectionState);
            actual.LastActiveOnUtc.Should().Be(lastActiveOnUtc);
            actual.CloudToDeviceMessageCount.Should().Be(cloudToDeviceMessageCount);
            actual.AuthenticationType.Should().Be(authenticationType);
            actual.DeviceScope.Should().Be(deviceScope);
            actual.ParentScopes.Should().BeEquivalentTo(parentScopes);
        }

        [TestMethod]
        public void ClientTwin_Properties_Serializes()
        {
            // arrange

            const string reportedStringKey = nameof(reportedStringKey);
            const string reportedStringValue = nameof(reportedStringValue);
            const string reportedIntKey = nameof(reportedIntKey);
            const int reportedIntValue = 3;
            const string reportedDateTimeOffsetKey = nameof(reportedDateTimeOffsetKey);
            const string reportedBoolKey = nameof(reportedBoolKey);
            const bool reportedBoolValue = true;
            DateTimeOffset reportedDateTimeOffsetValue = DateTimeOffset.UtcNow;
            const string reportedCustomKey = nameof(reportedCustomKey);
            var reportedCustomValue = new CustomType { CustomInt = reportedIntValue, CustomString = reportedStringValue };

            const string desiredKey = nameof(desiredKey);
            const string desiredValue = nameof(desiredValue);

            var twin = new ClientTwin
            {
                Properties =
                {
                    Desired =
                    {
                        [desiredKey] = desiredValue,
                    },
                    Reported =
                    {
                        [reportedStringKey] = reportedStringValue,
                        [reportedIntKey] = reportedIntValue,
                        [reportedDateTimeOffsetKey] = reportedDateTimeOffsetValue,
                        [reportedBoolKey] = reportedBoolValue,
                        [reportedCustomKey] = reportedCustomValue,
                    },
                }
            };

            // act

            string json = JsonConvert.SerializeObject(twin);
            ClientTwin actual = JsonConvert.DeserializeObject<ClientTwin>(json);

            // assert

            // Using TryGetValue

            actual.Properties.Reported.TryGetValue(reportedStringKey, out string actualStringValue).Should().BeTrue();
            actualStringValue.Should().Be(reportedStringValue);

            actual.Properties.Reported.TryGetValue(reportedIntKey, out int actualIntValue).Should().BeTrue();
            actualIntValue.Should().Be(reportedIntValue);

            actual.Properties.Reported.TryGetValue(reportedDateTimeOffsetKey, out DateTimeOffset actualDateTimeOffsetValue).Should().BeTrue();
            actualDateTimeOffsetValue.Should().Be(reportedDateTimeOffsetValue);

            actual.Properties.Reported.TryGetValue(reportedBoolKey, out bool actualBoolValue).Should().BeTrue();
            actualBoolValue.Should().Be(reportedBoolValue);

            actual.Properties.Reported.TryGetValue(reportedCustomKey, out CustomType actualCustomValue).Should().BeTrue();
            actualCustomValue.CustomInt.Should().Be(reportedCustomValue.CustomInt);
            actualCustomValue.CustomString.Should().Be(reportedCustomValue.CustomString);

            actual.Properties.Desired.TryGetValue(desiredKey, out string actualDesiredValue).Should().BeTrue();
            actualDesiredValue.Should().Be(desiredValue);

            // Using cast from indexer

            ((string)actual.Properties.Reported[reportedStringKey]).Should().Be(reportedStringValue);
            ((int)actual.Properties.Reported[reportedIntKey]).Should().Be(reportedIntValue);
            ((DateTimeOffset)actual.Properties.Reported[reportedDateTimeOffsetKey]).Should().Be(reportedDateTimeOffsetValue);
            ((bool)actual.Properties.Reported[reportedBoolKey]).Should().Be(reportedBoolValue);

            ((string)actual.Properties.Desired[desiredKey]).Should().Be(desiredValue);
        }

        [TestMethod]
        public void ClientTwin_Properties_DeserializesComplexTwin()
        {
            // arrange
            // To understand the expected values below, one must read the JSON in this file
            string complexTwinJson = File.ReadAllText("Registry/ComplexTwin.json");

            // act
            ClientTwin twin = JsonConvert.DeserializeObject<ClientTwin>(complexTwinJson);

            // assert

            twin.Properties.Should().NotBeNull();
            twin.Properties.Desired.Should().NotBeNull();
            twin.Properties.Reported.Should().NotBeNull();

            // Desired root properties

            twin.Properties.Desired.Version.Should().Be(2);
            twin.Properties.Desired.Count.Should().BeGreaterThan(0);
            twin.Properties.Desired.Metadata.Should().NotBeNull();

            // Desired root metadata

            var lastUpdatedExpected = DateTimeOffset.Parse("2022-07-14T19:52:01.8575042Z");
            twin.Properties.Desired.Metadata.LastUpdatedVersion.Should().Be(2);
            twin.Properties.Desired.Metadata.LastUpdatedOnUtc.Should().Be(lastUpdatedExpected);

            // Desired root property metadata

            twin.Properties.Desired.Metadata
                .TryGetPropertyMetadata("thermostat2", out ClientTwinMetadata thermostat2Metadata)
                .Should().BeTrue();
            thermostat2Metadata.LastUpdatedVersion.Should().Be(2);
            thermostat2Metadata.LastUpdatedOnUtc.Should().Be(lastUpdatedExpected);

            // Desired nested property metadata
            thermostat2Metadata
                .TryGetPropertyMetadata("targetTemperature", out ClientTwinMetadata thermostat2TargetTemperatureMetadata)
                .Should().BeTrue();
            thermostat2TargetTemperatureMetadata.LastUpdatedVersion.Should().Be(2);
            thermostat2TargetTemperatureMetadata.LastUpdatedOnUtc.Should().Be(lastUpdatedExpected);

            // Reported root properties

            twin.Properties.Reported.Version.Should().Be(12);
            twin.Properties.Reported.Count.Should().Be(4);

            // Reported root simple property

            twin.Properties.Reported
                .TryGetValue("serialNumber", out string serialNumber)
                .Should().BeTrue();
            serialNumber.Should().Be("SR-123456");

            // Reported root complex property

            twin.Properties.Reported
                .TryGetValue("thermostat1", out ThermostatReported thermostat1Reported)
                .Should().BeTrue();
            thermostat1Reported.Component.Should().Be("c");
            thermostat1Reported.TargetTemperature.Value.Should().Be(70);
            thermostat1Reported.TargetTemperature.Code.Should().Be(203);
            thermostat1Reported.TargetTemperature.Version.Should().Be(0, "Initialized value so version is 0");
            thermostat1Reported.TargetTemperature.Description.Should().Be("Initialized with default value");
            thermostat1Reported.MaxTempSinceLastReboot.Should().Be(36.1D);

            // Reported root metadata
            twin.Properties.Reported.Metadata
                .LastUpdatedOnUtc
                .Should().Be(DateTimeOffset.Parse("2022-07-29T00:55:15.5412825Z"));

            // Reported root property metadata
            var reportedPropertyLastUpdated = DateTimeOffset.Parse("2022-07-29T00:55:15.2912928Z");
            twin.Properties.Reported.Metadata
                .TryGetPropertyMetadata("thermostat1", out ClientTwinMetadata thermostat1ReportedMetadata)
                .Should().BeTrue();
            thermostat1ReportedMetadata.LastUpdatedOnUtc.Should().Be(reportedPropertyLastUpdated);

            // Reported nested property metadata
            thermostat1ReportedMetadata
                .TryGetPropertyMetadata("maxTempSinceLastReboot", out ClientTwinMetadata maxTempSinceLastRebootMetadata)
                .Should().BeTrue();
            maxTempSinceLastRebootMetadata.LastUpdatedOnUtc.Should().Be(reportedPropertyLastUpdated);
        }

        private class CustomType
        {
            [JsonProperty("customInt")]
            public int CustomInt { get; set; }

            [JsonProperty("customString")]
            public string CustomString { get; set; }
        }

        private class ThermostatReported
        {
            [JsonProperty("__t")]
            public string Component { get; set; }

            public WritablePropertyResponse<int> TargetTemperature { get; set; }

            [JsonProperty("maxTempSinceLastReboot")]
            public double MaxTempSinceLastReboot { get; set; }
        }

        private class WritablePropertyResponse<T>
        {
            [JsonProperty("value")]
            public T Value { get; set; }

            [JsonProperty("ac")]
            public int Code { get; set; }

            [JsonProperty("av")]
            public int Version { get; set; }

            [JsonProperty("ad")]
            public string Description { get; set; }
        }
    }
}