// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class CombinedClientOperationsMultiplexingOverAmqpTests : IDisposable
    {
        private const string MethodName = "MethodE2ECombinedOperationsTest";
        private readonly string DevicePrefix = $"E2E_{nameof(CombinedClientOperationsMultiplexingOverAmqpTests)}_";
        private readonly ConsoleEventListener _listener;
        private static TestLogging _log = TestLogging.GetInstance();

        public CombinedClientOperationsMultiplexingOverAmqpTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task DeviceSak_DeviceCombinedClientOperations_MuxWithoutPooling_Amqp()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceSak_DeviceCombinedClientOperations_MuxWithoutPooling_AmqpWs()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceSak_DeviceCombinedClientOperations_MuxWithPooling_Amqp()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceSak_DeviceCombinedClientOperations_MuxWithPooling_AmqpWs()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IoTHubSak_DeviceCombinedClientOperations_MuxWithoutPooling_Amqp()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IoTHubSak_DeviceCombinedClientOperations_MuxWithoutPooling_AmqpWs()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IoTHubSak_DeviceCombinedClientOperations_MuxWithPooling_Amqp()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IoTHubSak_DeviceCombinedClientOperations_MuxWithPooling_AmqpWs()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        private async Task DeviceCombinedClientOperations(
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope)
        {
            // Initialize service client for service-side operations
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            // Message payload for C2D operation
            string payload, messageId, p1Value;
            Dictionary<string, List<string>> messagesSent = new Dictionary<string, List<string>>();

            // Twin properties
            Dictionary<string, List<string>> twinPropertyMap = new Dictionary<string, List<string>>();

            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                IList<Task> initOperations = new List<Task>();

                // Send C2D Message
                _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingOverAmqpTests)}: Send C2D for device={testDevice.Id}");
                Message msg = MessageReceiveE2ETests.ComposeC2DTestMessage(out payload, out messageId, out p1Value);
                messagesSent.Add(testDevice.Id, new List<string> { payload, p1Value });
                var sendC2DMessage = serviceClient.SendAsync(testDevice.Id, msg);
                initOperations.Add(sendC2DMessage);

                // Set method handler
                _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingOverAmqpTests)}: Set direct method {MethodName} for device={testDevice.Id}");
                var methodReceivedTask = MethodE2ETests.SetDeviceReceiveMethod(deviceClient, MethodName);
                initOperations.Add(methodReceivedTask);

                // Set the twin desired properties callback
                _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingOverAmqpTests)}: Set desired property callback for device={testDevice.Id}");
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
                _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingOverAmqpTests)}: Operation 1: Send D2C for device={testDevice.Id}");
                var sendD2CMessage = MessageSendE2ETests.SendSingleMessageAndVerifyAsync(deviceClient, testDevice.Id);
                clientOperations.Add(sendD2CMessage);

                // C2D Operation
                _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingOverAmqpTests)}: Operation 2: Receive C2D for device={testDevice.Id}");
                List<string> msgSent = messagesSent[testDevice.Id];
                payload = msgSent[0];
                p1Value = msgSent[1];
                var verifyDeviceClientReceivesMessage = MessageReceiveE2ETests.VerifyReceivedC2DMessageAsync(transport, deviceClient, payload, p1Value);
                clientOperations.Add(verifyDeviceClientReceivesMessage);

                // Invoke direct methods
                _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingOverAmqpTests)}: Operation 3: Direct methods test for device={testDevice.Id}");
                var serviceInvokeMethod = MethodE2ETests.ServiceSendMethodAndVerifyResponse(testDevice.Id, MethodName, MethodE2ETests.DeviceResponseJson, MethodE2ETests.ServiceRequestJson);
                clientOperations.Add(serviceInvokeMethod);

                // Set reported twin properties
                _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingOverAmqpTests)}: Operation 4: Set reported property for device={testDevice.Id}");
                var setReportedProperties = TwinE2ETests.Twin_DeviceSetsReportedPropertyAndGetsItBack(deviceClient);
                clientOperations.Add(setReportedProperties);

                // Receive set desired twin properties
                _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingOverAmqpTests)}: Operation 5: Receive desired property for device={testDevice.Id}");
                List<string> twinProperties = twinPropertyMap[testDevice.Id];
                var propName = twinProperties[0];
                var propValue = twinProperties[1];
                var updateDesiredProperties = TwinE2ETests.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue);
                clientOperations.Add(updateDesiredProperties);

                await Task.WhenAll(clientOperations).ConfigureAwait(false);
                _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingOverAmqpTests)}: All operations completed for device={testDevice.Id}");
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

            await MultiplexingOverAmqp.TestMultiplexingOperationAsync(
                DevicePrefix,
                transport,
                poolSize,
                devicesCount,
                initOperation,
                testOperation,
                cleanupOperation).ConfigureAwait(false);
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
