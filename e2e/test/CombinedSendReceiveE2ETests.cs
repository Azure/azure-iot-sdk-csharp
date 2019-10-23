// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;


namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public partial class CombinedSendReceiveE2ETests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(CombinedSendReceiveE2ETests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;
        public CombinedSendReceiveE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public void CombinedSendReceiveMessages_Amqp()
        {
            CombinedSendReceiveMultipleMessages(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        public void CombinedSendReceiveMessages_AmqpWs()
        {
            CombinedSendReceiveMultipleMessages(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        public void CombinedSendReceiveMessages_Mqtt()
        {
            CombinedSendReceiveMultipleMessages(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        public void CombinedSendReceiveMessages_MqttWs()
        {
            CombinedSendReceiveMultipleMessages(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        public void CombinedSendReceiveMessages_Http()
        {
            CombinedSendReceiveMultipleMessages(TestDeviceType.Sasl, Client.TransportType.Http1);
        }

        public void CombinedSendReceiveMultipleMessages(TestDeviceType type, Client.TransportType transport)
        {
            var task1 = Task.Run(async () =>
            {
                MessageReceiveE2ETests.SendReceiveMultipleMessages(type, transport);
            });

            var task2 = Task.Run(async () =>
            {
                MessageSendE2ETests.SendMultipleMessages(type, transport);
            });

            Task.WaitAll(task1, task2);
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
