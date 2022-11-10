// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
    [TestCategory("IoTHub")]
    public class CombinedClientOperationsPoolAmqpTests : E2EMsTestBase
    {
        private const string MethodName = "MethodE2ECombinedOperationsTest";
        private readonly string _devicePrefix = $"{nameof(CombinedClientOperationsPoolAmqpTests)}_";

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceCombinedClientOperations_SingleConnection_Amqp()
        {
            await DeviceCombinedClientOperationsAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DeviceCombinedClientOperations_SingleConnection_AmqpWs()
        {
            await DeviceCombinedClientOperationsAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DeviceCombinedClientOperations_MultipleConnections_Amqp()
        {
            await DeviceCombinedClientOperationsAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task DeviceCombinedClientOperations_MultipleConnections_AmqpWs()
        {
            await DeviceCombinedClientOperationsAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount)
                .ConfigureAwait(false);
        }

        private async Task DeviceCombinedClientOperationsAsync(
            Client.TransportType transport,
            int poolSize,
            int devicesCount)
        {
            // Initialize service client for service-side operations
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            // Message payload and properties for C2D operation
            var messagesSent = new Dictionary<string, Tuple<Message, string>>();

            // Twin properties
            var twinPropertyMap = new Dictionary<string, List<string>>();

            async Task InitOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                IList<Task> initOperations = new List<Task>();

                // Send C2D Message
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Send C2D for device={testDevice.Id}");
                (Message msg, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2dTestMessage();
                using (msg)
                {
                    messagesSent.Add(testDevice.Id, Tuple.Create(msg, payload));
                    Task sendC2dMessage = serviceClient.SendAsync(testDevice.Id, msg);
                    initOperations.Add(sendC2dMessage);

                    // Set method handler
                    VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Set direct method {MethodName} for device={testDevice.Id}");
                    Task<Task> methodReceivedTask = MethodE2ETests.SetDeviceReceiveMethodAsync(deviceClient, MethodName);
                    initOperations.Add(methodReceivedTask);

                    // Set the twin desired properties callback
                    VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Set desired property callback for device={testDevice.Id}");
                    string propName = Guid.NewGuid().ToString();
                    string propValue = Guid.NewGuid().ToString();
                    twinPropertyMap.Add(testDevice.Id, new List<string> { propName, propValue });
                    Task<Task> updateReceivedTask = TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync(deviceClient, propName, propValue);
                    initOperations.Add(updateReceivedTask);

                    await Task.WhenAll(initOperations).ConfigureAwait(false);
                }
            }

            async Task TestOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                IList<Task> clientOperations = new List<Task>();
                await deviceClient.OpenAsync().ConfigureAwait(false);

                // D2C Operation
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 1: Send D2C for device={testDevice.Id}");
                Task sendD2cMessage = MessageSendE2ETests.SendSingleMessageAsync(deviceClient);
                clientOperations.Add(sendD2cMessage);

                // C2D Operation
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 2: Receive C2D for device={testDevice.Id}");
                Tuple<Message, string> msgSent = messagesSent[testDevice.Id];
                Message msg = msgSent.Item1;
                string payload = msgSent.Item2;

                Task verifyDeviceClientReceivesMessage = MessageReceiveE2ETests.VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, msg, payload);
                clientOperations.Add(verifyDeviceClientReceivesMessage);

                // Invoke direct methods
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 3: Direct methods test for device={testDevice.Id}");
                Task serviceInvokeMethod = MethodE2ETests.ServiceSendMethodAndVerifyResponseAsync(testDevice.Id, MethodName, MethodE2ETests.DeviceResponseJson, MethodE2ETests.ServiceRequestJson);
                clientOperations.Add(serviceInvokeMethod);

                // Set reported twin properties
                VerboseTestLogger.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 4: Set reported property for device={testDevice.Id}");
                Task setReportedProperties = TwinE2ETests.Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString());
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
                messagesSent.Clear();
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
                    ConnectionStringAuthScope.Device,
                    false)
                .ConfigureAwait(false);
        }
    }
}
