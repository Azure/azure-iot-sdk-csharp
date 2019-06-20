// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class DeviceClientLifecycleTests
    {
        private readonly string DevicePrefix = $"E2E_{nameof(DeviceClientLifecycleTests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public DeviceClientLifecycleTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        #region ReceiveAsyncAfterDispose

        [TestMethod]
        public async Task DCLC_ReceiveAsyncAfterDispose_Sasl_Amqp()
        {
            await DCLC_ReceiveAsyncAfterDispose(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DCLC_ReceiveAsyncAfterDispose_Sasl_AmqpWs()
        {
            await DCLC_ReceiveAsyncAfterDispose(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        // NOTE: MQTT transport is not throwing any exception if the device client is either disposed or closed (ewertons).

        //[TestMethod]
        //public async Task DCLC_ReceiveAsyncAfterDispose_Sasl_Mqtt()
        //{
        //    await DCLC_ReceiveAsyncAfterDispose(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task DCLC_ReceiveAsyncAfterDispose_Sasl_MqttWs()
        //{
        //    await DCLC_ReceiveAsyncAfterDispose(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        //}

        [TestMethod]
        public async Task DCLC_ReceiveAsyncAfterDispose_x509_Amqp()
        {
            await DCLC_ReceiveAsyncAfterDispose(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DCLC_ReceiveAsyncAfterDispose_x509_AmqpWs()
        {
            await DCLC_ReceiveAsyncAfterDispose(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        //[TestMethod]
        //public async Task DCLC_ReceiveAsyncAfterDispose_x509_Mqtt()
        //{
        //    await DCLC_ReceiveAsyncAfterDispose(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task DCLC_ReceiveAsyncAfterDispose_x509_MqttWs()
        //{
        //    await DCLC_ReceiveAsyncAfterDispose(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        //}

        private async Task DCLC_ReceiveAsyncAfterDispose(TestDeviceType testDeviceType, Client.TransportType transportType)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, testDeviceType).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transportType))
            {
                Exception exceptionCaught = null;

                try
                {
                    await deviceClient.OpenAsync().ConfigureAwait(false);

                    Task<Client.Message> t = deviceClient.ReceiveAsync();

                    deviceClient.Dispose();

                    Client.Message message = await t.ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _log.WriteLine("Received exception:" + exception);
                    exceptionCaught = exception;
                }

                if (exceptionCaught == null)
                {
                    Assert.Fail("No exception was thrown.");
                }
                else if (!(exceptionCaught is ObjectDisposedException))
                {
                    Assert.Fail("Exception caught is not of type ObjectDisposedException");
                }
            }
        }

        #endregion ReceiveAsyncAfterDispose

        #region ReceiveAsyncAfterCloseAsync

        [TestMethod]
        public async Task DCLC_ReceiveAsyncAfterCloseAsync_Sasl_Amqp()
        {
            await DCLC_ReceiveAsyncAfterCloseAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DCLC_ReceiveAsyncAfterCloseAsync_Sasl_AmqpWs()
        {
            await DCLC_ReceiveAsyncAfterCloseAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        //[TestMethod]
        //public async Task DCLC_ReceiveAsyncAfterCloseAsync_Sasl_Mqtt()
        //{
        //    await DCLC_ReceiveAsyncAfterCloseAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task DCLC_ReceiveAsyncAfterCloseAsync_Sasl_MqttWs()
        //{
        //    await DCLC_ReceiveAsyncAfterCloseAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        //}

        [TestMethod]
        public async Task DCLC_ReceiveAsyncAfterCloseAsync_x509_Amqp()
        {
            await DCLC_ReceiveAsyncAfterCloseAsync(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DCLC_ReceiveAsyncAfterCloseAsync_x509_AmqpWs()
        {
            await DCLC_ReceiveAsyncAfterCloseAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        //[TestMethod]
        //public async Task DCLC_ReceiveAsyncAfterCloseAsync_x509_Mqtt()
        //{
        //    await DCLC_ReceiveAsyncAfterCloseAsync(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task DCLC_ReceiveAsyncAfterCloseAsync_x509_MqttWs()
        //{
        //    await DCLC_ReceiveAsyncAfterCloseAsync(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        //}

        private async Task DCLC_ReceiveAsyncAfterCloseAsync(TestDeviceType testDeviceType, Client.TransportType transportType)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, testDeviceType).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transportType))
            {
                Exception exceptionCaught = null;

                try
                {
                    await deviceClient.OpenAsync().ConfigureAwait(false);

                    Task<Client.Message> t = deviceClient.ReceiveAsync();

                    await deviceClient.CloseAsync().ConfigureAwait(false);

                    Client.Message message = await t.ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _log.WriteLine("Received exception:" + exception);
                    exceptionCaught = exception;
                }

                if (exceptionCaught == null)
                {
                    Assert.Fail("No exception was thrown.");
                }
                else if (!(exceptionCaught is ObjectDisposedException))
                {
                    Assert.Fail("Exception caught is not of type ObjectDisposedException");
                }
            }
        }

        #endregion ReceiveAsyncAfterCloseAsync

        #region SendAsyncAfterDispose

        [TestMethod]
        public async Task DCLC_SendAsyncAfterDispose_Sasl_Amqp()
        {
            await DCLC_SendAsyncAfterDispose(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DCLC_SendAsyncAfterDispose_Sasl_AmqpWs()
        {
            await DCLC_SendAsyncAfterDispose(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        //[TestMethod]
        //public async Task DCLC_SendAsyncAfterDispose_Sasl_Mqtt()
        //{
        //    await DCLC_SendAsyncAfterDispose(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task DCLC_SendAsyncAfterDispose_Sasl_MqttWs()
        //{
        //    await DCLC_SendAsyncAfterDispose(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        //}

        [TestMethod]
        public async Task DCLC_SendAsyncAfterDispose_x509_Amqp()
        {
            await DCLC_SendAsyncAfterDispose(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DCLC_SendAsyncAfterDispose_x509_AmqpWs()
        {
            await DCLC_SendAsyncAfterDispose(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        //[TestMethod]
        //public async Task DCLC_SendAsyncAfterDispose_x509_Mqtt()
        //{
        //    await DCLC_SendAsyncAfterDispose(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task DCLC_SendAsyncAfterDispose_x509_MqttWs()
        //{
        //    await DCLC_SendAsyncAfterDispose(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        //}
        private async Task DCLC_SendAsyncAfterDispose(TestDeviceType testDeviceType, Client.TransportType transportType)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, testDeviceType).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transportType))
            {
                Exception exceptionCaught = null;

                try
                {
                    await deviceClient.OpenAsync().ConfigureAwait(false);

                    Task t = deviceClient.SendEventAsync(GetMeAMessage());

                    deviceClient.Dispose();

                    await t.ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _log.WriteLine("Received exception:" + exception);
                    exceptionCaught = exception;
                }

                if (exceptionCaught == null)
                {
                    Assert.Fail("No exception was thrown.");
                }
                else if (!(exceptionCaught is ObjectDisposedException))
                {
                    Assert.Fail("Exception caught is not of type ObjectDisposedException");
                }
            }
        }

        #endregion SendAsyncAfterDispose

        #region SendAsyncAfterCloseAsync

        [TestMethod]
        public async Task DCLC_SendAsyncAfterCloseAsync_Sasl_Amqp()
        {
            await DCLC_SendAsyncAfterCloseAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DCLC_SendAsyncAfterCloseAsync_Sasl_AmqpWs()
        {
            await DCLC_SendAsyncAfterCloseAsync(TestDeviceType.Sasl, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        //[TestMethod]
        //public async Task DCLC_SendAsyncAfterCloseAsync_Sasl_Mqtt()
        //{
        //    await DCLC_SendAsyncAfterCloseAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task DCLC_SendAsyncAfterCloseAsync_Sasl_MqttWs()
        //{
        //    await DCLC_SendAsyncAfterCloseAsync(TestDeviceType.Sasl, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        //}

        [TestMethod]
        public async Task DCLC_SendAsyncAfterCloseAsync_x509_Amqp()
        {
            await DCLC_SendAsyncAfterCloseAsync(TestDeviceType.X509, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DCLC_SendAsyncAfterCloseAsync_x509_AmqpWs()
        {
            await DCLC_SendAsyncAfterCloseAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        //[TestMethod]
        //public async Task DCLC_SendAsyncAfterCloseAsync_x509_Mqtt()
        //{
        //    await DCLC_SendAsyncAfterCloseAsync(TestDeviceType.X509, Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        //}

        //[TestMethod]
        //public async Task DCLC_SendAsyncAfterCloseAsync_x509_MqttWs()
        //{
        //    await DCLC_SendAsyncAfterCloseAsync(TestDeviceType.X509, Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        //}

        private async Task DCLC_SendAsyncAfterCloseAsync(TestDeviceType testDeviceType, Client.TransportType transportType)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, testDeviceType).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transportType))
            {
                Exception exceptionCaught = null;

                try
                {
                    await deviceClient.OpenAsync().ConfigureAwait(false);

                    Task t = deviceClient.SendEventAsync(GetMeAMessage());

                    await deviceClient.CloseAsync().ConfigureAwait(false);

                    await t.ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _log.WriteLine("Received exception:" + exception);
                    exceptionCaught = exception;
                }

                if (exceptionCaught == null)
                {
                    Assert.Fail("No exception was thrown.");
                }
                else if (!(exceptionCaught is ObjectDisposedException))
                {
                    Assert.Fail("Exception caught is not of type ObjectDisposedException");
                }
            }
        }

        #endregion SendAsyncAfterCloseAsync

        private Client.Message GetMeAMessage()
        {
            return new Client.Message(Encoding.ASCII.GetBytes(DateTime.Now.ToLongDateString()));
        }
    }
}
