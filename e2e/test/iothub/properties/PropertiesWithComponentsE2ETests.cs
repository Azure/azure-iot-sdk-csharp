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
    public class PropertiesWithComponentsE2ETests : E2EMsTestBase
    {
        public const string ComponentName = "testableComponent";

        private readonly string _devicePrefix = $"E2E_{nameof(PropertiesWithComponentsE2ETests)}_";

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
        public async Task PropertiesWithComponents_DeviceSetsPropertyAndGetsItBack(Client.TransportType transportType)
        {
            await PropertiesWithComponents_DeviceSetsPropertyAndGetsItBackSingleDeviceAsync(transportType, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task PropertiesWithComponents_DeviceSetsPropertyMapAndGetsItBack(Client.TransportType transportType)
        {
            await PropertiesWithComponents_DeviceSetsPropertyAndGetsItBackSingleDeviceAsync(transportType, s_mapOfPropertyValues).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceUnsubscribes(Client.TransportType transportType)
        {
            await PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceUnsubscribes(transportType, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task PropertiesWithComponents_ServiceSetsWritablePropertyMapAndDeviceUnsubscribes(Client.TransportType transportType)
        {
            await PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceUnsubscribes(transportType, s_mapOfPropertyValues).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesEvent(Client.TransportType transportType)
        {
            await PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync(transportType, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task PropertiesWithComponents_ServiceSetsWritablePropertyMapAndDeviceReceivesEvent(Client.TransportType transportType)
        {
            await PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync(transportType, s_mapOfPropertyValues).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesItOnNextGet(Client.TransportType transportType)
        {
            await PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesItOnNextGetAsync(transportType).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task PropertiesWithComponents_DeviceSetsPropertyAndServiceReceivesIt(Client.TransportType transportType)
        {
            await PropertiesWithComponents_DeviceSetsPropertyAndServiceReceivesItAsync(transportType).ConfigureAwait(false);
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
        public async Task PropertiesWithComponents_ClientHandlesRejectionInvalidPropertyName(Client.TransportType transportType)
        {
            await PropertiesWithComponents_ClientHandlesRejectionInvalidPropertyNameAsync(transportType).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesEventAndResponds(Client.TransportType transportType)
        {
            await PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesEventAndRespondsAsync(transportType, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task PropertiesWithComponents_ServiceSetsWritablePropertyMapAndDeviceReceivesEventAndResponds(Client.TransportType transportType)
        {
            await PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesEventAndRespondsAsync(transportType, s_mapOfPropertyValues).ConfigureAwait(false);
        }

        private async Task PropertiesWithComponents_DeviceSetsPropertyAndGetsItBackSingleDeviceAsync<T>(Client.TransportType transport, T propValue)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await PropertiesWithComponents_DeviceSetsPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, propValue, Logger).ConfigureAwait(false);
        }

        public static async Task PropertiesWithComponents_DeviceSetsPropertyAndGetsItBackAsync<T>(DeviceClient deviceClient, string deviceId, T propValue, MsTestLogger logger)
        {
            string propName = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(PropertiesWithComponents_DeviceSetsPropertyAndGetsItBackAsync)}: name={propName}, value={propValue}");

            var props = new ClientPropertyCollection();
            props.AddComponentProperty(ComponentName, propName, propValue);
            await deviceClient.UpdateClientPropertiesAsync(props).ConfigureAwait(false);

            // Validate the updated properties from the device-client
            ClientProperties clientProperties = await deviceClient.GetClientPropertiesAsync().ConfigureAwait(false);
            bool isPropertyPresent = clientProperties.ReportedByClient.TryGetValue(ComponentName, propName, out T propFromCollection);
            isPropertyPresent.Should().BeTrue();
            propFromCollection.Should().BeEquivalentTo<T>(propValue);

            // Validate the updated twin from the service-client
            Twin completeTwin = await s_registryManager.GetTwinAsync(deviceId).ConfigureAwait(false);
            dynamic actualProp = completeTwin.Properties.Reported[ComponentName][propName];

            // The value will be retrieved as a TwinCollection, so we'll serialize the value and then compare.
            string serializedActualPropertyValue = JsonConvert.SerializeObject(actualProp);
            serializedActualPropertyValue.Should().Be(JsonConvert.SerializeObject(propValue));
        }

        public static async Task RegistryManagerUpdateWritablePropertyAsync<T>(string deviceId, string componentName, string propName, T propValue)
        {
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            var twinPatch = new Twin();
            var componentProperties = new TwinCollection
            {
                [ConventionBasedConstants.ComponentIdentifierKey] = ConventionBasedConstants.ComponentIdentifierValue,
                [propName] = propValue
            };
            twinPatch.Properties.Desired[componentName] = componentProperties;

            await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*").ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceUnsubscribes<T>(Client.TransportType transport, T propValue)
        {
            string propName = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

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

            await RegistryManagerUpdateWritablePropertyAsync(testDevice.Id, ComponentName, propName, propValue)
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync<T>(Client.TransportType transport, T propValue)
        {
            using var cts = new CancellationTokenSource(s_maxWaitTimeForCallback);
            string propName = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);
            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);

            await testDeviceCallbackHandler.SetClientPropertyUpdateCallbackHandlerAsync<T>(propName, ComponentName).ConfigureAwait(false);
            testDeviceCallbackHandler.ExpectedClientPropertyValue = propValue;

            await Task
                .WhenAll(
                    RegistryManagerUpdateWritablePropertyAsync(testDevice.Id, ComponentName, propName, propValue),
                    testDeviceCallbackHandler.WaitForClientPropertyUpdateCallbcakAsync(cts.Token))
                .ConfigureAwait(false);

            // Validate the updated properties from the device-client
            ClientProperties clientProperties = await deviceClient.GetClientPropertiesAsync().ConfigureAwait(false);

            clientProperties.WritablePropertyRequests.TryGetWritableClientProperty(ComponentName, propName, out WritableClientProperty writableClientProperty).Should().BeTrue();
            writableClientProperty.TryGetValue(out T propFromCollection).Should().BeTrue();
            propFromCollection.Should().BeEquivalentTo(propValue);

            // Validate the updated twin from the service-client
            Twin completeTwin = await s_registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            dynamic actualProp = completeTwin.Properties.Desired[ComponentName][propName];

            // The value will be retrieved as a TwinCollection, so we'll serialize the value and then compare.
            string serializedActualPropertyValue = JsonConvert.SerializeObject(actualProp);
            serializedActualPropertyValue.Should().Be(JsonConvert.SerializeObject(propValue));

            await deviceClient.SubscribeToWritablePropertyUpdateRequestsAsync(null).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesEventAndRespondsAsync<T>(Client.TransportType transport, T propValue)
        {
            using var cts = new CancellationTokenSource(s_maxWaitTimeForCallback);
            string propName = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesEventAndRespondsAsync)}: name={propName}, value={propValue}");

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            using var writablePropertyCallbackSemaphore = new SemaphoreSlim(0, 1);
            await deviceClient
                .SubscribeToWritablePropertyUpdateRequestsAsync(
                    async (writableProperties) =>
                    {
                        try
                        {
                            writableProperties.TryGetWritableClientProperty(ComponentName, propName, out WritableClientProperty writableClientProperty).Should().BeTrue();
                            writableClientProperty.TryGetValue(out T propertyFromCollection).Should().BeTrue();
                            propertyFromCollection.Should().BeEquivalentTo(propValue);

                            var writablePropertyAcks = new ClientPropertyCollection();
                            writablePropertyAcks.AddWritableClientPropertyAcknowledgement(writableClientProperty.CreateAcknowledgement(CommonClientResponseCodes.OK));

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
                    RegistryManagerUpdateWritablePropertyAsync(testDevice.Id, ComponentName, propName, propValue),
                    writablePropertyCallbackSemaphore.WaitAsync(cts.Token))
                .ConfigureAwait(false);

            // Validate the updated properties from the device-client
            ClientProperties clientProperties = await deviceClient.GetClientPropertiesAsync().ConfigureAwait(false);

            // Validate that the writable property update request was received
            clientProperties.WritablePropertyRequests.TryGetWritableClientProperty(ComponentName, propName, out WritableClientProperty writableClientProperty).Should().BeTrue();
            writableClientProperty.TryGetValue(out T writablePropertyRequest).Should().BeTrue();
            writablePropertyRequest.Should().BeEquivalentTo(propValue);

            // Validate that the writable property update request was acknowledged

            bool isWritablePropertyAckPresent = clientProperties.ReportedByClient.TryGetValue(ComponentName, propName, out IWritablePropertyAcknowledgementPayload writablePropertyAck);
            isWritablePropertyAckPresent.Should().BeTrue();
            // TryGetValue doesn't have nested deserialization, so we'll have to deserialize the retrieved value
            deviceClient.PayloadConvention.PayloadSerializer.ConvertFromJsonObject<T>(writablePropertyAck.Value).Should().BeEquivalentTo(propValue);

            bool isWritablePropertyAckPresentSpecific = clientProperties.ReportedByClient.TryGetValue(ComponentName, propName, out NewtonsoftJsonWritablePropertyAcknowledgementPayload writablePropertyAckNewtonSoft);
            isWritablePropertyAckPresentSpecific.Should().BeTrue();
            // TryGetValue doesn't have nested deserialization, so we'll have to deserialize the retrieved value
            deviceClient.PayloadConvention.PayloadSerializer.ConvertFromJsonObject<T>(writablePropertyAckNewtonSoft.Value).Should().BeEquivalentTo(propValue);

            bool isWritablePropertyAckPresentAsValue = clientProperties.ReportedByClient.TryGetValue(ComponentName, propName, out T writablePropertyAckValue);
            isWritablePropertyAckPresentAsValue.Should().BeTrue();
            writablePropertyAckValue.Should().BeEquivalentTo(propValue);
        }

        private async Task PropertiesWithComponents_ServiceSetsWritablePropertyAndDeviceReceivesItOnNextGetAsync(Client.TransportType transport)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var twinPatch = new Twin();
            var componentProperties = new TwinCollection
            {
                [ConventionBasedConstants.ComponentIdentifierKey] = ConventionBasedConstants.ComponentIdentifierValue,
                [propName] = propValue
            };
            twinPatch.Properties.Desired[ComponentName] = componentProperties;
            await registryManager.UpdateTwinAsync(testDevice.Id, twinPatch, "*").ConfigureAwait(false);

            ClientProperties clientProperties = await deviceClient.GetClientPropertiesAsync().ConfigureAwait(false);

            clientProperties.WritablePropertyRequests.TryGetWritableClientProperty(ComponentName, propName, out WritableClientProperty writableClientProperty).Should().BeTrue();
            writableClientProperty.TryGetValue(out string propFromCollection).Should().BeTrue();
            propFromCollection.Should().Be(propValue);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task PropertiesWithComponents_DeviceSetsPropertyAndServiceReceivesItAsync(Client.TransportType transport)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var patch = new ClientPropertyCollection();
            patch.AddComponentProperty(ComponentName, propName, propValue);
            await deviceClient.UpdateClientPropertiesAsync(patch).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);

            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            dynamic actualProp = serviceTwin.Properties.Reported[ComponentName][propName];

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
            var propertyPatch1 = new ClientPropertyCollection();
            propertyPatch1.AddComponentProperty(
                ComponentName,
                propName1,
                new Dictionary<string, object>
                {
                    [propName2] = propValue
                });
            await deviceClient.UpdateClientPropertiesAsync(propertyPatch1).ConfigureAwait(false);

            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(ComponentName).Should().BeTrue();

            TwinCollection componentPatch1 = serviceTwin.Properties.Reported[ComponentName];
            componentPatch1.Contains(propName1).Should().BeTrue();

            TwinCollection property1Value = componentPatch1[propName1];
            property1Value.Contains(propName2).Should().BeTrue();

            string property2Value = property1Value[propName2];
            property2Value.Should().Be(propValue);

            // Sending a null value for a property will result in service removing the property from the client's twin representation.
            // For the property patch sent here will result in propName2 being removed.
            var propertyPatch2 = new ClientPropertyCollection();
            propertyPatch2.AddComponentProperty(
                ComponentName,
                propName1,
                new Dictionary<string, object>
                {
                    [propName2] = null
                });
            await deviceClient.UpdateClientPropertiesAsync(propertyPatch2).ConfigureAwait(false);

            serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(ComponentName).Should().BeTrue();

            TwinCollection componentPatch2 = serviceTwin.Properties.Reported[ComponentName];
            componentPatch2.Contains(propName1).Should().BeTrue();

            string serializedActualProperty = JsonConvert.SerializeObject(componentPatch2[propName1]);
            serializedActualProperty.Should().Be(propEmptyValue);

            // For the property patch sent here will result in propName1 being removed.
            var propertyPatch3 = new ClientPropertyCollection();
            propertyPatch3.AddComponentProperty(ComponentName, propName1, null);
            await deviceClient.UpdateClientPropertiesAsync(propertyPatch3).ConfigureAwait(false);

            serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(ComponentName).Should().BeTrue();

            // The only elements within the component should be the component identifiers.
            TwinCollection componentPatch3 = serviceTwin.Properties.Reported[ComponentName];
            componentPatch3.Count.Should().Be(1);
            componentPatch3.Contains(ConventionBasedConstants.ComponentIdentifierKey).Should().BeTrue();

            // For the property patch sent here will result in the component being removed.
            var propertyPatch4 = new ClientPropertyCollection();
            propertyPatch4.AddRootProperty(ComponentName, null);
            await deviceClient.UpdateClientPropertiesAsync(propertyPatch4).ConfigureAwait(false);

            serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(ComponentName).Should().BeFalse();
        }

        private async Task PropertiesWithComponents_ClientHandlesRejectionInvalidPropertyNameAsync(Client.TransportType transport)
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
                            [ComponentName] = 
                                new Dictionary<string, object> {
                                    { ConventionBasedConstants.ComponentIdentifierKey, ConventionBasedConstants.ComponentIdentifierValue },
                                    { propName1, 123 },
                                    { propName2, "abcd" }
                                }
                        })
                    .ConfigureAwait(false);
            };
            await func.Should().ThrowAsync<IotHubException>();

            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            serviceTwin.Properties.Reported.Contains(ComponentName).Should().BeFalse();
        }
    }

    internal class CustomClientPropertyWithComponent
    {
        // The properties in here need to be public otherwise NewtonSoft.Json cannot serialize and deserialize them properly.
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
