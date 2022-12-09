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
            actual.DeviceETag.ToString().Should().Be(deviceETag.ToString());
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

        private class CustomType
        {
            [JsonProperty("customInt")]
            public int CustomInt { get; set; }

            [JsonProperty("customString")]
            public string CustomString { get; set; }
        }
    }
}