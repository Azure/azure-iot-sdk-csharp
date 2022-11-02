﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Twins
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class TwinE2EPoolAmqpTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(TwinE2EPoolAmqpTests)}_";

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_SingleConnection_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_SingleConnection_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceSetsReportedPropertyAndGetsItBack_SingleConnection_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_IotHubSak_DeviceSetsReportedPropertyAndGetsItBack_SingleConnection_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_SingleConnection_Amqp()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_SingleConnection_AmqpWs()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_SingleConnection_Amqp()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_SingleConnection_AmqpWs()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MultipleConnections_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceSetsReportedPropertyAndGetsItBack_MultipleConnections_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_IotHubSak_DeviceSetsReportedPropertyAndGetsItBack_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MultipleConnections_Amqp()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MultipleConnections_AmqpWs()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MultipleConnections_Amqp()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MultipleConnections_AmqpWs()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
            TestDeviceType type,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            async Task TestOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                VerboseTestLogger.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Setting reported propery and verifying twin for device {testDevice.Id}");
                await TwinE2ETests.Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString()).ConfigureAwait(false);
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    _devicePrefix,
                    transport,
                    poolSize,
                    devicesCount,
                    null,
                    TestOperationAsync,
                    null,
                    authScope,
                    true)
                .ConfigureAwait(false);
        }

        private async Task ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
            TestDeviceType type,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, string, string, Task<Task>> setTwinPropertyUpdateCallbackAsync,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            var twinPropertyMap = new Dictionary<string, List<string>>();

            async Task InitOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                string propName = Guid.NewGuid().ToString();
                string propValue = Guid.NewGuid().ToString();
                twinPropertyMap.Add(testDevice.Id, new List<string> { propName, propValue });

                VerboseTestLogger.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Setting desired propery callback for device {testDevice.Id}");
                VerboseTestLogger.WriteLine($"{nameof(ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp)}: name={propName}, value={propValue}");
                Task updateReceivedTask = await setTwinPropertyUpdateCallbackAsync(deviceClient, propName, propValue).ConfigureAwait(false);
            }

            async Task TestOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                VerboseTestLogger.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Updating the desired properties for device {testDevice.Id}");
                List<string> twinProperties = twinPropertyMap[testDevice.Id];
                string propName = twinProperties[0];
                string propValue = twinProperties[1];

                await TwinE2ETests.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue).ConfigureAwait(false);
            }

            Task CleanupOperationAsync()
            {
                twinPropertyMap.Clear();
                return Task.FromResult(0);
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    _devicePrefix,
                    transport,
                    poolSize,
                    devicesCount,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync,
                    authScope,
                    true)
                .ConfigureAwait(false);
        }
    }
}
