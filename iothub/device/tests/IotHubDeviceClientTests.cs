// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public sealed class IotHubDeviceClientTests : IDisposable
    {
        private static readonly string fakeHostName = "acme.azure-devices.net";
        private static readonly string fakeDeviceId = "fake";
        private static readonly string fakeSharedAccessKey = "dGVzdFN0cmluZzE=";
        private static readonly string fakeSharedAccessKeyName = "AllAccessKey";
        private static readonly string fakeConnectionString = $"HostName={fakeHostName};SharedAccessKeyName={fakeSharedAccessKeyName};DeviceId={fakeDeviceId};SharedAccessKey={fakeSharedAccessKey}";

        private static readonly IotHubConnectionCredentials s_iotHubConnectionCredentials = new(fakeConnectionString);
#pragma warning disable SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2 s_cert = new();
#pragma warning restore SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2Collection s_certs = new();

        private readonly DirectMethodResponse _directMethodResponseWithPayload = new(200)
        {
            Payload = 123,
        };

        private readonly DirectMethodResponse _directMethodResponseWithEmptyByteArrayPayload = new(200)
        {
            Payload = Array.Empty<byte>()
        };

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_EmptyDeviceId_Throws()
        {
            Action act = () => _ = new ClientAuthenticationWithX509Certificate(s_cert, "");
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_NullDeviceId_Throws()
        {
            Action act = () => _ = new ClientAuthenticationWithX509Certificate(s_cert, null);
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_EmptyModuleId_Throws()
        {
            Action act = () => _ = new ClientAuthenticationWithX509Certificate(s_cert, "device1", "");
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_NullCertificate_Throws()
        {
            Action act = () => _ = new ClientAuthenticationWithX509Certificate(null, "device1");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_NullCertificateChain_Throws()
        {
            Action act = () => _ = new ClientAuthenticationWithX509Certificate(s_cert, certificateChain: null, "device1");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_ChainCertsAmqpWs_Throws()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithX509Certificate(s_cert, s_certs, "fakeDeviceId");
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));

            // act
            Action act = () => _ = new IotHubDeviceClient(hostName, authMethod, options);

            // assert
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_ChainCertsMqtttWs_Throws()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithX509Certificate(s_cert, s_certs, "fakeDeviceId");
            var options = new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket));

            // act
            Action act = () => _ = new IotHubDeviceClient(hostName, authMethod, options);

            // assert
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public async Task DeviceAuthenticationWithX509Certificate_ChainCertsAmqpTcp_DoesNotThrow()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithX509Certificate(s_cert, s_certs, "fakeDeviceId");
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.Tcp));

            // act
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options); };

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task DeviceAuthenticationWithX509Certificate_ChainCertsMqtttTcp_DoesNotThrow()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithX509Certificate(s_cert, s_certs, "fakeDeviceId");
            var options = new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.Tcp));

            // act
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options); };

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_ParamsHostNameAuthMethod_Works()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            // act
            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod);
            
            // assert
            deviceClient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_ParamsHostNameAuthMethodTransportType_Works()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            var transportSettings = new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket);
            var options = new IotHubClientOptions(transportSettings);

            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            // act
            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);

            // assert
            deviceClient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_ParamsHostNameGatewayAuthMethod_Works()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            const string gatewayHostName = "gateway.acme.azure-devices.net";
            var options = new IotHubClientOptions(new IotHubClientMqttSettings()) { GatewayHostName = gatewayHostName };

            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            // act
            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);

            // assert
            deviceClient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_ParamsHostNameGatewayAuthMethodTransport_Works()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            const string gatewayHostName = "gateway.acme.azure-devices.net";
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket))
            {
                GatewayHostName = gatewayHostName,
            };

            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            // act
            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
            
            // assert
            deviceClient.Should().NotBeNull();
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public async Task IotHubDeviceClient_Params_GatewayAuthMethod_Works()
        {
            // arrange
            const string gatewayHostname = "myGatewayDevice";
            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            // act
            await using var deviceClient = new IotHubDeviceClient(gatewayHostname, authMethod);
        
            // assert
            deviceClient.Should().NotBeNull();
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public async Task IotHubDeviceClient_ParamsGatewayAuthMethodTransport_ws_Works()
        {
            // arrange
            const string gatewayHostname = "myGatewayDevice";
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));
            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            // act
            await using var deviceClient = new IotHubDeviceClient(gatewayHostname, authMethod, options);

            // assert
            deviceClient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_ParamsGatewayAuthMethodTransport_tcp_Works()
        {
            // arrange
            const string gatewayHostname = "myGatewayDevice";
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings());
            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            // act
            await using var deviceClient = new IotHubDeviceClient(gatewayHostname, authMethod, options);

            // assert
            deviceClient.Should().NotBeNull();
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateFromConnectionString_WithModuleIdThrows()
        {
            Action act = () => _ = new IotHubDeviceClient("HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=;ModuleId=mod1");
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_GetFileUploadSasUri_NullParam_ThrowsAsync()
        {
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            Action act = () => _ = deviceClient.GetFileUploadSasUriAsync(null).ConfigureAwait(false);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_GetFileUploadSasUri_NullThrows()
        {
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            Action act = () => _ = deviceClient.GetFileUploadSasUriAsync(new FileUploadSasUriRequest(null)).ConfigureAwait(false);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_GetFileUploadSasUri_EmptyThrows()
        {
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            Action act = () => _ = deviceClient.GetFileUploadSasUriAsync(new FileUploadSasUriRequest("")).ConfigureAwait(false);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_Verify_FileUploadSasUriRequest()
        {
            // arrange and act
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var request = new FileUploadSasUriResponse()
            {
                CorrelationId = "123",
                HostName = fakeHostName,
                ContainerName = "container",
                BlobName = "blob",
                SasToken = "token",
            };

            // assert
            request.CorrelationId.Should().Be("123");
            request.HostName.Should().Be(fakeHostName);
            request.ContainerName.Should().Be("container");
            request.BlobName.Should().Be("blob");
            request.SasToken.Should().Be("token");
            request.GetBlobUri().Should().Be("https://acme.azure-devices.net/container/blobtoken");
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CompleteFileUpload_NullParam_Throws()
        {
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            Action act = () => _ = deviceClient.CompleteFileUploadAsync(null).ConfigureAwait(false);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CompleteFileUpload_NullThrows()
        {
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            Action act = () => _ = deviceClient.CompleteFileUploadAsync(new FileUploadCompletionNotification(null, true)).ConfigureAwait(false);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CompleteFileUpload_EmptyThrows()
        {
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            Action act = () => _ = deviceClient.CompleteFileUploadAsync(new FileUploadCompletionNotification("", true)).ConfigureAwait(false);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_Verify_FileUploadCompletionNotification()
        {
            // arrange and act
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var notification = new FileUploadCompletionNotification("123", true)
            {
                StatusCode = 200,
                StatusDescription = "OK",
            };

            // assert
            notification.CorrelationId.Should().Be("123");
            notification.IsSuccess.Should().BeTrue();
            notification.StatusCode.Should().Be(200);
            notification.StatusDescription.Should().Be("OK");

        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_Unsubscribe()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;

            // act
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) => Task.FromResult(_directMethodResponseWithPayload))
                .ConfigureAwait(false);

            await deviceClient
                .SetDirectMethodCallbackAsync(null)
                .ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.EnableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);

            innerHandler.Verify(
                x => x.DisableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_NullMethodRest()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;

            bool isMethodHandlerCalled = false;
            bool actualMethodBody = false;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        bool methodReceived = payload.TryGetPayload(out actualMethodBody);
                        return Task.FromResult(_directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            // act
            await deviceClient.OnMethodCalledAsync(null).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.Never);
            isMethodHandlerCalled.Should().BeFalse();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_NullPayload()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;

            bool isMethodHandlerCalled = false;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        string methodReceived = payload.GetPayloadAsJsonString();
                        return Task.FromResult(_directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);
            var DirectMethodRequest = new DirectMethodRequest("TestMethodName");

            // act
            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.Once);
            isMethodHandlerCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasEmptyBody()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;

            bool isMethodHandlerCalled = false;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        return Task.FromResult(_directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            var DirectMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                PayloadConvention = DefaultPayloadConvention.Instance,
                RequestId = "request",
                JsonPayload = new JRaw("Json"),
                ResponseTimeout = TimeSpan.FromSeconds(5),
                ConnectionTimeout = TimeSpan.FromSeconds(5),
            };

            // act
            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            innerHandler.Verify(
                x => x.DisableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
            isMethodHandlerCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;
            bool isMethodHandlerCalled = false;
            bool actualMethodBody = false;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        bool methodReceived = payload.TryGetPayload(out actualMethodBody);
                        var connectionTimeout = payload.ConnectionTimeoutInSeconds;
                        var responseTimeout = payload.ResponseTimeoutInSeconds;
                        return Task.FromResult(_directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            var payload = new CustomDirectMethodPayload { Grade = "good" };
            var DirectMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)),
                PayloadConvention = DefaultPayloadConvention.Instance,
                ResponseTimeout = TimeSpan.FromSeconds(1),
                ConnectionTimeout = TimeSpan.FromSeconds(1),
            };

            // act
            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            isMethodHandlerCalled.Should().BeTrue();
            DirectMethodRequest.GetPayloadAsJsonString().Should().BeEquivalentTo(JsonConvert.SerializeObject(payload));
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson_InvalidTimeout()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;
            bool isMethodHandlerCalled = false;
            bool actualMethodBody = false;
            int? connectionTimeout = 0;
            int? responseTimeout = 0;

            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        bool methodReceived = payload.TryGetPayload(out actualMethodBody);
                        connectionTimeout = payload.ConnectionTimeoutInSeconds;
                        responseTimeout = payload.ResponseTimeoutInSeconds;
                        return Task.FromResult(_directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            var payload = new CustomDirectMethodPayload { Grade = "good" };
            var DirectMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)),
                PayloadConvention = DefaultPayloadConvention.Instance,
                ResponseTimeout = TimeSpan.FromSeconds(-1),
                ConnectionTimeout = TimeSpan.FromSeconds(-1),
            };

            // act
            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            isMethodHandlerCalled.Should().BeTrue();
            connectionTimeout.Should().BeNull();
            responseTimeout.Should().BeNull();
            DirectMethodRequest.GetPayloadAsJsonString().Should().BeEquivalentTo(JsonConvert.SerializeObject(payload));
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_StringPayload()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;
            bool isMethodHandlerCalled = false;
            bool responseReceivedAsExpected = false;
            string response = null;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        responseReceivedAsExpected = payload.TryGetPayload(out response);
                        var connectionTimeout = payload.ConnectionTimeoutInSeconds;
                        var responseTimeout = payload.ResponseTimeoutInSeconds;
                        return Task.FromResult(_directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            const string payload = "test";
            var directMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            isMethodHandlerCalled.Should().BeTrue();
            responseReceivedAsExpected.Should().BeTrue();
            response.Should().BeEquivalentTo(payload);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_BooleanPayload()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;
            bool isMethodHandlerCalled = false;
            bool responseReceivedAsExpected = false;
            bool response = false;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        responseReceivedAsExpected = payload.TryGetPayload(out response);
                        return Task.FromResult(_directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            bool boolean = true;
            var directMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(boolean)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            isMethodHandlerCalled.Should().BeTrue();
            responseReceivedAsExpected.Should().BeTrue();
            response.Should().Be(boolean);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_ArrayPayload()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;
            bool isMethodHandlerCalled = false;
            bool responseReceivedAsExpected = false;
            byte[] response = null;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        responseReceivedAsExpected = payload.TryGetPayload(out response);
                        return Task.FromResult(_directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            byte[] bytes = new byte[] { 1, 2, 3 };
            var directMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bytes)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            isMethodHandlerCalled.Should().BeTrue();
            responseReceivedAsExpected.Should().BeTrue();
            response.Should().BeEquivalentTo(bytes);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_ListPayload()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;
            bool isMethodHandlerCalled = false;
            bool responseReceivedAsExpected = false;
            List<double> response = null;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        responseReceivedAsExpected = payload.TryGetPayload(out response);
                        return Task.FromResult(_directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            var list = new List<double>() { 1.0, 2.0, 3.0 };
            var directMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(list)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            isMethodHandlerCalled.Should().BeTrue();
            responseReceivedAsExpected.Should().BeTrue();
            response.Should().BeEquivalentTo(list);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_DictionaryPayload()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;
            bool isMethodHandlerCalled = false;
            bool responseReceivedAsExpected = false;
            Dictionary<string, object> response = null;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        responseReceivedAsExpected = payload.TryGetPayload(out response);
                        return Task.FromResult(_directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            var map = new Dictionary<string, object>() { { "key1", "val1" }, { "key2", 2 } };
            var directMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(map)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            isMethodHandlerCalled.Should().BeTrue();
            responseReceivedAsExpected.Should().BeTrue();
            response.Should().BeEquivalentTo(map);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasCustomPayloadResult()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;
            bool isMethodHandlerCalled = false;
            DirectMethodResponse response = new DirectMethodResponse(200)
            {
                Payload = true,
                PayloadConvention = DefaultPayloadConvention.Instance,
            };
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        return Task.FromResult(response);
                    })
                .ConfigureAwait(false);

            var payload = new CustomDirectMethodPayload { Grade = "good" };
            var directMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)),
                PayloadConvention = DefaultPayloadConvention.Instance,
                RequestId = "1",
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            isMethodHandlerCalled.Should().BeTrue();
            response.Status.Should().Be(200);
            response.Payload.Should().Be(true);
            response.RequestId.Should().Be("1");
            response.PayloadConvention.Should().Be(DefaultPayloadConvention.Instance);
            response.GetPayloadObjectBytes().Should().NotBeNull();
            response.Payload = null;
            response.GetPayloadObjectBytes().Should().BeNull();

        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalledNoMethodHandler()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;

            var payload = new CustomDirectMethodPayload { Grade = "good" };
            var directMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerOverwriteExistingDelegate()
        {
            // arrange
            const string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            await using var deviceClient = new IotHubDeviceClient(connectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            CustomDirectMethodPayload actualMethodBody = null;
            Func<DirectMethodRequest, Task<DirectMethodResponse>> methodCallback = (methodRequest) =>
            {
                actualMethodName = methodRequest.MethodName;
                bool methodReceived = methodRequest.TryGetPayload(out actualMethodBody);
                methodCallbackCalled = true;
                return Task.FromResult(_directMethodResponseWithEmptyByteArrayPayload);
            };

            const string methodName = "TestMethodName";
            var methodBody = new CustomDirectMethodPayload { Grade = "good" };
            await deviceClient.SetDirectMethodCallbackAsync(methodCallback).ConfigureAwait(false);
            var directMethodRequest = new DirectMethodRequest(methodName)
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(methodBody)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.EnableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            methodCallbackCalled.Should().BeTrue();
            actualMethodName.Should().Be(methodName);
            actualMethodBody.Should().BeEquivalentTo(methodBody);

            // arrange
            bool methodCallbackCalled2 = false;
            string actualMethodName2 = string.Empty;
            CustomDirectMethodPayload actualMethodBody2 = null;
            Func<DirectMethodRequest, Task<DirectMethodResponse>> methodCallback2 = (methodRequest) =>
            {
                actualMethodName2 = methodRequest.MethodName;
                bool methodReceived = methodRequest.TryGetPayload(out actualMethodBody2);
                methodCallbackCalled2 = true;
                return Task.FromResult(_directMethodResponseWithEmptyByteArrayPayload);
            };

            var methodBody2 = new CustomDirectMethodPayload { Grade = "bad" };
            await deviceClient.SetDirectMethodCallbackAsync(methodCallback2).ConfigureAwait(false);
            directMethodRequest = new DirectMethodRequest(methodName)
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(methodBody2)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.EnableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            methodCallbackCalled2.Should().BeTrue();
            actualMethodName2.Should().Be(methodName);
            actualMethodBody2.Should().BeEquivalentTo(methodBody2);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerUnsetLastMethodHandler()
        {
            // arrange
            const string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            await using var deviceClient = new IotHubDeviceClient(connectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            CustomDirectMethodPayload actualMethodBody = null;
            Func<DirectMethodRequest, Task<DirectMethodResponse>> methodCallback = (methodRequest) =>
            {
                actualMethodName = methodRequest.MethodName;
                bool methodReceived = methodRequest.TryGetPayload(out actualMethodBody);
                methodCallbackCalled = true;
                return Task.FromResult(_directMethodResponseWithEmptyByteArrayPayload);
            };

            const string methodName = "TestMethodName";
            var methodBody = new CustomDirectMethodPayload { Grade = "good" };
            await deviceClient.SetDirectMethodCallbackAsync(methodCallback).ConfigureAwait(false);
            var directMethodRequest = new DirectMethodRequest(methodName)
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(methodBody)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.EnableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);

            methodCallbackCalled.Should().BeTrue();
            actualMethodName.Should().Be(methodName);
            actualMethodBody.Should().BeEquivalentTo(methodBody);

            // arrange
            methodCallbackCalled = false;
            await deviceClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);
            directMethodRequest = new DirectMethodRequest(methodName)
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(methodBody)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.DisableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            methodCallbackCalled.Should().BeFalse();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerUnsetWhenNoMethodHandler()
        {
            // arrange
            const string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            await using var deviceClient = new IotHubDeviceClient(connectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;

            // act
            await deviceClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);
            
            // assert
            innerHandler.Verify(
                x => x.DisableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnConnectionOpenedInvokeHandlerForStatusChange()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            bool handlerCalled = false;
            var connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> statusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionStatusInfo = c;
            };
            deviceClient.ConnectionStatusChangeCallback = statusChangeHandler;

            // act
            // Connection status change from disconnected to connected
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            // assert
            handlerCalled.Should().BeTrue();
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.Connected);
            connectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.ConnectionOk);
            connectionStatusInfo.StatusLastChangedOnUtc.Should().NotBe(null);
            connectionStatusInfo.RecommendedAction.Should().Be(RecommendedAction.PerformNormally);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnConnectionOpenedWithNullHandler()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            bool handlerCalled = false;
            var connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> StatusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionStatusInfo = c;
            };
            deviceClient.ConnectionStatusChangeCallback = StatusChangeHandler;
            deviceClient.ConnectionStatusChangeCallback = null;

            // act
            // Connection status change from disconnected to connected
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            // assert
            handlerCalled.Should().BeFalse();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnConnectionOpenedNotInvokeHandlerWithoutStatusChange()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            bool handlerCalled = false;
            var connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> statusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionStatusInfo = c;
            };
            deviceClient.ConnectionStatusChangeCallback = statusChangeHandler;
            // current status = disabled

            // act
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            // assert
            handlerCalled.Should().BeTrue();
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.Connected);
            connectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.ConnectionOk);
            handlerCalled = false;

            // act
            // current status = connected
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            // assert
            handlerCalled.Should().BeFalse();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnConnectionClosedInvokeHandlerAndRecoveryForStatusChange()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;
            var sender = new object();
            bool handlerCalled = false;
            var connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> statusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionStatusInfo = c;
            };
            deviceClient.ConnectionStatusChangeCallback = statusChangeHandler;

            // act
            // current status = disabled
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));
            
            // assert
            handlerCalled.Should().BeTrue();
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.Connected);
            connectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.ConnectionOk);

            handlerCalled = false;

            // act
            // current status = connected
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.DisconnectedRetrying, ConnectionStatusChangeReason.CommunicationError));

            // assert
            handlerCalled.Should().BeTrue();
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.DisconnectedRetrying);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnConnectionClosed_InvokeHandlerAndQuit()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;
            var sender = new object();
            bool handlerCalled = false;
            ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> StatusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionStatusInfo = c;
            };
            deviceClient.ConnectionStatusChangeCallback = StatusChangeHandler;

            // act
            // current status = disabled
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));
            
            // assert
            handlerCalled.Should().BeTrue();
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.Connected);
            connectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.ConnectionOk);

            // act
            handlerCalled = false;
            // current status = connected
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Closed, ConnectionStatusChangeReason.ClientClosed));

            // assert
            handlerCalled.Should().BeTrue();
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.Closed);
        }

        [TestMethod]
        public async Task MessageIdDefaultNotSet_SendEventDoesNotSetMessageId()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            innerHandler
                .Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler.Object;

            // act
            var messageWithoutId = new TelemetryMessage();
            var messageWithId = new TelemetryMessage
            {
                MessageId = messageId,
            };
            await deviceClient.SendTelemetryAsync(messageWithoutId).ConfigureAwait(false);
            await deviceClient.SendTelemetryAsync(messageWithId).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task MessageIdDefaultSetToNull_SendEventDoesNotSetMessageId()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                SdkAssignsMessageId = SdkAssignsMessageId.Never,
            };
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString, options);

            var innerHandler = new Mock<IDelegatingHandler>();
            innerHandler
                .Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler.Object;

            // act
            var messageWithoutId = new TelemetryMessage();
            var messageWithId = new TelemetryMessage
            {
                MessageId = messageId,
            };
            await deviceClient.SendTelemetryAsync(messageWithoutId).ConfigureAwait(false);
            await deviceClient.SendTelemetryAsync(messageWithId).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task MessageIdDefaultSetToGuid_SendEventSetMessageIdIfNotSet()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString, options);

            var innerHandler = new Mock<IDelegatingHandler>();
            innerHandler
                .Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler.Object;

            // act
            var messageWithoutId = new TelemetryMessage();
            var messageWithId = new TelemetryMessage
            {
                MessageId = messageId,
            };
            await deviceClient.SendTelemetryAsync(messageWithoutId).ConfigureAwait(false);
            await deviceClient.SendTelemetryAsync(messageWithId).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().NotBeNullOrEmpty();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task MessageIdDefaultNotSet_SendEventBatchDoesNotSetMessageId()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            innerHandler
                .Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler.Object;

            // act
            var messageWithoutId = new TelemetryMessage();
            var messageWithId = new TelemetryMessage
            {
                MessageId = messageId,
            };
            await deviceClient.SendTelemetryAsync(new List<TelemetryMessage> { messageWithoutId, messageWithId }).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task MessageIdDefaultSetToNull_SendEventBatchDoesNotSetMessageId()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                SdkAssignsMessageId = SdkAssignsMessageId.Never,
            };
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString, options);

            var innerHandler = new Mock<IDelegatingHandler>();
            innerHandler
                .Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler.Object;

            // act
            var messageWithoutId = new TelemetryMessage();
            var messageWithId = new TelemetryMessage
            {
                MessageId = messageId,
            };
            await deviceClient.SendTelemetryAsync(new List<TelemetryMessage> { messageWithoutId, messageWithId }).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task MessageIdDefaultSetToGuid_SendEventBatchSetMessageIdIfNotSet()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString, options);

            var innerHandler = new Mock<IDelegatingHandler>();
            innerHandler
                .Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler.Object;

            // act
            var messageWithoutId = new TelemetryMessage();
            var messageWithId = new TelemetryMessage
            {
                MessageId = messageId,
            };
            await deviceClient.SendTelemetryAsync(new List<TelemetryMessage> { messageWithoutId, messageWithId }).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().NotBeNullOrEmpty();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateWithConnectionString_InvalidSasTimeToLive_ThrowsException()
        {
            // arrange and act
            Action createDeviceClientAuth = () => _ = new ClientAuthenticationWithSharedAccessKeyRefresh(fakeConnectionString, TimeSpan.FromSeconds(-60));

            // assert
            createDeviceClientAuth.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateWithConnectionString_InvalidSasRenewalBuffer_ThrowsException()
        {
            // arrange and act
            Action createDeviceClientAuth = () => _ = new ClientAuthenticationWithSharedAccessKeyRefresh(fakeConnectionString, sasTokenRenewalBuffer: 200);

            // assert
            createDeviceClientAuth.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CreateWithConnectionString_SasTokenTimeToLiveRenewalConfigurable()
        {
            // arrange
            var sasTokenTimeToLive = TimeSpan.FromMinutes(20);
            int sasTokenRenewalBuffer = 50;
            var auth = new ClientAuthenticationWithSharedAccessKeyRefresh(fakeConnectionString, sasTokenTimeToLive, sasTokenRenewalBuffer);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());

            // act
            DateTime startTime = DateTime.UtcNow;
            await using var deviceClient = new IotHubDeviceClient(fakeHostName, auth, options);

            // assert
            ClientAuthenticationWithTokenRefresh sasTokenRefresher = deviceClient.IotHubConnectionCredentials.SasTokenRefresher;
            sasTokenRefresher.Should().BeAssignableTo<ClientAuthenticationWithSharedAccessKeyRefresh>();

            // The calculation of the sas token expiration will begin once the ClientAuthenticationWithTokenRefresh object has been initialized.
            // Since the initialization is internal to the ClientFactory logic and is not observable, we will allow a buffer period to our assertions.
            var buffer = TimeSpan.FromSeconds(2);

            // The initial expiration time calculated is (current UTC time - sas TTL supplied).
            // The actual expiration time associated with a sas token is recalculated during token generation, but relies on the same sas TTL supplied.

            DateTime expectedExpirationTime = startTime.Add(-sasTokenTimeToLive);
            sasTokenRefresher.ExpiresOnUtc.Should().BeCloseTo(expectedExpirationTime, buffer);

            int expectedBufferSeconds = (int)(sasTokenTimeToLive.TotalSeconds * ((float)sasTokenRenewalBuffer / 100));
            DateTime expectedRefreshTime = expectedExpirationTime.AddSeconds(-expectedBufferSeconds);
            sasTokenRefresher.RefreshesOnUtc.Should().BeCloseTo(expectedRefreshTime, buffer);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CreateFromAuthenticationMethod_SasTokenTimeToLiveRenewalConfigurable()
        {
            // arrange
            var sasTokenTimeToLive = TimeSpan.FromMinutes(20);
            int sasTokenRenewalBuffer = 50;
            var auth = new TestDeviceAuthenticationWithTokenRefresh(sasTokenTimeToLive, sasTokenRenewalBuffer);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());

            // act
            DateTime startTime = DateTime.UtcNow;
            await using var deviceClient = new IotHubDeviceClient(fakeHostName, auth, options);

            // assert
            ClientAuthenticationWithTokenRefresh sasTokenRefresher = deviceClient.IotHubConnectionCredentials.SasTokenRefresher;

            // The calculation of the sas token expiration will begin once the ClientAuthenticationWithTokenRefresh object has been initialized.
            // Since the initialization is internal to the ClientFactory logic and is not observable, we will allow a buffer period to our assertions.
            var buffer = TimeSpan.FromSeconds(2);

            // The initial expiration time calculated is (current UTC time - sas TTL supplied).
            // The actual expiration time associated with a sas token is recalculated during token generation, but relies on the same sas TTL supplied.

            DateTime expectedExpirationTime = startTime.Add(-sasTokenTimeToLive);
            sasTokenRefresher.ExpiresOnUtc.Should().BeCloseTo(expectedExpirationTime, buffer);

            int expectedBufferSeconds = (int)(sasTokenTimeToLive.TotalSeconds * ((float)sasTokenRenewalBuffer / 100));
            DateTime expectedRefreshTime = expectedExpirationTime.AddSeconds(-expectedBufferSeconds);
            sasTokenRefresher.RefreshesOnUtc.Should().BeCloseTo(expectedRefreshTime, buffer);
        }

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public void IotHubDeviceClient_InitWithMqttTransportAndModelId_DoesNotThrow(IotHubClientTransportProtocol protocol)
        {
            IotHubDeviceClient_InitWithNonHttpTransportAndModelId_DoesNotThrow(new IotHubClientMqttSettings(protocol));
        }

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public void IotHubDeviceClient_InitWithAmqpTransportAndModelId_DoesNotThrow(IotHubClientTransportProtocol protocol)
        {
            IotHubDeviceClient_InitWithNonHttpTransportAndModelId_DoesNotThrow(new IotHubClientAmqpSettings(protocol));
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SendTelemetryAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.SendTelemetryAsync(new TelemetryMessage(), ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SendTelemetryBatchAsync_Cancelled_ThrowsOperationCanceledException()
        {
            //arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.SendTelemetryAsync(new List<TelemetryMessage> { new TelemetryMessage(), new TelemetryMessage() }, ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SendTelemetryAsync_WithoutExplicitOpenAsync_ThrowsInvalidOperationException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            // act
            Func<Task> act = async () => await deviceClient.SendTelemetryAsync(new TelemetryMessage());

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SendTelemetryBatchAsync_WithoutExplicitOpenAsync_ThrowsInvalidOperationException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            // act
            Func<Task> act = async () => await deviceClient.SendTelemetryAsync(new List<TelemetryMessage> { new TelemetryMessage(), new TelemetryMessage() });

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_GetFileUploadSasUriAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);
            var request = new FileUploadSasUriRequest("fileName");
            
            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.GetFileUploadSasUriAsync(request, ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            request.BlobName.Should().Be("fileName");
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CompleteFileUploadAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.CompleteFileUploadAsync(new FileUploadCompletionNotification("complete", true), ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OpenAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.OpenAsync(ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_UpdateReportedPropertiesAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.UpdateReportedPropertiesAsync(new ReportedProperties(), ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_GetTwinAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.GetTwinPropertiesAsync(ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CloseAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.CloseAsync(ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetDirectMethodCallbackAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.SetDirectMethodCallbackAsync(null, ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetDesiredPropertyCallbackAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(fakeConnectionString);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.SetDesiredPropertyUpdateCallbackAsync((patch) => Task.FromResult(true), ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        private static void IotHubDeviceClient_InitWithNonHttpTransportAndModelId_DoesNotThrow(IotHubClientTransportSettings transportSettings)
        {
            // arrange

            var clientOptions = new IotHubClientOptions(transportSettings)
            {
                ModelId = "dtmi:com:example:testModel;1",
            };

            // act
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(fakeConnectionString, clientOptions); };

            // assert
            act.Should().NotThrowAsync();
        }

        public void Dispose()
        {
            s_cert.Dispose();
        }

        private class TestDeviceAuthenticationWithTokenRefresh : ClientAuthenticationWithTokenRefresh
        {
            // This authentication method relies on the default sas token time to live and renewal buffer set by the SDK.
            public TestDeviceAuthenticationWithTokenRefresh(TimeSpan ttl, int refreshBuffer)
                : base(deviceId: "someTestDevice", suggestedTimeToLive: ttl, timeBufferPercentage: refreshBuffer)
            {
            }

            ///<inheritdoc/>
            protected override Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive)
            {
                return Task.FromResult<string>("someToken");
            }
        }

        private class CustomDirectMethodPayload
        {
            [JsonProperty("grade")]
            public string Grade { get; set; }
        }
    }
}
