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
    public class MethodE2ETests
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
                deviceName = "E2E_Method_CSharp_" + Guid.NewGuid().ToString();
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
        [TestCategory("Method-E2E")]
        public async Task Twin_Service_Send_Method_And_Device_Receives_And_Send_Response_Over_MQTT()
        {
            await sendMethodAndRespond(Client.TransportType.Mqtt);
        }

#if WIP_C2D_METHODS_AMQP
        [TestMethod]
        [TestCategory("Method-E2E")]
        public async Task Twin_Service_Send_Method_And_Device_Receives_And_Send_Response_Over_AMQP()
        {
            await sendMethodAndRespond(Client.TransportType.Amqp);
        }
#endif

        async Task sendMethodAndRespond(Client.TransportType transport)
        {
            string deviceResponseJson = "{\"name\":\"e2e_test\"}";
            string serviceRequestJson = "{\"a\":123}";
            string methodName = "MethodE2ETest";

            var assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, transport);
            await deviceClient.OpenAsync();
            deviceClient.SetMethodHandler(methodName,
                (request, context) =>
                {
                    assertResult.SetResult(new Tuple<bool, bool>(request.Name.Equals(methodName), request.DataAsJson.Equals(serviceRequestJson)));
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(deviceResponseJson), 200));
                },
                null);

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
            Task<CloudToDeviceMethodResult> directResponseFuture = serviceClient.InvokeDeviceMethodAsync(
                deviceName,
                new CloudToDeviceMethod(methodName, TimeSpan.FromMinutes(5)).SetPayloadJson(serviceRequestJson)
            );
            Assert.IsTrue(assertResult.Task.Result.Item1, "Method name is not matching with the send data");
            Assert.IsTrue(assertResult.Task.Result.Item2, "Json data is not matching with the send data");
            CloudToDeviceMethodResult response = await directResponseFuture;
            Assert.AreEqual(200, response.Status);
            Assert.AreEqual(deviceResponseJson, response.GetPayloadAsJson());

            await deviceClient.CloseAsync();
        }
    }
}
