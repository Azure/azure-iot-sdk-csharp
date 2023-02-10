// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
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
        private readonly string _devicePrefix = $"{nameof(TwinE2ETests)}_";

        private static readonly RegistryManager _registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

        private static readonly List<object> s_listOfPropertyValues = new()
        {
            1,
            "someString",
            false,
            new CustomTwinProperty
            {
                Id = 123,
                Name = "someName"
            }
        };

        private static readonly TestDateTime s_dateTimeProperty = new()
        {
            Iso8601String = new DateTimeOffset(638107582284599400, TimeSpan.FromHours(1)).ToString("o", CultureInfo.InvariantCulture)
        };

        // ISO 8601 date time string with trailing zeros in the microseconds portion. This is to verify the Newtonsoft.Json known issue is worked around in the SDK.
        // See https://github.com/JamesNK/Newtonsoft.Json/issues/1511 for more details about the known issue.
        private static readonly string s_iso8601DateTimeString = "2023-01-31T10:37:08.4599400+01:00";

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    Client.TransportType.Amqp_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    Client.TransportType.Amqp_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    Client.TransportType.Amqp_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    Client.TransportType.Amqp_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    Client.TransportType.Mqtt_Tcp_Only,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    Client.TransportType.Amqp_Tcp_Only,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    Client.TransportType.Amqp_WebSocket_Only,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    SetTwinPropertyUpdateCallbackObsoleteHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    SetTwinPropertyUpdateCallbackObsoleteHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    SetTwinPropertyUpdateCallbackObsoleteHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    SetTwinPropertyUpdateCallbackObsoleteHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    Client.TransportType.Amqp_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    Client.TransportType.Amqp_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_DateTimeProperties_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetDateTimePropertiesAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_DateTimeProperties_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetDateTimePropertiesAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_DateTimeProperties_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetDateTimePropertiesAsync(
                    Client.TransportType.Amqp_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_DateTimeProperties_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetDateTimePropertiesAsync(
                    Client.TransportType.Amqp_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    Client.TransportType.Amqp_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    Client.TransportType.Amqp_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Mqtt()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_MqttWs()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Amqp()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    Client.TransportType.Amqp_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_AmqpWs()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    Client.TransportType.Amqp_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_Mqtt()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyNameAsync(
                    Client.TransportType.Mqtt_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_MqttWs()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyNameAsync(
                    Client.TransportType.Mqtt_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_Amqp()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyNameAsync(
                    Client.TransportType.Amqp_Tcp_Only)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_AmqpWs()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyNameAsync(
                    Client.TransportType.Amqp_WebSocket_Only)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        [TestCategory("LongRunning")]
        public async Task Twin_ClientSetsReportedPropertyWithoutDesiredPropertyCallback(Client.TransportType transportType)
        {
            // arrange

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transportType);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString()).ConfigureAwait(false);

            int connectionStatusChangeCount = 0;
            ConnectionStatusChangesHandler connectionStatusChangesHandler = (ConnectionStatus status, ConnectionStatusChangeReason reason) =>
            {
                Interlocked.Increment(ref connectionStatusChangeCount);
            };

            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            // act
            await RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            // assert
            Assert.AreEqual(0, connectionStatusChangeCount, "AMQP should not be disconnected.");
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString()).ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(Client.TransportType transport)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, s_listOfPropertyValues).ConfigureAwait(false);
        }

        public static async Task Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(DeviceClient deviceClient, string deviceId, object propValue)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_DeviceSetsReportedPropertyAndGetsItBackAsync)}: name={propName}, value={propValue}");

            var props = new TwinCollection();
            props[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

            // Validate the updated twin from the device-client
            Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            dynamic actual = deviceTwin.Properties.Reported[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actual), JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            Twin completeTwin = await _registryManager.GetTwinAsync(deviceId).ConfigureAwait(false);
            dynamic actualProp = completeTwin.Properties.Reported[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actualProp), JsonConvert.SerializeObject(propValue));
        }

        public static async Task<Task> SetTwinPropertyUpdateCallbackHandlerAsync(DeviceClient deviceClient, string expectedPropName, object expectedPropValue)
        {
            var propertyUpdateReceived = new TaskCompletionSource<bool>();
            string userContext = "myContext";

            await deviceClient
                .SetDesiredPropertyUpdateCallbackAsync(
                    (patch, context) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DesiredProperty: {patch}, {context}");

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

        private static async Task<Task> SetTwinPropertyUpdateCallbackObsoleteHandlerAsync(DeviceClient deviceClient, string expectedPropName, object expectedPropValue)
        {
#pragma warning disable CS0618

            string userContext = "myContext";
            var propertyUpdateReceived = new TaskCompletionSource<bool>();

            await deviceClient
                .SetDesiredPropertyUpdateCallback(
                    (patch, context) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DesiredProperty: {patch}, {context}");

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
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;

            await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*").ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(Client.TransportType transport, object propValue)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            // Set a callback
            await deviceClient.
                SetDesiredPropertyUpdateCallbackAsync(
                    (patch, context) =>
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DesiredProperty: {patch}, {context}");

                        // After unsubscribing it should never reach here
                        Assert.IsNull(patch);

                        return Task.FromResult<bool>(true);
                    },
                    null)
                .ConfigureAwait(false);

            // Unsubscribe
            await deviceClient
                .SetDesiredPropertyUpdateCallbackAsync(null, null)
                .ConfigureAwait(false);

            await RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue)
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(Client.TransportType transport, Func<DeviceClient, string, object, Task<Task>> setTwinPropertyUpdateCallbackAsync, object propValue)
        {
            string propName = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            Task updateReceivedTask = await setTwinPropertyUpdateCallbackAsync(deviceClient, propName, propValue).ConfigureAwait(false);

            await Task.WhenAll(
                RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue),
                updateReceivedTask).ConfigureAwait(false);

            // Validate the updated twin from the device-client
            Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            dynamic actual = deviceTwin.Properties.Desired[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actual), JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            Twin completeTwin = await _registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            dynamic actualProp = completeTwin.Properties.Desired[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actualProp), JsonConvert.SerializeObject(propValue));

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(null, null).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(Client.TransportType transport)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(testDevice.Id, twinPatch, "*").ConfigureAwait(false);

            Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            Assert.AreEqual<string>(deviceTwin.Properties.Desired[propName].ToString(), propValue);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetDateTimePropertiesAsync(Client.TransportType transport)
        {
            string jsonString = JsonConvert.SerializeObject(s_dateTimeProperty);

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var twinPatch = new Twin();

            // Here is an example to create a TwinCollection with json object if the users use date-formatted
            // string and don't want to drop the trailing zeros in the microseconds portion while parsing the
            // json properties. The Newtonsoft.Json serializer settings added in SDK apply to serial-/deserialization
            // during the client operations. Therefore, to ensure the datetime has the trailing zeros and this
            // test can verify the returned peoperties not dropping them, manually set up "DateParseHandling.None"
            // while creating a new TwinCollection object as the twin property.
            JObject jobjectProperty = JsonConvert.DeserializeObject<JObject>(
                jsonString,
                new JsonSerializerSettings
                {
                    DateParseHandling = DateParseHandling.None
                });

            twinPatch.Properties.Desired = new TwinCollection(jobjectProperty, null);
            await registryManager.UpdateTwinAsync(testDevice.Id, twinPatch, "*").ConfigureAwait(false);

            Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            Assert.AreEqual<string>(deviceTwin.Properties.Desired["Iso8601String"].ToString(), s_iso8601DateTimeString);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(Client.TransportType transport)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            var patch = new TwinCollection();
            patch[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(patch).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);

            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            Assert.AreEqual<string>(serviceTwin.Properties.Reported[propName].ToString(), propValue);

            VerboseTestLogger.WriteLine("verified " + serviceTwin.Properties.Reported[propName].ToString() + "=" + propValue);
        }

        private async Task Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(Client.TransportType transport)
        {
            string propName1 = Guid.NewGuid().ToString();
            string propName2 = Guid.NewGuid().ToString();
            string propEmptyValue = "{}";

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);
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
            string propName1 = "$" + Guid.NewGuid().ToString();
            string propName2 = Guid.NewGuid().ToString();

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            bool exceptionThrown = false;
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
            catch (IotHubException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown, "IotHubException was expected for updating reported property with an invalid property name, but was not thrown.");

            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            Assert.IsFalse(serviceTwin.Properties.Reported.Contains(propName1));
        }
    }

    internal class CustomTwinProperty
    {
        // The properties in here need to be public otherwise NewtonSoft.Json cannot serialize and deserialize them properly.
        public int Id { get; set; }

        public string Name { get; set; }
    }

    internal class TestDateTime
    {
        public string Iso8601String { get; set; }
    }
}
