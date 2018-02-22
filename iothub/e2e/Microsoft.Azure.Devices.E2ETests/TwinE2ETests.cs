// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    public class TwinE2ETests
    {
        private static string hubConnectionString;
        private static string hostName;
        private static RegistryManager registryManager;

        private const string DevicePrefix = "E2E_Twin_CSharp_";

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        static public void ClassInitialize(TestContext testContext)
        {
            var environment = TestUtil.InitializeEnvironment(DevicePrefix);
            hubConnectionString = environment.Item1;
            registryManager = environment.Item2;
            hostName = TestUtil.GetHostName(hubConnectionString);
        }

        [ClassCleanup]
        static public void ClassCleanup()
        {
            TestUtil.UnInitializeEnvironment(registryManager).GetAwaiter().GetResult();
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType.Amqp_Tcp_Only);
        }

#if NETCOREAPP2_0
        // TODO: #302 In NetCoreApp2.0 the test is failing with TimeoutException.
        [Ignore]
#endif
        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType.Amqp_WebSocket_Only);
        }
        
        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_Mqtt()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_Tcp_Only, 
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_MqttWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_WebSocket_Only, 
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_Tcp_Only, 
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

#if NETCOREAPP2_0
        // TODO: #302 In NetCoreApp2.0 the test is failing with TimeoutException.
        [Ignore]
#endif
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_WebSocket_Only, 
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_Mqtt()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_Tcp_Only,
                TestUtil.FaultType_GracefulShutdownMqtt,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_MqttWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownMqtt,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

#if NETCOREAPP2_0
        // TODO: #302 In NetCoreApp2.0 the test is failing with TimeoutException.
        [Ignore]
#endif
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Mqtt_WebSocket_Only);
        }
        
        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Amqp_Tcp_Only);
        }

#if NETCOREAPP2_0
        // TODO: #302 In NetCoreApp2.0 the test is failing with TimeoutException.
        [Ignore]
#endif
        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter(Client.TransportType.Amqp_Tcp_Only);
        }

#if NETCOREAPP2_0
        // TODO: #302 In NetCoreApp2.0 the test is failing with TimeoutException.
        [Ignore]
#endif
        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter(Client.TransportType.Amqp_WebSocket_Only);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_Mqtt()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_Tcp_Only, 
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_MqttWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_Tcp_Only, 
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_Mqtt()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_Tcp_Only,
                TestUtil.FaultType_GracefulShutdownMqtt,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_MqttWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownMqtt,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        } 

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Mqtt()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_MqttWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType.Amqp_Tcp_Only);
        }

#if NETCOREAPP2_0
        // TODO: #302 In NetCoreApp2.0 the test is failing with TimeoutException.
        [Ignore]
#endif
        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Mqtt()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_MqttWs()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType.Amqp_Tcp_Only);
        }

#if NETCOREAPP2_0
        // TODO: #302 In NetCoreApp2.0 the test is failing with TimeoutException.
        [Ignore]
