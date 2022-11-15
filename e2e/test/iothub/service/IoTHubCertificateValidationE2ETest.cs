// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Exceptions;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("InvalidServiceCertificate")]
    public class IotHubCertificateValidationE2ETest : E2EMsTestBase
    {
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ServiceClient_QueryDevicesInvalidServiceCertificateHttp_Fails()
        {
            // arrange
            using var sc = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionStringInvalidServiceCertificate);

            // act
            Func<Task> act = async () => await sc.Query.CreateAsync<ClientTwin>("select * from devices").ConfigureAwait(false);

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.StatusCode.Should().Be(System.Net.HttpStatusCode.RequestTimeout);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.Unknown);
            error.And.IsTransient.Should().BeTrue();
            error.And.InnerException.Should().BeOfType<HttpRequestException>();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ServiceClient_SendMessageToDeviceInvalidServiceCertificateAmqpTcp_Fails()
        {
            // arrange
            IotHubTransportProtocol protocol = IotHubTransportProtocol.Tcp;

            // act
            Func<Task> act = async () => await TestServiceClientInvalidServiceCertificateAsync(protocol).ConfigureAwait(false);

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.StatusCode.Should().Be(System.Net.HttpStatusCode.RequestTimeout);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.Unknown);
            error.And.IsTransient.Should().BeTrue();
            error.And.InnerException.Should().BeOfType<SocketException>();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ServiceClient_SendMessageToDeviceInvalidServiceCertificateAmqpWs_Fails()
        {
            // arrange
            IotHubTransportProtocol protocol = IotHubTransportProtocol.WebSocket;
 
            // act
            Func<Task> act = async () => await TestServiceClientInvalidServiceCertificateAsync(protocol).ConfigureAwait(false);
            
            //assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.StatusCode.Should().Be(System.Net.HttpStatusCode.RequestTimeout);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.Unknown);
            error.And.IsTransient.Should().BeTrue();
            error.And.InnerException.Should().BeOfType<WebSocketException>();
        }

        private static async Task TestServiceClientInvalidServiceCertificateAsync(IotHubTransportProtocol protocol)
        {
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol,
                RetryPolicy = new IotHubServiceNoRetry(),
            };
            using var service = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionStringInvalidServiceCertificate, options);
            var testMessage = new Message();
            await service.Messages.OpenAsync().ConfigureAwait(false);
            await service.Messages.SendAsync("testDevice1", testMessage).ConfigureAwait(false);
            await service.Messages.CloseAsync().ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task JobClient_ScheduleTwinUpdateInvalidServiceCertificateHttp_Fails()
        {
            // arrange
            using var sc = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionStringInvalidServiceCertificate);
            var ScheduledTwinUpdateOptions = new ScheduledJobsOptions
            {
                JobId = "testDevice",
                MaxExecutionTime = TimeSpan.FromSeconds(60)
            };

            // act
            Func<Task> act = async () => 
                await sc.ScheduledJobs.ScheduleTwinUpdateAsync(
                    "DeviceId IN ['testDevice']",
                    new ClientTwin(),
                    DateTimeOffset.UtcNow,
                    ScheduledTwinUpdateOptions)
                .ConfigureAwait(false);

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.StatusCode.Should().Be(System.Net.HttpStatusCode.RequestTimeout);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.Unknown);
            error.And.IsTransient.Should().BeTrue();
            error.And.InnerException.Should().BeOfType<HttpRequestException>();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateAmqpTcp_Fails()
        {
            // act
            Func<Task> act = async () => await TestDeviceClientInvalidServiceCertificateAsync(new IotHubClientAmqpSettings()).ConfigureAwait(false);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            error.And.IsTransient.Should().BeTrue();
            error.And.InnerException.Should().BeOfType<SocketException>();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateMqttTcp_Fails()
        {
            // act
            Func<Task> act = async () => await TestDeviceClientInvalidServiceCertificateAsync(new IotHubClientMqttSettings()).ConfigureAwait(false);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.Timeout);
            error.And.IsTransient.Should().BeTrue();
            error.And.InnerException.Should().BeOfType<MqttCommunicationTimedOutException>();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateAmqpWs_Fails()
        {
            // act
            Func<Task> act = async () =>
                await TestDeviceClientInvalidServiceCertificateAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket)).ConfigureAwait(false);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            error.And.IsTransient.Should().BeTrue();
            error.And.InnerException.Should().BeOfType<WebSocketException>();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_SendAsyncInvalidServiceCertificateMqttWs_Fails()
        {
            // act
            Func<Task> act = async () =>
                await TestDeviceClientInvalidServiceCertificateAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)).ConfigureAwait(false);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            error.And.IsTransient.Should().BeTrue();
            error.And.InnerException.Should().BeOfType<MqttCommunicationException>();
        }

        private static async Task TestDeviceClientInvalidServiceCertificateAsync(IotHubClientTransportSettings transportSettings)
        {
            await using var deviceClient =
                new IotHubDeviceClient(
                    TestConfiguration.IotHub.DeviceConnectionStringInvalidServiceCertificate,
                    new IotHubClientOptions(transportSettings)
                    {
                        RetryPolicy = new IotHubClientNoRetry(),
                    });
            var testMessage = new TelemetryMessage();
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.SendTelemetryAsync(testMessage).ConfigureAwait(false);
        }
    }
}
