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
    public class CombinedClientOperationsPoolAmqpTests : IDisposable
    {
        private const string MethodName = "MethodE2ECombinedOperationsTest";
        private readonly string DevicePrefix = $"E2E_{nameof(CombinedClientOperationsPoolAmqpTests)}_";
        private readonly ConsoleEventListener _listener;
        private static TestLogging _log = TestLogging.GetInstance();

        public CombinedClientOperationsPoolAmqpTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task DeviceSak_DeviceCombinedClientOperations_SingleConnection_Amqp()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task DeviceSak_DeviceCombinedClientOperations_SingleConnection_AmqpWs()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task IoTHubSak_DeviceCombinedClientOperations_SingleConnection_Amqp()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task IoTHubSak_DeviceCombinedClientOperations_SingleConnection_AmqpWs()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task DeviceSak_DeviceCombinedClientOperations_MultipleConnections_Amqp()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceSak_DeviceCombinedClientOperations_MultipleConnections_AmqpWs()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IoTHubSak_DeviceCombinedClientOperations_MultipleConnections_Amqp()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IoTHubSak_DeviceCombinedClientOperations_MultipleConnections_AmqpWs()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        private async Task DeviceCombinedClientOperations(
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            // Initialize service client for service-side operations
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            // Message payload for C2D operation
            Dictionary<string, List<string>> messagesSent = new Dictionary<string, List<string>>();

            // Twin properties
            Dictionary<string, List<string>> twinPropertyMap = new Dictionary<string, List<string>>();

            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                IList<Task> initOperations = new List<Task>();

                // Send C2D Message
                _log.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Send C2D for device={testDevice.Id}");
                (Message msg, string messageId, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2DTestMessage();
                messagesSent.Add(testDevice.Id, new List<string> { payload, p1Value });
                var sendC2DMessage = serviceClient.SendAsync(testDevice.Id, msg);
                initOperations.Add(sendC2DMessage);

                // Set method handler
                _log.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Set direct method {MethodName} for device={testDevice.Id}");
                var methodReceivedTask = MethodE2ETests.SetDeviceReceiveMethod(deviceClient, MethodName);
                initOperations.Add(methodReceivedTask);

                // Set the twin desired properties callback
                _log.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Set desired property callback for device={testDevice.Id}");
                var propName = Guid.NewGuid().ToString();
                var propValue = Guid.NewGuid().ToString();
                twinPropertyMap.Add(testDevice.Id, new List<string> { propName, propValue });
                var updateReceivedTask = TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync(deviceClient, propName, propValue);
                initOperations.Add(updateReceivedTask);

                await Task.WhenAll(initOperations).ConfigureAwait(false);
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                IList<Task> clientOperations = new List<Task>();
                await deviceClient.OpenAsync().ConfigureAwait(false);

                // D2C Operation
                _log.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 1: Send D2C for device={testDevice.Id}");
                var sendD2CMessage = MessageSendE2ETests.SendSingleMessageAndVerifyAsync(deviceClient, testDevice.Id);
                clientOperations.Add(sendD2CMessage);

                // C2D Operation
                _log.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 2: Receive C2D for device={testDevice.Id}");
                List<string> msgSent = messagesSent[testDevice.Id];
                var payload = msgSent[0];
                var p1Value = msgSent[1];
                var verifyDeviceClientReceivesMessage = MessageReceiveE2ETests.VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, payload, p1Value);
                clientOperations.Add(verifyDeviceClientReceivesMessage);

                // Invoke direct methods
                _log.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 3: Direct methods test for device={testDevice.Id}");
                var serviceInvokeMethod = MethodE2ETests.ServiceSendMethodAndVerifyResponse(testDevice.Id, MethodName, MethodE2ETests.DeviceResponseJson, MethodE2ETests.ServiceRequestJson);
                clientOperations.Add(serviceInvokeMethod);

                // Set reported twin properties
                _log.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 4: Set reported property for device={testDevice.Id}");
                var setReportedProperties = TwinE2ETests.Twin_DeviceSetsReportedPropertyAndGetsItBack(deviceClient);
                clientOperations.Add(setReportedProperties);

                // Receive set desired twin properties
                _log.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: Operation 5: Receive desired property for device={testDevice.Id}");
                List<string> twinProperties = twinPropertyMap[testDevice.Id];
                var propName = twinProperties[0];
                var propValue = twinProperties[1];
                var updateDesiredProperties = TwinE2ETests.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue);
                clientOperations.Add(updateDesiredProperties);

                await Task.WhenAll(clientOperations).ConfigureAwait(false);
                _log.WriteLine($"{nameof(CombinedClientOperationsPoolAmqpTests)}: All operations completed for device={testDevice.Id}");
            };

            Func<IList<DeviceClient>, Task> cleanupOperation = async (deviceClients) =>
            {
                await serviceClient.CloseAsync().ConfigureAwait(false);
                serviceClient.Dispose();

                foreach (DeviceClient deviceClient in deviceClients)
                {
                    deviceClient.Dispose();
                }

                messagesSent.Clear();
                twinPropertyMap.Clear();
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
                false).ConfigureAwait(false);
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
