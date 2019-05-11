// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
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
    public class TwinE2EMultiplexingOverAmqpTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(TwinE2EMultiplexingOverAmqpTests)}_";
        private readonly ConsoleEventListener _listener;
        private static TestLogging _log = TestLogging.GetInstance();

        public TwinE2EMultiplexingOverAmqpTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithoutPooling_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithoutPooling_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithoutPooling_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IotHubSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithoutPooling_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithPooling_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithPooling_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithPooling_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IotHubSak_DeviceSetsReportedPropertyAndGetsItBack_MuxWithPooling_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithoutPooling_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithoutPooling_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithPooling_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithPooling_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithoutPooling_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithoutPooling_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithPooling_Amqp()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MuxedWithPooling_AmqpWs()
        {
            await Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync
                ).ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBackMuxedOverAmqp(
            ConnectionStringAuthScope authScope,
            Client.TransportType transport,
            int poolSize,
            int devicesCount)
        {
            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                await Task.FromResult<bool>(false).ConfigureAwait(false);
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(TwinE2EMultiplexingOverAmqpTests)}: Setting reported propery and verifying twin for device {testDevice.Id}");
                await TwinE2ETests.Twin_DeviceSetsReportedPropertyAndGetsItBack(deviceClient).ConfigureAwait(false);
            };

            Func<IList<DeviceClient>, Task> cleanupOperation = async (deviceClients) =>
            {
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    deviceClient.Dispose();
                }
                await Task.FromResult<bool>(false).ConfigureAwait(false);
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

        private async Task Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp(
            ConnectionStringAuthScope authScope,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, string, string, Task<Task>> setTwinPropertyUpdateCallbackAsync
            )
        {
            Dictionary<string, List<string>> twinPropertyMap = new Dictionary<string, List<string>>();

            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                var propName = Guid.NewGuid().ToString();
                var propValue = Guid.NewGuid().ToString();
                twinPropertyMap.Add(testDevice.Id, new List<string> { propName, propValue });

                _log.WriteLine($"{nameof(TwinE2EMultiplexingOverAmqpTests)}: Setting desired propery for device {testDevice.Id}");
                _log.WriteLine($"{nameof(Twin_ServiceSetsDesiredPropertyAndDeviceReceivesEventMuxedOverAmqp)}: name={propName}, value={propValue}");
                Task updateReceivedTask = await setTwinPropertyUpdateCallbackAsync(deviceClient, propName, propValue).ConfigureAwait(false);
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(TwinE2EMultiplexingOverAmqpTests)}: Verifying desired property is set for device {testDevice.Id}");
                List<string> twinProperties = twinPropertyMap[testDevice.Id];
                var propName = twinProperties[0];
                var propValue = twinProperties[1];

                await TwinE2ETests.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue).ConfigureAwait(false);
            };

            Func<IList<DeviceClient>, Task> cleanupOperation = async (deviceClients) =>
            {
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    deviceClient.Dispose();
                }

                twinPropertyMap.Clear();
                await Task.FromResult<bool>(false).ConfigureAwait(false);
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
