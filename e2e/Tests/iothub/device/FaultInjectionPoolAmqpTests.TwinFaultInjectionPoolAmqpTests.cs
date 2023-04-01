// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.Azure.Devices.E2ETests.Twins;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class FaultInjectionPoolAmqpTests
    {
        private readonly string Twin_DevicePrefix = $"TwinFaultInjectionPoolAmqpTests";

        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_GracefulShutdownAmqp, FaultInjectionConstants.FaultCloseReason_Bye)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_GracefulShutdownAmqp, FaultInjectionConstants.FaultCloseReason_Bye)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpConn, "")]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpConn, "")]
        public async Task Twin_DeviceReportedPropertiesTcpConnectionRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(protocol),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                faultType,
                faultReason,
                ct).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceReportedPropertiesAmqpSessionLossRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(protocol),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpSess,
                "",
                ct).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpTwinReq, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpTwinReq, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpTwinResp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpTwinResp, FaultInjectionConstants.FaultCloseReason_Boom)]
        public async Task Twin_DeviceReportedPropertiesTwinLinkDropRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(protocol),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                faultType,
                faultReason,
                ct).ConfigureAwait(false);
        }

        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_GracefulShutdownAmqp, FaultInjectionConstants.FaultCloseReason_Bye)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_GracefulShutdownAmqp, FaultInjectionConstants.FaultCloseReason_Bye)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpConn, "")]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpConn, "")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnectionRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(protocol),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                faultType,
                faultReason,
                ct).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(protocol),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpSess,
                "",
                ct).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpTwinReq, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpTwinReq, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpTwinResp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpTwinResp, FaultInjectionConstants.FaultCloseReason_Boom)]
        public async Task Twin_DeviceDesiredPropertyUpdateTwinLinkDropRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(protocol),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                faultType,
                faultReason,
                ct).ConfigureAwait(false);
        }

        private async Task Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            CancellationToken ct)
        {
            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler _, CancellationToken ct)
            {
                VerboseTestLogger.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Setting reported propery and verifying twin for device {testDevice.Id}");
                await TwinE2ETests.Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(
                        testDevice.DeviceClient,
                        testDevice.Id,
                        Guid.NewGuid().ToString(),
                        ct)
                    .ConfigureAwait(false);
            }

            await FaultInjectionPoolingOverAmqp
                .TestFaultInjectionPoolAmqpAsync(
                    Twin_DevicePrefix,
                    transportSettings,
                    null,
                    poolSize,
                    devicesCount,
                    faultType,
                    reason,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    (d, c, ct) => Task.FromResult(false),
                    TestOperationAsync,
                    (d, c, ct) => Task.FromResult(false),
                    ct)
                .ConfigureAwait(false);
        }

        private async Task Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            CancellationToken ct)
        {
            var twinPropertyMap = new Dictionary<string, List<string>>();

            async Task InitAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                string propName = Guid.NewGuid().ToString();
                string propValue = Guid.NewGuid().ToString();
                twinPropertyMap.Add(testDevice.Id, new List<string> { propName, propValue });

                VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolAmqpTests)}: Setting desired propery callback for device {testDevice.Id}");
                VerboseTestLogger.WriteLine($"{nameof(Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp)}: name={propName}, value={propValue}");
                await testDeviceCallbackHandler.SetTwinPropertyUpdateCallbackHandlerAndProcessAsync<string>(ct).ConfigureAwait(false);
            }

            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                List<string> twinProperties = twinPropertyMap[testDevice.Id];
                string propName = twinProperties[0];
                string propValue = twinProperties[1];

                VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolAmqpTests)}: Updating the desired properties for device {testDevice.Id}");
                testDeviceCallbackHandler.ExpectedTwinPatchKeyValuePair = new Tuple<string, object>(propName, propValue);

                Task serviceSendTask = TwinE2ETests.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue, ct);
                Task twinReceivedTask = testDeviceCallbackHandler.WaitForTwinCallbackAsync(ct);

                await Task.WhenAll(serviceSendTask, twinReceivedTask).ConfigureAwait(false);
            }

            await FaultInjectionPoolingOverAmqp
                .TestFaultInjectionPoolAmqpAsync(
                    Twin_DevicePrefix,
                    transportSettings,
                    null,
                    poolSize,
                    devicesCount,
                    faultType,
                    reason,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    InitAsync,
                    TestOperationAsync,
                    (d, c, ct) => Task.FromResult(false),
                    ct)
                .ConfigureAwait(false);
        }
    }
}
