using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// If you see intermittent failures on devices that are created by this file, check to see if you have multiple suites 
// running at the same time because one test run could be accidentally destroying devices created by a different test run.

namespace Microsoft.Azure.Devices.E2ETests
{
    public class TestUtil
    {
        public static string GetHostName(string connectionString)
        {
            Regex regex = new Regex("HostName=([^;]+)", RegexOptions.None);
            return regex.Match(connectionString).Groups[1].Value;
        }

        public static string GetDeviceConnectionString(Device device, string hostName)
        {
            var connectionString = new StringBuilder();
            connectionString.AppendFormat("HostName={0}", hostName);
            connectionString.AppendFormat(";DeviceId={0}", device.Id);
            connectionString.AppendFormat(";SharedAccessKey={0}", device.Authentication.SymmetricKey.PrimaryKey);
            return connectionString.ToString();
        }

        public static Tuple<string, RegistryManager> InitializeEnvironment(string devicePrefix)
        {
            string iotHubConnectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
            RegistryManager rm = RegistryManager.CreateFromConnectionString(iotHubConnectionString);

            // Ensure to remove all previous devices.
            foreach (Device device in rm.GetDevicesAsync(int.MaxValue).Result)
            {
                if (device.Id.StartsWith(devicePrefix))
                {
                    RemoveDevice(device.Id, rm);
                }
            }

            return new Tuple<string, RegistryManager>(iotHubConnectionString, rm);
        }

        public static void UnInitializeEnvironment(RegistryManager rm)
        {
            Task.Run(async () =>
            {
                await rm.CloseAsync();
            }).Wait();
        }

        public static Tuple<string, string> CreateDevice(string devicePrefix, string hostName, RegistryManager registryManager)
        {
            string deviceName = null;
            string deviceConnectionString = null;

            Task.Run(async () =>
            {
                deviceName = devicePrefix + Guid.NewGuid();
                Debug.WriteLine("Creating device " + deviceName);
                var device = await registryManager.AddDeviceAsync(new Device(deviceName));
                deviceConnectionString = TestUtil.GetDeviceConnectionString(device, hostName);
                Debug.WriteLine("Device successfully created");
            }).Wait();

            Thread.Sleep(1000);
            return new Tuple<string, string>(deviceName, deviceConnectionString);
        }

        public static void RemoveDevice(string deviceName, RegistryManager registryManager)
        {
            Task.Run(async () =>
            {
                Debug.WriteLine("Removing device " + deviceName);
                await registryManager.RemoveDeviceAsync(deviceName);
                Debug.WriteLine("Device successfully removed");
            }).Wait();
        }
    }
}