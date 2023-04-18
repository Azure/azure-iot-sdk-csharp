﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MQTTnet.Client;

namespace Microsoft.Azure.Devices.Client.Tests.Transport.Mqtt
{
    public class MqttTransportHandlerTests
    {
        [TestMethod]
        public async Task MqttTransportHandler_OpenAsyncCallsConnectAsync()
        {
            var cancellationToken = new CancellationToken();
            var options = new MqttClientOptions();

            var mockMqttClient = new Mock<IMqttClient>();

            using MqttTransportHandler mqttTransportHandler = CreateTransportHandler(mockMqttClient.Object);

            await mqttTransportHandler.OpenAsync(cancellationToken);

            mockMqttClient.Verify(p => p.ConnectAsync(It.IsAny<MqttClientOptions>(), cancellationToken));
        }

        internal static MqttTransportHandler CreateTransportHandler(IMqttClient mockMqttClient)
        {
            var pipelineContext = new PipelineContext();
            var clientConfigurationMock = new IotHubConnectionCredentials();
            pipelineContext.IotHubConnectionCredentials = clientConfigurationMock;
            pipelineContext.ProductInfo = new ProductInfo();
            pipelineContext.IotHubClientTransportSettings = new IotHubClientMqttSettings();


            var transportHandler = new MqttTransportHandler(pipelineContext)
            {
                // make the mqtt client used by the handler mocked so no network calls are actually made
                _mqttClient = mockMqttClient
            };
            return transportHandler;
        }
    }
}
