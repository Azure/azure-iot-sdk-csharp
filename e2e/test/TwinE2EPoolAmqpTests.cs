// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class TwinE2EPoolAmqpTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(TwinE2EPoolAmqpTests)}_";
        private readonly ConsoleEventListener _listener;
        private static TestLogging _log = TestLogging.GetInstance();

        public TwinE2EPoolAmqpTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_SingleConnection_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_SingleConnection_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Twin_IoTHubSak_DeviceSetsReportedPropertyAndGetsItBack_SingleConnection_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Twin_IotHubSak_DeviceSetsReportedPropertyAndGetsItBack_SingleConnection_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_SingleConnection_Amqp()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_SingleConnection_AmqpWs()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_SingleConnection_Amqp()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_SingleConnection_AmqpWs()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MultipleConnections_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_DeviceSetsReportedPropertyAndGetsItBack_MultipleConnections_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IotHubSak_DeviceSetsReportedPropertyAndGetsItBack_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MultipleConnections_Amqp()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MultipleConnections_AmqpWs()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MultipleConnections_Amqp()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MultipleConnections_AmqpWs()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
            TestDeviceType type,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Setting reported propery and verifying twin for device {testDevice.Id}");
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

            await PoolingOverAmqp.TestPoolAmqpAsync(
                DevicePrefix,
                transport,
                poolSize,
                devicesCount,
                (d, t) => { return Task.FromResult(false); },
                testOperation,
                cleanupOperation,
                authScope,
                true).ConfigureAwait(false);
        }

        private async Task ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
            TestDeviceType type,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, string, string, Task<Task>> setTwinPropertyUpdateCallbackAsync,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            Dictionary<string, List<string>> twinPropertyMap = new Dictionary<string, List<string>>();

            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                var propName = Guid.NewGuid().ToString();
                var propValue = Guid.NewGuid().ToString();
                twinPropertyMap.Add(testDevice.Id, new List<string> { propName, propValue });

                _log.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Setting desired propery callback for device {testDevice.Id}");
                _log.WriteLine($"{nameof(ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp)}: name={propName}, value={propValue}");
                Task updateReceivedTask = await setTwinPropertyUpdateCallbackAsync(deviceClient, propName, propValue).ConfigureAwait(false);
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Updating the desired properties for device {testDevice.Id}");
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

            await PoolingOverAmqp.TestPoolAmqpAsync(
                DevicePrefix,
                transport,
                poolSize,
                devicesCount,
                initOperation,
                testOperation,
                cleanupOperation,
                authScope,
                true).ConfigureAwait(false);
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
