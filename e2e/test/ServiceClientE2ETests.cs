// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class ServiceClientE2ETests : IDisposable
    {
        private const string DeviceResponseJson = "{\"name\":\"e2e_test\"}";
        private const string ServiceRequestJson = "{\"a\":123}";
        private readonly string DirectMethodName = $"{nameof(ServiceClientE2ETests)}Method";
        private TimeSpan CustomTimeout = TimeSpan.FromMinutes(5);
        private readonly string DevicePrefix = $"E2E_{nameof(ServiceClientE2ETests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public ServiceClientE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task Message_TimeOutReachedResponse()
        {
            await FastTimeout().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_NoTimeoutPassed()
        {
            await DefaultTimeout().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Service_InvokeMethodWithCustomTimeout_Amqp()
        {
            await Service_InvokeMethodWithCustomTimeout(TransportType.Amqp).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Service_InvokeMethodWithCustomTimeout_AmqpWs()
        {
            await Service_InvokeMethodWithCustomTimeout(TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Service_InvokeMethodWithCustomTimeout_Amqp_WithProxy()
        {
            await Service_InvokeMethodWithCustomTimeout(
                TransportType.Amqp, 
                new ServiceClientTransportSettings()
                {
                    AmqpProxy = new WebProxy(Configuration.IoTHub.ProxyServerAddress),
                    HttpProxy = new WebProxy(Configuration.IoTHub.ProxyServerAddress)
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Service_InvokeMethodWithCustomTimeout_AmqpWs_WithProxy()
        {
            await Service_InvokeMethodWithCustomTimeout(
                TransportType.Amqp_WebSocket_Only,
                new ServiceClientTransportSettings()
                {
                    AmqpProxy = new WebProxy(Configuration.IoTHub.ProxyServerAddress),
                    HttpProxy = new WebProxy(Configuration.IoTHub.ProxyServerAddress)
                }).ConfigureAwait(false);
        }

        private async Task Service_InvokeMethodWithCustomTimeout(TransportType transportType)
        {
            await Service_InvokeMethodWithCustomTimeout(TransportType.Amqp_WebSocket_Only, new ServiceClientTransportSettings()).ConfigureAwait(false);
        }

        private async Task Service_InvokeMethodWithCustomTimeout(TransportType transportType, ServiceClientTransportSettings transportSettings)
        {
            TestDevice device = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            DeviceClient deviceClient = device.CreateDeviceClient(Client.TransportType.Amqp);
            await MethodE2ETests.SetDeviceReceiveMethod(deviceClient, DirectMethodName).ConfigureAwait(false);

            var serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, transportType, transportSettings);
            var result = await serviceClient.InvokeDeviceMethodAsync(device.Id, new CloudToDeviceMethod(DirectMethodName, CustomTimeout)).ConfigureAwait(false);
            Assert.AreEqual(200, result.Status);
        }

        private async Task FastTimeout()
        {
            TimeSpan? timeout = TimeSpan.FromTicks(1);
            await TestTimeout(timeout).ConfigureAwait(false);
        }

        private async Task DefaultTimeout()
        {
            TimeSpan? timeout = null;
            await TestTimeout(timeout).ConfigureAwait(false);
        }

        private async Task TestTimeout(TimeSpan? timeout)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using (ServiceClient sender = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                _log.WriteLine($"Testing ServiceClient SendAsync() timeout={timeout}");
                await sender.SendAsync(testDevice.Id, new Message(Encoding.ASCII.GetBytes("Dummy Message")), timeout).ConfigureAwait(false);
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
