// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Test.ConnectionString;
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
        private const string DumpyConnectionString = "HostName=Do.Not.Exist;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;SharedAccessKey=dGVzdFN0cmluZzE=";

        [TestMethod]
        public async Task MqttTransportHandler()
        {
            CancellationToken cancellationToken = new CancellationToken();
            var options = new MqttClientOptions();

            var mockMqttClient = new Mock<IMqttClient>();
            //mockMqttClient.Setup(p => p.ConnectAsync(It.IsAny<MqttClientOptions>(), cancellationToken));

            var mqttTransportHandler = createTransportHandler(mockMqttClient.Object);

            await mqttTransportHandler.OpenAsync(cancellationToken);

            mockMqttClient.Verify(p => p.ConnectAsync(It.IsAny<MqttClientOptions>(), cancellationToken));
        }

        internal MqttTransportHandler createTransportHandler(IMqttClient mockMqttClient)
        {
            var pipelineContext = new PipelineContext();
            pipelineContext.ProductInfo = new ProductInfo();
            var transportHandler = new MqttTransportHandler(
                pipelineContext,
                IotHubConnectionStringExtensions.Parse(DumpyConnectionString),
                new MqttTransportSettings(TransportType.Mqtt_Tcp_Only));

            // make the mqtt client used by the handler mocked so no network calls are actually made
            transportHandler.mqttClient = mockMqttClient;
            //transportHandler.mqttClientOptions = options;

            return transportHandler;
        }
    }
}
