using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    public class TwinE2ETests
    {
        private static string hubConnectionString;
        private static string hostName;
        private static RegistryManager registryManager;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        static public void ClassInitialize(TestContext testContext)
        {
            var environment = TestUtil.InitializeEnvironment("E2E_Twin_CSharp_");
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

        private async Task _Twin_DeviceSetsReportedPropertyAndGetsItBack(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Twin_CSharp_", hostName, registryManager);
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            TwinCollection props = new TwinCollection();
            props[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(props);

            var deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue);

            await deviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
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

        private async Task _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEvent(Client.TransportType transport)
        {
            var tcs = new TaskCompletionSource<bool>();
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Twin_CSharp_", hostName, registryManager);
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

        private async Task _Twin_ServiceSetsDesiredPropertyAndDeviceReceivesItOnNextGet(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Twin_CSharp_", hostName, registryManager);
            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(deviceInfo.Item1, twinPatch, "*");
            
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            var deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<string>(deviceTwin.Properties.Desired[propName].ToString(), propValue);
            await deviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
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

        private async Task _Twin_DeviceSetsReportedPropertyAndServiceReceivesIt(Client.TransportType transport)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Twin_CSharp_", hostName, registryManager);
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
