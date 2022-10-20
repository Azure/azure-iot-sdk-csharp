// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubDeviceClientTests
    {
        private const string FakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
        private const string FakeConnectionStringWithModuleId = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=;ModuleId=mod1";
        private const string TestModelId = "dtmi:com:example:testModel;1";
        private const string FakeHostName = "acme.azure-devices.net";

        private const int DefaultSasRenewalBufferPercentage = 15;
        private static readonly TimeSpan s_defaultSasTimeToLive = TimeSpan.FromHours(1);

        private static readonly IotHubConnectionCredentials s_iotHubConnectionCredentials = new(FakeConnectionString);

        private DirectMethodResponse directMethodResponseWithPayload = new DirectMethodResponse(200)
        {
            Payload = 123,
        };

        private DirectMethodResponse directMethodResponseWithNoPayload = new DirectMethodResponse(200);

        private DirectMethodResponse directMethodResponseWithEmptyByteArrayPayload = new DirectMethodResponse(200)
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
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceAuthenticationWithX509Certificate_ChainCertsAmqpWs_Throws()
        {
            // arrange
            string hostName = "acme.azure-devices.net";
#pragma warning disable SYSLIB0026 // Type or member is obsolete
            using var cert = new X509Certificate2();
#pragma warning restore SYSLIB0026 // Type or member is obsolete
            var certs = new X509Certificate2Collection();
            var authMethod = new ClientAuthenticationWithX509Certificate(cert, "fakeDeviceId", chainCertificates: certs);
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));

            // act
            using var dc = new IotHubDeviceClient(hostName, authMethod, options);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceAuthenticationWithX509Certificate_ChainCertsMqtttWs_Throws()
        {
            // arrange
            string hostName = "acme.azure-devices.net";
#pragma warning disable SYSLIB0026 // Type or member is obsolete
            using var cert = new X509Certificate2();
#pragma warning restore SYSLIB0026 // Type or member is obsolete
            var certs = new X509Certificate2Collection();
            var authMethod = new ClientAuthenticationWithX509Certificate(cert, "fakeDeviceId", chainCertificates: certs);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket));

            // act
            using var dc = new IotHubDeviceClient(hostName, authMethod, options);
        }

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_ChainCertsAmqpTcp_DoesNotThrow()
        {
            // arrange
            string hostName = "acme.azure-devices.net";
#pragma warning disable SYSLIB0026 // Type or member is obsolete
            using var cert = new X509Certificate2();
#pragma warning restore SYSLIB0026 // Type or member is obsolete
            var certs = new X509Certificate2Collection();
            var authMethod = new ClientAuthenticationWithX509Certificate(cert, "fakeDeviceId", chainCertificates: certs);
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.Tcp));

            // act
            using var dc = new IotHubDeviceClient(hostName, authMethod, options);

            // should not throw
        }

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_ChainCertsMqtttTcp_DoesNotThrow()
        {
            // arrange
            string hostName = "acme.azure-devices.net";
#pragma warning disable SYSLIB0026 // Type or member is obsolete
            using var cert = new X509Certificate2();
#pragma warning restore SYSLIB0026 // Type or member is obsolete
            var certs = new X509Certificate2Collection();
            var authMethod = new ClientAuthenticationWithX509Certificate(cert, "fakeDeviceId", chainCertificates: certs);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.Tcp));

            // act
            using var dc = new IotHubDeviceClient(hostName, authMethod, options);

            // should not throw
        }

        [TestMethod]
        public void IotHubDeviceClient_ParamsHostNameAuthMethod_Works()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithSakRefresh(
                s_iotHubConnectionCredentials.SharedAccessKey,
                "device1",
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName);

            using var deviceClient = new IotHubDeviceClient(hostName, authMethod);
        }

        [TestMethod]
        public void IotHubDeviceClient_ParamsHostNameAuthMethodTransportType_Works()
        {
            string hostName = "acme.azure-devices.net";
            var transportSettings = new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket);
            var options = new IotHubClientOptions(transportSettings);

            var authMethod = new ClientAuthenticationWithSakRefresh(
                s_iotHubConnectionCredentials.SharedAccessKey,
                "device1",
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName);

            using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        [TestMethod]
        public void IotHubDeviceClient_ParamsHostNameGatewayAuthMethod_Works()
        {
            string hostName = "acme.azure-devices.net";
            string gatewayHostName = "gateway.acme.azure-devices.net";
            var options = new IotHubClientOptions(new IotHubClientMqttSettings()) { GatewayHostName = gatewayHostName };

            var authMethod = new ClientAuthenticationWithSakRefresh(
                s_iotHubConnectionCredentials.SharedAccessKey,
                "device1",
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName);

            using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        [TestMethod]
        public void IotHubDeviceClient_ParamsHostNameGatewayAuthMethodTransport_Works()
        {
            string hostName = "acme.azure-devices.net";
            string gatewayHostName = "gateway.acme.azure-devices.net";
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket))
            {
                GatewayHostName = gatewayHostName,
            };

            var authMethod = new ClientAuthenticationWithSakRefresh(
                s_iotHubConnectionCredentials.SharedAccessKey,
                "device1",
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName);

            using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void IotHubDeviceClient_Params_GatewayAuthMethod_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var authMethod = new ClientAuthenticationWithSakRefresh(
                s_iotHubConnectionCredentials.SharedAccessKey,
                "device1",
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName);

            using var deviceClient = new IotHubDeviceClient(gatewayHostname, authMethod);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void IotHubDeviceClient_ParamsGatewayAuthMethodTransport_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));
            var authMethod = new ClientAuthenticationWithSakRefresh(
                s_iotHubConnectionCredentials.SharedAccessKey,
                "device1",
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName);

            using var deviceClient = new IotHubDeviceClient(
                gatewayHostname,
                authMethod,
                options);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void IotHubDeviceClient_ParamsGatewayAuthMethodTransportArray_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));
            var authMethod = new ClientAuthenticationWithSakRefresh(
                s_iotHubConnectionCredentials.SharedAccessKey,
                "device1",
                sharedAccessKeyName: s_iotHubConnectionCredentials.SharedAccessKeyName);

            using var deviceClient = new IotHubDeviceClient(
                gatewayHostname,
                authMethod,
                options);
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateFromConnectionString_WithModuleIdThrows()
        {
            Action act = () => new IotHubDeviceClient(FakeConnectionStringWithModuleId);
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_Unsubscribe()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            // act
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) => Task.FromResult(directMethodResponseWithPayload))
                .ConfigureAwait(false);

            await deviceClient
                .SetDirectMethodCallbackAsync(null)
                .ConfigureAwait(false);

            // assert
            await innerHandler
                .Received()
                .DisableMethodsAsync(Arg.Any<CancellationToken>())
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_NullMethodRest()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool isMethodHandlerCalled = false;
            await deviceClient.SetDirectMethodCallbackAsync((payload) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(directMethodResponseWithPayload);
            }).ConfigureAwait(false);

            // act
            await deviceClient.OnMethodCalledAsync(null).ConfigureAwait(false);

            // assert
            await innerHandler.Received(0).SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeFalse();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasEmptyBody()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool isMethodHandlerCalled = false;
            await deviceClient.SetDirectMethodCallbackAsync((payload) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(directMethodResponseWithPayload);
            }).ConfigureAwait(false);

            var DirectMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            await deviceClient.SetDirectMethodCallbackAsync((payload) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(directMethodResponseWithPayload);
            }).ConfigureAwait(false);

            CustomDirectMethodPayload payload = new CustomDirectMethodPayload { Grade = "good" };
            var DirectMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_StringPayload()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            bool responseReceivedAsExpected = false;
            string response = null;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        responseReceivedAsExpected = payload.TryGetPayload(out response);
                        return Task.FromResult(directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            string payload = "test";
            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
            responseReceivedAsExpected.Should().BeTrue();
            response.Should().BeEquivalentTo(payload);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_BooleanPayload()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            bool responseReceivedAsExpected = false;
            bool response = false;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        responseReceivedAsExpected = payload.TryGetPayload(out response);
                        return Task.FromResult(directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            bool boolean = true;
            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(boolean)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
            responseReceivedAsExpected.Should().BeTrue();
            response.Should().Be(boolean);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_ArrayPayload()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            bool responseReceivedAsExpected = false;
            byte[] response = null;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        responseReceivedAsExpected = payload.TryGetPayload(out response);
                        return Task.FromResult(directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            byte[] bytes = new byte[] { 1, 2, 3 };
            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bytes)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
            responseReceivedAsExpected.Should().BeTrue();
            response.Should().BeEquivalentTo(bytes);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_ListPayload()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            bool responseReceivedAsExpected = false;
            List<double> response = null;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        responseReceivedAsExpected = payload.TryGetPayload(out response);
                        return Task.FromResult(directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            List<double> list = new List<double>() { 1.0, 2.0, 3.0 };
            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(list)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
            responseReceivedAsExpected.Should().BeTrue();
            response.Should().BeEquivalentTo(list);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_DictionaryPayload()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            bool responseReceivedAsExpected = false;
            Dictionary<string, object> response = null;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        responseReceivedAsExpected = payload.TryGetPayload(out response);
                        return Task.FromResult(directMethodResponseWithPayload);
                    })
                .ConfigureAwait(false);

            var map = new Dictionary<string, object>() { { "key1", "val1" }, { "key2", 2 } };
            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(map)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
            responseReceivedAsExpected.Should().BeTrue();
            response.Should().BeEquivalentTo(map);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_NoPayloadResult()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (payload) =>
                    {
                        isMethodHandlerCalled = true;
                        return Task.FromResult(directMethodResponseWithNoPayload);
                    })
                .ConfigureAwait(false);

            var payload = new CustomDirectMethodPayload { Grade = "good" };
            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalledNoMethodHandler()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            CustomDirectMethodPayload payload = new CustomDirectMethodPayload { Grade = "good" };
            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.DidNotReceive().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerOverwriteExistingDelegate()
        {
            // arrange
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = new IotHubDeviceClient(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            CustomDirectMethodPayload actualMethodBody = null;
            Func<DirectMethodRequest, Task<DirectMethodResponse>> methodCallback = (methodRequest) =>
            {
                actualMethodName = methodRequest.MethodName;
                bool methodReceived = methodRequest.TryGetPayload(out actualMethodBody);
                methodCallbackCalled = true;
                return Task.FromResult(directMethodResponseWithEmptyByteArrayPayload);
            };

            string methodName = "TestMethodName";
            CustomDirectMethodPayload methodBody = new CustomDirectMethodPayload { Grade = "good" };
            await deviceClient.SetDirectMethodCallbackAsync(methodCallback).ConfigureAwait(false);
            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = methodName,
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(methodBody)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
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
                return Task.FromResult(directMethodResponseWithEmptyByteArrayPayload);
            };

            CustomDirectMethodPayload methodBody2 = new CustomDirectMethodPayload { Grade = "bad" };
            await deviceClient.SetDirectMethodCallbackAsync(methodCallback2).ConfigureAwait(false);
            directMethodRequest = new DirectMethodRequest
            {
                MethodName = methodName,
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(methodBody2)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            methodCallbackCalled2.Should().BeTrue();
            methodName.Should().Be(actualMethodName2);
            methodBody2.Should().BeEquivalentTo(actualMethodBody2);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerUnsetLastMethodHandler()
        {
            // arrange
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = new IotHubDeviceClient(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            CustomDirectMethodPayload actualMethodBody = null;
            Func<DirectMethodRequest, Task<DirectMethodResponse>> methodCallback = (methodRequest) =>
            {
                actualMethodName = methodRequest.MethodName;
                bool methodReceived = methodRequest.TryGetPayload(out actualMethodBody);
                methodCallbackCalled = true;
                return Task.FromResult(directMethodResponseWithEmptyByteArrayPayload);
            };

            string methodName = "TestMethodName";
            var methodBody = new CustomDirectMethodPayload { Grade = "good" };
            await deviceClient.SetDirectMethodCallbackAsync(methodCallback).ConfigureAwait(false);
            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = methodName,
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(methodBody)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);

            methodCallbackCalled.Should().BeTrue();
            methodName.Should().Be(actualMethodName);
            methodBody.Should().BeEquivalentTo(actualMethodBody);

            // arrange
            methodCallbackCalled = false;
            await deviceClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);
            directMethodRequest = new DirectMethodRequest
            {
                MethodName = methodName,
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(methodBody)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await deviceClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            await innerHandler.Received().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            methodCallbackCalled.Should().BeFalse();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerUnsetWhenNoMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = new IotHubDeviceClient(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            await deviceClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public void IotHubDeviceClient_OnConnectionOpenedInvokeHandlerForStatusChange()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            bool handlerCalled = false;
            ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
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
        public void IotHubDeviceClient_OnConnectionOpenedWithNullHandler()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            bool handlerCalled = false;
            ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
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
        public void IotHubDeviceClient_OnConnectionOpenedNotInvokeHandlerWithoutStatusChange()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
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
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            handlerCalled.Should().BeFalse();
        }

        [TestMethod]
        public void IotHubDeviceClient_OnConnectionClosedInvokeHandlerAndRecoveryForStatusChange()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
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
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.SendTelemetryAsync(Arg.Any<TelemetryMessage>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler;

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
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString, options);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.SendTelemetryAsync(Arg.Any<TelemetryMessage>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler;

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
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString, options);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.SendTelemetryAsync(Arg.Any<TelemetryMessage>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler;

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
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.SendTelemetryAsync(Arg.Any<TelemetryMessage>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler;

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
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString, options);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.SendTelemetryAsync(Arg.Any<TelemetryMessage>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler;

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
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString, options);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.SendTelemetryAsync(Arg.Any<TelemetryMessage>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler;

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
            Action createDeviceClientAuth = () => { new ClientAuthenticationWithConnectionString(FakeConnectionString, TimeSpan.FromSeconds(-60)); };

            // assert
            createDeviceClientAuth.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateWithConnectionString_InvalidSasRenewalBuffer_ThrowsException()
        {
            // arrange
            // act
            Action createDeviceClientAuth = () => { new ClientAuthenticationWithConnectionString(FakeConnectionString, timeBufferPercentage: 200); };

            // assert
            createDeviceClientAuth.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateWithConnectionString_SasTokenTimeToLiveRenewalConfigurable()
        {
            // arrange
            var sasTokenTimeToLive = TimeSpan.FromMinutes(20);
            int sasTokenRenewalBuffer = 50;
            var auth = new ClientAuthenticationWithConnectionString(FakeConnectionString, sasTokenTimeToLive, sasTokenRenewalBuffer);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());

            // act
            DateTime startTime = DateTime.UtcNow;
            using IotHubDeviceClient deviceClient = new IotHubDeviceClient(FakeHostName, auth, options);

            // assert
            var sasTokenRefresher = deviceClient.IotHubConnectionCredentials.SasTokenRefresher;
            sasTokenRefresher.Should().BeAssignableTo<ClientAuthenticationWithSakRefresh>();

            // The calculation of the sas token expiration will begin once the AuthenticationWithTokenRefresh object has been initialized.
            // Since the initialization is internal to the ClientFactory logic and is not observable, we will allow a buffer period to our assertions.
            var buffer = TimeSpan.FromSeconds(2);

            // The initial expiration time calculated is (current UTC time - sas TTL supplied).
            // The actual expiration time associated with a sas token is recalculated during token generation, but relies on the same sas TTL supplied.

            var expectedExpirationTime = startTime.Add(-sasTokenTimeToLive);
            sasTokenRefresher.ExpiresOnUtc.Should().BeCloseTo(expectedExpirationTime, (int)buffer.TotalMilliseconds);

            int expectedBufferSeconds = (int)(sasTokenTimeToLive.TotalSeconds * ((float)sasTokenRenewalBuffer / 100));
            var expectedRefreshTime = expectedExpirationTime.AddSeconds(-expectedBufferSeconds);
            sasTokenRefresher.RefreshesOnUtc.Should().BeCloseTo(expectedRefreshTime, (int)buffer.TotalMilliseconds);
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateFromAuthenticationMethod_SasTokenTimeToLiveRenewalConfigurable()
        {
            // arrange
            var sasTokenTimeToLive = TimeSpan.FromMinutes(20);
            int sasTokenRenewalBuffer = 50;
            var auth = new TestDeviceAuthenticationWithTokenRefresh(sasTokenTimeToLive, sasTokenRenewalBuffer);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());

            // act
            DateTime startTime = DateTime.UtcNow;
            using IotHubDeviceClient deviceClient = new IotHubDeviceClient(FakeHostName, auth, options);

            // assert
            var sasTokenRefresher = deviceClient.IotHubConnectionCredentials.SasTokenRefresher;

            // The calculation of the sas token expiration will begin once the AuthenticationWithTokenRefresh object has been initialized.
            // Since the initialization is internal to the ClientFactory logic and is not observable, we will allow a buffer period to our assertions.
            var buffer = TimeSpan.FromSeconds(2);

            // The initial expiration time calculated is (current UTC time - sas TTL supplied).
            // The actual expiration time associated with a sas token is recalculated during token generation, but relies on the same sas TTL supplied.

            DateTime expectedExpirationTime = startTime.Add(-sasTokenTimeToLive);
            sasTokenRefresher.ExpiresOnUtc.Should().BeCloseTo(expectedExpirationTime, (int)buffer.TotalMilliseconds);

            int expectedBufferSeconds = (int)(sasTokenTimeToLive.TotalSeconds * ((float)sasTokenRenewalBuffer / 100));
            DateTime expectedRefreshTime = expectedExpirationTime.AddSeconds(-expectedBufferSeconds);
            sasTokenRefresher.RefreshesOnUtc.Should().BeCloseTo(expectedRefreshTime, (int)buffer.TotalMilliseconds);
        }

        [TestMethod]
        public void IotHubDeviceClient_InitWithMqttTcpTransportAndModelId_DoesNotThrow()
        {
            IotHubDeviceClient_InitWithNonHttpTransportAndModelId_DoesNotThrow(new IotHubClientMqttSettings());
        }

        [TestMethod]
        public void IotHubDeviceClient_InitWithMqttWsTransportAndModelId_DoesNotThrow()
        {
            IotHubDeviceClient_InitWithNonHttpTransportAndModelId_DoesNotThrow(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket));
        }

        [TestMethod]
        public void IotHubDeviceClient_InitWithAmqpTcpTransportAndModelId_DoesNotThrow()
        {
            IotHubDeviceClient_InitWithNonHttpTransportAndModelId_DoesNotThrow(new IotHubClientAmqpSettings());
        }

        [TestMethod]
        public void IotHubDeviceClient_InitWithAmqpWsTransportAndModelId_DoesNotThrow()
        {
            IotHubDeviceClient_InitWithNonHttpTransportAndModelId_DoesNotThrow(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));
        }

        private void IotHubDeviceClient_InitWithNonHttpTransportAndModelId_DoesNotThrow(IotHubClientTransportSettings transportSettings)
        {
            // arrange

            var clientOptions = new IotHubClientOptions(transportSettings)
            {
                ModelId = TestModelId,
            };

            // act and assert
            FluentActions
                .Invoking(() => { using var deviceClient = new IotHubDeviceClient(FakeConnectionString, clientOptions); })
                .Should()
                .NotThrow();
        }

        [TestMethod]
        public void IotHubDeviceClient_SendTelemetryAsync_Cancelled_ThrowsOperationCanceledException()
        {
            //arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) to ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.SendTelemetryAsync(Arg.Any<TelemetryMessage>(), Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            ErrorDelegatingHandler errorHandler = new ErrorDelegatingHandler(null, mainProtocolHandler);

            deviceClient.InnerHandler = errorHandler;

            // act

            // We will pass in an expired token to make sure the ErrorDelegationHandler or the InternalClient will not throw a different type of exception.
            // This can happen if the ErrorDelegationHandler or the InternalClient checks the token for expiry before calling into the protocol specific delegate.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await deviceClient.SendTelemetryAsync(new TelemetryMessage(), cts.Token);

            // assert
            act.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_SendTelemetryAsync_WithoutExplicitOpenAsync_ThrowsInvalidOperationException()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            // act
            Func<Task> act = async () => await deviceClient.SendTelemetryAsync(new TelemetryMessage());

            // assert
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_SendTelemetryAsync_BeforeExplicitOpenAsync_ThrowsInvalidOperationException()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            // act
            Func<Task> act = async () =>
            {
                await deviceClient.SendTelemetryAsync(new TelemetryMessage());
                await deviceClient.OpenAsync();
            };

            // assert
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_SendTelemetryAsync_AfterExplicitOpenAsync_DoesNotThrow()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            // act
            Func<Task> act = async () =>
            {
                await deviceClient.OpenAsync();
                await deviceClient.SendTelemetryAsync(new TelemetryMessage());
            };

            // should not throw
        }

        [TestMethod]
        public void IotHubDeviceClient_OpenAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) to ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.OpenAsync(Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            ErrorDelegatingHandler errorHandler = new ErrorDelegatingHandler(null, mainProtocolHandler);

            deviceClient.InnerHandler = errorHandler;

            // act

            // We will pass in an expired token to make sure the ErrorDelegationHandler or the InternalClient will not throw a different type of exception.
            // This can happen if the ErrorDelegationHandler or the InternalClient checks the token for expiry before calling into the protocol specific delegate.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await deviceClient.OpenAsync(cts.Token);

            // assert
            act.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_UpdateReportedPropertiesAsync_Cancelled_ThrowsOperationCanceledException()
        {
            //arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) to ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.UpdateReportedPropertiesAsync(Arg.Any<ReportedPropertyCollection>(), Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            ErrorDelegatingHandler errorHandler = new ErrorDelegatingHandler(null, mainProtocolHandler);

            deviceClient.InnerHandler = errorHandler;

            // act

            // We will pass in an expired token to make sure the ErrorDelegationHandler or the InternalClient will not throw a different type of exception.
            // This can happen if the ErrorDelegationHandler or the InternalClient checks the token for expiry before calling into the protocol specific delegate.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await deviceClient.UpdateReportedPropertiesAsync(new ReportedPropertyCollection(), cts.Token);

            // assert
            act.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_GetTwinAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) to ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.GetTwinAsync(Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            ErrorDelegatingHandler errorHandler = new ErrorDelegatingHandler(null, mainProtocolHandler);

            deviceClient.InnerHandler = errorHandler;

            // act

            // We will pass in an expired token to make sure the ErrorDelegationHandler or the InternalClient will not throw a different type of exception.
            // This can happen if the ErrorDelegationHandler or the InternalClient checks the token for expiry before calling into the protocol specific delegate.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await deviceClient.GetTwinAsync(cts.Token);

            // assert
            act.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_CloseAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) to ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.CloseAsync(Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            ErrorDelegatingHandler errorHandler = new ErrorDelegatingHandler(null, mainProtocolHandler);

            deviceClient.InnerHandler = errorHandler;

            // act

            // We will pass in an expired token to make sure the ErrorDelegationHandler or the InternalClient will not throw a different type of exception.
            // This can happen if the ErrorDelegationHandler or the InternalClient checks the token for expiry before calling into the protocol specific delegate.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await deviceClient.CloseAsync(cts.Token);

            // assert
            act.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_SetDesiredPropertyCallbackAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) to ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.EnableTwinPatchAsync(Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            mainProtocolHandler
                .When(x => x.DisableTwinPatchAsync(Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            ErrorDelegatingHandler errorHandler = new ErrorDelegatingHandler(null, mainProtocolHandler);

            deviceClient.InnerHandler = errorHandler;

            // act

            // We will pass in an expired token to make sure the ErrorDelegationHandler or the InternalClient will not throw a different type of exception.
            // This can happen if the ErrorDelegationHandler or the InternalClient checks the token for expiry before calling into the protocol specific delegate.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await deviceClient.SetDesiredPropertyUpdateCallbackAsync(
                (patch) => Task.FromResult(true),
                cts.Token);

            // assert
            act.Should().Throw<OperationCanceledException>();
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
            [JsonProperty(PropertyName = "grade")]
            public string Grade { get; set; }
        }
    }
}
