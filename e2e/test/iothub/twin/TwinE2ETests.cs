﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.E2ETests.Twins
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class TwinE2ETests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"E2E_{nameof(TwinE2ETests)}_";
        private static TestLogger s_log = TestLogger.GetInstance();

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType.Mqtt_Tcp_Only, SetTwinPropertyUpdateCallbackHandlerAsync, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType.Mqtt_WebSocket_Only, SetTwinPropertyUpdateCallbackHandlerAsync, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType.Amqp_Tcp_Only, SetTwinPropertyUpdateCallbackHandlerAsync, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType.Amqp_WebSocket_Only, SetTwinPropertyUpdateCallbackHandlerAsync, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType.Mqtt_Tcp_Only, SetTwinPropertyUpdateCallbackHandlerAsync, JArray.Parse("[1, \"someString\", false]")).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType.Mqtt_WebSocket_Only, SetTwinPropertyUpdateCallbackHandlerAsync, JArray.Parse("[1, \"someString\", false]")).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType.Amqp_Tcp_Only, SetTwinPropertyUpdateCallbackHandlerAsync, JArray.Parse("[1, \"someString\", false]")).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType.Amqp_WebSocket_Only, SetTwinPropertyUpdateCallbackHandlerAsync, JArray.Parse("[1, \"someString\", false]")).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType.Mqtt_Tcp_Only, SetTwinPropertyUpdateCallbackObsoleteHandlerAsync, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType.Mqtt_WebSocket_Only, SetTwinPropertyUpdateCallbackObsoleteHandlerAsync, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType.Amqp_Tcp_Only, SetTwinPropertyUpdateCallbackObsoleteHandlerAsync, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType.Amqp_WebSocket_Only, SetTwinPropertyUpdateCallbackObsoleteHandlerAsync, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Mqtt()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_MqttWs()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Amqp()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_AmqpWs()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_Mqtt()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyNameAsync(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_MqttWs()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyNameAsync(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_Amqp()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyNameAsync(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_AmqpWs()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyNameAsync(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        [TestCategory("LongRunning")]
        public async Task Twin_ClientSetsReportedPropertyWithoutDesiredPropertyCallback(Client.TransportType transportType)
        {
            // arrange

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transportType);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, Guid.NewGuid().ToString()).ConfigureAwait(false);

            int connectionStatusChangeCount = 0;
            ConnectionStatusChangesHandler connectionStatusChangesHandler = (ConnectionStatus status, ConnectionStatusChangeReason reason) =>
            {
                Interlocked.Increment(ref connectionStatusChangeCount);
            };

            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            s_log.Trace($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            // act
            await RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            // assert
            Assert.AreEqual(0, connectionStatusChangeCount, "AMQP should not be disconnected.");
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, JArray.Parse("[1, 2, 3]")).ConfigureAwait(false);
        }

        public static async Task Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(DeviceClient deviceClient, object propValue)
        {
            var propName = Guid.NewGuid().ToString();

            s_log.Trace($"{nameof(Twin_DeviceSetsReportedPropertyAndGetsItBackAsync)}: name={propName}, value={propValue}");

            var props = new TwinCollection();
            props[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

            Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            var actual = deviceTwin.Properties.Reported[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actual), JsonConvert.SerializeObject(propValue));
        }

        public static async Task<Task> SetTwinPropertyUpdateCallbackHandlerAsync(DeviceClient deviceClient, string expectedPropName, object expectedPropValue)
        {
            var propertyUpdateReceived = new TaskCompletionSource<bool>();
            string userContext = "myContext";

            await deviceClient
                .SetDesiredPropertyUpdateCallbackAsync(
                    (patch, context) =>
                    {
                        s_log.Trace($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DesiredProperty: {patch}, {context}");

                        try
                        {
                            Assert.AreEqual(JsonConvert.SerializeObject(expectedPropValue), JsonConvert.SerializeObject(patch[expectedPropName]));
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

        private async Task<Task> SetTwinPropertyUpdateCallbackObsoleteHandlerAsync(DeviceClient deviceClient, string expectedPropName, object expectedPropValue)
        {
#pragma warning disable CS0618

            string userContext = "myContext";
            var propertyUpdateReceived = new TaskCompletionSource<bool>();

            await deviceClient
                .SetDesiredPropertyUpdateCallback(
                    (patch, context) =>
                    {
                        s_log.Trace($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DesiredProperty: {patch}, {context}");

                        try
                        {
                            Assert.AreEqual(expectedPropValue, patch[expectedPropName].ToString());
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
#pragma warning restore CS0618

            return propertyUpdateReceived.Task;
        }

        public static async Task RegistryManagerUpdateDesiredPropertyAsync(string deviceId, string propName, object propValue)
        {
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;

            await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*").ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType transport, Func<DeviceClient, string, object, Task<Task>> setTwinPropertyUpdateCallbackAsync, object propValue)
        {
            var propName = Guid.NewGuid().ToString();

            s_log.Trace($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            Task updateReceivedTask = await setTwinPropertyUpdateCallbackAsync(deviceClient, propName, propValue).ConfigureAwait(false);

            await Task.WhenAll(
                RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue),
                updateReceivedTask).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(testDevice.Id, twinPatch, "*").ConfigureAwait(false);

            Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            Assert.AreEqual<string>(deviceTwin.Properties.Desired[propName].ToString(), propValue);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var patch = new TwinCollection();
            patch[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(patch).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);

            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            Assert.AreEqual<string>(serviceTwin.Properties.Reported[propName].ToString(), propValue);

            s_log.Trace("verified " + serviceTwin.Properties.Reported[propName].ToString() + "=" + propValue);
        }

        private async Task Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(Client.TransportType transport)
        {
            var propName1 = Guid.NewGuid().ToString();
            var propName2 = Guid.NewGuid().ToString();
            var propEmptyValue = "{}";

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await deviceClient
                .UpdateReportedPropertiesAsync(
                    new TwinCollection
                    {
                        [propName1] = null
                    })
                .ConfigureAwait(false);
            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            Assert.IsFalse(serviceTwin.Properties.Reported.Contains(propName1));

            await deviceClient
                .UpdateReportedPropertiesAsync(
                    new TwinCollection
                    {
                        [propName1] = new TwinCollection
                        {
                            [propName2] = null
                        }
                    })
                .ConfigureAwait(false);
            serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            Assert.IsTrue(serviceTwin.Properties.Reported.Contains(propName1));
            string value1 = serviceTwin.Properties.Reported[propName1].ToString();

            Assert.AreEqual(value1, propEmptyValue);

            await deviceClient
                .UpdateReportedPropertiesAsync(
                    new TwinCollection
                    {
                        [propName1] = new TwinCollection
                        {
                            [propName2] = null
                        }
                    })
                .ConfigureAwait(false);
            serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            Assert.IsTrue(serviceTwin.Properties.Reported.Contains(propName1));
            string value2 = serviceTwin.Properties.Reported[propName1].ToString();
            Assert.AreEqual(value2, propEmptyValue);
        }

        private async Task Twin_ClientHandlesRejectionInvalidPropertyNameAsync(Client.TransportType transport)
        {
            var propName1 = "$" + Guid.NewGuid().ToString();
            var propName2 = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var exceptionThrown = false;
            try
            {
                await deviceClient
                    .UpdateReportedPropertiesAsync(
                        new TwinCollection
                        {
                            [propName1] = 123,
                            [propName2] = "abcd"
                        })
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown, "Exception was expected, but not thrown.");

            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            Assert.IsFalse(serviceTwin.Properties.Reported.Contains(propName1));
        }
    }
}
