// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Properties
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class PropertiesE2ETests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"E2E_{nameof(PropertiesE2ETests)}_";

        private static readonly RegistryManager s_registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
        private static readonly TimeSpan s_maxWaitTimeForCallback = TimeSpan.FromSeconds(30);

        private static readonly Dictionary<string, object> s_mapOfPropertyValues = new Dictionary<string, object>
        {
            { "key1", 123 },
            { "key2", "someString" },
            { "key3", true }
        };

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Properties_DeviceSetsPropertyAndGetsItBack(Client.TransportType transportType)
        {
            await Properties_DeviceSetsPropertyAndGetsItBackSingleDeviceAsync(transportType).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Properties_DeviceSetsPropertyMapAndGetsItBack(Client.TransportType transportType)
        {
            await Properties_DeviceSetsPropertyMapAndGetsItBackSingleDeviceAsync(transportType).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Properties_ServiceSetsWritablePropertyAndDeviceUnsubscribes(Client.TransportType transportType)
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceUnsubscribes(transportType, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Properties_ServiceSetsWritablePropertyAndDeviceReceivesEvent(Client.TransportType transportType)
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync(transportType, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Properties_ServiceSetsWritablePropertyMapAndDeviceReceivesEvent(Client.TransportType transportType)
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync(transportType, s_mapOfPropertyValues).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAndResponds(Client.TransportType transportType)
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAndRespondsAsync(transportType, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Properties_ServiceSetsWritablePropertyMapAndDeviceReceivesEventAndResponds(Client.TransportType transportType)
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAndRespondsAsync(transportType, s_mapOfPropertyValues).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Properties_ServiceSetsWritablePropertyAndDeviceReceivesItOnNextGet(Client.TransportType transportType)
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceReceivesItOnNextGetAsync(transportType).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Properties_DeviceSetsPropertyAndServiceReceivesIt(Client.TransportType transportType)
        {
            await Properties_DeviceSetsPropertyAndServiceReceivesItAsync(transportType).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Properties_DeviceSendsNullValueForPropertyResultsServiceRemovingIt(Client.TransportType transportType)
        {
            await Properties_DeviceSendsNullValueForPropertyResultsServiceRemovingItAsync(transportType).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task Properties_ClientHandlesRejectionInvalidPropertyName_Mqtt(Client.TransportType transportType)
        {
            await Properties_ClientHandlesRejectionInvalidPropertyNameAsync(transportType).ConfigureAwait(false);
        }

        private async Task Properties_DeviceSetsPropertyAndGetsItBackSingleDeviceAsync(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await Properties_DeviceSetsPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString(), Logger).ConfigureAwait(false);
        }

        private async Task Properties_DeviceSetsPropertyMapAndGetsItBackSingleDeviceAsync(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await Properties_DeviceSetsPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, s_mapOfPropertyValues, Logger).ConfigureAwait(false);
        }

        public static async Task Properties_DeviceSetsPropertyAndGetsItBackAsync<T>(DeviceClient deviceClient, string deviceId, T propValue, MsTestLogger logger)
        {
            string propName = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(Properties_DeviceSetsPropertyAndGetsItBackAsync)}: name={propName}, value={propValue}");

            var props = new ClientPropertyCollection();
            props.AddRootProperty(propName, propValue);
            await deviceClient.UpdateClientPropertiesAsync(props).ConfigureAwait(false);

            // Validate the updated properties from the device-client
            ClientProperties clientProperties = await deviceClient.GetClientPropertiesAsync().ConfigureAwait(false);
            bool isPropertyPresent = clientProperties.ReportedFromClient.TryGetValue(propName, out T propFromCollection);
            isPropertyPresent.Should().BeTrue();
            propFromCollection.Should().BeEquivalentTo(propValue);

            // Validate the updated twin from the service-client
            Twin completeTwin = await s_registryManager.GetTwinAsync(deviceId).ConfigureAwait(false);
            dynamic actualProp = completeTwin.Properties.Reported[propName];

            // The value will be retrieved as a TwinCollection, so we'll serialize the value and then compare.
            string serializedActualPropertyValue = JsonConvert.SerializeObject(actualProp);
            serializedActualPropertyValue.Should().Be(JsonConvert.SerializeObject(propValue));
        }

        public static async Task RegistryManagerUpdateWritablePropertyAsync<T>(string deviceId, string propName, T propValue)
        {
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;

            await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*").ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task Properties_ServiceSetsWritablePropertyAndDeviceUnsubscribes(Client.TransportType transport, object propValue)
        {
            string propName = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            // Set a callback
            await deviceClient.
                SubscribeToWritablePropertyUpdateRequestsAsync(
                    patch =>
                    {
                        Assert.Fail("After having unsubscribed from receiving client property update notifications " +
                            "this callback should not have been invoked.");

                        return Task.FromResult(true);
                    })
                .ConfigureAwait(false);

            // Unsubscribe
            await deviceClient
                .SubscribeToWritablePropertyUpdateRequestsAsync(null)
                .ConfigureAwait(false);

            await RegistryManagerUpdateWritablePropertyAsync(testDevice.Id, propName, propValue)
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync<T>(Client.TransportType transport, T propValue)
        {
            using var cts = new CancellationTokenSource(s_maxWaitTimeForCallback);
            string propName = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);
            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            await testDeviceCallbackHandler.SetClientPropertyUpdateCallbackHandlerAsync<T>(propName).ConfigureAwait(false);
            testDeviceCallbackHandler.ExpectedClientPropertyValue = propValue;

            await Task
                .WhenAll(
                    RegistryManagerUpdateWritablePropertyAsync(testDevice.Id, propName, propValue),
                    testDeviceCallbackHandler.WaitForClientPropertyUpdateCallbcakAsync(cts.Token))
                .ConfigureAwait(false);

            // Validate the updated properties from the device-client
            ClientProperties clientProperties = await deviceClient.GetClientPropertiesAsync().ConfigureAwait(false);
            bool isPropertyPresent = clientProperties.WritablePropertyRequests.TryGetValue(propName, out T propValueFromCollection);
            isPropertyPresent.Should().BeTrue();
            propValueFromCollection.Should().BeEquivalentTo(propValue);

            // Validate the updated twin from the service-client
            Twin completeTwin = await s_registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            dynamic actualProp = completeTwin.Properties.Desired[propName];

            // The value will be retrieved as a TwinCollection, so we'll serialize the value and then compare.
            string serializedActualPropertyValue = JsonConvert.SerializeObject(actualProp);
            serializedActualPropertyValue.Should().Be(JsonConvert.SerializeObject(propValue));

            await deviceClient.SubscribeToWritablePropertyUpdateRequestsAsync(null).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAndRespondsAsync<T>(Client.TransportType transport, T propValue)
        {
            using var cts = new CancellationTokenSource(s_maxWaitTimeForCallback);
            string propName = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            using var writablePropertyCallbackSemaphore = new SemaphoreSlim(0, 1);
            await deviceClient
                .SubscribeToWritablePropertyUpdateRequestsAsync(
                    async (writableProperties) =>
                    {
                        try
                        {
                            bool isPropertyPresent = writableProperties.TryGetValue(propName, out T propertyFromCollection);

                            isPropertyPresent.Should().BeTrue();
                            propertyFromCollection.Should().BeEquivalentTo(propValue);

                            var writablePropertyAcks = new ClientPropertyCollection();
                            foreach (KeyValuePair<string, object> writableProperty in writableProperties)
                            {
                                if (writableProperty.Value is WritableClientProperty writableClientProperty)
                                {
                                    writablePropertyAcks.Add(writableProperty.Key, writableClientProperty.AcknowledgeWith(CommonClientResponseCodes.OK));
                                }
                            }

                            await deviceClient.UpdateClientPropertiesAsync(writablePropertyAcks).ConfigureAwait(false);
                        }
                        finally
                        {
                            writablePropertyCallbackSemaphore.Release();
                        }
                    },
                    cts.Token)
                .ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            await Task
                .WhenAll(
                    RegistryManagerUpdateWritablePropertyAsync(testDevice.Id, propName, propValue),
                    writablePropertyCallbackSemaphore.WaitAsync(cts.Token))
                .ConfigureAwait(false);

            // Validate the updated properties from the device-client
            ClientProperties clientProperties = await deviceClient.GetClientPropertiesAsync().ConfigureAwait(false);

            // Validate that the writable property update request was received
            bool isWritablePropertyRequestPresent = clientProperties.WritablePropertyRequests.TryGetValue(propName, out T writablePropertyRequest);
            isWritablePropertyRequestPresent.Should().BeTrue();
            writablePropertyRequest.Should().BeEquivalentTo(propValue);

            // Validate that the writable property update request was acknowledged

            bool isWritablePropertyAckPresent = clientProperties.ReportedFromClient.TryGetValue(propName, out IWritablePropertyResponse writablePropertyAck);
            isWritablePropertyAckPresent.Should().BeTrue();
            // TryGetValue doesn't have nested deserialization, so we'll have to deserialize the retrieved value
            deviceClient.PayloadConvention.PayloadSerializer.ConvertFromJsonObject<T>(writablePropertyAck.Value).Should().BeEquivalentTo(propValue);

            bool isWritablePropertyAckPresentSpecific = clientProperties.ReportedFromClient.TryGetValue(propName, out NewtonsoftJsonWritablePropertyResponse writablePropertyAckNewtonSoft);
            isWritablePropertyAckPresentSpecific.Should().BeTrue();
            // TryGetValue doesn't have nested deserialization, so we'll have to deserialize the retrieved value
            deviceClient.PayloadConvention.PayloadSerializer.ConvertFromJsonObject<T>(writablePropertyAckNewtonSoft.Value).Should().BeEquivalentTo(propValue);

            bool isWritablePropertyAckPresentAsValue = clientProperties.ReportedFromClient.TryGetValue(propName, out T writablePropertyAckValue);
            isWritablePropertyAckPresentAsValue.Should().BeTrue();
            writablePropertyAckValue.Should().BeEquivalentTo(propValue);
        }

        private async Task Properties_ServiceSetsWritablePropertyAndDeviceReceivesItOnNextGetAsync(Client.TransportType transport)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(testDevice.Id, twinPatch, "*").ConfigureAwait(false);

            ClientProperties clientProperties = await deviceClient.GetClientPropertiesAsync().ConfigureAwait(false);
            bool isPropertyPresent = clientProperties.WritablePropertyRequests.TryGetValue(propName, out string propFromCollection);
            isPropertyPresent.Should().BeTrue();
            propFromCollection.Should().Be(propValue);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task Properties_DeviceSetsPropertyAndServiceReceivesItAsync(Client.TransportType transport)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var patch = new ClientPropertyCollection();
            patch.AddRootProperty(propName, propValue);
            await deviceClient.UpdateClientPropertiesAsync(patch).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);

            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            dynamic actualProp = serviceTwin.Properties.Reported[propName];

            // The value will be retrieved as a TwinCollection, so we'll serialize the value and then compare.
            string serializedActualPropertyValue = JsonConvert.SerializeObject(actualProp);
            serializedActualPropertyValue.Should().Be(JsonConvert.SerializeObject(propValue));
        }

        private async Task Properties_DeviceSendsNullValueForPropertyResultsServiceRemovingItAsync(Client.TransportType transport)
        {
            string propName1 = Guid.NewGuid().ToString();
            string propName2 = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();
            string propEmptyValue = "{}";

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            // First send a property patch with valid values for both prop1 and prop2.
            await deviceClient
                .UpdateClientPropertiesAsync(
                    new ClientPropertyCollection
                    {
                        [propName1] = new Dictionary<string, object>
                        {
                            [propName2] = propValue
                        }
                    })
                .ConfigureAwait(false);
            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(propName1).Should().BeTrue();

            TwinCollection prop1Value = serviceTwin.Properties.Reported[propName1];
            prop1Value.Contains(propName2).Should().BeTrue();

            string prop2Value = prop1Value[propName2];
            prop2Value.Should().Be(propValue);

            // Sending a null value for a property will result in service removing the property from the client's twin representation.
            // For the property patch sent here will result in propName2 being removed.
            await deviceClient
                .UpdateClientPropertiesAsync(
                    new ClientPropertyCollection
                    {
                        [propName1] = new Dictionary<string, object>
                        {
                            [propName2] = null
                        }
                    })
                .ConfigureAwait(false);
            serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(propName1).Should().BeTrue();

            string serializedActualProperty = JsonConvert.SerializeObject(serviceTwin.Properties.Reported[propName1]);
            serializedActualProperty.Should().Be(propEmptyValue);

            // For the property patch sent here will result in propName1 being removed.
            await deviceClient
                .UpdateClientPropertiesAsync(
                    new ClientPropertyCollection
                    {
                        [propName1] = null
                    })
                .ConfigureAwait(false);
            serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(propName1).Should().BeFalse();
        }

        private async Task Properties_ClientHandlesRejectionInvalidPropertyNameAsync(Client.TransportType transport)
        {
            string propName1 = "$" + Guid.NewGuid().ToString();
            string propName2 = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            Func<Task> func = async () =>
            {
                await deviceClient
                    .UpdateClientPropertiesAsync(
                        new ClientPropertyCollection
                        {
                            [propName1] = 123,
                            [propName2] = "abcd"
                        })
                    .ConfigureAwait(false);
            };
            await func.Should().ThrowAsync<IotHubException>();

            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(propName1).Should().BeFalse();
            serviceTwin.Properties.Reported.Contains(propName2).Should().BeFalse();
        }
    }

    internal class CustomClientProperty
    {
        // The properties in here need to be public otherwise NewtonSoft.Json cannot serialize and deserialize them properly.
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
