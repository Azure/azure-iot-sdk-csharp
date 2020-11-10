﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ClientDeviceStreamingRequest = Microsoft.Azure.Devices.Client.DeviceStreamRequest;
using ServiceDeviceStreamingRequest = Microsoft.Azure.Devices.DeviceStreamRequest;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("DeviceStreaming")]
    public partial class DeviceStreamingTests : E2EMsTestBase
    {
        private static readonly string _devicePrefix = $"{nameof(DeviceStreamingTests)}_";
        private static readonly string _modulePrefix = $"{nameof(DeviceStreamingTests)}_";
        private static readonly string s_proxyServerAddress = Configuration.IoTHub.ProxyServerAddress;

        [TestMethod]
        public async Task DeviceStreaming_RequestAccepted_Sas_Amqp()
        {
            var amqpTransportSettings = new AmqpTransportSettings(Client.TransportType.Amqp_Tcp_Only);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await TestDeviceStreamingAsync(TestDeviceType.Sasl, transportSettings, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceStreaming_RequestAccepted_Sas_AmqpWs()
        {
            var amqpTransportSettings = new AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await TestDeviceStreamingAsync(TestDeviceType.Sasl, transportSettings, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceStreaming_RequestAccepted_Sas_Mqtt()
        {
            var mqttTransportSettings = new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_Tcp_Only);
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await TestDeviceStreamingAsync(TestDeviceType.Sasl, transportSettings, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceStreaming_RequestAccepted_Sas_MqttWs()
        {
            var mqttTransportSettings = new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await TestDeviceStreamingAsync(TestDeviceType.Sasl, transportSettings, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceStreaming_RequestAccepted_x509_Amqp()
        {
            var amqpTransportSettings = new AmqpTransportSettings(Client.TransportType.Amqp_Tcp_Only);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await TestDeviceStreamingAsync(TestDeviceType.X509, transportSettings, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceStreaming_RequestAccepted_x509_Mqtt()
        {
            var mqttTransportSettings = new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_Tcp_Only);
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await TestDeviceStreamingAsync(TestDeviceType.X509, transportSettings, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceStreaming_RequestRejected_Sas_Amqp()
        {
            var amqpTransportSettings = new AmqpTransportSettings(Client.TransportType.Amqp_Tcp_Only);
            ITransportSettings[] transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await TestDeviceStreamingAsync(TestDeviceType.Sasl, transportSettings, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceStreaming_RequestRejected_Sas_AmqpWs()
        {
            var amqpTransportSettings = new Client.AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await TestDeviceStreamingAsync(TestDeviceType.Sasl, transportSettings, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceStreaming_RequestRejected_Sas_Mqtt()
        {
            var mqttTransportSettings = new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_Tcp_Only);
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await TestDeviceStreamingAsync(TestDeviceType.Sasl, transportSettings, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceStreaming_RequestRejected_Sas_MqttWs()
        {
            var mqttTransportSettings = new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await TestDeviceStreamingAsync(TestDeviceType.Sasl, transportSettings, false).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task DeviceStreaming_WaitForDeviceStreamRequestAsync_5secs_TimesOut_Amqp()
        {
            var amqpTransportSettings = new AmqpTransportSettings(Client.TransportType.Amqp_Tcp_Only);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transportSettings);
            await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

            try
            {
                ClientDeviceStreamingRequest clientRequestTask = await deviceClient.WaitForDeviceStreamRequestAsync(cts.Token).ConfigureAwait(false);
            }
            catch (IotHubCommunicationException ce)
            {
                throw ce.InnerException;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task DeviceStreaming_WaitForDeviceStreamRequestAsync_5secs_TimesOut_Mqtt()
        {
            var mqttTransportSettings = new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_Tcp_Only);
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transportSettings);
            await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

            try
            {
                ClientDeviceStreamingRequest clientRequestTask = await deviceClient.WaitForDeviceStreamRequestAsync(cts.Token).ConfigureAwait(false);
            }
            catch (IotHubCommunicationException ce)
            {
                throw ce.InnerException;
            }
        }

        [TestMethod]
        public async Task ModuleStreaming_RequestAccepted_Sas_Amqp()
        {
            var amqpTransportSettings = new AmqpTransportSettings(Client.TransportType.Amqp_Tcp_Only);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await TestModuleStreamingAsync(TestDeviceType.Sasl, transportSettings, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleStreaming_RequestAccepted_Sas_AmqpWs()
        {
            var amqpTransportSettings = new AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await TestModuleStreamingAsync(TestDeviceType.Sasl, transportSettings, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleStreaming_RequestAccepted_Sas_Mqtt()
        {
            var mqttTransportSettings = new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_Tcp_Only);
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await TestModuleStreamingAsync(TestDeviceType.Sasl, transportSettings, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleStreaming_RequestAccepted_Sas_MqttWs()
        {
            var mqttTransportSettings = new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await TestModuleStreamingAsync(TestDeviceType.Sasl, transportSettings, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleStreaming_RequestRejected_Sas_MqttWs()
        {
            var mqttTransportSettings = new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await TestModuleStreamingAsync(TestDeviceType.Sasl, transportSettings, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleStreaming_RequestRejected_Sas_Amqp()
        {
            var amqpTransportSettings = new AmqpTransportSettings(Client.TransportType.Amqp_Tcp_Only);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await TestModuleStreamingAsync(TestDeviceType.Sasl, transportSettings, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleStreaming_RequestRejected_Sas_AmqpWs()
        {
            var amqpTransportSettings = new AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only);
            var transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await TestModuleStreamingAsync(TestDeviceType.Sasl, transportSettings, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleStreaming_RequestRejected_Sas_Mqtt()
        {
            var mqttTransportSettings = new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_Tcp_Only);
            var transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await TestModuleStreamingAsync(TestDeviceType.Sasl, transportSettings, false).ConfigureAwait(false);
        }

        private async Task TestDeviceStreamingAsync(TestDeviceType type, ITransportSettings[] transportSettings, bool acceptRequest)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transportSettings);
            await serviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

            Task<ClientDeviceStreamingRequest> clientRequestTask = deviceClient.WaitForDeviceStreamRequestAsync(cts.Token);

            Task<DeviceStreamResponse> serviceRequestTask = serviceClient.CreateStreamAsync(testDevice.Id, new ServiceDeviceStreamingRequest("bla"));

            ClientDeviceStreamingRequest clientRequest = await clientRequestTask.ConfigureAwait(false);

            Assert.IsNotNull(clientRequest, "Received an unexpected null device streaming request");

            Logger.Trace("Device streaming request received (name=" + clientRequest.Name + "; uri=" + clientRequest.Uri + "; authToken=" + clientRequest.AuthorizationToken + ")");

            if (acceptRequest)
            {
                await deviceClient.AcceptDeviceStreamRequestAsync(clientRequest, cts.Token).ConfigureAwait(false);

                DeviceStreamResponse serviceResponse = await serviceRequestTask.ConfigureAwait(false);

                Assert.IsNotNull(serviceResponse, "Received an unexpected null device streaming response");

                Logger.Trace("Device streaming response received (name=" + serviceResponse.StreamName + "; accepted=" + serviceResponse.IsAccepted + "; uri=" + serviceResponse.Uri + "; authToken=" + serviceResponse.AuthorizationToken + ")");

                Assert.IsTrue(serviceResponse.IsAccepted, "Service expected Device Streaming respose with IsAccepted true, but got false");

                await TestEchoThroughStreamingGatewayAsync(clientRequest, serviceResponse, cts).ConfigureAwait(false);
            }
            else
            {
                await deviceClient.RejectDeviceStreamRequestAsync(clientRequest, cts.Token).ConfigureAwait(false);

                DeviceStreamResponse serviceResponse = await serviceRequestTask.ConfigureAwait(false);

                Assert.IsNotNull(serviceResponse, "Received an unexpected null device streaming response");

                Logger.Trace("Device streaming response received (name=" + serviceResponse.StreamName + "; accepted=" + serviceResponse.IsAccepted + "; uri=" + serviceResponse.Uri + "; auth_token=" + serviceResponse.AuthorizationToken + ")");

                Assert.IsFalse(serviceResponse.IsAccepted, "Service expected Device Streaming respose with IsAccepted false, but got true");
            }

            await serviceClient.CloseAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task TestModuleStreamingAsync(TestDeviceType type, ITransportSettings[] transportSettings, bool acceptRequest)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix, Logger).ConfigureAwait(false);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            using var moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transportSettings);
            await serviceClient.OpenAsync().ConfigureAwait(false);
            await moduleClient.OpenAsync(cts.Token).ConfigureAwait(false);

            Task<ClientDeviceStreamingRequest> clientRequestTask = moduleClient.WaitForDeviceStreamRequestAsync(cts.Token);

            Task<DeviceStreamResponse> serviceRequestTask = serviceClient.CreateStreamAsync(testModule.DeviceId, testModule.Id, new ServiceDeviceStreamingRequest("bla"));

            ClientDeviceStreamingRequest clientRequest = await clientRequestTask.ConfigureAwait(false);

            Assert.IsNotNull(clientRequest, "Received an unexpected null device streaming request");

            Logger.Trace("Device streaming request received (name=" + clientRequest.Name + "; uri=" + clientRequest.Uri + "; authToken=" + clientRequest.AuthorizationToken + ")");

            if (acceptRequest)
            {
                await moduleClient.AcceptDeviceStreamRequestAsync(clientRequest, cts.Token).ConfigureAwait(false);

                DeviceStreamResponse serviceResponse = await serviceRequestTask.ConfigureAwait(false);

                Assert.IsNotNull(serviceResponse, "Received an unexpected null device streaming response");

                Logger.Trace("Device streaming response received (name=" + serviceResponse.StreamName + "; accepted=" + serviceResponse.IsAccepted + "; uri=" + serviceResponse.Uri + "; authToken=" + serviceResponse.AuthorizationToken + ")");

                Assert.IsTrue(serviceResponse.IsAccepted, "Service expected Device Streaming respose with IsAccepted true, but got false");

                await TestEchoThroughStreamingGatewayAsync(clientRequest, serviceResponse, cts).ConfigureAwait(false);
            }
            else
            {
                await moduleClient.RejectDeviceStreamRequestAsync(clientRequest, cts.Token).ConfigureAwait(false);

                DeviceStreamResponse serviceResponse = await serviceRequestTask.ConfigureAwait(false);

                Assert.IsNotNull(serviceResponse, "Received an unexpected null device streaming response");

                Logger.Trace("Device streaming response received (name=" + serviceResponse.StreamName + "; accepted=" + serviceResponse.IsAccepted + "; uri=" + serviceResponse.Uri + "; authToken=" + serviceResponse.AuthorizationToken + ")");

                Assert.IsFalse(serviceResponse.IsAccepted, "Service expected Device Streaming respose with IsAccepted false, but got true");
            }

            await serviceClient.CloseAsync().ConfigureAwait(false);
            await moduleClient.CloseAsync().ConfigureAwait(false);
        }

        public static async Task<ClientWebSocket> GetStreamingClientAsync(Uri uri, string authorizationToken, CancellationToken cancellationToken)
        {
            var wsClient = new ClientWebSocket();
            wsClient.Options.SetRequestHeader("Authorization", "Bearer " + authorizationToken);

            await wsClient.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);

            return wsClient;
        }

        private async Task TestEchoThroughStreamingGatewayAsync(ClientDeviceStreamingRequest clientRequest, DeviceStreamResponse serviceResponse, CancellationTokenSource cts)
        {
            Task<ClientWebSocket> deviceWSClientTask = GetStreamingClientAsync(clientRequest.Uri, clientRequest.AuthorizationToken, cts.Token);
            Task<ClientWebSocket> serviceWSClientTask = GetStreamingClientAsync(serviceResponse.Uri, serviceResponse.AuthorizationToken, cts.Token);

            await Task.WhenAll(deviceWSClientTask, serviceWSClientTask).ConfigureAwait(false);

            ClientWebSocket deviceWSClient = deviceWSClientTask.Result;
            ClientWebSocket serviceWSClient = serviceWSClientTask.Result;

            byte[] serviceBuffer = Encoding.ASCII.GetBytes("This is a test message !!!@#$@$423423\r\n");
            byte[] clientBuffer = new byte[serviceBuffer.Length];

            await Task
                .WhenAll(
                    serviceWSClient.SendAsync(new ArraySegment<byte>(serviceBuffer), WebSocketMessageType.Binary, true, cts.Token),
                    deviceWSClient.ReceiveAsync(new ArraySegment<byte>(clientBuffer), cts.Token).ContinueWith((wsrr) =>
                    {
                        Assert.AreEqual(wsrr.Result.Count, serviceBuffer.Length, "Number of bytes received by device WS client is different than sent by service WS client");
                        Assert.IsTrue(clientBuffer.SequenceEqual(serviceBuffer), "Content received by device WS client is different than sent by service WS client");
                    }, TaskScheduler.Current))
                .ConfigureAwait(false);

            await Task
                .WhenAll(
                    deviceWSClient.SendAsync(new ArraySegment<byte>(clientBuffer), WebSocketMessageType.Binary, true, cts.Token),
                    serviceWSClient.ReceiveAsync(new ArraySegment<byte>(serviceBuffer), cts.Token).ContinueWith((wsrr) =>
                    {
                        Assert.AreEqual(wsrr.Result.Count, serviceBuffer.Length, "Number of bytes received by service WS client is different than sent by device WS client");
                        Assert.IsTrue(clientBuffer.SequenceEqual(serviceBuffer), "Content received by service WS client is different than sent by device WS client");
                    }, TaskScheduler.Current))
                .ConfigureAwait(false);

            await Task
                .WhenAll(
                    deviceWSClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "End of test", cts.Token),
                    serviceWSClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "End of test", cts.Token))
                .ConfigureAwait(false);

            deviceWSClient.Dispose();
            serviceWSClient.Dispose();
        }
    }
}
