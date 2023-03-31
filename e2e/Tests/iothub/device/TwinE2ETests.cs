﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Twins
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class TwinE2ETests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(TwinE2ETests)}_";

        private static readonly IotHubServiceClient s_serviceClient = TestDevice.ServiceClient;

        private static readonly List<object> s_listOfPropertyValues = new()
        {
            1,
            "someString",
            false,
            new CustomTwinProperty
            {
                Id = 123,
                Name = "someName",
            },
        };

        // ISO 8601 date-formatted string with trailing zeros in the microseconds portion.
        // This is to verify the Newtonsoft.Json known issue is worked around in the SDK.
        // See https://github.com/JamesNK/Newtonsoft.Json/issues/1511 for more details about the known issue.
        private const string DateTimeValue = "2023-01-31T10:37:08.4599400";

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    new IotHubClientMqttSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    new IotHubClientAmqpSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    new IotHubClientMqttSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    new IotHubClientAmqpSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceSetsInvalidReportedPropertyThrowsException_Amqp(IotHubClientTransportProtocol transportProtocol)
        {
            await Twin_DeviceSetsInvalidReportedPropertyThrowsExceptionAsync(
                    new IotHubClientAmqpSettings(transportProtocol))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceSetsInvalidReportedPropertyThrowsException_Mqtt(IotHubClientTransportProtocol transportProtocol)
        {
            await Twin_DeviceSetsInvalidReportedPropertyThrowsExceptionAsync(
                    new IotHubClientMqttSettings(transportProtocol))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    new IotHubClientMqttSettings(),
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    new IotHubClientAmqpSettings(),
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientMqttSettings(),
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientAmqpSettings(),
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientMqttSettings(),
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientAmqpSettings(),
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    new IotHubClientMqttSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    new IotHubClientAmqpSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        // This is mainly for testing serialization/deserialization behavior which is independent of the
        // transport protocol used, so we are not covering cases for other protocols than Mqtt here.
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_DateTimeProperties_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetDateTimePropertiesAsync(
                    new IotHubClientMqttSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    new IotHubClientMqttSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    new IotHubClientAmqpSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Mqtt()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    new IotHubClientMqttSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_MqttWs()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Amqp()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    new IotHubClientAmqpSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_AmqpWs()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAfterOpenCloseOpen_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAfterOpenCloseOpenAsync(
                    new IotHubClientMqttSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAfterOpenCloseOpen_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAfterOpenCloseOpenAsync(
                    new IotHubClientAmqpSettings())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpen_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpenAsync(
                    new IotHubClientMqttSettings(),
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpen_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpenAsync(
                    new IotHubClientAmqpSettings(),
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [DataTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        [TestCategory("LongRunning")]
        public async Task Twin_ClientSetsReportedPropertyWithoutDesiredPropertyCallback(IotHubClientTransportProtocol transportProtocol)
        {
            // arrange

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(transportProtocol));
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString()).ConfigureAwait(false);

            int connectionStatusChangeCount = 0;
            void ConnectionStatusChangeHandler(ConnectionStatusInfo connInfo)
            {
                Interlocked.Increment(ref connectionStatusChangeCount);
            }
            deviceClient.ConnectionStatusChangeCallback = ConnectionStatusChangeHandler;

            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            // act
            await RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            // assert
            Assert.AreEqual(0, connectionStatusChangeCount, "AMQP should not be disconnected.");
        }

        public static async Task Twin_DeviceSetsReportedPropertyAndGetsItBackAsync<T>(IotHubDeviceClient deviceClient, string deviceId, T propValue, CancellationToken ct)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_DeviceSetsReportedPropertyAndGetsItBackAsync)}: name={propName}, value={propValue}");

            var props = new ReportedProperties
            {
                [propName] = propValue
            };

            await deviceClient.OpenAsync(ct).ConfigureAwait(false);
            long newTwinVersion = await deviceClient.UpdateReportedPropertiesAsync(props, ct).ConfigureAwait(false);

            // Validate the updated twin from the device-client
            TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync(ct).ConfigureAwait(false);
            bool propertyFound = deviceTwin.Reported.TryGetValue(propName, out T actual);
            propertyFound.Should().BeTrue();
            // We don't support nested deserialization yet, so we'll need to serialize the response and compare them.
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            ClientTwin completeTwin = await s_serviceClient.Twins.GetAsync(deviceId, ct).ConfigureAwait(false);
            object actualProp = completeTwin.Properties.Reported[propName];
            JsonConvert.SerializeObject(actualProp).Should().Be(JsonConvert.SerializeObject(propValue));
            completeTwin.Properties.Reported.Version.Should().Be(newTwinVersion);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(IotHubClientTransportSettings transportSettings)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(IotHubClientTransportSettings transportSettings)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, s_listOfPropertyValues).ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsInvalidReportedPropertyThrowsExceptionAsync(IotHubClientTransportSettings transportSettings)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            string propName = Guid.NewGuid().ToString();
            var propValue = new Dictionary<string, object>
            {
                { "serialNumber", 5283920058631410000 },
            };

            VerboseTestLogger.WriteLine($"{nameof(Twin_DeviceSetsReportedPropertyAndGetsItBackAsync)}: name={propName}, value={propValue}");

            var props = new ReportedProperties
            {
                [propName] = propValue
            };

            using var openCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await deviceClient.OpenAsync(openCts.Token).ConfigureAwait(false);

            using var updateTwinCts = new CancellationTokenSource(s_defaultOperationTimeout);
            Func<Task> actionAsync = async () => await deviceClient.UpdateReportedPropertiesAsync(props, updateTwinCts.Token).ConfigureAwait(false);
            await actionAsync
                .Should()
                .ThrowAsync<IotHubClientException>()
                .Where(ex => ex.ErrorCode == IotHubClientErrorCode.ArgumentInvalid);
        }

        private async Task Twin_DeviceSetsReportedPropertyAfterOpenCloseOpenAsync(IotHubClientTransportSettings transportSettings)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            // Close and re-open the client under test.
            using var cts = new CancellationTokenSource(s_defaultOperationTimeout);
            await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);
            await deviceClient.CloseAsync(cts.Token).ConfigureAwait(false);
            await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

            // The client should still be able to send reported properties even though it was re-opened.
            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpenAsync<T>(
            IotHubClientTransportSettings transportSettings,
            T propValue)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            // Close and re-open the client under test.
            using var cts = new CancellationTokenSource(s_defaultOperationTimeout);
            await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);
            await deviceClient.CloseAsync(cts.Token).ConfigureAwait(false);
            await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
            using var subscribeCallbackCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await testDeviceCallbackHandler.SetTwinPropertyUpdateCallbackHandlerAndProcessAsync<T>(subscribeCallbackCts.Token).ConfigureAwait(false);

            testDeviceCallbackHandler.ExpectedTwinPatchKeyValuePair = new Tuple<string, object>(propName, propValue);
            using var twinResponseCts = new CancellationTokenSource(s_defaultTwinResponseTimeout);
            Task updateReceivedTask = testDeviceCallbackHandler.WaitForTwinCallbackAsync(twinResponseCts.Token);

            // The client should still be able to receive desired properties even though it was re-opened.
            await Task
                .WhenAll(
                    RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue),
                    updateReceivedTask)
                .ConfigureAwait(false);

            // Validate the updated twin from the device-client
            using var getDeviceTwinCts = new CancellationTokenSource(s_defaultOperationTimeout);
            TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync(getDeviceTwinCts.Token).ConfigureAwait(false);
            bool propertyFound = deviceTwin.Desired.TryGetValue(propName, out T actual);
            propertyFound.Should().BeTrue();
            // We don't support nested deserialization yet, so we'll need to serialize the response and compare them.
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            using var getServiceTwinCts = new CancellationTokenSource(s_defaultOperationTimeout);
            ClientTwin completeTwin = await s_serviceClient.Twins.GetAsync(testDevice.Id, getServiceTwinCts.Token).ConfigureAwait(false);
            dynamic actualProp = completeTwin.Properties.Desired[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actualProp), JsonConvert.SerializeObject(propValue));
        }

        public static async Task RegistryManagerUpdateDesiredPropertyAsync(string deviceId, string propName, object propValue, CancellationToken ct)
        {
            var twinPatch = new ClientTwin();
            twinPatch.Properties.Desired[propName] = propValue;

            await s_serviceClient.Twins.UpdateAsync(deviceId, twinPatch, cancellationToken: ct).ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(IotHubClientTransportSettings transportSettings, object propValue, CancellationToken ct)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            await TestDevice.OpenWithRetryAsync(deviceClient, ct).ConfigureAwait(false);

            // Set a callback
            await deviceClient.
                SetDesiredPropertyUpdateCallbackAsync(
                    (patch) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes)}: DesiredProperty: {patch}.");

                        // After unsubscribing it should never reach here
                        Assert.IsNull(patch);

                        return Task.FromResult(true);
                    },
                    ct)
                .ConfigureAwait(false);

            // Unsubscribe
            await deviceClient
                .SetDesiredPropertyUpdateCallbackAsync(null, ct)
                .ConfigureAwait(false);

            await RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue, ct)
                .ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync<T>(
            IotHubClientTransportSettings transportSettings,
            T propValue)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            using var openCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await deviceClient.OpenAsync(openCts.Token).ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);

            using var subscribeCallbackCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await testDeviceCallbackHandler.SetTwinPropertyUpdateCallbackHandlerAndProcessAsync<T>(subscribeCallbackCts.Token).ConfigureAwait(false);

            testDeviceCallbackHandler.ExpectedTwinPatchKeyValuePair = new Tuple<string, object>(propName, propValue);
            using var twinResponseCts = new CancellationTokenSource(s_defaultTwinResponseTimeout);
            Task updateReceivedTask = testDeviceCallbackHandler.WaitForTwinCallbackAsync(twinResponseCts.Token);

            await Task.WhenAll(
                RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue),
                updateReceivedTask).ConfigureAwait(false);

            // Validate the updated twin from the device-client
            using var getDeviceTwinCts = new CancellationTokenSource(s_defaultOperationTimeout);
            TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync(getDeviceTwinCts.Token).ConfigureAwait(false);
            bool propertyFound = deviceTwin.Desired.TryGetValue(propName, out T actual);
            propertyFound.Should().BeTrue();
            // We don't support nested deserialization yet, so we'll need to serialize the response and compare them.
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            using var getServiceTwinCts = new CancellationTokenSource(s_defaultOperationTimeout);
            ClientTwin completeTwin = await s_serviceClient.Twins.GetAsync(testDevice.Id, getServiceTwinCts.Token).ConfigureAwait(false);
            object actualProp = completeTwin.Properties.Desired[propName];
            JsonConvert.SerializeObject(actualProp).Should().Be(JsonConvert.SerializeObject(propValue));
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(IotHubClientTransportSettings transportSettings)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            var twinPatch = new ClientTwin();
            twinPatch.Properties.Desired[propName] = propValue;

            await s_serviceClient.Twins.UpdateAsync(testDevice.Id, twinPatch).ConfigureAwait(false);

            using var openCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await deviceClient.OpenAsync(openCts.Token).ConfigureAwait(false);

            using var getDeviceTwinCts = new CancellationTokenSource(s_defaultOperationTimeout);
            TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync(getDeviceTwinCts.Token).ConfigureAwait(false);
            bool propertyFound = deviceTwin.Desired.TryGetValue(propName, out string actual);
            propertyFound.Should().BeTrue();
            actual.Should().Be(propValue);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetDateTimePropertiesAsync(IotHubClientTransportSettings transportSettings)
        {
            const string propName = "Iso8601String";
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            var twinPatch = new ClientTwin();
            twinPatch.Properties.Desired[propName] = DateTimeValue;
            using var updateTwinCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await s_serviceClient.Twins.UpdateAsync(testDevice.Id, twinPatch, cancellationToken: updateTwinCts.Token).ConfigureAwait(false);

            using var openCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await TestDevice.OpenWithRetryAsync(deviceClient, openCts.Token).ConfigureAwait(false);

            using var getTwinCts = new CancellationTokenSource(s_defaultOperationTimeout);
            TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync(getTwinCts.Token).ConfigureAwait(false);
            bool propertyFound = deviceTwin.Desired.TryGetValue(propName, out string actual);
            propertyFound.Should().BeTrue();
            actual.Should().Be(DateTimeValue);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(IotHubClientTransportSettings transportSettings)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            using var openCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await deviceClient.OpenAsync(openCts.Token).ConfigureAwait(false);

            var patch = new ReportedProperties
            {
                [propName] = propValue
            };

            using var updateTwinCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await deviceClient.UpdateReportedPropertiesAsync(patch, updateTwinCts.Token).ConfigureAwait(false);

            using var closeCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await deviceClient.CloseAsync(closeCts.Token).ConfigureAwait(false);

            using var getTwinCts = new CancellationTokenSource(s_defaultOperationTimeout);
            ClientTwin serviceTwin = await s_serviceClient.Twins.GetAsync(testDevice.Id, getTwinCts.Token).ConfigureAwait(false);
            Assert.AreEqual<string>(serviceTwin.Properties.Reported[propName].ToString(), propValue);

            VerboseTestLogger.WriteLine($"Verified {serviceTwin.Properties.Reported[propName]}={propValue}");
        }

        private async Task Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(IotHubClientTransportSettings transportSettings)
        {
            string propName1 = Guid.NewGuid().ToString();
            string propName2 = Guid.NewGuid().ToString();

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);

            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            using var openCts = new CancellationTokenSource(s_defaultOperationTimeout);
            await deviceClient.OpenAsync(openCts.Token).ConfigureAwait(false);

            await deviceClient
                .UpdateReportedPropertiesAsync(
                    new ReportedProperties
                    {
                        [propName1] = null
                    })
                .ConfigureAwait(false);

            using var getTwinCts = new CancellationTokenSource(s_defaultOperationTimeout);
            ClientTwin serviceTwin = await s_serviceClient.Twins.GetAsync(testDevice.Id, getTwinCts.Token).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(propName1).Should().BeFalse();

            await deviceClient
                .UpdateReportedPropertiesAsync(
                    new ReportedProperties
                    {
                        [propName1] = new Dictionary<string, object>
                        {
                            [propName2] = null
                        }
                    })
                .ConfigureAwait(false);

            using var getTwinCts2 = new CancellationTokenSource(s_defaultOperationTimeout);
            serviceTwin = await s_serviceClient.Twins.GetAsync(testDevice.Id, getTwinCts2.Token).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(propName1).Should().BeTrue();
            serviceTwin.Properties.Reported.TryGetValue(propName1, out Dictionary<string, object> value1).Should().BeTrue();
            value1.Count.Should().Be(0);
        }

        [DataTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        [TestCategory("LongRunning")]
        public async Task Twin_Client_SetETag_Works(IotHubClientTransportProtocol transportProtocol)
        {
            // arrange

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(transportProtocol));
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            ClientTwin twin = await s_serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);
            ETag oldEtag = twin.ETag;

            twin.Properties.Desired[propName] = propValue;

            using var updateTwinCts = new CancellationTokenSource(s_defaultOperationTimeout);
            twin = await s_serviceClient.Twins.UpdateAsync(testDevice.Id, twin, true, updateTwinCts.Token).ConfigureAwait(false);

            twin.ETag = oldEtag;

            // set the 'onlyIfUnchanged' flag to true to check that, with an out of date ETag, the request throws a PreconditionFailedException.
            using var updateTwinCts2 = new CancellationTokenSource(s_defaultOperationTimeout);
            Func<Task> act = async () => { twin = await s_serviceClient.Twins.UpdateAsync(testDevice.Id, twin, true, updateTwinCts2.Token).ConfigureAwait(false); };
            var error = await act.Should().ThrowAsync<IotHubServiceException>("Expected test to throw a precondition failed exception since it updated a twin with an out of date ETag");
            error.And.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.PreconditionFailed);
            error.And.IsTransient.Should().BeFalse();

            // set the 'onlyIfUnchanged' flag to false to check that, even with an out of date ETag, the request performs without exception.
            using var updateTwinCts3 = new CancellationTokenSource(s_defaultOperationTimeout);
            await FluentActions
                .Invoking(async () => { twin = await s_serviceClient.Twins.UpdateAsync(testDevice.Id, twin, false, updateTwinCts3.Token).ConfigureAwait(false); })
                .Should()
                .NotThrowAsync<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to false");

            // set the 'onlyIfUnchanged' flag to true to check that, with an up-to-date ETag, the request performs without exception.
            twin.Properties.Desired[propName] = propValue + "1";
            twin.ETag = ETag.All;
            using var updateTwinCts4 = new CancellationTokenSource(s_defaultOperationTimeout);
            await FluentActions
                .Invoking(async () => { twin = await s_serviceClient.Twins.UpdateAsync(testDevice.Id, twin, true, updateTwinCts4.Token).ConfigureAwait(false); })
                .Should()
                .NotThrowAsync<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to true");
        }
    }

    internal class CustomTwinProperty
    {
        // The properties in here need to be public otherwise NewtonSoft.Json cannot serialize and deserialize them properly.
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
