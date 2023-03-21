// Copyright (c) Microsoft. All rights reserved.
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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MultipleConnections_Amqp()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(),
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
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
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
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    authScope: ConnectionStringAuthScope.IotHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_IotHubSak_DeviceSetsReportedPropertyAndGetsItBack_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    authScope: ConnectionStringAuthScope.IotHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MultipleConnections_Amqp()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(),
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
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
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
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                    authScope: ConnectionStringAuthScope.IotHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MultipleConnections_AmqpWs()
        {
            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                    authScope: ConnectionStringAuthScope.IotHub)
                .ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
            TestDeviceType type,
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                VerboseTestLogger.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Setting reported propery and verifying twin for device {testDevice.Id}");
                await TwinE2ETests.Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(testDevice.DeviceClient, testDevice.Id, Guid.NewGuid().ToString()).ConfigureAwait(false);
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    _devicePrefix,
                    transportSettings,
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
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            Func<IotHubDeviceClient, string, string, Task<Task>> setTwinPropertyUpdateCallbackAsync,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            var twinPropertyMap = new Dictionary<string, List<string>>();

            async Task InitOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                string propName = Guid.NewGuid().ToString();
                string propValue = Guid.NewGuid().ToString();
                twinPropertyMap.Add(testDevice.Id, new List<string> { propName, propValue });

                VerboseTestLogger.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Setting desired propery callback for device {testDevice.Id}");
                VerboseTestLogger.WriteLine($"{nameof(ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp)}: name={propName}, value={propValue}");
                Task updateReceivedTask = await setTwinPropertyUpdateCallbackAsync(testDevice.DeviceClient, propName, propValue).ConfigureAwait(false);
            }

            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler _)
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
                    transportSettings,
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
