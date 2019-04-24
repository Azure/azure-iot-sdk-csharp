// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class MessageSendE2EMultiplexingOverAmqpTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MessageSendE2EMultiplexingOverAmqpTests)}_";
        private readonly string ModulePrefix = $"E2E_{nameof(MessageSendE2EMultiplexingOverAmqpTests)}_";
        private readonly ConsoleEventListener _listener;
        private static TestLogging _log = TestLogging.GetInstance();

        public MessageSendE2EMultiplexingOverAmqpTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MuxWithoutPooling_Amqp()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MuxWithoutPooling_Amqp()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MuxWithPooling_Amqp()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceSendSingleMessage_MuxWithPooling_AmqpWs()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MuxWithPooling_Amqp()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceSendSingleMessage_MuxWithPooling_AmqpWs()
        {
            await SendMessageMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        //[TestMethod]
        //public async Task Message_DeviceSak_ModuleSendSingleMessage_MuxWithoutPooling_Amqp()
        //{
        //    await SendMessageModuleMuxedOverAmqp(
        //        Client.TransportType.Amqp_Tcp_Only,
        //        MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
        //        MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount
        //        ).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task Message_DeviceSak_ModuleSendSingleMessage_MuxWithoutPooling_AmqpWs()
        //{
        //    await SendMessageModuleMuxedOverAmqp(
        //        Client.TransportType.Amqp_WebSocket_Only,
        //        MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
        //        MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount
        //        ).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task Message_DeviceSak_ModuleSendSingleMessage_MuxWithPooling_Amqp()
        //{
        //    await SendMessageModuleMuxedOverAmqp(
        //        Client.TransportType.Amqp_Tcp_Only,
        //        MultiplexingOverAmqp.MuxWithPoolingPoolSize,
        //        MultiplexingOverAmqp.MuxWithPoolingDevicesCount
        //        ).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task Message_DeviceSak_ModuleSendSingleMessage_MuxWithPooling_AmqpWs()
        //{
        //    await SendMessageModuleMuxedOverAmqp(
        //        Client.TransportType.Amqp_WebSocket_Only,
        //        MultiplexingOverAmqp.MuxWithPoolingPoolSize,
        //        MultiplexingOverAmqp.MuxWithPoolingDevicesCount
        //        ).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task Message_DeviceSak_ModuleSendSingleMessageSameDevice_MuxWithoutPooling_Amqp()
        //{
        //    await SendMessageModuleMuxedOverAmqp(
        //        Client.TransportType.Amqp_Tcp_Only,
        //        MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
        //        MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
        //        true
        //        ).ConfigureAwait(false);
        //}

        //private async Task SendMessageModuleMuxedOverAmqp(
        //    Client.TransportType transport,
        //    int poolSize,
        //    int devicesCount,
        //    bool useSameDevice = false)
        //{
        //    var transportSettings = new ITransportSettings[]
        //    {
        //        new AmqpTransportSettings(transport)
        //        {
        //            AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
        //            {
        //                MaxPoolSize = unchecked((uint)poolSize),
        //                Pooling = true
        //            }
        //        }
        //    };

        //    ICollection<ModuleClient> moduleClients = new List<ModuleClient>();
        //    Dictionary<ModuleClient, int> moduleClientConnectionStatusChangeCount = new Dictionary<ModuleClient, int>();

        //    try
        //    {
        //        _log.WriteLine($"{nameof(MessageSendE2EMultiplexingOverAmqpTests)}: Starting the test execution for {devicesCount} modules");

        //        for (int i = 0; i < devicesCount; i++)
        //        {
        //            ConnectionStatus? lastConnectionStatus = null;
        //            ConnectionStatusChangeReason? lastConnectionStatusChangeReason = null;
        //            int setConnectionStatusChangesHandlerCount = 0;

        //            string devicePrefix = useSameDevice ? DevicePrefix : $"{DevicePrefix}_{i}_";
        //            string modulePrefix = useSameDevice ? $"{ModulePrefix}_{i}_" : ModulePrefix;

        //            TestModule testModule = await TestModule.GetTestModuleAsync(devicePrefix, modulePrefix).ConfigureAwait(false);
        //            ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transportSettings);
        //            moduleClients.Add(moduleClient);

        //            moduleClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
        //            {
        //                setConnectionStatusChangesHandlerCount++;
        //                lastConnectionStatus = status;
        //                lastConnectionStatusChangeReason = statusChangeReason;
        //                _log.WriteLine($"{nameof(MessageSendE2EMultiplexingOverAmqpTests)}.{nameof(ConnectionStatusChangesHandler)}: status={status} statusChangeReason={statusChangeReason} count={setConnectionStatusChangesHandlerCount}");
        //                moduleClientConnectionStatusChangeCount[moduleClient] = setConnectionStatusChangesHandlerCount;
        //            });

        //            _log.WriteLine($"{nameof(MessageSendE2EMultiplexingOverAmqpTests)}: Preparing to send message for module {i}");
        //            await moduleClient.OpenAsync().ConfigureAwait(false);
        //            await MessageSend.SendSingleMessageModuleAndVerifyAsync(moduleClient, testModule.DeviceId).ConfigureAwait(false);
        //        }
        //    }
        //    finally
        //    {
        //        // Close and dispose all of the module client instances here
        //        foreach (ModuleClient moduleClient in moduleClients)
        //        {
        //            await moduleClient.CloseAsync().ConfigureAwait(false);

        //            // The connection status change count should be 2: connect (open) and disabled (close)
        //            Assert.IsTrue(moduleClientConnectionStatusChangeCount[moduleClient] == 2, $"Connection status change count for deviceClient {TestLogging.GetHashCode(moduleClient)} is {moduleClientConnectionStatusChangeCount[moduleClient]}");

        //            _log.WriteLine($"{nameof(MessageSendE2EMultiplexingOverAmqpTests)}: Disposing moduleClient {TestLogging.GetHashCode(moduleClient)}");
        //            moduleClient.Dispose();
        //        }
        //    }
        //}

        private async Task SendMessageMuxedOverAmqp(
            TestDeviceType type,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            Dictionary<string, EventHubTestListener> eventHubListeners = new Dictionary<string, EventHubTestListener>();

            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                EventHubTestListener testListener = await EventHubTestListener.CreateListener(testDevice.Id).ConfigureAwait(false);
                eventHubListeners.Add(testDevice.Id, testListener);
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(MessageSendE2EMultiplexingOverAmqpTests)}: Preparing to send message for device {testDevice.Id}");
                await deviceClient.OpenAsync().ConfigureAwait(false);

                string payload;
                string p1Value;
                Client.Message testMessage = MessageSendE2ETests.ComposeD2CTestMessage(out payload, out p1Value);
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);

                EventHubTestListener testListener = eventHubListeners[testDevice.Id];
                bool isReceived = await testListener.WaitForMessage(testDevice.Id, payload, p1Value).ConfigureAwait(false);
                Assert.IsTrue(isReceived, "Message is not received.");
            };

            Func<IList<DeviceClient>, Task> cleanupOperation = async (deviceClients) =>
            {
                foreach (var listener in eventHubListeners)
                {
                    await listener.Value.CloseAsync().ConfigureAwait(false);
                }
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    deviceClient.Dispose();
                }
            };

            await MultiplexingOverAmqp.TestMultiplexingOperationAsync(
                DevicePrefix,
                transport,
                poolSize,
                devicesCount,
                initOperation,
                testOperation,
                cleanupOperation
                ).ConfigureAwait(false);
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
