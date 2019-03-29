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
    public class CombinedClientOperationsMultiplexingTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(CombinedClientOperationsMultiplexingTests)}_";
        private readonly int MuxWithoutPoolingDevicesCount = 2;
        private readonly int MuxWithoutPoolingPoolSize = 1; // For enabling multiplexing without pooling, the pool size needs to be set to 1
        private readonly int MuxWithPoolingDevicesCount = 4;
        private readonly int MuxWithPoolingPoolSize = 2;

        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public CombinedClientOperationsMultiplexingTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceCombinedClientOperations_MuxWithoutPooling_Amqp()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceCombinedClientOperations_MuxWithoutPooling_AmqpWs()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceCombinedClientOperations_MuxWithPooling_Amqp()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_DeviceCombinedClientOperations_MuxWithPooling_AmqpWs()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceCombinedClientOperations_MuxWithoutPooling_Amqp()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceCombinedClientOperations_MuxWithoutPooling_AmqpWs()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceCombinedClientOperations_MuxWithPooling_Amqp()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_DeviceCombinedClientOperations_MuxWithPooling_AmqpWs()
        {
            await DeviceCombinedClientOperations(
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub
                ).ConfigureAwait(false);
        }

        private async Task DeviceCombinedClientOperations(
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope
            )
        {
            var transportSettings = new ITransportSettings[]
            {
                new AmqpTransportSettings(transport)
                {
                    AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                    {
                        MaxPoolSize = unchecked((uint)poolSize),
                        Pooling = true
                    }
                }
            };

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            ICollection<DeviceClient> deviceClients = new List<DeviceClient>();
            Dictionary<DeviceClient, int> deviceClientConnectionStatusChangeCount = new Dictionary<DeviceClient, int>();

            try
            {
                _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingTests)}: Starting the test execution for {devicesCount} devices");

                for (int i = 0; i < devicesCount; i++)
                {
                    ConnectionStatus? lastConnectionStatus = null;
                    ConnectionStatusChangeReason? lastConnectionStatusChangeReason = null;
                    int setConnectionStatusChangesHandlerCount = 0;

                    TestDevice testDevice = await TestDevice.GetTestDeviceAsync($"{DevicePrefix}_{i}_").ConfigureAwait(false);
                    DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings, authScope);
                    deviceClients.Add(deviceClient);

                    deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
                    {
                        setConnectionStatusChangesHandlerCount++;
                        lastConnectionStatus = status;
                        lastConnectionStatusChangeReason = statusChangeReason;
                        _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingTests)}.{nameof(ConnectionStatusChangesHandler)}: status={status} statusChangeReason={statusChangeReason} count={setConnectionStatusChangesHandlerCount}");
                        deviceClientConnectionStatusChangeCount[deviceClient] = setConnectionStatusChangesHandlerCount;
                    });

                    // Perform D2C Operation
                    _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingTests)}: Preparing to send message for device {i}");
                    await deviceClient.OpenAsync().ConfigureAwait(false);
                    await MessageSend.SendSingleMessageAndVerifyAsync(deviceClient, testDevice.Id).ConfigureAwait(false);

                    // Perform C2D Operation
                    _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingTests)}: Setting the device {i} to receive C2D message.");
                    string payload, messageId, p1Value;
                    Message msg = MessageReceive.ComposeC2DTestMessage(out payload, out messageId, out p1Value);
                    await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                    await MessageReceive.VerifyReceivedC2DMessageAsync(transport, deviceClient, payload, p1Value).ConfigureAwait(false);

                    // Invoke direct methods
                    _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingTests)}: Testing direct methods for device {i}");
                    Task methodReceivedTask = await MethodUtil.SetDeviceReceiveMethod(deviceClient).ConfigureAwait(false);
                    await Task.WhenAll(
                        MethodUtil.ServiceSendMethodAndVerifyResponse(testDevice.Id),
                        methodReceivedTask).ConfigureAwait(false);

                    // Set reported twin properties
                    _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingTests)}: Setting reported Twin properties for device {i}");
                    await TwinUtil.Twin_DeviceSetsReportedPropertyAndGetsItBack(deviceClient).ConfigureAwait(false);

                    // Receive set desired twin properties
                    _log.WriteLine($"{nameof(CombinedClientOperationsMultiplexingTests)}: Received set desired tein properties for device {i}");
                    var propName = Guid.NewGuid().ToString();
                    var propValue = Guid.NewGuid().ToString();
                    Task updateReceivedTask = await TwinUtil.SetTwinPropertyUpdateCallbackHandlerAsync(deviceClient, propName, propValue).ConfigureAwait(false);
                    await Task.WhenAll(
                        TwinUtil.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue),
                        updateReceivedTask).ConfigureAwait(false);

                }
            }
            finally
            {
                // Close and dispose all of the device client instances here
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    await deviceClient.CloseAsync().ConfigureAwait(false);

                    // The connection status change count should be 2: connect (open) and disabled (close)
                    Assert.IsTrue(deviceClientConnectionStatusChangeCount[deviceClient] == 2);

                    _log.WriteLine($"{nameof(MessageSendE2EMultiplexingTests)}: Disposing deviceClient {TestLogging.GetHashCode(deviceClient)}");
                    deviceClient.Dispose();
                }
            }
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
