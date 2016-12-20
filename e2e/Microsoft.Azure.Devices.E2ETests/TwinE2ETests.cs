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
        public async Task Twin_Device_Connects_And_Gets_Twin()
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, Client.TransportType.Mqtt);
            var deviceTwin = await deviceClient.GetTwinAsync();
            await deviceClient.CloseAsync();
        }

        [TestMethod]
        public async Task Twin_Service_Connects_And_Gets_Twin()
        {
            var registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
            var serviceTwin = await registryManager.GetTwinAsync(deviceName);
            await registryManager.CloseAsync();

        }

        [TestMethod]
        public async Task Twin_Service_Sets_Desired_Property_And_Device_Receives_Event()
        {
            var tcs = new TaskCompletionSource<bool>();
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, Client.TransportType.Mqtt);
            await deviceClient.OpenAsync();
            await deviceClient.SetDesiredPropertyUpdateCallback((patch, context) =>
            {
                return Task.Run(() =>
                {
                    Assert.AreEqual(patch[propName].ToString(), propValue);
                    tcs.SetResult(true);
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
        public async Task Twin_Service_Sets_Desired_Property_And_Device_Receives_It_On_Next_Get()
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            var registryManager = RegistryManager.CreateFromConnectionString(hubConnectionString);
            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;
            await registryManager.UpdateTwinAsync(deviceName, twinPatch, "*");
            await registryManager.CloseAsync();

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, Client.TransportType.Mqtt);
            var deviceTwin = await deviceClient.GetTwinAsync();
            Assert.AreEqual<string>(deviceTwin.Properties.Desired[propName].ToString(), propValue);
            await deviceClient.CloseAsync();
        }

        [TestMethod]
        public async Task Twin_Device_Sets_Reported_Property_And_Service_Receives_It()
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, Client.TransportType.Mqtt);
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
