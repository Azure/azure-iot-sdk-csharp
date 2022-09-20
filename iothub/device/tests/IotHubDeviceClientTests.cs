﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Exceptions;
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

        private DirectMethodResponse directMethodResponseWithPayload = new DirectMethodResponse()
        {
            Status = 200,
            Payload = Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}")
        };

        private DirectMethodResponse directMethodResponseWithNoPayload = new DirectMethodResponse()
        {
            Status = 200,
        };

        private DirectMethodResponse directMethodResponseWithEmptyByteArrayPayload = new DirectMethodResponse()
        {
            Status = 200,
            Payload = new byte[0]
        };

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_NullCertificate_Throws()
        {
            Action act = () => new DeviceAuthenticationWithX509Certificate("device1", null);

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
            var authMethod = new DeviceAuthenticationWithX509Certificate("fakeDeviceId", cert, certs);
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
            var authMethod = new DeviceAuthenticationWithX509Certificate("fakeDeviceId", cert, certs);
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
            var authMethod = new DeviceAuthenticationWithX509Certificate("fakeDeviceId", cert, certs);
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
            var authMethod = new DeviceAuthenticationWithX509Certificate("fakeDeviceId", cert, certs);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.Tcp));

            // act
            using var dc = new IotHubDeviceClient(hostName, authMethod, options);

            // should not throw
        }

        [TestMethod]
        public void IotHubDeviceClient_ParamsHostNameAuthMethod_Works()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh(
                "device1",
                s_iotHubConnectionCredentials.SharedAccessKey,
                s_iotHubConnectionCredentials.SharedAccessKeyName);

            using var deviceClient = new IotHubDeviceClient(hostName, authMethod);
        }

        [TestMethod]
        public void IotHubDeviceClient_ParamsHostNameAuthMethodTransportType_Works()
        {
            string hostName = "acme.azure-devices.net";
            var transportSettings = new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket);
            var options = new IotHubClientOptions(transportSettings);

            var authMethod = new DeviceAuthenticationWithSakRefresh(
                "device1",
                s_iotHubConnectionCredentials.SharedAccessKey,
                s_iotHubConnectionCredentials.SharedAccessKeyName);

            using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        [TestMethod]
        public void IotHubDeviceClient_ParamsHostNameGatewayAuthMethod_Works()
        {
            string hostName = "acme.azure-devices.net";
            string gatewayHostName = "gateway.acme.azure-devices.net";
            var options = new IotHubClientOptions(new IotHubClientMqttSettings()) { GatewayHostName = gatewayHostName };

            var authMethod = new DeviceAuthenticationWithSakRefresh(
                "device1",
                s_iotHubConnectionCredentials.SharedAccessKey,
                s_iotHubConnectionCredentials.SharedAccessKeyName);

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

            var authMethod = new DeviceAuthenticationWithSakRefresh(
                "device1",
                s_iotHubConnectionCredentials.SharedAccessKey,
                s_iotHubConnectionCredentials.SharedAccessKeyName);

            using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void IotHubDeviceClient_Params_GatewayAuthMethod_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var authMethod = new DeviceAuthenticationWithSakRefresh(
                "device1",
                s_iotHubConnectionCredentials.SharedAccessKey,
                s_iotHubConnectionCredentials.SharedAccessKeyName);

            using var deviceClient = new IotHubDeviceClient(gatewayHostname, authMethod);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void IotHubDeviceClient_ParamsGatewayAuthMethodTransport_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));
            var authMethod = new DeviceAuthenticationWithSakRefresh(
                "device1",
                s_iotHubConnectionCredentials.SharedAccessKey,
                s_iotHubConnectionCredentials.SharedAccessKeyName);

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
            var authMethod = new DeviceAuthenticationWithSakRefresh(
                "device1",
                s_iotHubConnectionCredentials.SharedAccessKey,
                s_iotHubConnectionCredentials.SharedAccessKeyName);

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
        public void IotHubDeviceClient_DefaultDiagnosticSamplingPercentage_Ok()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            const int DefaultPercentage = 0;
            DefaultPercentage.Should().Be(deviceClient.DiagnosticSamplingPercentage);
        }

        [TestMethod]
        public void IotHubDeviceClient_SetDiagnosticSamplingPercentageInRange_Ok()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            const int ValidPercentage = 80;
            deviceClient.DiagnosticSamplingPercentage = ValidPercentage;
            ValidPercentage.Should().Be(deviceClient.DiagnosticSamplingPercentage);
        }

        [TestMethod]
        public void IotHubDeviceClient_SetDiagnosticSamplingPercentageOutOfRange_Fail()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            const int DefaultPercentage = 0;
            const int InvalidPercentageExceedUpperLimit = 200;
            const int InvalidPercentageExceedLowerLimit = -100;

            try
            {
                Action act = () => deviceClient.DiagnosticSamplingPercentage = InvalidPercentageExceedUpperLimit;
                act.Should().Throw<ArgumentOutOfRangeException>();
            }
            catch (ArgumentOutOfRangeException)
            {
                DefaultPercentage.Should().Be(deviceClient.DiagnosticSamplingPercentage);
            }

            try
            {
                Action act = () => deviceClient.DiagnosticSamplingPercentage = InvalidPercentageExceedLowerLimit;
                act.Should().Throw<ArgumentOutOfRangeException>();
            }
            catch (ArgumentOutOfRangeException)
            {
                DefaultPercentage.Should().Be(deviceClient.DiagnosticSamplingPercentage);
            }
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
                .SetMethodHandlerAsync(
                    (payload, context) => Task.FromResult(directMethodResponseWithPayload), "custom data")
                .ConfigureAwait(false);

            await deviceClient
                .SetMethodHandlerAsync(null, null)
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
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync((payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(directMethodResponseWithPayload);
            }, "custom data").ConfigureAwait(false);

            await deviceClient.OnMethodCalledAsync(null).ConfigureAwait(false);
            await innerHandler.Received(0).SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeFalse();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasEmptyBody()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync((payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(directMethodResponseWithPayload);
            }, "custom data").ConfigureAwait(false);

            var DirectMethodRequest = new DirectMethodRequest()
            {
                MethodName = "TestMethodName",
            };

            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync((payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(directMethodResponseWithPayload);
            }, "custom data").ConfigureAwait(false);

            var DirectMethodRequest = new DirectMethodRequest()
            {
                MethodName = "TestMethodName",
                Payload = "{\"grade\":\"good\"}",
            };

            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_BooleanPayload()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            bool response = false;
            string responseAsString = null;
            await deviceClient.SetMethodHandlerAsync((payload, context) =>
            {
                isMethodHandlerCalled = true;
                response = payload.GetPayload<bool>();
                responseAsString = payload.PayloadAsJsonString;
                return Task.FromResult(directMethodResponseWithPayload);
            }, "custom data").ConfigureAwait(false);

            bool boolean = true;
            var DirectMethodRequest = new DirectMethodRequest()
            {
                MethodName = "TestMethodName",
                Payload = boolean,
            };

            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
            response.Should().BeTrue();
            responseAsString.Should().Be(JsonConvert.SerializeObject(boolean));
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_ArrayPayload()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            byte[] response = null;
            string responseAsString = null;
            await deviceClient.SetMethodHandlerAsync((payload, context) =>
            {
                isMethodHandlerCalled = true;
                response = payload.GetPayload<byte[]>();
                responseAsString = payload.PayloadAsJsonString;
                return Task.FromResult(directMethodResponseWithPayload);
            }, "custom data").ConfigureAwait(false);

            byte[] bytes = new byte[] { 1, 2, 3 };
            var DirectMethodRequest = new DirectMethodRequest()
            {
                MethodName = "TestMethodName",
                Payload = bytes,
            };

            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
            response.Should().BeEquivalentTo(bytes);
            responseAsString.Should().Be(JsonConvert.SerializeObject(bytes));
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_ListPayload()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            List<double> response = null;
            string responseAsString = null;
            await deviceClient.SetMethodHandlerAsync((payload, context) =>
            {
                isMethodHandlerCalled = true;
                response = payload.GetPayload<List<double>>();
                responseAsString = payload.PayloadAsJsonString;
                return Task.FromResult(directMethodResponseWithPayload);
            }, "custom data").ConfigureAwait(false);

            List<double> list = new List<double>() { 1.0, 2.0, 3.0 };
            var DirectMethodRequest = new DirectMethodRequest()
            {
                MethodName = "TestMethodName",
                Payload = list,
            };

            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
            response.Should().BeEquivalentTo(list);
            responseAsString.Should().Be(JsonConvert.SerializeObject(list));
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_DictionaryPayload()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            Dictionary<string, object> response = null;
            string responseAsString = null;
            await deviceClient.SetMethodHandlerAsync((payload, context) =>
            {
                isMethodHandlerCalled = true;
                response = payload.GetPayload<Dictionary<string, object>>();
                responseAsString = payload.PayloadAsJsonString;
                return Task.FromResult(directMethodResponseWithPayload);
            }, "custom data").ConfigureAwait(false);

            Dictionary<string, object> map = new Dictionary<string, object>() { { "key1", "val1" }, { "key2", 2 } };
            var DirectMethodRequest = new DirectMethodRequest()
            {
                MethodName = "TestMethodName",
                Payload = map,
            };

            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
            responseAsString.Should().Be(JsonConvert.SerializeObject(map));
            response.Should().BeEquivalentTo(map);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_NoPayloadResult()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync((payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(directMethodResponseWithNoPayload);
            }, "custom data").ConfigureAwait(false);

            var DirectMethodRequest = new DirectMethodRequest()
            {
                MethodName = "TestMethodName",
                Payload = "{\"grade\":\"good\"}",
            };

            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<DirectMethodResponse>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            isMethodHandlerCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalledNoMethodHandler()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var DirectMethodRequest = new DirectMethodRequest()
            {
                MethodName = "TestMethodName",
                Payload = "{\"grade\":\"good\"}",
            };

            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<DirectMethodResponse>(resp => resp.Status == 501), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerOverwriteExistingDelegate()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = new IotHubDeviceClient(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            Func<DirectMethodRequest, object, Task<DirectMethodResponse>> methodCallback = (methodRequest, userContext) =>
            {
                actualMethodName = methodRequest.MethodName;
                actualMethodBody = (string)methodRequest.Payload;
                actualMethodUserContext = userContext;
                methodCallbackCalled = true;
                return Task.FromResult(directMethodResponseWithEmptyByteArrayPayload);
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            var DirectMethodRequest = new DirectMethodRequest()
            {
                MethodName = methodName,
                Payload = methodBody,
            };

            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            methodCallbackCalled.Should().BeTrue();
            methodName.Should().Be(actualMethodName);
            methodBody.Should().Be(actualMethodBody);
            methodUserContext.Should().Be((string)actualMethodUserContext);

            bool methodCallbackCalled2 = false;
            string actualMethodName2 = string.Empty;
            string actualMethodBody2 = string.Empty;
            object actualMethodUserContext2 = null;
            Func<DirectMethodRequest, object, Task<DirectMethodResponse>> methodCallback2 = (methodRequest, userContext) =>
            {
                actualMethodName2 = methodRequest.MethodName;
                actualMethodBody2 = (string)methodRequest.Payload;
                actualMethodUserContext2 = userContext;
                methodCallbackCalled2 = true;
                return Task.FromResult(directMethodResponseWithEmptyByteArrayPayload);
            };

            string methodUserContext2 = "UserContext2";
            string methodBody2 = "{\"grade\":\"bad\"}";
            await deviceClient.SetMethodHandlerAsync(methodCallback2, methodUserContext2).ConfigureAwait(false);
            DirectMethodRequest = new DirectMethodRequest()
            {
                MethodName = methodName,
                Payload = methodBody2,
            };

            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            methodCallbackCalled2.Should().BeTrue();
            methodName.Should().Be(actualMethodName2);
            methodBody2.Should().Be(actualMethodBody2);
            methodUserContext2.Should().Be((string)actualMethodUserContext2);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerUnsetLastMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = new IotHubDeviceClient(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            Func<DirectMethodRequest, object, Task<DirectMethodResponse>> methodCallback = (methodRequest, userContext) =>
            {
                actualMethodName = methodRequest.MethodName;
                actualMethodBody = (string)methodRequest.Payload;
                actualMethodUserContext = userContext;
                methodCallbackCalled = true;
                return Task.FromResult(directMethodResponseWithEmptyByteArrayPayload);
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            var DirectMethodRequest = new DirectMethodRequest()
            {
                MethodName = methodName,
                Payload = methodBody,
            };

            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);

            methodCallbackCalled.Should().BeTrue();
            methodName.Should().Be(actualMethodName);
            methodBody.Should().Be(actualMethodBody);
            methodUserContext.Should().Be((string)actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(null, null).ConfigureAwait(false);
            DirectMethodRequest = new DirectMethodRequest()
            {
                MethodName = methodName,
                Payload = methodBody,
            };

            await deviceClient.OnMethodCalledAsync(DirectMethodRequest).ConfigureAwait(false);

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

            await deviceClient.SetMethodHandlerAsync(null, null).ConfigureAwait(false);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public void DeviceClientOnConnectionOpenedInvokeHandlerForStatusChange()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            bool handlerCalled = false;
            ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> statusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionStatusInfo = c;
            };
            deviceClient.SetConnectionStatusChangeHandler(statusChangeHandler);

            // Connection status change from disconnected to connected
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            handlerCalled.Should().BeTrue();
            connectionStatusInfo.Status.Should().Be(ConnectionStatus.Connected);
            connectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.ConnectionOk);
        }

        [TestMethod]
        public void DeviceClientOnConnectionOpenedWithNullHandler()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            bool handlerCalled = false;
            ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> statusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionStatusInfo = c;
            };
            deviceClient.SetConnectionStatusChangeHandler(statusChangeHandler);
            deviceClient.SetConnectionStatusChangeHandler(null);

            // Connection status change from disconnected to connected
            deviceClient.OnConnectionStatusChanged(new ConnectionStatusInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            handlerCalled.Should().BeFalse();
        }

        [TestMethod]
        public void DeviceClientOnConnectionOpenedNotInvokeHandlerWithoutStatusChange()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            bool handlerCalled = false;
            ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> statusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionStatusInfo = c;
            };
            deviceClient.SetConnectionStatusChangeHandler(statusChangeHandler);
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
        public void DeviceClientOnConnectionClosedInvokeHandlerAndRecoveryForStatusChange()
        {
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var sender = new object();
            bool handlerCalled = false;
            ConnectionStatusInfo connectionStatusInfo = new ConnectionStatusInfo();
            Action<ConnectionStatusInfo> statusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionStatusInfo = c;
            };
            deviceClient.SetConnectionStatusChangeHandler(statusChangeHandler);

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
        public void CompleteAsyncThrowsForNullMessage()
        {
            using IotHubDeviceClient client = new IotHubDeviceClient(FakeConnectionString);

            Func<Task> act = async () => await client.CompleteMessageAsync((Message)null);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CompleteAsyncWithCancellationTokenThrowsForNullMessage()
        {
            using IotHubDeviceClient client = new IotHubDeviceClient(FakeConnectionString);

            Func<Task> act = async () => await client.CompleteMessageAsync((Message)null, CancellationToken.None);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CompleteAsyncThrowsForNullLockToken()
        {
            using IotHubDeviceClient client = new IotHubDeviceClient(FakeConnectionString);

            Func<Task> act = async () => await client.CompleteMessageAsync((string)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CompleteAsyncWithCancellationTokenThrowsForNullLockToken()
        {
            using IotHubDeviceClient client = new IotHubDeviceClient(FakeConnectionString);

            Func<Task> act = async () => await client.CompleteMessageAsync((string)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void RejectAsyncThrowsForNullMessage()
        {
            using IotHubDeviceClient client = new IotHubDeviceClient(FakeConnectionString);

            Func<Task> act = async () => await client.RejectMessageAsync((Message)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void RejectAsyncWithCancellationTokenThrowsForNullMessage()
        {
            using IotHubDeviceClient client = new IotHubDeviceClient(FakeConnectionString);

            Func<Task> act = async () => await client.RejectMessageAsync((Message)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void RejectAsyncThrowsForNullLockToken()
        {
            using IotHubDeviceClient client = new IotHubDeviceClient(FakeConnectionString);

            Func<Task> act = async () => await client.RejectMessageAsync((string)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void RejectAsyncWithCancellationTokenThrowsForNullLockToken()
        {
            using IotHubDeviceClient client = new IotHubDeviceClient(FakeConnectionString);

            Func<Task> act = async () => await client.RejectMessageAsync((string)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AbandonAsyncThrowsForNullMessage()
        {
            using IotHubDeviceClient client = new IotHubDeviceClient(FakeConnectionString);

            Func<Task> act = async () => await client.AbandonMessageAsync((Message)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AbandonAsyncWithCancellationTokenThrowsForNullMessage()
        {
            using IotHubDeviceClient client = new IotHubDeviceClient(FakeConnectionString);

            Func<Task> act = async () => await client.AbandonMessageAsync((Message)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AbandonAsyncThrowsForNullLockToken()
        {
            using IotHubDeviceClient client = new IotHubDeviceClient(FakeConnectionString);

            Func<Task> act = async () => await client.AbandonMessageAsync((string)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AbandonAsyncWithCancellationTokenThrowsForNullLockToken()
        {
            using IotHubDeviceClient client = new IotHubDeviceClient(FakeConnectionString);

            Func<Task> act = async () => await client.AbandonMessageAsync((string)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task MessageIdDefaultNotSet_SendEventDoesNotSetMessageId()
        {
            // arrange
            var messageId = Guid.NewGuid().ToString();
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.SendEventAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler;

            // act
            var messageWithoutId = new Message();
            var messageWithId = new Message
            {
                MessageId = messageId,
            };
            await deviceClient.SendEventAsync(messageWithoutId).ConfigureAwait(false);
            await deviceClient.SendEventAsync(messageWithId).ConfigureAwait(false);

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
            innerHandler.SendEventAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler;

            // act
            var messageWithoutId = new Message();
            var messageWithId = new Message
            {
                MessageId = messageId,
            };
            await deviceClient.SendEventAsync(messageWithoutId).ConfigureAwait(false);
            await deviceClient.SendEventAsync(messageWithId).ConfigureAwait(false);

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
            innerHandler.SendEventAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler;

            // act
            var messageWithoutId = new Message();
            var messageWithId = new Message
            {
                MessageId = messageId,
            };
            await deviceClient.SendEventAsync(messageWithoutId).ConfigureAwait(false);
            await deviceClient.SendEventAsync(messageWithId).ConfigureAwait(false);

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
            innerHandler.SendEventAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler;

            // act
            var messageWithoutId = new Message();
            var messageWithId = new Message
            {
                MessageId = messageId,
            };

            await deviceClient.SendEventBatchAsync(new List<Message> { messageWithoutId, messageWithId }).ConfigureAwait(false);

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
            innerHandler.SendEventAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler;

            // act
            var messageWithoutId = new Message();
            var messageWithId = new Message
            {
                MessageId = messageId,
            };

            await deviceClient.SendEventBatchAsync(new List<Message> { messageWithoutId, messageWithId }).ConfigureAwait(false);

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
            innerHandler.SendEventAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));
            deviceClient.InnerHandler = innerHandler;

            // act
            var messageWithoutId = new Message();
            var messageWithId = new Message
            {
                MessageId = messageId,
            };

            await deviceClient.SendEventBatchAsync(new List<Message> { messageWithoutId, messageWithId }).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().NotBeNullOrEmpty();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateWithConnectionString_InvalidSasTimeToLive_ThrowsException()
        {
            // arrange
            // act
            Action createDeviceClientAuth = () => { new DeviceAuthenticationWithConnectionString(FakeConnectionString, TimeSpan.FromSeconds(-60)); };

            // assert
            createDeviceClientAuth.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateWithConnectionString_InvalidSasRenewalBuffer_ThrowsException()
        {
            // arrange
            // act
            Action createDeviceClientAuth = () => { new DeviceAuthenticationWithConnectionString(FakeConnectionString, timeBufferPercentage: 200); };

            // assert
            createDeviceClientAuth.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateWithConnectionString_SasTokenTimeToLiveRenewalConfigurable()
        {
            // arrange
            var sasTokenTimeToLive = TimeSpan.FromMinutes(20);
            int sasTokenRenewalBuffer = 50;
            var auth = new DeviceAuthenticationWithConnectionString(FakeConnectionString, sasTokenTimeToLive, sasTokenRenewalBuffer);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());

            // act
            DateTime startTime = DateTime.UtcNow;
            using IotHubDeviceClient deviceClient = new IotHubDeviceClient(FakeHostName, auth, options);

            // assert
            var sasTokenRefresher = deviceClient.IotHubConnectionCredentials.SasTokenRefresher;
            sasTokenRefresher.Should().BeAssignableTo<DeviceAuthenticationWithSakRefresh>();

            // The calculation of the sas token expiration will begin once the AuthenticationWithTokenRefresh object has been initialized.
            // Since the initialization is internal to the ClientFactory logic and is not observable, we will allow a buffer period to our assertions.
            var buffer = TimeSpan.FromSeconds(2);

            // The initial expiration time calculated is (current UTC time - sas TTL supplied).
            // The actual expiration time associated with a sas token is recalculated during token generation, but relies on the same sas TTL supplied.

            var expectedExpirationTime = startTime.Add(-sasTokenTimeToLive);
            sasTokenRefresher.ExpiresOn.Should().BeCloseTo(expectedExpirationTime, (int)buffer.TotalMilliseconds);

            int expectedBufferSeconds = (int)(sasTokenTimeToLive.TotalSeconds * ((float)sasTokenRenewalBuffer / 100));
            var expectedRefreshTime = expectedExpirationTime.AddSeconds(-expectedBufferSeconds);
            sasTokenRefresher.RefreshesOn.Should().BeCloseTo(expectedRefreshTime, (int)buffer.TotalMilliseconds);
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
            sasTokenRefresher.ExpiresOn.Should().BeCloseTo(expectedExpirationTime, (int)buffer.TotalMilliseconds);

            int expectedBufferSeconds = (int)(sasTokenTimeToLive.TotalSeconds * ((float)sasTokenRenewalBuffer / 100));
            DateTime expectedRefreshTime = expectedExpirationTime.AddSeconds(-expectedBufferSeconds);
            sasTokenRefresher.RefreshesOn.Should().BeCloseTo(expectedRefreshTime, (int)buffer.TotalMilliseconds);
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
        public void IotHubDeviceClient_ReceiveAsync_Cancelled_ThrowsOperationCanceledException()
        {
            //arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) to ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.ReceiveMessageAsync(Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            ErrorDelegatingHandler errorHandler = new ErrorDelegatingHandler(null, mainProtocolHandler);

            deviceClient.InnerHandler = errorHandler;

            // We will pass in an expired token to make sure the ErrorDelegationHandler will not throw a different type of exception.
            // This can happen if the ErrorDelegationHandler checks the token for expiry before calling into the protocol specific delegate.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act

            Func<Task> act = async () => await deviceClient.ReceiveMessageAsync(cts.Token);

            // assert
            act.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_CompleteAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) to ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.CompleteMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            ErrorDelegatingHandler errorHandler = new ErrorDelegatingHandler(null, mainProtocolHandler);

            deviceClient.InnerHandler = errorHandler;

            // We will pass in an expired token to make sure the ErrorDelegationHandler or the InternalClient will not throw a different type of exception.
            // This can happen if the ErrorDelegationHandler or the InternalClient checks the token for expiry before calling into the protocol specific delegate.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // act

            Func<Task> act = async () => await deviceClient.CompleteMessageAsync("SomeToken", cts.Token);

            // assert

            act.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_RejectAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) to ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.RejectMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            ErrorDelegatingHandler errorHandler = new ErrorDelegatingHandler(null, mainProtocolHandler);

            deviceClient.InnerHandler = errorHandler;

            // act

            // We will pass in an expired token to make sure the ErrorDelegationHandler or the InternalClient will not throw a different type of exception.
            // This can happen if the ErrorDelegationHandler or the InternalClient checks the token for expiry before calling into the protocol specific delegate.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await deviceClient.RejectMessageAsync("SomeToken", cts.Token);

            // assert

            act.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_SendEventAsync_Cancelled_ThrowsOperationCanceledException()
        {
            //arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) to ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.SendEventAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            ErrorDelegatingHandler errorHandler = new ErrorDelegatingHandler(null, mainProtocolHandler);

            deviceClient.InnerHandler = errorHandler;

            // act

            // We will pass in an expired token to make sure the ErrorDelegationHandler or the InternalClient will not throw a different type of exception.
            // This can happen if the ErrorDelegationHandler or the InternalClient checks the token for expiry before calling into the protocol specific delegate.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await deviceClient.SendEventAsync(new Message(), cts.Token);

            // assert
            act.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_SendEventAsync_WithoutExplicitOpenAsync_ThrowsInvalidOperationException()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            // act
            Func<Task> act = async () => await deviceClient.SendEventAsync(new Message());

            // assert
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_SendEventAsync_BeforeExplicitOpenAsync_ThrowsInvalidOperationException()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            // act
            Func<Task> act = async () =>
            {
                await deviceClient.SendEventAsync(new Message());
                await deviceClient.OpenAsync();
            };

            // assert
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_SendEventAsync_AfterExplicitOpenAsync_DoesNotThrow()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            // act
            Func<Task> act = async () =>
            {
                await deviceClient.OpenAsync();
                await deviceClient.SendEventAsync(new Message());
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
        public void IotHubDeviceClient_AbandonAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var deviceClient = new IotHubDeviceClient(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) to ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.AbandonMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            ErrorDelegatingHandler errorHandler = new ErrorDelegatingHandler(null, mainProtocolHandler);

            deviceClient.InnerHandler = errorHandler;

            // act

            // We will pass in an expired token to make sure the ErrorDelegationHandler or the InternalClient will not throw a different type of exception.
            // This can happen if the ErrorDelegationHandler or the InternalClient checks the token for expiry before calling into the protocol specific delegate.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await deviceClient.AbandonMessageAsync("SomeLockToken", cts.Token);

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
                .When(x => x.SendTwinPatchAsync(Arg.Any<TwinCollection>(), Arg.Any<CancellationToken>()))
                .Do(x => { throw new OperationCanceledException(); });

            ErrorDelegatingHandler errorHandler = new ErrorDelegatingHandler(null, mainProtocolHandler);

            deviceClient.InnerHandler = errorHandler;

            // act

            // We will pass in an expired token to make sure the ErrorDelegationHandler or the InternalClient will not throw a different type of exception.
            // This can happen if the ErrorDelegationHandler or the InternalClient checks the token for expiry before calling into the protocol specific delegate.
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await deviceClient.UpdateReportedPropertiesAsync(new TwinCollection(), cts.Token);

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
                .When(x => x.SendTwinGetAsync(Arg.Any<CancellationToken>()))
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
                (patch, context) => Task.FromResult(true),
                deviceClient,
                cts.Token);

            // assert
            act.Should().Throw<OperationCanceledException>();
        }

        private class TestDeviceAuthenticationWithTokenRefresh : DeviceAuthenticationWithTokenRefresh
        {
            // This authentication method relies on the default sas token time to live and renewal buffer set by the SDK.
            public TestDeviceAuthenticationWithTokenRefresh(TimeSpan ttl, int refreshBuffer) : base("someTestDevice", ttl, refreshBuffer)
            {
            }

            ///<inheritdoc/>
            protected override Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive)
            {
                return Task.FromResult<string>("someToken");
            }
        }
    }
}
