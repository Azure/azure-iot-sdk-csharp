// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Threading;
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
            TestUtil.UnInitializeEnvironment(registryManager);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Mqtt()
        {
            await _Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_MqttWs()
        {
            await _Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_Amqp()
        {
            await _Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndGetsItBack_AmqpWs()
        {
            await _Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType.Amqp_WebSocket_Only);
        }
        
        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_Mqtt()
        {
            await _Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_Tcp_Only, 
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
            await _Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_WebSocket_Only, 
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_Amqp()
        {
            await _Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_Tcp_Only, 
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_AmqpWs()
        {
            await _Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_WebSocket_Only, 
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
            await _Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_Tcp_Only,
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
            await _Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownMqtt,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_Amqp()
        {
            await _Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_AmqpWs()
        {
            await _Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Mqtt()
        {
            await _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MqttWs()
        {
            await _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Mqtt_WebSocket_Only);
        }
        
        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_Amqp()
        {
            await _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_AmqpWs()
        {
            await _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_Mqtt()
        {
            await _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_MqttWs()
        {
            await _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_Amqp()
        {
            await _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter(Client.TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter_AmqpWs()
        {
            await _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter(Client.TransportType.Amqp_WebSocket_Only);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Twin-E2E")]
        [TestCategory("Recovery")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_Mqtt()
        {
            await _Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_Tcp_Only, 
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
            await _Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_WebSocket_Only,
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
            await _Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_Tcp_Only, 
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
            await _Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_WebSocket_Only,
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
            await _Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_Tcp_Only,
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
            await _Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_WebSocket_Only,
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
            await _Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_Tcp_Only,
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
            await _Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        } 

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Mqtt()
        {
            await _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_MqttWs()
        {
            await _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_Amqp()
        {
            await _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet_AmqpWs()
        {
            await _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Mqtt()
        {
            await _Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_MqttWs()
        {
            await _Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_Amqp()
        {
            await _Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Twin-E2E")]
        public async Task Twin_DeviceSetsReportedPropertyAndServiceReceivesIt_AmqpWs()
        {
            await _Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType.Amqp_WebSocket_Only);
        }

        private async Task _Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType transport)
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
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }

        private async Task _Twin_DeviceReportedPropertiesRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue1 = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            TaskCompletionSource<bool> tcs = null;
            deviceClient.SetConnectionStatusChangesHandler((status, changeReason) =>
            {
                if (status == ConnectionStatus.Connected && tcs != null)
                {
                    tcs.SetResult(true);
                }
            });
            TwinCollection props = new TwinCollection();
            props[propName] = propValue1;
            await deviceClient.UpdateReportedPropertiesAsync(props);

            var deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue1);

            // send error command
            tcs = new TaskCompletionSource<bool>();
            await deviceClient.SendEventAsync(TestUtil.ComposeErrorInjectionProperties(faultType, reason, delayInSec));
            await tcs.Task.ConfigureAwait(false);

            deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue1);

            var propValue2 = Guid.NewGuid().ToString();
            props[propName] = propValue2;
            await deviceClient.UpdateReportedPropertiesAsync(props);

            deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue2);

            await deviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }


        private async Task _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType transport)
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
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }

        private async Task _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_WithObseleteCallbackSetter(Client.TransportType transport)
        {
            var tcs = new TaskCompletionSource<bool>();
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            await deviceClient.OpenAsync();
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

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(deviceInfo.Item1, twinPatch, "*");
            
            await tcs.Task;
            await deviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }

        private async Task _Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            var tcs = new TaskCompletionSource<bool>();
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangesHandler);
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

            // send error command
            await deviceClient.SendEventAsync(TestUtil.ComposeErrorInjectionProperties(faultType, reason, delayInSec));
            await Task.Delay(TimeSpan.FromSeconds(3));

            tcs = new TaskCompletionSource<bool>();
            twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(deviceInfo.Item1, twinPatch, "*");

            //await Task.Delay(TimeSpan.FromMinutes(4));
            //if(!tcs.Task.IsCompleted)
            //{
            //    throw new Exception("Task didn't complete within 4 minutes");
            //}
            //else
            //{
            //    if (tcs.Task.IsFaulted)
            //    {
            //        throw new Exception("Task failed");
            //    }
            //}

            await tcs.Task;

            await deviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }
        
        private static void ConnectionStatusChangesHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
        }
        private async Task _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType transport)
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
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }

        private async Task _Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType transport)
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
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }
    }
}
