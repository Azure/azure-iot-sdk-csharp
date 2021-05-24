// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        private static readonly RegistryManager _registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

        private static readonly List<object> s_listOfPropertyValues = new List<object>
        {
            1,
            "someString",
            false,
            new CustomClientProperty
            {
                Id = 123,
                Name = "someName"
            }
        };

        [LoggedTestMethod]
        public async Task Properties_DeviceSetsPropertyAndGetsItBack_Mqtt()
        {
            await Properties_DeviceSetsPropertyAndGetsItBackSingleDeviceAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_DeviceSetsPropertyAndGetsItBack_MqttWs()
        {
            await Properties_DeviceSetsPropertyAndGetsItBackSingleDeviceAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_DeviceSetsPropertyArrayAndGetsItBack_Mqtt()
        {
            await Properties_DeviceSetsPropertyArrayAndGetsItBackSingleDeviceAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_DeviceSetsPropertyArrayAndGetsItBack_MqttWs()
        {
            await Properties_DeviceSetsPropertyArrayAndGetsItBackSingleDeviceAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_ServiceSetsWritablePropertyAndDeviceUnsubscribes_Mqtt()
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceUnsubscribes(
                    Client.TransportType.Mqtt_Tcp_Only,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_ServiceSetsWritablePropertyAndDeviceUnsubscribes_MqttWs()
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceUnsubscribes(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_ServiceSetsWritablePropertyAndDeviceReceivesEvent_Mqtt()
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    SetClientPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_ServiceSetsWritablePropertyAndDeviceReceivesEvent_MqttWs()
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    SetClientPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_ServiceSetsWritablePropertyArrayAndDeviceReceivesEvent_Mqtt()
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    SetClientPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_ServiceSetsWritablePropertyArrayAndDeviceReceivesEvent_MqttWs()
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    SetClientPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_ServiceSetsWritablePropertyAndDeviceReceivesItOnNextGet_Mqtt()
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceReceivesItOnNextGetAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_ServiceSetsWritablePropertyAndDeviceReceivesItOnNextGet_MqttWs()
        {
            await Properties_ServiceSetsWritablePropertyAndDeviceReceivesItOnNextGetAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_DeviceSetsPropertyAndServiceReceivesIt_Mqtt()
        {
            await Properties_DeviceSetsPropertyAndServiceReceivesItAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_DeviceSetsPropertyAndServiceReceivesIt_MqttWs()
        {
            await Properties_DeviceSetsPropertyAndServiceReceivesItAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_ServiceDoesNotCreateNullPropertyInCollection_Mqtt()
        {
            await Properties_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_ServiceDoesNotCreateNullPropertyInCollection_MqttWs()
        {
            await Properties_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_ClientHandlesRejectionInvalidPropertyName_Mqtt()
        {
            await Properties_ClientHandlesRejectionInvalidPropertyNameAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Properties_ClientHandlesRejectionInvalidPropertyName_MqttWs()
        {
            await Properties_ClientHandlesRejectionInvalidPropertyNameAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        private async Task Properties_DeviceSetsPropertyAndGetsItBackSingleDeviceAsync(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await Properties_DeviceSetsPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString(), Logger).ConfigureAwait(false);
        }

        private async Task Properties_DeviceSetsPropertyArrayAndGetsItBackSingleDeviceAsync(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await Properties_DeviceSetsPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, s_listOfPropertyValues, Logger).ConfigureAwait(false);
        }

        public static async Task Properties_DeviceSetsPropertyAndGetsItBackAsync<T>(DeviceClient deviceClient, string deviceId, T propValue, MsTestLogger logger)
        {
            var propName = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(Properties_DeviceSetsPropertyAndGetsItBackAsync)}: name={propName}, value={propValue}");

            var props = new ClientPropertyCollection();
            props[propName] = propValue;
            await deviceClient.UpdateClientPropertiesAsync(props).ConfigureAwait(false);

            // Validate the updated twin from the device-client
            ClientProperties deviceTwin = await deviceClient.GetClientPropertiesAsync().ConfigureAwait(false);
            if (deviceTwin.TryGetValue<T>(propName, out var propFromCollection))
            {
                Assert.AreEqual(JsonConvert.SerializeObject(propFromCollection), JsonConvert.SerializeObject(propValue));
            }
            else
            {
                Assert.Fail($"The property {propName} was not found in the collection");
            }

            // Validate the updated twin from the service-client
            Twin completeTwin = await _registryManager.GetTwinAsync(deviceId).ConfigureAwait(false);
            var actualProp = completeTwin.Properties.Reported[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actualProp), JsonConvert.SerializeObject(propValue));
        }

        public static async Task<Task> SetClientPropertyUpdateCallbackHandlerAsync<T>(DeviceClient deviceClient, string expectedPropName, T expectedPropValue, MsTestLogger logger)
        {
            var propertyUpdateReceived = new TaskCompletionSource<bool>();
            string userContext = "myContext";

            await deviceClient
                .SubscribeToWritablePropertiesEventAsync(
                    (patch, context) =>
                    {
                        logger.Trace($"{nameof(SetClientPropertyUpdateCallbackHandlerAsync)}: WritableProperty: {patch}, {context}");

                        try
                        {
                            if (patch.TryGetValue<T>(expectedPropName, out var propertyFromCollection))
                            {
                                Assert.AreEqual(JsonConvert.SerializeObject(expectedPropValue), JsonConvert.SerializeObject(propertyFromCollection));
                            } else
                            {
                                Assert.Fail("Property was not found in the collection.");
                            }
                            Assert.AreEqual(userContext, context, "Context");
                        }
                        catch (Exception e)
                        {
                            propertyUpdateReceived.SetException(e);
                        }
                        finally
                        {
                            propertyUpdateReceived.SetResult(true);
                        }

                        return Task.FromResult<bool>(true);
                    },
                    userContext)
                .ConfigureAwait(false);

            return propertyUpdateReceived.Task;
        }

        public static async Task RegistryManagerUpdateWritablePropertyAsync<T>(string deviceId, string propName, T propValue)
        {
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            var twinPatch = new Twin();
            if (propValue is List<object>)
            {
                twinPatch.Properties.Desired[propName] = (Newtonsoft.Json.Linq.JToken)(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(propValue)));
            }
            else
            {
                twinPatch.Properties.Desired[propName] = propValue;
            }

            await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*").ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task Properties_ServiceSetsWritablePropertyAndDeviceUnsubscribes(Client.TransportType transport, object propValue)
        {
            var propName = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            // Set a callback
            await deviceClient.
                SubscribeToWritablePropertiesEventAsync(
                    (patch, context) =>
                    {
                        Logger.Trace($"{nameof(SetClientPropertyUpdateCallbackHandlerAsync)}: WritableProperty: {patch}, {context}");

                        // After unsubscribing it should never reach here
                        Assert.IsNull(patch);

                        return Task.FromResult<bool>(true);
                    },
                    null)
                .ConfigureAwait(false);

            // Unsubscribe
            await deviceClient
                .SubscribeToWritablePropertiesEventAsync(null, null)
                .ConfigureAwait(false);

            await RegistryManagerUpdateWritablePropertyAsync(testDevice.Id, propName, propValue)
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync<T>(Client.TransportType transport, Func<DeviceClient, string, object, MsTestLogger, Task<Task>> setTwinPropertyUpdateCallbackAsync, T propValue)
        {
            var propName = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(Properties_ServiceSetsWritablePropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            Task updateReceivedTask = await setTwinPropertyUpdateCallbackAsync(deviceClient, propName, propValue, Logger).ConfigureAwait(false);

            await Task.WhenAll(
                RegistryManagerUpdateWritablePropertyAsync(testDevice.Id, propName, propValue),
                updateReceivedTask).ConfigureAwait(false);

            // Validate the updated twin from the device-client
            ClientProperties deviceTwin = await deviceClient.GetClientPropertiesAsync().ConfigureAwait(false);
            if (deviceTwin.Writable.TryGetValue<T>(propName, out var propFromCollection))
            {
                Assert.AreEqual(JsonConvert.SerializeObject(propFromCollection), JsonConvert.SerializeObject(propValue));
            } else
            {
                Assert.Fail($"The property {propName} was not found in the Writable collection");
            }
            
            // Validate the updated twin from the service-client
            Twin completeTwin = await _registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            var actualProp = completeTwin.Properties.Desired[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actualProp), JsonConvert.SerializeObject(propValue));

            await deviceClient.SubscribeToWritablePropertiesEventAsync(null, null).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task Properties_ServiceSetsWritablePropertyAndDeviceReceivesItOnNextGetAsync(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(testDevice.Id, twinPatch, "*").ConfigureAwait(false);

            ClientProperties deviceTwin = await deviceClient.GetClientPropertiesAsync().ConfigureAwait(false);
            if (deviceTwin.Writable.TryGetValue(propName, out string propFromCollection))
            {
                Assert.AreEqual<string>(propFromCollection, propValue);
            } else
            {
                Assert.Fail("Property not found in ClientProperties");
            }
            await deviceClient.CloseAsync().ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task Properties_DeviceSetsPropertyAndServiceReceivesItAsync(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var patch = new ClientPropertyCollection();
            patch[propName] = propValue;
            await deviceClient.UpdateClientPropertiesAsync(patch).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);

            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            Assert.AreEqual<string>(serviceTwin.Properties.Reported[propName].ToString(), propValue);

            Logger.Trace("verified " + serviceTwin.Properties.Reported[propName].ToString() + "=" + propValue);
        }

        private async Task Properties_ServiceDoesNotCreateNullPropertyInCollectionAsync(Client.TransportType transport)
        {
            var propName1 = Guid.NewGuid().ToString();
            var propName2 = Guid.NewGuid().ToString();
            var propEmptyValue = "{}";

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await deviceClient
                .UpdateClientPropertiesAsync(
                    new ClientPropertyCollection
                    {
                        [propName1] = null
                    })
                .ConfigureAwait(false);
            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            Assert.IsFalse(serviceTwin.Properties.Reported.Contains(propName1));

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
            Assert.IsTrue(serviceTwin.Properties.Reported.Contains(propName1));
            string value1 = serviceTwin.Properties.Reported[propName1].ToString();

            Assert.AreEqual(value1, propEmptyValue);
        }

        private async Task Properties_ClientHandlesRejectionInvalidPropertyNameAsync(Client.TransportType transport)
        {
            var propName1 = "$" + Guid.NewGuid().ToString();
            var propName2 = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var exceptionThrown = false;
            try
            {
                await deviceClient
                    .UpdateClientPropertiesAsync(
                        new ClientPropertyCollection
                        {
                            [propName1] = 123,
                            [propName2] = "abcd"
                        })
                    .ConfigureAwait(false);
            }
            catch (IotHubException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown, "IotHubException was expected for updating reported property with an invalid property name, but was not thrown.");

            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            Assert.IsFalse(serviceTwin.Properties.Reported.Contains(propName1));
        }
    }

    internal class CustomClientProperty
    {
        // The properties in here need to be public otherwise NewtonSoft.Json cannot serialize and deserialize them properly.
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
