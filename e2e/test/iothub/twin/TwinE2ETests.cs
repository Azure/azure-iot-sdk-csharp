// Copyright (c) Microsoft. All rights reserved.
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
    [TestCategory("IoTHub")]
    public class TwinE2ETests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(TwinE2ETests)}_";

        private static readonly IotHubServiceClient _serviceClient = new(TestConfiguration.IotHub.ConnectionString);

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
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientAmqpSettings(),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientMqttSettings(),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientAmqpSettings(),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
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
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpen_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpenAsync(
                    new IotHubClientAmqpSettings(),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
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

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(transportProtocol));
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

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

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, s_listOfPropertyValues).ConfigureAwait(false);
        }

        public static async Task Twin_DeviceSetsReportedPropertyAndGetsItBackAsync<T>(IotHubDeviceClient deviceClient, string deviceId, T propValue)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_DeviceSetsReportedPropertyAndGetsItBackAsync)}: name={propName}, value={propValue}");

            var props = new ReportedProperties();
            props[propName] = propValue;
            await deviceClient.OpenAsync().ConfigureAwait(false);
            long newTwinVersion = await deviceClient.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

            // Validate the updated twin from the device-client
           TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync().ConfigureAwait(false);
            bool propertyFound = deviceTwin.Reported.TryGetValue(propName, out T actual);
            propertyFound.Should().BeTrue();
            // We don't support nested deserialization yet, so we'll need to serialize the response and compare them.
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            ClientTwin completeTwin = await _serviceClient.Twins.GetAsync(deviceId).ConfigureAwait(false);
            object actualProp = completeTwin.Properties.Reported[propName];
            JsonConvert.SerializeObject(actualProp).Should().Be(JsonConvert.SerializeObject(propValue));
            completeTwin.Properties.Reported.Version.Should().Be(newTwinVersion);
        }

        private async Task Twin_DeviceSetsReportedPropertyAfterOpenCloseOpenAsync(IotHubClientTransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            // Close and re-open the client under test.
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            // The client should still be able to send reported properties even though it was re-opened.
            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesAfterOpenCloseOpenAsync<T>(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubDeviceClient, string, object, Task<Task>> setTwinPropertyUpdateCallbackAsync, T propValue)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            // Close and re-open the client under test.
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            Task updateReceivedTask = await setTwinPropertyUpdateCallbackAsync(deviceClient, propName, propValue).ConfigureAwait(false);

            // The client should still be able to receive desired properties even though it was re-opened.
            await Task.WhenAll(
                RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue),
                updateReceivedTask).ConfigureAwait(false);

            // Validate the updated twin from the device-client
            // Validate the updated twin from the device-client
            TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync().ConfigureAwait(false);
            bool propertyFound = deviceTwin.Desired.TryGetValue(propName, out T actual);
            propertyFound.Should().BeTrue();
            // We don't support nested deserialization yet, so we'll need to serialize the response and compare them.
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            ClientTwin completeTwin = await _serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);
            dynamic actualProp = completeTwin.Properties.Desired[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actualProp), JsonConvert.SerializeObject(propValue));

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(null).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        public static async Task<Task> SetTwinPropertyUpdateCallbackHandlerAsync<T>(IotHubDeviceClient deviceClient, string expectedPropName, T expectedPropValue)
        {
            var propertyUpdateReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetDesiredPropertyUpdateCallbackAsync(
                    (patch) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DesiredProperty: {patch}.");

                        try
                        {
                            bool containsProperty = patch.TryGetValue(expectedPropName, out T propertyValue);
                            containsProperty.Should().BeTrue($"Expecting property update patch received to be {expectedPropName} but was: {patch.GetSerializedString()}");
                            // We don't support nested deserialization yet, so we'll need to serialize the response and compare them.
                            JsonConvert.SerializeObject(propertyValue).Should().Be(JsonConvert.SerializeObject(expectedPropValue), "The property value should match what was set by service");
                        }
                        catch (Exception e)
                        {
                            propertyUpdateReceived.TrySetException(e);
                        }
                        finally
                        {
                            propertyUpdateReceived.TrySetResult(true);
                        }

                        return Task.FromResult<bool>(true);
                    })
                .ConfigureAwait(false);

            return propertyUpdateReceived.Task;
        }

        public static async Task RegistryManagerUpdateDesiredPropertyAsync(string deviceId, string propName, object propValue)
        {
            var twinPatch = new ClientTwin();
            twinPatch.Properties.Desired[propName] = propValue;

            await _serviceClient.Twins.UpdateAsync(deviceId, twinPatch).ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(IotHubClientTransportSettings transportSettings, object propValue)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            // Set a callback
            await deviceClient.
                SetDesiredPropertyUpdateCallbackAsync(
                    (patch) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DesiredProperty: {patch}.");

                        // After unsubscribing it should never reach here
                        Assert.IsNull(patch);

                        return Task.FromResult<bool>(true);
                    })
                .ConfigureAwait(false);

            // Unsubscribe
            await deviceClient
                .SetDesiredPropertyUpdateCallbackAsync(null)
                .ConfigureAwait(false);

            await RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue)
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync<T>(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubDeviceClient, string, object, Task<Task>> setTwinPropertyUpdateCallbackAsync,
            T propValue)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            Task updateReceivedTask = await setTwinPropertyUpdateCallbackAsync(deviceClient, propName, propValue).ConfigureAwait(false);

            await Task.WhenAll(
                RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue),
                updateReceivedTask).ConfigureAwait(false);

            // Validate the updated twin from the device-client
            TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync().ConfigureAwait(false);
            bool propertyFound = deviceTwin.Desired.TryGetValue(propName, out T actual);
            propertyFound.Should().BeTrue();
            // We don't support nested deserialization yet, so we'll need to serialize the response and compare them.
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            ClientTwin completeTwin = await _serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);
            object actualProp = completeTwin.Properties.Desired[propName];
            JsonConvert.SerializeObject(actualProp).Should().Be(JsonConvert.SerializeObject(propValue));

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(null).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(IotHubClientTransportSettings transportSettings)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            var twinPatch = new ClientTwin();
            twinPatch.Properties.Desired[propName] = propValue;
            await _serviceClient.Twins.UpdateAsync(testDevice.Id, twinPatch).ConfigureAwait(false);

            await deviceClient.OpenAsync().ConfigureAwait(false);
            Client.TwinProperties deviceTwin = await deviceClient.GetTwinPropertiesAsync().ConfigureAwait(false);
            bool propertyFound = deviceTwin.Desired.TryGetValue(propName, out string actual);
            propertyFound.Should().BeTrue();
            actual.Should().Be(propValue);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(IotHubClientTransportSettings transportSettings)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            var patch = new ReportedProperties();
            patch[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(patch).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);

            ClientTwin serviceTwin = await _serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);
            Assert.AreEqual<string>(serviceTwin.Properties.Reported[propName].ToString(), propValue);

            VerboseTestLogger.WriteLine("verified " + serviceTwin.Properties.Reported[propName].ToString() + "=" + propValue);
        }

        private async Task Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(IotHubClientTransportSettings transportSettings)
        {
            string propName1 = Guid.NewGuid().ToString();
            string propName2 = Guid.NewGuid().ToString();
            string propEmptyValue = "{}";

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            await deviceClient
                .UpdateReportedPropertiesAsync(
                    new ReportedProperties
                    {
                        [propName1] = null
                    })
                .ConfigureAwait(false);
            ClientTwin serviceTwin = await _serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);
            Assert.IsFalse(serviceTwin.Properties.Reported.Contains(propName1));

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
            serviceTwin = await _serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);
            Assert.IsTrue(serviceTwin.Properties.Reported.Contains(propName1));
            string value1 = serviceTwin.Properties.Reported[propName1].ToString();

            Assert.AreEqual(value1, propEmptyValue);

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
            serviceTwin = await _serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);
            Assert.IsTrue(serviceTwin.Properties.Reported.Contains(propName1));
            string value2 = serviceTwin.Properties.Reported[propName1].ToString();
            Assert.AreEqual(value2, propEmptyValue);
        }

        [DataTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        [TestCategory("LongRunning")]
        public async Task Twin_Client_SetETag_Works(IotHubClientTransportProtocol transportProtocol)
        {
            // arrange

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(transportProtocol));
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            ClientTwin twin = await _serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);
            ETag oldEtag = twin.ETag;

            twin.Properties.Desired[propName] = propValue;

            twin = await _serviceClient.Twins.UpdateAsync(testDevice.Id, twin, true).ConfigureAwait(false);

            twin.ETag = oldEtag;

            // set the 'onlyIfUnchanged' flag to true to check that, with an out of date ETag, the request throws a PreconditionFailedException.
            Func<Task> act = async () => { twin = await _serviceClient.Twins.UpdateAsync(testDevice.Id, twin, true).ConfigureAwait(false); };
            var error = await act.Should().ThrowAsync<IotHubServiceException>("Expected test to throw a precondition failed exception since it updated a twin with an out of date ETag");
            error.And.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.PreconditionFailed);
            error.And.IsTransient.Should().BeFalse();

            // set the 'onlyIfUnchanged' flag to false to check that, even with an out of date ETag, the request performs without exception.
            FluentActions
                .Invoking(async () => { twin = await _serviceClient.Twins.UpdateAsync(testDevice.Id, twin, false).ConfigureAwait(false); })
                .Should()
                .NotThrow<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to false");

            // set the 'onlyIfUnchanged' flag to true to check that, with an up-to-date ETag, the request performs without exception.
            twin.Properties.Desired[propName] = propValue + "1";
            twin.ETag = new ETag("*");
            FluentActions
                .Invoking(async () => { twin = await _serviceClient.Twins.UpdateAsync(testDevice.Id, twin, true).ConfigureAwait(false); })
                .Should()
                .NotThrow<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to true");
        }
    }

    internal class CustomTwinProperty
    {
        // The properties in here need to be public otherwise NewtonSoft.Json cannot serialize and deserialize them properly.
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
