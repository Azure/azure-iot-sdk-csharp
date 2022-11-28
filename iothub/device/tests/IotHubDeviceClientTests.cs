// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubDeviceClientTests : IDisposable
    {
        private const string FakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
        private const string FakeHostName = "acme.azure-devices.net";

        private static readonly IotHubConnectionCredentials s_iotHubConnectionCredentials = new(FakeConnectionString);
#pragma warning disable SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2 s_cert = new();
#pragma warning restore SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2Collection s_certs = new();

        private DirectMethodResponse _directMethodResponseWithPayload = new(200)
        {
            Payload = 123,
        };

        private DirectMethodResponse _directMethodResponseWithEmptyByteArrayPayload = new(200)
        {
            Payload = new byte[0]
        };

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_NullCertificate_Throws()
        {
            Action act = () => new ClientAuthenticationWithX509Certificate(null, "device1");

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_NullCertificateChain_Throws()
        {            Action act = () => new ClientAuthenticationWithX509Certificate(s_cert, certificateChain: null, "device1");

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationWithX509Certificate_ChainCertsAmqpWs_Throws()
        {
            // arrange
            string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithX509Certificate(s_cert, s_certs, "fakeDeviceId");
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));

            // act
            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceAuthenticationWithX509Certificate_ChainCertsMqtttWs_Throws()
        {
            // arrange
            string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithX509Certificate(s_cert, s_certs, "fakeDeviceId");
            var options = new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket));

            // act
            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        [TestMethod]
        public async Task DeviceAuthenticationWithX509Certificate_ChainCertsAmqpTcp_DoesNotThrow()
        {
            // arrange
            string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithX509Certificate(s_cert, s_certs, "fakeDeviceId");
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.Tcp));

            // act
            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);

            // should not throw
        }

        [TestMethod]
        public async Task DeviceAuthenticationWithX509Certificate_ChainCertsMqtttTcp_DoesNotThrow()
        {
            // arrange
            string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithX509Certificate(s_cert, s_certs, "fakeDeviceId");
            var options = new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.Tcp));

            // act
            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);

            // should not throw
        }

        [TestMethod]
        public async Task IotHubDeviceClient_ParamsHostNameAuthMethod_Works()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_ParamsHostNameAuthMethodTransportType_Works()
        {
            string hostName = "acme.azure-devices.net";
            var transportSettings = new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket);
            var options = new IotHubClientOptions(transportSettings);

            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_ParamsHostNameGatewayAuthMethod_Works()
        {
            string hostName = "acme.azure-devices.net";
            string gatewayHostName = "gateway.acme.azure-devices.net";
            var options = new IotHubClientOptions(new IotHubClientMqttSettings()) { GatewayHostName = gatewayHostName };

            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_ParamsHostNameGatewayAuthMethodTransport_Works()
        {
            string hostName = "acme.azure-devices.net";
            string gatewayHostName = "gateway.acme.azure-devices.net";
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket))
            {
                GatewayHostName = gatewayHostName,
            };

            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public async Task IotHubDeviceClient_Params_GatewayAuthMethod_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            await using var deviceClient = new IotHubDeviceClient(gatewayHostname, authMethod);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public async Task IotHubDeviceClient_ParamsGatewayAuthMethodTransport_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));
            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            await using var deviceClient = new IotHubDeviceClient(
                gatewayHostname,
                authMethod,
                options);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public async Task IotHubDeviceClient_ParamsGatewayAuthMethodTransportArray_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));
            var authMethod = new ClientAuthenticationWithSharedAccessKeyRefresh(
                sharedAccessKey: s_iotHubConnectionCredentials.SharedAccessKey,
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName,
                deviceId: "device1");

            await using var deviceClient = new IotHubDeviceClient(
                gatewayHostname,
                authMethod,
                options);
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateFromConnectionString_WithModuleIdThrows()
        {
            Action act = () => new IotHubDeviceClient("HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=;ModuleId=mod1");
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_Unsubscribe()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
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
                x => x.DisableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_NullMethodRest()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
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

            // act
            await deviceClient.OnMethodCalledAsync(null).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.Never);
            isMethodHandlerCalled.Should().BeFalse();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasEmptyBody()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
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

            var DirectMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            isMethodHandlerCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
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

            var payload = new CustomDirectMethodPayload { Grade = "good" };
            var DirectMethodRequest = new DirectMethodRequest(payload)
            {
                MethodName = "TestMethodName",
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            isMethodHandlerCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_StringPayload()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
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
                        return Task.FromResult(_directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            string payload = "test";
            var directMethodRequest = new DirectMethodRequest(payload)
            {
                MethodName = "TestMethodName",
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
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
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
            var directMethodRequest = new DirectMethodRequest(boolean)
            {
                MethodName = "TestMethodName",
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
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
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
            var directMethodRequest = new DirectMethodRequest(bytes)
            {
                MethodName = "TestMethodName",
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
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
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

            List<double> list = new List<double>() { 1.0, 2.0, 3.0 };
            var directMethodRequest = new DirectMethodRequest(list)
            {
                MethodName = "TestMethodName",
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
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
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
            var directMethodRequest = new DirectMethodRequest(map)
            {
                MethodName = "TestMethodName",
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
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_NoPayloadResult()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;
            bool isMethodHandlerCalled = false;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        return Task.FromResult(new DirectMethodResponse(200));
                    })
                .ConfigureAwait(false);

            var payload = new CustomDirectMethodPayload { Grade = "good" };
            var directMethodRequest = new DirectMethodRequest(payload)
            {
                MethodName = "TestMethodName",
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            isMethodHandlerCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalledNoMethodHandler()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;

            var payload = new CustomDirectMethodPayload { Grade = "good" };
            var directMethodRequest = new DirectMethodRequest(payload)
            {
                MethodName = "TestMethodName",
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
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
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

            string methodName = "TestMethodName";
            var methodBody = new CustomDirectMethodPayload { Grade = "good" };
            await deviceClient.SetDirectMethodCallbackAsync(methodCallback).ConfigureAwait(false);
            var directMethodRequest = new DirectMethodRequest(methodBody)
            {
                MethodName = methodName,
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.EnableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            methodCallbackCalled.Should().BeTrue();
            methodName.Should().Be(actualMethodName);
            methodBody.Should().BeEquivalentTo(actualMethodBody);

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
            directMethodRequest = new DirectMethodRequest(methodBody2)
            {
                MethodName = methodName,
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.EnableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            methodCallbackCalled2.Should().BeTrue();
            methodName.Should().Be(actualMethodName2);
            methodBody2.Should().BeEquivalentTo(actualMethodBody2);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerUnsetLastMethodHandler()
        {
            // arrange
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
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

            string methodName = "TestMethodName";
            var methodBody = new CustomDirectMethodPayload { Grade = "good" };
            await deviceClient.SetDirectMethodCallbackAsync(methodCallback).ConfigureAwait(false);
            var directMethodRequest = new DirectMethodRequest(methodBody)
            {
                MethodName = methodName,
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.EnableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);

            methodCallbackCalled.Should().BeTrue();
            methodName.Should().Be(actualMethodName);
            methodBody.Should().BeEquivalentTo(actualMethodBody);

            // arrange
            methodCallbackCalled = false;
            await deviceClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);
            directMethodRequest = new DirectMethodRequest(methodBody)
            {
                MethodName = methodName,
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
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            await using var deviceClient = new IotHubDeviceClient(connectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler.Object;

            await deviceClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);
            innerHandler.Verify(
                x => x.DisableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnConnectionOpenedInvokeHandlerForStatusChange()
        {
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            bool handlerCalled = false;
            var connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> StatusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionStatusInfo = c;
            };
            deviceClient.ConnectionStatusChangeCallback = StatusChangeHandler;

            // Connection status change from disconnected to connected
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            handlerCalled.Should().BeTrue();
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.Connected);
            connectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.ConnectionOk);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnConnectionOpenedWithNullHandler()
        {
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            bool handlerCalled = false;
            var connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> StatusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionStatusInfo = c;
            };
            deviceClient.ConnectionStatusChangeCallback = StatusChangeHandler;
            deviceClient.ConnectionStatusChangeCallback = null;

            // Connection status change from disconnected to connected
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            handlerCalled.Should().BeFalse();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnConnectionOpenedNotInvokeHandlerWithoutStatusChange()
        {
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            bool handlerCalled = false;
            var connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> StatusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionStatusInfo = c;
            };
            deviceClient.ConnectionStatusChangeCallback = StatusChangeHandler;
            // current status = disabled

            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            handlerCalled.Should().BeTrue();
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.Connected);
            connectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.ConnectionOk);
            handlerCalled = false;

            // current status = connected
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            handlerCalled.Should().BeFalse();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnConnectionClosedInvokeHandlerAndRecoveryForStatusChange()
        {
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
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

            // current status = disabled
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));
            handlerCalled.Should().BeTrue();
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.Connected);
            connectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.ConnectionOk);

            handlerCalled = false;

            // current status = connected
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.DisconnectedRetrying, ConnectionStatusChangeReason.CommunicationError));

            handlerCalled.Should().BeTrue();
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.DisconnectedRetrying);
        }

        [TestMethod]
        public async Task MessageIdDefaultNotSet_SendEventDoesNotSetMessageId()
        {
            // arrange
            var messageId = Guid.NewGuid().ToString();
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

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
            var messageId = Guid.NewGuid().ToString();
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                SdkAssignsMessageId = SdkAssignsMessageId.Never,
            };
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString, options);

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
            var messageId = Guid.NewGuid().ToString();
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString, options);

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
            var messageId = Guid.NewGuid().ToString();
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

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

            await deviceClient.SendTelemetryBatchAsync(new List<TelemetryMessage> { messageWithoutId, messageWithId }).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task MessageIdDefaultSetToNull_SendEventBatchDoesNotSetMessageId()
        {
            // arrange
            var messageId = Guid.NewGuid().ToString();
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                SdkAssignsMessageId = SdkAssignsMessageId.Never,
            };
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString, options);

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

            await deviceClient.SendTelemetryBatchAsync(new List<TelemetryMessage> { messageWithoutId, messageWithId }).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task MessageIdDefaultSetToGuid_SendEventBatchSetMessageIdIfNotSet()
        {
            // arrange
            var messageId = Guid.NewGuid().ToString();
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString, options);

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

            await deviceClient.SendTelemetryBatchAsync(new List<TelemetryMessage> { messageWithoutId, messageWithId }).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().NotBeNullOrEmpty();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateWithConnectionString_InvalidSasTimeToLive_ThrowsException()
        {
            // arrange
            // act
            Action createDeviceClientAuth = () => { new ClientAuthenticationWithSharedAccessKeyRefresh(FakeConnectionString, TimeSpan.FromSeconds(-60)); };

            // assert
            createDeviceClientAuth.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateWithConnectionString_InvalidSasRenewalBuffer_ThrowsException()
        {
            // arrange
            // act
            Action createDeviceClientAuth = () => { new ClientAuthenticationWithSharedAccessKeyRefresh(FakeConnectionString, sasTokenRenewalBuffer: 200); };

            // assert
            createDeviceClientAuth.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CreateWithConnectionString_SasTokenTimeToLiveRenewalConfigurable()
        {
            // arrange
            var sasTokenTimeToLive = TimeSpan.FromMinutes(20);
            int sasTokenRenewalBuffer = 50;
            var auth = new ClientAuthenticationWithSharedAccessKeyRefresh(FakeConnectionString, sasTokenTimeToLive, sasTokenRenewalBuffer);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());

            // act
            DateTime startTime = DateTime.UtcNow;
            await using var deviceClient = new IotHubDeviceClient(FakeHostName, auth, options);

            // assert
            var sasTokenRefresher = deviceClient.IotHubConnectionCredentials.SasTokenRefresher;
            sasTokenRefresher.Should().BeAssignableTo<ClientAuthenticationWithSharedAccessKeyRefresh>();

            // The calculation of the sas token expiration will begin once the ClientAuthenticationWithTokenRefresh object has been initialized.
            // Since the initialization is internal to the ClientFactory logic and is not observable, we will allow a buffer period to our assertions.
            var buffer = TimeSpan.FromSeconds(2);

            // The initial expiration time calculated is (current UTC time - sas TTL supplied).
            // The actual expiration time associated with a sas token is recalculated during token generation, but relies on the same sas TTL supplied.

            var expectedExpirationTime = startTime.Add(-sasTokenTimeToLive);
            sasTokenRefresher.ExpiresOnUtc.Should().BeCloseTo(expectedExpirationTime, buffer);

            int expectedBufferSeconds = (int)(sasTokenTimeToLive.TotalSeconds * ((float)sasTokenRenewalBuffer / 100));
            var expectedRefreshTime = expectedExpirationTime.AddSeconds(-expectedBufferSeconds);
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
            await using var deviceClient = new IotHubDeviceClient(FakeHostName, auth, options);

            // assert
            var sasTokenRefresher = deviceClient.IotHubConnectionCredentials.SasTokenRefresher;

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
            //arrange
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.SendTelemetryAsync(new TelemetryMessage(), ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SendTelemetryAsync_WithoutExplicitOpenAsync_ThrowsInvalidOperationException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            // act
            Func<Task> act = async () => await deviceClient.SendTelemetryAsync(new TelemetryMessage());

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OpenAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

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
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

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
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

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
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.CloseAsync(ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetDesiredPropertyCallbackAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            await using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await deviceClient.SetDesiredPropertyUpdateCallbackAsync((patch) => Task.FromResult(true), ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        private void IotHubDeviceClient_InitWithNonHttpTransportAndModelId_DoesNotThrow(IotHubClientTransportSettings transportSettings)
        {
            // arrange

            var clientOptions = new IotHubClientOptions(transportSettings)
            {
                ModelId = "dtmi:com:example:testModel;1",
            };

            // act
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(FakeConnectionString, clientOptions); };

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
            [JsonPropertyName("grade")]
            public string Grade { get; set; }
        }
    }
}
