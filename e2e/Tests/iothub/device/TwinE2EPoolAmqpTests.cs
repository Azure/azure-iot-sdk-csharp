// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Twins
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class TwinE2EPoolAmqpTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(TwinE2EPoolAmqpTests)}_";

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp, ConnectionStringAuthScope.Device)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, ConnectionStringAuthScope.Device)]
        [DataRow(IotHubClientTransportProtocol.Tcp, ConnectionStringAuthScope.IotHub)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, ConnectionStringAuthScope.IotHub)]
        public async Task Twin_DeviceSak_DeviceSetsReportedPropertyAndGetsItBack_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol, ConnectionStringAuthScope authScope)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    authScope,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp, ConnectionStringAuthScope.Device)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, ConnectionStringAuthScope.Device)]
        [DataRow(IotHubClientTransportProtocol.Tcp, ConnectionStringAuthScope.IotHub)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, ConnectionStringAuthScope.IotHub)]
        public async Task Twin_DeviceSak_ServiceSetsDesiredPropertyAndDeviceReceivesEvent_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol, ConnectionStringAuthScope authScope)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    authScope,
                    ct)
                .ConfigureAwait(false);
        }

        private async Task Twin_DeviceSetsReportedPropertyAndGetsItBackPoolOverAmqp(
            TestDeviceType type,
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope,
            CancellationToken ct)
        {
            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler _, CancellationToken ct)
            {
                VerboseTestLogger.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Setting reported propery and verifying twin for device {testDevice.Id}");
                await TwinE2ETests.Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(testDevice.DeviceClient, testDevice.Id, Guid.NewGuid().ToString(), ct).ConfigureAwait(false);
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
                    ct)
                .ConfigureAwait(false);
        }

        private async Task ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp(
            TestDeviceType type,
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope,
            CancellationToken ct)
        {
            async Task InitOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                VerboseTestLogger.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Setting desired propery callback for device {testDevice.Id}");
                await testDeviceCallbackHandler.SetTwinPropertyUpdateCallbackHandlerAndProcessAsync<string>(ct).ConfigureAwait(false);
            }

            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                VerboseTestLogger.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Updating the desired properties for device {testDevice.Id}");
                string propName = Guid.NewGuid().ToString();
                string propValue = Guid.NewGuid().ToString();
                VerboseTestLogger.WriteLine($"{nameof(ServiceSetsDesiredPropertyAndDeviceReceivesEventPoolOverAmqp)}: name={propName}, value={propValue}");

                testDeviceCallbackHandler.ExpectedTwinPatchKeyValuePair = new Tuple<string, object>(propName, propValue);

                Task updateReceivedTask = testDeviceCallbackHandler.WaitForTwinCallbackAsync(ct);

                await Task.WhenAll(
                    TwinE2ETests.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue, ct),
                    updateReceivedTask).ConfigureAwait(false);
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    _devicePrefix,
                    transportSettings,
                    poolSize,
                    devicesCount,
                    InitOperationAsync,
                    TestOperationAsync,
                    (ct) => Task.FromResult(false),
                    authScope,
                    ct)
                .ConfigureAwait(false);
        }
    }
}
