// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
            var cancellationToken = new CancellationToken();
            var options = new MqttClientOptions();

            var mockMqttClient = new Mock<IMqttClient>();

            using MqttTransportHandler mqttTransportHandler = MqttTransportHandlerTests.CreateTransportHandler(mockMqttClient.Object);

            await mqttTransportHandler.OpenAsync(cancellationToken);

            mockMqttClient.Verify(p => p.ConnectAsync(It.IsAny<MqttClientOptions>(), cancellationToken));
        }

        internal static MqttTransportHandler CreateTransportHandler(IMqttClient mockMqttClient)
        {
            var pipelineContext = new PipelineContext();
            var clientConfigurationMock = new IotHubConnectionCredentials();
            //clientConfigurationMock.SetupGet(x => x.ClientOptions).Returns(clientOptionsMock.Object);
            pipelineContext.IotHubConnectionCredentials = clientConfigurationMock;
            pipelineContext.ProductInfo = new ProductInfo();

            var transportHandler = new MqttTransportHandler(
                pipelineContext,
                new IotHubClientMqttSettings())
            {
                // make the mqtt client used by the handler mocked so no network calls are actually made
                _mqttClient = mockMqttClient
            };

            return transportHandler;
        }
    }
}
