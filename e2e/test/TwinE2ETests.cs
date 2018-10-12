// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class TwinE2ETests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(TwinE2ETests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public TwinE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_Mqtt()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_Tcp_Only, 
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_MqttWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_WebSocket_Only, 
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_Tcp_Only, 
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

#if NETCOREAPP2_0
        // TODO: #302 In NetCoreApp2.0 the test is failing with TimeoutException.
        [Ignore]
#endif
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_WebSocket_Only, 
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_Mqtt()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_MqttWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

#if NETCOREAPP2_0
        // TODO: #302 In NetCoreApp2.0 the test is failing with TimeoutException.
        [Ignore]
#endif
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Mqtt_Tcp_Only, SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Mqtt_WebSocket_Only, SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }
        
        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Amqp_Tcp_Only, SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Amqp_WebSocket_Only, SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Mqtt_Tcp_Only, SetTwinPropertyUpdateCallbackObsoleteHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Mqtt_WebSocket_Only, SetTwinPropertyUpdateCallbackObsoleteHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Amqp_Tcp_Only, SetTwinPropertyUpdateCallbackObsoleteHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Amqp_WebSocket_Only, SetTwinPropertyUpdateCallbackObsoleteHandlerAsync).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_Mqtt()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_Tcp_Only, 
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_MqttWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_Tcp_Only, 
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_Mqtt()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_MqttWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        } 

        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Mqtt()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollection(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_MqttWs()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollection(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Amqp()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollection(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_AmqpWs()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollection(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_Mqtt()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyName(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_MqttWs()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyName(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_Amqp()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyName(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_AmqpWs()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyName(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport))
            {
                TwinCollection props = new TwinCollection();
                props[propName] = propValue;
                await deviceClient.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

                Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
                Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue);

                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task Twin_DeviceReportedPropertiesRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            var propName = Guid.NewGuid().ToString();
            var props = new TwinCollection();

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                var propValue = Guid.NewGuid().ToString();
                props[propName] = propValue;

                await deviceClient.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

                var deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
                Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue);
            };

            await FaultInjection.TestErrorInjectionTemplate(
                DevicePrefix,
                TestDeviceType.Sasl,
                transport,
                faultType,
                reason,
                delayInSec,
                FaultInjection.DefaultDurationInSec,
                (d, t) => { return Task.FromResult<bool>(false); },
                testOperation,
                () => { return Task.FromResult<bool>(false); }).ConfigureAwait(false);
        }

        private async Task<Task> SetTwinPropertyUpdateCallbackHandlerAsync(DeviceClient deviceClient, string expectedPropName, string expectedPropValue)
        {
            var propertyUpdateReceived = new TaskCompletionSource<bool>();
            string userContext = "myContext";

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(
                (patch, context) =>
                {
                    _log.WriteLine($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DesiredProperty: {patch}, {context}");

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
                }, userContext).ConfigureAwait(false);

            return propertyUpdateReceived.Task;
        }
        
        private async Task<Task> SetTwinPropertyUpdateCallbackObsoleteHandlerAsync(DeviceClient deviceClient, string expectedPropName, string expectedPropValue)
        {
#pragma warning disable CS0618

            string userContext = "myContext";
            var propertyUpdateReceived = new TaskCompletionSource<bool>();

            await deviceClient.SetDesiredPropertyUpdateCallback(
                (patch, context) =>
                {
                    _log.WriteLine($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DesiredProperty: {patch}, {context}");
                    
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
                }, userContext).ConfigureAwait(false);
#pragma warning restore CS0618

            return propertyUpdateReceived.Task;
        }

        private async Task RegistryManagerUpdateDesiredPropertyAsync(string deviceId, string propName, string propValue)
        {
            using (RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                var twinPatch = new Twin();
                twinPatch.Properties.Desired[propName] = propValue;

                await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*").ConfigureAwait(false);
                await registryManager.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType transport, Func<DeviceClient, string, string, Task<Task>> setTwinPropertyUpdateCallbackAsync)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent)}: name={propName}, value={propValue}");
            
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport))
            {
                Task updateReceivedTask = await setTwinPropertyUpdateCallbackAsync(deviceClient, propName, propValue).ConfigureAwait(false);

                await Task.WhenAll(
                    RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue),
                    updateReceivedTask).ConfigureAwait(false);

                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }
  
        private async Task Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            var propName = Guid.NewGuid().ToString();
            var props = new TwinCollection();

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                var propValue = Guid.NewGuid().ToString();
                _log.WriteLine($"{nameof(Twin_DeviceDesiredPropertyUpdateRecovery)}: name={propName}, value={propValue}");

                Task updateReceivedTask = await SetTwinPropertyUpdateCallbackHandlerAsync(deviceClient, propName, propValue).ConfigureAwait(false);

                await Task.WhenAll(
                    RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue),
                    updateReceivedTask).ConfigureAwait(false);
            };

            await FaultInjection.TestErrorInjectionTemplate(
                DevicePrefix,
                TestDeviceType.Sasl,
                transport,
                faultType,
                reason,
                delayInSec,
                FaultInjection.DefaultDurationInSec,
                (d, t) => { return Task.FromResult<bool>(false); },
                testOperation,
                () => { return Task.FromResult<bool>(false); }).ConfigureAwait(false);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            using (RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport))
            {
                var twinPatch = new Twin();
                twinPatch.Properties.Desired[propName] = propValue;
                await registryManager.UpdateTwinAsync(testDevice.Id, twinPatch, "*").ConfigureAwait(false);

                var deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
                Assert.AreEqual<string>(deviceTwin.Properties.Desired[propName].ToString(), propValue);

                await deviceClient.CloseAsync().ConfigureAwait(false);
                await registryManager.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();
            
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using (RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport))
            {
                var patch = new TwinCollection();
                patch[propName] = propValue;
                await deviceClient.UpdateReportedPropertiesAsync(patch).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);

                var serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
                Assert.AreEqual<string>(serviceTwin.Properties.Reported[propName].ToString(), propValue);

                _log.WriteLine("verified " + serviceTwin.Properties.Reported[propName].ToString() + "=" + propValue);
            }
        }

        private async Task Twin_ServiceDoesNotCreateNullPropertyInCollection(Client.TransportType transport)
        {
            var propName1 = Guid.NewGuid().ToString();
            var propName2 = Guid.NewGuid().ToString();
            var propEmptyValue = "{}";

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            using (RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport))
            {
                await deviceClient.UpdateReportedPropertiesAsync(new TwinCollection
                {
                    [propName1] = null
                }).ConfigureAwait(false);
                var serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
                Assert.IsFalse(serviceTwin.Properties.Reported.Contains(propName1));

                await deviceClient.UpdateReportedPropertiesAsync(new TwinCollection
                {
                    [propName1] = new TwinCollection
                    {
                        [propName2] = null
                    }
                }).ConfigureAwait(false);
                serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
                Assert.IsTrue(serviceTwin.Properties.Reported.Contains(propName1));
                String value1 = serviceTwin.Properties.Reported[propName1].ToString();

                Assert.AreEqual(value1, propEmptyValue);

                await deviceClient.UpdateReportedPropertiesAsync(new TwinCollection
                {
                    [propName1] = new TwinCollection
                    {
                        [propName2] = null
                    }
                }).ConfigureAwait(false);
                serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
                Assert.IsTrue(serviceTwin.Properties.Reported.Contains(propName1));
                String value2 = serviceTwin.Properties.Reported[propName1].ToString();
                Assert.AreEqual(value2, propEmptyValue);

                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task Twin_ClientHandlesRejectionInvalidPropertyName(Client.TransportType transport)
        {
            var propName1 = "$" + Guid.NewGuid().ToString();
            var propName2 = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            using (RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport))
            {
                var exceptionThrown = false;
                try
                {
                    await deviceClient.UpdateReportedPropertiesAsync(new TwinCollection
                    {
                        [propName1] = 123,
                        [propName2] = "abcd"
                    }).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    exceptionThrown = true;
                }

                if (!exceptionThrown)
                {
                    throw new AssertFailedException("Exception was expected, but not thrown.");
                }

                var serviceTwin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);
                Assert.IsFalse(serviceTwin.Properties.Reported.Contains(propName1));

                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