#endif
        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Mqtt()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollection(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_MqttWs()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollection(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_Amqp()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollection(Client.TransportType.Amqp_Tcp_Only);
        }

#if NETCOREAPP2_0
        // TODO: #302 In NetCoreApp2.0 the test is failing with TimeoutException.
        [Ignore]
#endif
        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceDoesNotCreateNullPropertyInCollection_AmqpWs()
        {
            await Twin_ServiceDoesNotCreateNullPropertyInCollection(Client.TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_Mqtt()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyName(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_MqttWs()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyName(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_Amqp()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyName(Client.TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ClientHandlesRejectionInvalidPropertyName_AmqpWs()
        {
            await Twin_ClientHandlesRejectionInvalidPropertyName(Client.TransportType.Amqp_WebSocket_Only);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            TwinCollection props = new TwinCollection();
            props[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(props);

            var deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue);

            await deviceClient.CloseAsync();
            await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
        }

        private async Task Twin_DeviceReportedPropertiesRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue1 = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            TwinCollection props = new TwinCollection();
            props[propName] = propValue1;
            await deviceClient.UpdateReportedPropertiesAsync(props);

            var deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue1);

            // send error command
            await deviceClient.SendEventAsync(TestUtil.ComposeErrorInjectionProperties(faultType, reason, delayInSec));

            deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue1);

            var propValue2 = Guid.NewGuid().ToString();
            props[propName] = propValue2;
            await deviceClient.UpdateReportedPropertiesAsync(props);

            deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue2);

            await deviceClient.CloseAsync();
            await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType transport)
        {
            var tcs = new TaskCompletionSource<bool>();
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            await deviceClient.OpenAsync();
            await deviceClient.SetDesiredPropertyUpdateCallbackAsync((patch, context) =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        Assert.AreEqual(patch[propName].ToString(), propValue);
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                    finally
                    {
                        tcs.SetResult(true);
                    }
                });

            }, null);

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(deviceInfo.Item1, twinPatch, "*");

            await tcs.Task;
            await deviceClient.CloseAsync();
            await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
        }
        
        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter(Client.TransportType transport)
        {
            var tcs = new TaskCompletionSource<bool>();
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);

#pragma warning disable CS0618
            await deviceClient.SetDesiredPropertyUpdateCallback((patch, context) =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        Assert.AreEqual(patch[propName].ToString(), propValue);
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                    finally
                    {
                        tcs.SetResult(true);
                    }
                });
            }, null);
