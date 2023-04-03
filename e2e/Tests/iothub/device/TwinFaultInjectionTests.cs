// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Twins
{
    [TestClass]
    [TestCategory("FaultInjection")]
    [TestCategory("IoTHub-Client")]
    public class TwinFaultInjectionTests : E2EMsTestBase
    {
        private static readonly string s_devicePrefix = $"{nameof(TwinFaultInjectionTests)}_";

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceReportedProperties_ConnectionLossRecovery_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceReportedPropertiesRecoveryAsync(
                    new IotHubClientMqttSettings(protocol),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceReportedProperties_ConnectionLossRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceReportedPropertiesRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceReportedProperties_GracefulShutdownRecovery_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceReportedPropertiesRecoveryAsync(
                    new IotHubClientMqttSettings(protocol),
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye,
                    ct)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceReportedProperties_GracefulShutdownRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceReportedPropertiesRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceReportedProperties_AmqpConnectionLossRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceReportedPropertiesRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "",
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceReportedProperties_AmqpSessionLossRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceReportedPropertiesRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "",
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpTwinReq, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpTwinReq, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpTwinResp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpTwinResp, FaultInjectionConstants.FaultCloseReason_Boom)]
        public async Task Twin_DeviceReportedProperties_AmqpLinkDropRecovery_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceReportedPropertiesRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    faultType,
                    faultReason,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DoNotParallelize]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceReportedProperties_QuotaExceededRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceReportedPropertiesRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_QuotaExceeded,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [TestCategory("FaultInjection")]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_GracefulShutdownMqtt, FaultInjectionConstants.FaultCloseReason_Bye)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_GracefulShutdownMqtt, FaultInjectionConstants.FaultCloseReason_Bye)]
        public async Task Twin_DeviceDesiredPropertyUpdate_ConnectionLossRecovery_Mqtt(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                    new IotHubClientMqttSettings(protocol),
                    faultType,
                    faultReason,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [TestCategory("FaultInjection")]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_GracefulShutdownAmqp, FaultInjectionConstants.FaultCloseReason_Bye)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_GracefulShutdownAmqp, FaultInjectionConstants.FaultCloseReason_Bye)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpConn, "")]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpConn, "")]
        public async Task Twin_DeviceDesiredPropertyUpdate_ConnectionLossRecovery_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    faultType,
                    faultReason,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceDesiredPropertyUpdate_AmqpSessionLossRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "",
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpTwinReq, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpTwinReq, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpTwinResp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpTwinResp, FaultInjectionConstants.FaultCloseReason_Boom)]
        public async Task Twin_DeviceDesiredPropertyUpdate_AmqpLinkDropRecovery_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    faultType,
                    faultReason,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DoNotParallelize]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Twin_DeviceDesiredPropertyUpdate_QuotaExceededRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_QuotaExceeded,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        private async Task Twin_DeviceReportedPropertiesRecoveryAsync(
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            CancellationToken ct)
        {
            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                var props = new ReportedProperties();
                string propName = Guid.NewGuid().ToString();
                string propValue = Guid.NewGuid().ToString();
                props[propName] = propValue;

                await testDevice.DeviceClient.UpdateReportedPropertiesAsync(props, ct).ConfigureAwait(false);

                TwinProperties deviceTwin = await testDevice.DeviceClient.GetTwinPropertiesAsync(ct).ConfigureAwait(false);
                deviceTwin.Should().NotBeNull();
                deviceTwin.Reported.Should().NotBeNull();
                deviceTwin.Reported.TryGetValue(propName, out string actualValue).Should().BeTrue();
                actualValue.Should().Be(propValue);
                deviceTwin.Reported[propName].Should().NotBeNull();
                deviceTwin.Reported[propName].Should().BeEquivalentTo(propValue);
            }

            await FaultInjection
                .TestErrorInjectionAsync(
                    s_devicePrefix,
                    TestDeviceType.Sasl,
                    transportSettings,
                    null,
                    faultType,
                    reason,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    (d, c, ct) => Task.FromResult(false),
                    TestOperationAsync,
                    (ct) => Task.FromResult(false),
                    ct)
                .ConfigureAwait(false);
        }

        private async Task Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            CancellationToken ct)
        {
            // Configure the callback and start accepting twin changes.
            async Task InitOperationAsync(TestDevice _, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                await testDeviceCallbackHandler.SetTwinPropertyUpdateCallbackHandlerAndProcessAsync<string>(ct).ConfigureAwait(false);
            }

            // Change the twin from the service side and verify the device received it.
            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                string propName = Guid.NewGuid().ToString();
                string propValue = Guid.NewGuid().ToString();
                VerboseTestLogger.WriteLine($"{nameof(Twin_DeviceDesiredPropertyUpdateRecoveryAsync)}: name={propName}, value={propValue}");
                testDeviceCallbackHandler.ExpectedTwinPatchKeyValuePair = new Tuple<string, object>(propName, propValue);

                Task serviceSendTask = RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue, ct);
                Task twinReceivedTask = testDeviceCallbackHandler.WaitForTwinCallbackAsync(ct);

                await Task.WhenAll(serviceSendTask, twinReceivedTask).ConfigureAwait(false);
            }

            await FaultInjection
                .TestErrorInjectionAsync(
                    s_devicePrefix,
                    TestDeviceType.Sasl,
                    transportSettings,
                    null,
                    faultType,
                    reason,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    InitOperationAsync,
                    TestOperationAsync,
                    (ct) => Task.FromResult(false),
                    ct)
                .ConfigureAwait(false);
        }

        private static async Task RegistryManagerUpdateDesiredPropertyAsync(string deviceId, string propName, string propValue, CancellationToken ct)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            var twinPatch = new ClientTwin();
            twinPatch.Properties.Desired[propName] = propValue;

            await serviceClient.Twins.UpdateAsync(deviceId, twinPatch, cancellationToken: ct).ConfigureAwait(false);
        }
    }
}
