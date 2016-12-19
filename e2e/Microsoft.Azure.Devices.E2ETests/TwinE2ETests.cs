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

        static string hubConnectionString;
        static string deviceName;
        static string deviceConnectionString;
        static string hostName;

        public TestContext TestContext { get; set; }

        static string GetHostName(string connectionString)
        {
            Regex regex = new Regex("HostName=([^;]+)", RegexOptions.None);
            return regex.Match(connectionString).Groups[1].Value;
        }

        static string GetDeviceConnectionString(Device device)
        {
            var deviceConnectionString = new StringBuilder();
            deviceConnectionString.AppendFormat("HostName={0}", hostName);
            deviceConnectionString.AppendFormat(";DeviceId={0}", device.Id);
            deviceConnectionString.AppendFormat(";SharedAccessKey={0}", device.Authentication.SymmetricKey.PrimaryKey);
            return deviceConnectionString.ToString();
        }

        [ClassInitialize]
        static public void ClassInitialize(TestContext testContext)
        {
            Task.Run(async () =>
            {
                hubConnectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
                deviceName = "E2E_Twin_CSharp_" + Guid.NewGuid().ToString();
                deviceConnectionString = null;
                hostName = GetHostName(hubConnectionString);

                var registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
                Debug.WriteLine("Creating device " + deviceName);
                var device = await registryManager.AddDeviceAsync(new Device(deviceName));
                deviceConnectionString = GetDeviceConnectionString(device);
                Debug.WriteLine("Device successfully created");
                await registryManager.CloseAsync();
            }).Wait();
        }

        [ClassCleanup]
        static public void ClassCleanup()
        {
            Task.Run(async () =>
            {
                var registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);

                Debug.WriteLine("Removing device " + deviceName);
                await registryManager.RemoveDeviceAsync(deviceName);
                Debug.WriteLine("Device successfully removed");
                await registryManager.CloseAsync();
            }).Wait();
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

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, transport);
            TwinCollection props = new TwinCollection();
            props[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(props);

            var deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue);

            await deviceClient.CloseAsync();
        }

        private async Task<String> getCurrentEtagFromService()
        {
            var registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
            var serviceTwin = await registryManager.GetTwinAsync(deviceName);
            string etag = serviceTwin.ETag;
            await registryManager.CloseAsync();
            return etag;
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

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, transport);
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

            var registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(deviceName, twinPatch, "*");
            await registryManager.CloseAsync();

            await tcs.Task;
            await deviceClient.CloseAsync();
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

            var registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(deviceName, twinPatch, "*");
            await registryManager.CloseAsync();

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, transport);
            var deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<string>(deviceTwin.Properties.Desired[propName].ToString(), propValue);
            await deviceClient.CloseAsync();
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

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, transport);
            var patch = new TwinCollection();
            patch[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(patch);
            await deviceClient.CloseAsync();

            var registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
            var serviceTwin = await registryManager.GetTwinAsync(deviceName);
            Assert.AreEqual<string>(serviceTwin.Properties.Reported[propName].ToString(), propValue);

            TestContext.WriteLine("verified " + serviceTwin.Properties.Reported[propName].ToString() + "=" + propValue);
            await registryManager.CloseAsync();

        }
    }
}
