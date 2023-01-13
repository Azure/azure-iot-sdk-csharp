// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MQTTnet;
using MQTTnet.Client;

namespace Microsoft.Azure.Devices.Client.Tests.Transport.Mqtt
{
    [TestClass]
    [TestCategory("Unit")]
    public class MqttTransportHandlerTests
    {
        [TestMethod]
        public async Task MqttTransportHandler_OpenAsyncCallsConnectAsync()
        {
            // arrange
            var cancellationToken = new CancellationToken();
            var options = new MqttClientOptions();
            var mockMqttClient = new Mock<IMqttClient>();
            using MqttTransportHandler mqttTransportHandler = CreateTransportHandler(mockMqttClient.Object);

            // act
            await mqttTransportHandler.OpenAsync(cancellationToken);

            // assert
            mockMqttClient.Verify(p => p.ConnectAsync(It.IsAny<MqttClientOptions>(), cancellationToken));
        }

        
        [TestMethod]
        public async Task MqttTransportHandler_CloseAsyncCallsConnectAsync()
        {
            // arrange
            var cancellationToken = new CancellationToken();
            var options = new MqttClientOptions();
            var mockMqttClient = new Mock<IMqttClient>();
            using MqttTransportHandler mqttTransportHandler = CreateTransportHandler(mockMqttClient.Object);

            // act
            await mqttTransportHandler.CloseAsync(cancellationToken);

            // assert
            mockMqttClient.Verify(p => p.DisconnectAsync(It.IsAny<MqttClientDisconnectOptions>(), cancellationToken));
        }

        [TestMethod]
        public async Task MqttTransportHandler_SendTelemetryBatchAsync_Throws()
        {
            // arrange
            var cancellationToken = new CancellationToken();
            var mockMqttClient = new Mock<IMqttClient>();
            using MqttTransportHandler mqttTransportHandler = CreateTransportHandler(mockMqttClient.Object);

            // act
            Func<Task> act = async () => await mqttTransportHandler.SendTelemetryBatchAsync(new[] { new TelemetryMessage() }, cancellationToken);
            
            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task MqttTransportHandler_SendTelemetryAsync()
        {
            // arrange
            var cancellationToken = new CancellationToken();
            var mockMqttClient = new Mock<IMqttClient>();
            using MqttTransportHandler mqttTransportHandler = CreateTransportHandler(mockMqttClient.Object);

            // act
            Func<Task> act = async () => await mqttTransportHandler.SendTelemetryAsync(new TelemetryMessage(), cancellationToken);

            // assert
            await act.Should().ThrowAsync<IotHubClientException>();
            mockMqttClient.Verify(p => p.PublishAsync(It.IsAny<MqttApplicationMessage>(), cancellationToken));
        }

        [TestMethod]
        public async Task MqttTransportHandler_EnableMethodAsync_Throws()
        {
            // arrange
            var cancellationToken = new CancellationToken();
            var mockMqttClient = new Mock<IMqttClient>();
            using MqttTransportHandler mqttTransportHandler = CreateTransportHandler(mockMqttClient.Object);

            // act
            Func<Task> act = async () => await mqttTransportHandler.EnableMethodsAsync(cancellationToken);
            
            // assert
            await act.Should().ThrowAsync<IotHubClientException>();
        }

        [TestMethod]
        public async Task MqttTransportHandler_DisableMethodAsync_Throws()
        {
            // arrange
            var cancellationToken = new CancellationToken();
            var mockMqttClient = new Mock<IMqttClient>();
            using MqttTransportHandler mqttTransportHandler = CreateTransportHandler(mockMqttClient.Object);

            // act
            Func<Task> act = async () => await mqttTransportHandler.DisableMethodsAsync(cancellationToken);
            
            // assert
            await act.Should().ThrowAsync<IotHubClientException>();
        }

        [TestMethod]
        public async Task MqttTransportHandler_SendMethodResponseAsync_Throws()
        {
            // arrange
            var cancellationToken = new CancellationToken();
            var mockMqttClient = new Mock<IMqttClient>();
            using MqttTransportHandler mqttTransportHandler = CreateTransportHandler(mockMqttClient.Object);

            // act
            Func<Task> act = async () => await mqttTransportHandler.SendMethodResponseAsync(new DirectMethodResponse(200), cancellationToken);
            
            // assert
            await act.Should().ThrowAsync<IotHubClientException>();
        }

        internal MqttTransportHandler CreateTransportHandler(IMqttClient mockMqttClient)
        {
            var pipelineContext = new PipelineContext()
            {
                ModelId = "model"
            };
            var clientConfigurationMock = new IotHubConnectionCredentials();
            //clientConfigurationMock.SetupGet(x => x.ClientOptions).Returns(clientOptionsMock.Object);
            pipelineContext.IotHubConnectionCredentials = clientConfigurationMock;
            pipelineContext.ProductInfo = new ProductInfo();
            RemoteCertificateValidationCallback callback = (sender, certificate, chain, sslPolicyErrors) => true;
            var settings = new IotHubClientMqttSettings() 
            { 
                RemoteCertificateValidationCallback = callback,
                WillMessage = new WillMessage()
                {
                    Payload= new byte[] { 1, 2, 3 },
                    QualityOfService = QualityOfService.AtMostOnce,
                },
                AuthenticationChain = "AuthenticationChain",
            };

            var transportHandler = new MqttTransportHandler(pipelineContext, settings)
            {
                // make the mqtt client used by the handler mocked so no network calls are actually made
                _mqttClient = mockMqttClient
            };

            return transportHandler;
        }
    }
}