#pragma warning restore CS0618

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(deviceInfo.Item1, twinPatch, "*");
            
            await tcs.Task;
            await deviceClient.CloseAsync();
            await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
        }

        private async Task Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            var tcs = new TaskCompletionSource<bool>();
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);

            ConnectionStatus? lastConnectionStatus = null;
            ConnectionStatusChangeReason? lastConnectionStatusChangeReason = null;
            int setConnectionStatusChangesHandlerCount = 0;
            var tcsConnected = new TaskCompletionSource<bool>();
            var tcsDisconnected = new TaskCompletionSource<bool>();

            deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
            {
                if (status == ConnectionStatus.Disconnected_Retrying)
                {
                    tcsDisconnected.TrySetResult(true);
                    Assert.AreEqual(ConnectionStatusChangeReason.No_Network, statusChangeReason);
                }
                else if (status == ConnectionStatus.Connected)
                {
                    tcsConnected.TrySetResult(true);
                }

                lastConnectionStatus = status;
                lastConnectionStatusChangeReason = statusChangeReason;
                setConnectionStatusChangesHandlerCount++;
            });

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync((patch, context) =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        Assert.AreEqual(patch[propName].ToString(), propValue);
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                    finally
                    {
                        tcs.SetResult(true);
                    }
                });

            }, null);

            // assert on successfuly connection
            await Task.WhenAny(
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                }), tcsConnected.Task);
            Assert.IsTrue(tcsConnected.Task.IsCompleted, "Initial connection failed");
            if (transport != Client.TransportType.Http1)
            {
                Assert.AreEqual(1, setConnectionStatusChangesHandlerCount);
                Assert.AreEqual(ConnectionStatus.Connected, lastConnectionStatus);
                Assert.AreEqual(ConnectionStatusChangeReason.Connection_Ok, lastConnectionStatusChangeReason);
            }

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(deviceInfo.Item1, twinPatch, "*");

            await tcs.Task;

            // send error command
            await deviceClient.SendEventAsync(TestUtil.ComposeErrorInjectionProperties(faultType, reason, delayInSec));

            // reset ConnectionStatusChangesHandler data
            setConnectionStatusChangesHandlerCount = 0;
            tcsConnected = new TaskCompletionSource<bool>();
            tcsDisconnected = new TaskCompletionSource<bool>();

            // wait for disconnection
            await Task.WhenAny(
                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }), tcsDisconnected.Task);
            Assert.IsTrue(tcsDisconnected.Task.IsCompleted, "Error injection did not interrupt the device");

            // allow max 30s for connection recovery
            await Task.WhenAny(
                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    return Task.FromResult(true);
                }), tcsConnected.Task);
            Assert.IsTrue(tcsConnected.Task.IsCompleted, "Recovery connection failed");

            tcs = new TaskCompletionSource<bool>();
            twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(deviceInfo.Item1, twinPatch, "*");

            await tcs.Task;

            await deviceClient.CloseAsync();
            await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
        }

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(deviceInfo.Item1, twinPatch, "*");
            
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            var deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<string>(deviceTwin.Properties.Desired[propName].ToString(), propValue);
            await deviceClient.CloseAsync();
            await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            var patch = new TwinCollection();
            patch[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(patch);
            await deviceClient.CloseAsync();

            var serviceTwin = await registryManager.GetTwinAsync(deviceInfo.Item1);
            Assert.AreEqual<string>(serviceTwin.Properties.Reported[propName].ToString(), propValue);

            TestContext.WriteLine("verified " + serviceTwin.Properties.Reported[propName].ToString() + "=" + propValue);
            await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
        }

        private async Task Twin_ServiceDoesNotCreateNullPropertyInCollection(Client.TransportType transport)
        {
            var propName1 = Guid.NewGuid().ToString();
            var propName2 = Guid.NewGuid().ToString();
            var propEmptyValue = "{}";

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);

            using (var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport))
            {

                await deviceClient.UpdateReportedPropertiesAsync(new TwinCollection
                {
                    [propName1] = null
                });
                var serviceTwin = await registryManager.GetTwinAsync(deviceInfo.Item1);
                Assert.IsFalse(serviceTwin.Properties.Reported.Contains(propName1));

                await deviceClient.UpdateReportedPropertiesAsync(new TwinCollection
                {
                    [propName1] = new TwinCollection
                    {
                        [propName2] = null
                    }
                });
                serviceTwin = await registryManager.GetTwinAsync(deviceInfo.Item1);
                Assert.IsTrue(serviceTwin.Properties.Reported.Contains(propName1));
                String value1 = serviceTwin.Properties.Reported[propName1].ToString();

                // TODO: #243 
                // If service team fixed the Issue #243 the following Assert.AreNotEqual should fail 
                // and need to be changed to Assert.AreEqual
                Assert.AreNotEqual(value1, propEmptyValue);

                await deviceClient.UpdateReportedPropertiesAsync(new TwinCollection
                {
                    [propName1] = new TwinCollection
                    {
                        [propName2] = null
                    }
                });
                serviceTwin = await registryManager.GetTwinAsync(deviceInfo.Item1);
                Assert.IsTrue(serviceTwin.Properties.Reported.Contains(propName1));
                String value2 = serviceTwin.Properties.Reported[propName1].ToString();
                Assert.AreEqual(value2, propEmptyValue);
            }

            await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
        }

        private async Task Twin_ClientHandlesRejectionInvalidPropertyName(Client.TransportType transport)
        {
            var propName1 = "$" + Guid.NewGuid().ToString();
            var propName2 = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);

            using (var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport))
            {
                var exceptionThrown = false;
                try
                {
                    await deviceClient.UpdateReportedPropertiesAsync(new TwinCollection
                    {
                        [propName1] = 123,
                        [propName2] = "abcd"
                    });
                }
                catch (Exception e)
                {
                    exceptionThrown = true;
                }

                if (!exceptionThrown)
                {
                    throw new AssertFailedException("Exception was expected, but not thrown.");
                }

                var serviceTwin = await registryManager.GetTwinAsync(deviceInfo.Item1);
                Assert.IsFalse(serviceTwin.Properties.Reported.Contains(propName1));
            }

            await TestUtil.RemoveDeviceAsync(deviceInfo.Item1, registryManager);
        }
    }
}
