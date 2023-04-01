// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.Azure.Devices.E2ETests.Messaging;
using Microsoft.Azure.Devices.E2ETests.Methods;
using Microsoft.Azure.Devices.E2ETests.Twins;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class CombinedClientOperationsPoolAmqpTests : E2EMsTestBase
    {
        private const string MethodName = "MethodE2ECombinedOperationsTest";
        private readonly string _devicePrefix = $"{nameof(CombinedClientOperationsPoolAmqpTests)}_";

        private static readonly DirectMethodResponsePayload s_deviceResponsePayload = new() { CurrentState = "on" };
        private static readonly DirectMethodRequestPayload s_serviceRequestPayload = new() { DesiredState = "off" };

        private static readonly TimeSpan s_defaultMethodResponseTimeout = TimeSpan.FromSeconds(30);

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task DeviceCombinedClientOperations_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await DeviceCombinedClientOperationsAsync(
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    ct)
                .ConfigureAwait(false);
        }

        private async Task DeviceCombinedClientOperationsAsync(
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            CancellationToken ct)
        {
            // Initialize service client for service-side operations
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            async Task InitOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                IList<Task> initOperations = new List<Task>();
                await serviceClient.Messages.OpenAsync(ct).ConfigureAwait(false);

                // Set incoming message callback
                Task incomingMessageCallbackSet = testDeviceCallbackHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<string>(ct);
                initOperations.Add(incomingMessageCallbackSet);

                // Set method handler
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Set direct method {MethodName} for device={testDevice.Id}");
                Task methodCallbackSet = testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload>(s_deviceResponsePayload, ct);
                initOperations.Add(methodCallbackSet);

                // Set the twin desired properties callback
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Set desired property callback for device={testDevice.Id}");
                Task twinPatchCallbackSet = testDeviceCallbackHandler.SetTwinPropertyUpdateCallbackHandlerAndProcessAsync<string>(ct);
                initOperations.Add(twinPatchCallbackSet);

                await Task.WhenAll(initOperations).ConfigureAwait(false);
            }

            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                IList<Task> clientOperations = new List<Task>();
                await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

                // D2C Operation
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 1: Send D2C for device={testDevice.Id}");
                TelemetryMessage message = TelemetryMessageHelper.ComposeTestMessage(out string _, out string _);
                Task sendD2cMessage = testDevice.DeviceClient.SendTelemetryAsync(message, ct);
                clientOperations.Add(sendD2cMessage);

                // C2D Operation
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Send outgoing message for device={testDevice.Id}");
                OutgoingMessage msg = OutgoingMessageHelper.ComposeTestMessage(out string payload, out string p1Value);
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: messageId='{msg.MessageId}' userId='{msg.UserId}' payload='{payload}' p1Value='{p1Value}'");

                Task sendOutgoingMessage = serviceClient.Messages.SendAsync(testDevice.Id, msg, ct);
                clientOperations.Add(sendOutgoingMessage);

                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 2: Receive C2D for device={testDevice.Id}");
                testDeviceCallbackHandler.ExpectedOutgoingMessage = msg;
                Task incomingMessageReceivedTask = testDeviceCallbackHandler.WaitForIncomingMessageCallbackAsync(ct);
                clientOperations.Add(incomingMessageReceivedTask);

                // Invoke direct methods
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 3: Direct methods test for device={testDevice.Id}");
                var directMethodRequest = new DirectMethodServiceRequest(MethodName)
                {
                    Payload = s_serviceRequestPayload,
                    ResponseTimeout = s_defaultMethodResponseTimeout,
                };
                testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

                Task serviceInvokeMethod = MethodE2ETests.ServiceSendMethodAndVerifyResponseAsync(testDevice.Id, null, directMethodRequest, s_deviceResponsePayload, ct);
                Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(ct);
                clientOperations.Add(serviceInvokeMethod);
                clientOperations.Add(methodReceivedTask);

                // Set reported twin properties
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 4: Set reported property for device={testDevice.Id}");
                Task setReportedProperties = TwinE2ETests.Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(testDevice.DeviceClient, testDevice.Id, Guid.NewGuid().ToString(), ct);
                clientOperations.Add(setReportedProperties);

                // Receive set desired twin properties
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 5: Receive desired property for device={testDevice.Id}");
                string propName = Guid.NewGuid().ToString();
                string propValue = Guid.NewGuid().ToString();
                testDeviceCallbackHandler.ExpectedTwinPatchKeyValuePair = new Tuple<string, object>(propName, propValue);
                Task updateDesiredProperties = TwinE2ETests.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue, ct);
                Task twinReceivedTask = testDeviceCallbackHandler.WaitForTwinCallbackAsync(ct);
                clientOperations.Add(updateDesiredProperties);
                clientOperations.Add(twinReceivedTask);

                await Task.WhenAll(clientOperations).ConfigureAwait(false);
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: All operations completed for device={testDevice.Id}");
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    _devicePrefix,
                    transportSettings,
                    poolSize,
                    devicesCount,
                    InitOperationAsync,
                    TestOperationAsync,
                    null,
                    ConnectionStringAuthScope.Device,
                    ct)
                .ConfigureAwait(false);
        }
    }
}
