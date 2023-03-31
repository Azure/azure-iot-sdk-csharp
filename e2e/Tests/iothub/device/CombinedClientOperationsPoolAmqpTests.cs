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

        private static readonly TimeSpan s_testOperationTimeSpan = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan s_defaultMethodResponseTimeout = TimeSpan.FromMinutes(1);

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task DeviceCombinedClientOperations_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol)
        {
            await DeviceCombinedClientOperationsAsync(
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        private async Task DeviceCombinedClientOperationsAsync(
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount)
        {
            // Initialize service client for service-side operations
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            // Message payload and properties for sending outgoing messages
            var outgoingMessagesSent = new Dictionary<string, Tuple<OutgoingMessage, string>>();

            // Twin properties
            var twinPropertyMap = new Dictionary<string, List<string>>();

            async Task InitOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                IList<Task> initOperations = new List<Task>();
                using var openServiceClientCts = new CancellationTokenSource(s_testOperationTimeSpan);
                await serviceClient.Messages.OpenAsync(openServiceClientCts.Token).ConfigureAwait(false);

                // Send outgoing message
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Send outgoing message for device={testDevice.Id}");
                OutgoingMessage msg = OutgoingMessageHelper.ComposeOutgoingTestMessage(out string payload, out string p1Value);
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: messageId='{msg.MessageId}' userId='{msg.UserId}' payload='{payload}' p1Value='{p1Value}'");
                outgoingMessagesSent.Add(testDevice.Id, Tuple.Create(msg, payload));

                using var sendOutgoingMessageCts = new CancellationTokenSource(s_testOperationTimeSpan);
                Task sendOutgoingMessage = serviceClient.Messages.SendAsync(testDevice.Id, msg, sendOutgoingMessageCts.Token);
                initOperations.Add(sendOutgoingMessage);

                // Set method handler
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Set direct method {MethodName} for device={testDevice.Id}");
                Task methodReceivedTask = testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload, DirectMethodResponsePayload>(s_deviceResponsePayload);
                initOperations.Add(methodReceivedTask);

                // Set the twin desired properties callback
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Set desired property callback for device={testDevice.Id}");
                string propName = Guid.NewGuid().ToString();
                string propValue = Guid.NewGuid().ToString();
                twinPropertyMap.Add(testDevice.Id, new List<string> { propName, propValue });
                Task<Task> updateReceivedTask = TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync(testDevice.DeviceClient, propName, propValue);
                initOperations.Add(updateReceivedTask);

                await Task.WhenAll(initOperations).ConfigureAwait(false);
            }

            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                IList<Task> clientOperations = new List<Task>();
                await testDevice.OpenWithRetryAsync().ConfigureAwait(false);

                // D2C Operation
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 1: Send D2C for device={testDevice.Id}");
                TelemetryMessage message = TelemetryE2ETests.ComposeD2cTestMessage(out string _, out string _);
                Task sendD2cMessage = testDevice.DeviceClient.SendTelemetryAsync(message);
                clientOperations.Add(sendD2cMessage);

                // C2D Operation
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 2: Receive C2D for device={testDevice.Id}");
                Tuple<OutgoingMessage, string> msgSent = outgoingMessagesSent[testDevice.Id];
                OutgoingMessage msg = msgSent.Item1;
                string payload = msgSent.Item2;

                Task verifyDeviceClientReceivesMessage = MessageReceiveE2ETests.VerifyReceivedC2dMessageAsync(testDevice.DeviceClient, testDevice.Id, msg, payload);
                clientOperations.Add(verifyDeviceClientReceivesMessage);

                // Invoke direct methods
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 3: Direct methods test for device={testDevice.Id}");
                var directMethodRequest = new DirectMethodServiceRequest(MethodName)
                {
                    Payload = s_serviceRequestPayload,
                    ResponseTimeout = s_defaultMethodResponseTimeout,
                };
                testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

                Task serviceInvokeMethod = MethodE2ETests.ServiceSendMethodAndVerifyResponseAsync(testDevice.Id, null, directMethodRequest, s_deviceResponsePayload);
                clientOperations.Add(serviceInvokeMethod);

                // Set reported twin properties
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 4: Set reported property for device={testDevice.Id}");
                Task setReportedProperties = TwinE2ETests.Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(testDevice.DeviceClient, testDevice.Id, Guid.NewGuid().ToString());
                clientOperations.Add(setReportedProperties);

                // Receive set desired twin properties
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 5: Receive desired property for device={testDevice.Id}");
                List<string> twinProperties = twinPropertyMap[testDevice.Id];
                string propName = twinProperties[0];
                string propValue = twinProperties[1];
                Task updateDesiredProperties = TwinE2ETests.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue);
                clientOperations.Add(updateDesiredProperties);

                await Task.WhenAll(clientOperations).ConfigureAwait(false);
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: All operations completed for device={testDevice.Id}");
            }

            Task CleanupOperationAsync()
            {
                outgoingMessagesSent.Clear();
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
                    ConnectionStringAuthScope.Device)
                .ConfigureAwait(false);
        }
    }
}
