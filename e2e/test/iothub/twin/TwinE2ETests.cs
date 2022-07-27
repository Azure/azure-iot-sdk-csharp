// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
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

        private static readonly RegistryManager _registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

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

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    new MqttTransportSettings())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    new MqttTransportSettings(TransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    new AmqpTransportSettings())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(
                    new AmqpTransportSettings(TransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    new MqttTransportSettings())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    new MqttTransportSettings(TransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    new AmqpTransportSettings())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBack_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(
                    new AmqpTransportSettings(TransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    new MqttTransportSettings(),
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    new MqttTransportSettings(TransportProtocol.WebSocket),
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    new AmqpTransportSettings(),
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new MqttTransportSettings(),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new MqttTransportSettings(TransportProtocol.WebSocket),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new AmqpTransportSettings(),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    Guid.NewGuid().ToString())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new MqttTransportSettings(),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new MqttTransportSettings(TransportProtocol.WebSocket),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new AmqpTransportSettings(),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyArrayAndDeviceReceivesEvent_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    SetTwinPropertyUpdateCallbackHandlerAsync,
                    s_listOfPropertyValues)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    new MqttTransportSettings())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    new MqttTransportSettings(TransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    new AmqpTransportSettings())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(
                    new AmqpTransportSettings(TransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    new MqttTransportSettings())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    new MqttTransportSettings(TransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    new AmqpTransportSettings())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(
                    new AmqpTransportSettings(TransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Mqtt()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    new MqttTransportSettings())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_MqttWs()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    new MqttTransportSettings(TransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Amqp()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    new AmqpTransportSettings())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_AmqpWs()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(
                    new AmqpTransportSettings(TransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_Mqtt()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyNameAsync(
                    new MqttTransportSettings())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_MqttWs()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyNameAsync(
                    new MqttTransportSettings(TransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_Amqp()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyNameAsync(
                    new AmqpTransportSettings())
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_AmqpWs()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyNameAsync(
                    new AmqpTransportSettings(TransportProtocol.WebSocket))
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(TransportProtocol.Tcp)]
        [DataRow(TransportProtocol.WebSocket)]
        [TestCategory("LongRunning")]
        public async Task Twin_ClientSetsReportedPropertyWithoutDesiredPropertyCallback(TransportProtocol transportProtocol)
        {
            // arrange

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(new AmqpTransportSettings(transportProtocol));
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(testDevice.ConnectionString, options);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString(), Logger).ConfigureAwait(false);

            int connectionStatusChangeCount = 0;
            ConnectionStatusChangesHandler connectionStatusChangesHandler = (ConnectionStatus status, ConnectionStatusChangeReason reason) =>
            {
                Interlocked.Increment(ref connectionStatusChangeCount);
            };

            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            // act
            await RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            // assert
            Assert.AreEqual(0, connectionStatusChangeCount, "AMQP should not be disconnected.");
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBackSingleDeviceAsync(ITransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(testDevice.ConnectionString, options);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString(), Logger).ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyArrayAndGetsItBackSingleDeviceAsync(ITransportSettings transportSettings)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(testDevice.ConnectionString, options);

            await Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, s_listOfPropertyValues, Logger).ConfigureAwait(false);
        }

        public static async Task Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(IotHubDeviceClient deviceClient, string deviceId, object propValue, MsTestLogger logger)
        {
            string propName = Guid.NewGuid().ToString();

            logger.Trace($"{nameof(Twin_DeviceSetsReportedPropertyAndGetsItBackAsync)}: name={propName}, value={propValue}");

            var props = new Client.TwinCollection();
            props[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

            // Validate the updated twin from the device-client
            Client.Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            dynamic actual = deviceTwin.Properties.Reported[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actual), JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            Twin completeTwin = await _registryManager.GetTwinAsync(deviceId).ConfigureAwait(false);
            dynamic actualProp = completeTwin.Properties.Reported[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actualProp), JsonConvert.SerializeObject(propValue));
        }

        public static async Task<Task> SetTwinPropertyUpdateCallbackHandlerAsync(IotHubDeviceClient deviceClient, string expectedPropName, object expectedPropValue, MsTestLogger logger)
        {
            var propertyUpdateReceived = new TaskCompletionSource<bool>();
            string userContext = "myContext";

            await deviceClient
                .SetDesiredPropertyUpdateCallbackAsync(
                    (patch, context) =>
                    {
                        logger.Trace($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DesiredProperty: {patch}, {context}");

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

        public static async Task RegistryManagerUpdateDesiredPropertyAsync(string deviceId, string propName, object propValue)
        {
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;

            await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*").ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceUnsubscribes(ITransportSettings transportSettings, object propValue)
        {
            string propName = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(testDevice.ConnectionString, options);

            // Set a callback
            await deviceClient.
                SetDesiredPropertyUpdateCallbackAsync(
                    (patch, context) =>
                    {
                        Logger.Trace($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DesiredProperty: {patch}, {context}");

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

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync(
            ITransportSettings transportSettings,
            Func<IotHubDeviceClient, string, object, MsTestLogger, Task<Task>> setTwinPropertyUpdateCallbackAsync, object propValue)
        {
            string propName = Guid.NewGuid().ToString();

            Logger.Trace($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventAsync)}: name={propName}, value={propValue}");

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(testDevice.ConnectionString, options);

            Task updateReceivedTask = await setTwinPropertyUpdateCallbackAsync(deviceClient, propName, propValue, Logger).ConfigureAwait(false);

            await Task.WhenAll(
                RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue),
                updateReceivedTask).ConfigureAwait(false);

            // Validate the updated twin from the device-client
            Client.Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            dynamic actual = deviceTwin.Properties.Desired[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actual), JsonConvert.SerializeObject(propValue));

            // Validate the updated twin from the service-client
            Twin completeTwin = await _registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            dynamic actualProp = completeTwin.Properties.Desired[propName];
            Assert.AreEqual(JsonConvert.SerializeObject(actualProp), JsonConvert.SerializeObject(propValue));

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(null, null).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGetAsync(ITransportSettings transportSettings)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(testDevice.ConnectionString, options);

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(testDevice.Id, twinPatch, "*").ConfigureAwait(false);

            Client.Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            Assert.AreEqual<string>(deviceTwin.Properties.Desired[propName].ToString(), propValue);

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesItAsync(ITransportSettings transportSettings)
        {
            string propName = Guid.NewGuid().ToString();
            string propValue = Guid.NewGuid().ToString();

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(testDevice.ConnectionString, options);

            var patch = new Client.TwinCollection();
            patch[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(patch).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);

            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            Assert.AreEqual<string>(serviceTwin.Properties.Reported[propName].ToString(), propValue);

            Logger.Trace("verified " + serviceTwin.Properties.Reported[propName].ToString() + "=" + propValue);
        }

        private async Task Twin_ServiceDoesNotCreateNullPropertyInCollectionAsync(ITransportSettings transportSettings)
        {
            string propName1 = Guid.NewGuid().ToString();
            string propName2 = Guid.NewGuid().ToString();
            string propEmptyValue = "{}";

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(testDevice.ConnectionString, options);

            await deviceClient
                .UpdateReportedPropertiesAsync(
                    new Client.TwinCollection
                    {
                        [propName1] = null
                    })
                .ConfigureAwait(false);
            Twin serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
            Assert.IsFalse(serviceTwin.Properties.Reported.Contains(propName1));

            await deviceClient
                .UpdateReportedPropertiesAsync(
                    new Client.TwinCollection
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
                    new Client.TwinCollection
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

        private async Task Twin_ClientHandlesRejectionInvalidPropertyNameAsync(ITransportSettings transportSettings)
        {
            string propName1 = "$" + Guid.NewGuid().ToString();
            string propName2 = Guid.NewGuid().ToString();

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(testDevice.ConnectionString, options);

            bool exceptionThrown = false;
            try
            {
                await deviceClient
                    .UpdateReportedPropertiesAsync(
                        new Client.TwinCollection
                        {
                            [propName1] = 123,
                            [propName2] = "abcd",
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
}
