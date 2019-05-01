using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class DeviceEventCounterLoggerE2ETests
    {
        private readonly string DevicePrefix = $"E2E_{nameof(DeviceEventCounterLoggerE2ETests)}_";

        public DeviceEventCounterLoggerE2ETests()
        {
            TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task DeviceEventCounterLogger_DeviceEventMonitor()
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            var deviceEventMonitor = new TestDeviceEventMonitor();
            List<string> deviceEventNames = DeviceEventMonitor.Attach(deviceEventMonitor);
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Amqp_Tcp_Only))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
            Assert.IsNotNull(deviceEventNames);
            Assert.IsTrue(deviceEventNames.Count > 0);
            List<string> receivedDeviceEvents = deviceEventMonitor.GetReceivedDeviceEvents();
            Assert.IsNotNull(receivedDeviceEvents);
            Assert.AreEqual(deviceEventNames.Count, receivedDeviceEvents.Count);
            foreach (string deviceEventName in deviceEventNames)
            {
                Assert.IsTrue(receivedDeviceEvents.Contains(deviceEventName));
            }
        }

        class TestDeviceEventMonitor : IDeviceEventMonitor
        {
            private readonly List<string> _receivedDeviceEvents = new List<string>();

            public void OnEvent(string deviceEventName)
            {
                _receivedDeviceEvents.Add(deviceEventName);
            }

            internal List<string> GetReceivedDeviceEvents()
            {
                return _receivedDeviceEvents;
            }
        }
    }
}
