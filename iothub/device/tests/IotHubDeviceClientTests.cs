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

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_NullCertificate_Throws()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", null);

            Action act = () => IotHubDeviceClient.Create(hostName, authMethod, new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket)));
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
            using var dc = IotHubDeviceClient.Create(hostName, authMethod, options);
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
            using var dc = IotHubDeviceClient.Create(hostName, authMethod, options);
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
            using var dc = IotHubDeviceClient.Create(hostName, authMethod, options);

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
            using var dc = IotHubDeviceClient.Create(hostName, authMethod, options);

            // should not throw
        }

        [TestMethod]
        public void IotHubDeviceClient_ParamsHostNameAuthMethod_Works()
        {
            string hostName = "acme.azure-devices.net";
            var options = new IotHubClientOptions();
            ClientConfiguration connInfo = new ClientConfiguration(s_iotHubConnectionCredentials, options);
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", connInfo);

            using var deviceClient = IotHubDeviceClient.Create(hostName, authMethod);
        }

        [TestMethod]
        public void IotHubDeviceClient_ParamsHostNameAuthMethodTransportType_Works()
        {
            string hostName = "acme.azure-devices.net";
            var transportSettings = new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket);
            var options = new IotHubClientOptions(transportSettings);

            // TODO (abmisr): IotHubClientOptions shouldn't be required in both
            // DeviceAuthenticationWithSakRefresh (via -> IotHubConnectionInfo) and again in IotHubDeviceClient.Create
            ClientConfiguration connInfo = new ClientConfiguration(s_iotHubConnectionCredentials, options);
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", connInfo);

            var deviceClient = IotHubDeviceClient.Create(hostName, authMethod, options);
        }

        [TestMethod]
        public void IotHubDeviceClient_ParamsHostNameGatewayAuthMethod_Works()
        {
            string hostName = "acme.azure-devices.net";
            string gatewayHostName = "gateway.acme.azure-devices.net";
            var options = new IotHubClientOptions(new IotHubClientMqttSettings()) { GatewayHostName = gatewayHostName };

            ClientConfiguration connInfo = new ClientConfiguration(s_iotHubConnectionCredentials, options);
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", connInfo);

            using var deviceClient = IotHubDeviceClient.Create(hostName, authMethod, options);
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

            ClientConfiguration connInfo = new ClientConfiguration(s_iotHubConnectionCredentials, options);
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", connInfo);

            using var deviceClient = IotHubDeviceClient.Create(hostName, authMethod, options);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void IotHubDeviceClient_Params_GatewayAuthMethod_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var options = new IotHubClientOptions();
            ClientConfiguration connInfo = new ClientConfiguration(s_iotHubConnectionCredentials, options);
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", connInfo);

            using var deviceClient = IotHubDeviceClient.Create(gatewayHostname, authMethod);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void IotHubDeviceClient_ParamsGatewayAuthMethodTransport_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));
            ClientConfiguration connInfo = new ClientConfiguration(s_iotHubConnectionCredentials, options);
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", connInfo);

            using var deviceClient = IotHubDeviceClient.Create(
                gatewayHostname,
                authMethod,
                new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket)));
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void IotHubDeviceClient_ParamsGatewayAuthMethodTransportArray_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket));
            ClientConfiguration connInfo = new ClientConfiguration(s_iotHubConnectionCredentials, options);
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", connInfo);

            using var deviceClient = IotHubDeviceClient.Create(
                gatewayHostname,
                authMethod,
                options);
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateFromConnectionString_WithModuleIdThrows()
        {
            Action act = () => IotHubDeviceClient.CreateFromConnectionString(FakeConnectionStringWithModuleId);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_DefaultDiagnosticSamplingPercentage_Ok()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            const int DefaultPercentage = 0;
            Assert.AreEqual(deviceClient.DiagnosticSamplingPercentage, DefaultPercentage);
        }

        [TestMethod]
        public void IotHubDeviceClient_SetDiagnosticSamplingPercentageInRange_Ok()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            const int ValidPercentage = 80;
            deviceClient.DiagnosticSamplingPercentage = ValidPercentage;
            Assert.AreEqual(deviceClient.DiagnosticSamplingPercentage, ValidPercentage);
        }

        [TestMethod]
        public void IotHubDeviceClient_SetDiagnosticSamplingPercentageOutOfRange_Fail()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            const int DefaultPercentage = 0;
            const int InvalidPercentageExceedUpperLimit = 200;
            const int InvalidPercentageExceedLowerLimit = -100;

            try
            {
                deviceClient.DiagnosticSamplingPercentage = InvalidPercentageExceedUpperLimit;
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException)
            {
                Assert.AreEqual(deviceClient.DiagnosticSamplingPercentage, DefaultPercentage);
            }

            try
            {
                deviceClient.DiagnosticSamplingPercentage = InvalidPercentageExceedLowerLimit;
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException)
            {
                Assert.AreEqual(deviceClient.DiagnosticSamplingPercentage, DefaultPercentage);
            }
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_Unsubscribe()
        {
            // arrange
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            // act
            await deviceClient
                .SetMethodHandlerAsync(
                    "TestMethodName",
                    (payload, context) => Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200)), "custom data")
                .ConfigureAwait(false);

            await deviceClient
                .SetMethodHandlerAsync("TestMethodName", null, null)
                .ConfigureAwait(false);

            // assert
            await innerHandler
                .Received()
                .DisableMethodsAsync(Arg.Any<CancellationToken>())
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_NullMethodRequest()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("testMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            await deviceClient.InternalClient.OnMethodCalledAsync(null).ConfigureAwait(false);
            await innerHandler.Received(0).SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(isMethodHandlerCalled);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasEmptyBody()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9");

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", Encoding.UTF8.GetBytes("{\"grade\":\"good\"}"));

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_SetMethodDefaultHandler()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodDefaultHandlerCalled = false;
            await deviceClient.SetMethodDefaultHandlerAsync((payload, context) =>
            {
                isMethodDefaultHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", Encoding.UTF8.GetBytes("{\"grade\":\"good\"}"));

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodDefaultHandlerCalled);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_SetMethodHandlerNotMatchedAndDefaultHandler()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            bool isMethodDefaultHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("TestMethodName2", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);
            await deviceClient.SetMethodDefaultHandlerAsync((payload, context) =>
            {
                isMethodDefaultHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", Encoding.UTF8.GetBytes("{\"grade\":\"good\"}"));

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(isMethodHandlerCalled);
            Assert.IsTrue(isMethodDefaultHandlerCalled);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_SetMethodHandlerAndDefaultHandler()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            bool isMethodDefaultHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);
            await deviceClient.SetMethodDefaultHandlerAsync((payload, context) =>
            {
                isMethodDefaultHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", Encoding.UTF8.GetBytes("{\"grade\":\"good\"}"));

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
            Assert.IsFalse(isMethodDefaultHandlerCalled);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_No_Result()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", Encoding.UTF8.GetBytes("{\"grade\":\"good\"}"));

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OnMethodCalledNoMethodHandler()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", Encoding.UTF8.GetBytes("{\"grade\":\"good\"}"));

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);

            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<MethodResponseInternal>(resp => resp.Status == 501), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerSetFirstMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            Func<MethodRequest, object, Task<MethodResponse>> methodCallback = (methodRequest, userContext) =>
            {
                actualMethodName = methodRequest.Name;
                actualMethodBody = methodRequest.DataAsJson;
                actualMethodUserContext = userContext;
                methodCallbackCalled = true;
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            innerHandler.ClearReceivedCalls();
            methodCallbackCalled = false;
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.DidNotReceive().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerSetFirstMethodDefaultHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            Func<MethodRequest, object, Task<MethodResponse>> methodCallback = (methodRequest, userContext) =>
            {
                actualMethodName = methodRequest.Name;
                actualMethodBody = methodRequest.DataAsJson;
                actualMethodUserContext = userContext;
                methodCallbackCalled = true;
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            innerHandler.ClearReceivedCalls();
            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.DidNotReceive().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerOverwriteExistingDelegate()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            Func<MethodRequest, object, Task<MethodResponse>> methodCallback = (methodRequest, userContext) =>
            {
                actualMethodName = methodRequest.Name;
                actualMethodBody = methodRequest.DataAsJson;
                actualMethodUserContext = userContext;
                methodCallbackCalled = true;
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            bool methodCallbackCalled2 = false;
            string actualMethodName2 = string.Empty;
            string actualMethodBody2 = string.Empty;
            object actualMethodUserContext2 = null;
            Func<MethodRequest, object, Task<MethodResponse>> methodCallback2 = (methodRequest, userContext) =>
            {
                actualMethodName2 = methodRequest.Name;
                actualMethodBody2 = methodRequest.DataAsJson;
                actualMethodUserContext2 = userContext;
                methodCallbackCalled2 = true;
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodUserContext2 = "UserContext2";
            string methodBody2 = "{\"grade\":\"bad\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback2, methodUserContext2).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId2", Encoding.UTF8.GetBytes(methodBody2))).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled2);
            Assert.AreEqual(methodName, actualMethodName2);
            Assert.AreEqual(methodBody2, actualMethodBody2);
            Assert.AreEqual(methodUserContext2, actualMethodUserContext2);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerOverwriteExistingDefaultDelegate()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            Func<MethodRequest, object, Task<MethodResponse>> methodCallback = (methodRequest, userContext) =>
            {
                actualMethodName = methodRequest.Name;
                actualMethodBody = methodRequest.DataAsJson;
                actualMethodUserContext = userContext;
                methodCallbackCalled = true;
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            bool methodCallbackCalled2 = false;
            string actualMethodName2 = string.Empty;
            string actualMethodBody2 = string.Empty;
            object actualMethodUserContext2 = null;
            Func<MethodRequest, object, Task<MethodResponse>> methodCallback2 = (methodRequest, userContext) =>
            {
                actualMethodName2 = methodRequest.Name;
                actualMethodBody2 = methodRequest.DataAsJson;
                actualMethodUserContext2 = userContext;
                methodCallbackCalled2 = true;
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodUserContext2 = "UserContext2";
            string methodBody2 = "{\"grade\":\"bad\"}";
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback2, methodUserContext2).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId2", Encoding.UTF8.GetBytes(methodBody2))).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled2);
            Assert.AreEqual(methodName, actualMethodName2);
            Assert.AreEqual(methodBody2, actualMethodBody2);
            Assert.AreEqual(methodUserContext2, actualMethodUserContext2);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerUnsetLastMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            Func<MethodRequest, object, Task<MethodResponse>> methodCallback = (methodRequest, userContext) =>
            {
                actualMethodName = methodRequest.Name;
                actualMethodBody = methodRequest.DataAsJson;
                actualMethodUserContext = userContext;
                methodCallbackCalled = true;
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.Received().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(methodCallbackCalled);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerUnsetLastMethodHandlerWithDefaultHandlerSet()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            Func<MethodRequest, object, Task<MethodResponse>> methodCallback = (methodRequest, userContext) =>
            {
                actualMethodName = methodRequest.Name;
                actualMethodBody = methodRequest.DataAsJson;
                actualMethodUserContext = userContext;
                methodCallbackCalled = true;
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            innerHandler.ClearReceivedCalls();
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.DidNotReceive().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodDefaultHandlerAsync(null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.Received().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(methodCallbackCalled);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerUnsetDefaultHandlerSet()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            Func<MethodRequest, object, Task<MethodResponse>> methodCallback = (methodRequest, userContext) =>
            {
                actualMethodName = methodRequest.Name;
                actualMethodBody = methodRequest.DataAsJson;
                actualMethodUserContext = userContext;
                methodCallbackCalled = true;
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            innerHandler.ClearReceivedCalls();
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.DidNotReceive().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);

            methodCallbackCalled = false;
            await deviceClient.SetMethodDefaultHandlerAsync(null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", Encoding.UTF8.GetBytes(methodBody))).ConfigureAwait(false);

            await innerHandler.Received().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(methodCallbackCalled);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetMethodHandlerUnsetWhenNoMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            await deviceClient.SetMethodHandlerAsync("TestMethodName", null, null).ConfigureAwait(false);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public void DeviceClientOnConnectionOpenedInvokeHandlerForStatusChange()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            bool handlerCalled = false;
            ConnectionInfo connectionInfo = new ConnectionInfo();
            Action<ConnectionInfo> statusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionInfo = c;
            };
            deviceClient.SetConnectionStatusChangeHandler(statusChangeHandler);

            // Connection status change from disconnected to connected
            deviceClient.InternalClient.OnConnectionStatusChanged(new ConnectionInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(ConnectionStatus.Connected, connectionInfo.Status);
            Assert.AreEqual(ConnectionStatusChangeReason.ConnectionOk, connectionInfo.ChangeReason);
        }

        [TestMethod]
        public void DeviceClientOnConnectionOpenedWithNullHandler()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            bool handlerCalled = false;
            ConnectionInfo connectionInfo = new ConnectionInfo();
            Action<ConnectionInfo> statusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionInfo = c;
            };
            deviceClient.SetConnectionStatusChangeHandler(statusChangeHandler);
            deviceClient.SetConnectionStatusChangeHandler(null);

            // Connection status change from disconnected to connected
            deviceClient.InternalClient.OnConnectionStatusChanged(new ConnectionInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            Assert.IsFalse(handlerCalled);
        }

        [TestMethod]
        public void DeviceClientOnConnectionOpenedNotInvokeHandlerWithoutStatusChange()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            bool handlerCalled = false;
            ConnectionInfo connectionInfo = new ConnectionInfo();
            Action<ConnectionInfo> statusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionInfo = c;
            };
            deviceClient.SetConnectionStatusChangeHandler(statusChangeHandler);
            // current status = disabled

            deviceClient.InternalClient.OnConnectionStatusChanged(new ConnectionInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(ConnectionStatus.Connected, connectionInfo.Status);
            Assert.AreEqual(ConnectionStatusChangeReason.ConnectionOk, connectionInfo.ChangeReason);
            handlerCalled = false;

            // current status = connected
            deviceClient.InternalClient.OnConnectionStatusChanged(new ConnectionInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            Assert.IsFalse(handlerCalled);
        }

        [TestMethod]
        public void DeviceClientOnConnectionClosedInvokeHandlerAndRecoveryForStatusChange()
        {
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var sender = new object();
            bool handlerCalled = false;
            ConnectionInfo connectionInfo = new ConnectionInfo();
            Action<ConnectionInfo> statusChangeHandler = (c) =>
            {
                handlerCalled = true;
                connectionInfo = c;
            };
            deviceClient.SetConnectionStatusChangeHandler(statusChangeHandler);

            // current status = disabled
            deviceClient.InternalClient.OnConnectionStatusChanged(new ConnectionInfo(ConnectionStatus.Connected, ConnectionStatusChangeReason.ConnectionOk));

            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(ConnectionStatus.Connected, connectionInfo.Status);
            Assert.AreEqual(ConnectionStatusChangeReason.ConnectionOk, connectionInfo.ChangeReason);
            handlerCalled = false;

            // current status = connected
            deviceClient.InternalClient.OnConnectionStatusChanged(new ConnectionInfo(ConnectionStatus.DisconnectedRetrying, ConnectionStatusChangeReason.CommunicationError));

            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(ConnectionStatus.DisconnectedRetrying, connectionInfo.Status);
            Assert.AreEqual(ConnectionStatusChangeReason.CommunicationError, connectionInfo.ChangeReason);
        }

        [TestMethod]
        public void CompleteAsyncThrowsForNullMessage()
        {
            IotHubDeviceClient client = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.CompleteMessageAsync((Message)null);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CompleteAsyncWithCancellationTokenThrowsForNullMessage()
        {
            IotHubDeviceClient client = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.CompleteMessageAsync((Message)null, CancellationToken.None);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CompleteAsyncThrowsForNullLockToken()
        {
            IotHubDeviceClient client = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.CompleteMessageAsync((string)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CompleteAsyncWithCancellationTokenThrowsForNullLockToken()
        {
            IotHubDeviceClient client = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.CompleteMessageAsync((string)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void RejectAsyncThrowsForNullMessage()
        {
            IotHubDeviceClient client = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.RejectMessageAsync((Message)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void RejectAsyncWithCancellationTokenThrowsForNullMessage()
        {
            IotHubDeviceClient client = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.RejectMessageAsync((Message)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void RejectAsyncThrowsForNullLockToken()
        {
            IotHubDeviceClient client = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.RejectMessageAsync((string)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void RejectAsyncWithCancellationTokenThrowsForNullLockToken()
        {
            IotHubDeviceClient client = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.RejectMessageAsync((string)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AbandonAsyncThrowsForNullMessage()
        {
            IotHubDeviceClient client = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.AbandonMessageAsync((Message)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AbandonAsyncWithCancellationTokenThrowsForNullMessage()
        {
            IotHubDeviceClient client = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.AbandonMessageAsync((Message)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AbandonAsyncThrowsForNullLockToken()
        {
            IotHubDeviceClient client = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.AbandonMessageAsync((string)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AbandonAsyncWithCancellationTokenThrowsForNullLockToken()
        {
            IotHubDeviceClient client = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.AbandonMessageAsync((string)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task MessageIdDefaultNotSet_SendEventDoesNotSetMessageId()
        {
            // arrange
            var messageId = Guid.NewGuid().ToString();
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

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
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString, options);

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
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString, options);

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
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

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
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString, options);

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
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString, options);

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
        public void IotHubDeviceClient_CreateFromConnectionString_InvalidSasTimeToLive_ThrowsException()
        {
            // arrange
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                SasTokenTimeToLive = TimeSpan.FromSeconds(-60),
            };

            // act
            Action createDeviceClient = () => { IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString, options); };

            // assert
            createDeviceClient.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateFromConnectionString_InvalidSasRenewalBuffer_ThrowsException()
        {
            // arrange
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                SasTokenRenewalBuffer = 200,
            };

            // act
            Action createDeviceClient = () => { IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString, options); };

            // assert
            createDeviceClient.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateFromConnectionString_SasTokenTimeToLiveRenewalConfigurable()
        {
            // arrange
            var sasTokenTimeToLive = TimeSpan.FromMinutes(20);
            int sasTokenRenewalBuffer = 50;
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                SasTokenTimeToLive = sasTokenTimeToLive,
                SasTokenRenewalBuffer = sasTokenRenewalBuffer,
            };
            var pipelineBuilderSubstitute = Substitute.For<IDeviceClientPipelineBuilder>();

            // act
            DateTime startTime = DateTime.UtcNow;
            InternalClient internalClient = ClientFactory.CreateInternal(
                pipelineBuilderSubstitute,
                new IotHubConnectionCredentials(FakeConnectionString),
                options);

            // assert
            var authMethod = internalClient.IotHubConnectionCredentials.SasTokenRefresher;
            authMethod.Should().BeAssignableTo<DeviceAuthenticationWithSakRefresh>();

            // The calculation of the sas token expiration will begin once the AuthenticationWithTokenRefresh object has been initialized.
            // Since the initialization is internal to the ClientFactory logic and is not observable, we will allow a buffer period to our assertions.
            var buffer = TimeSpan.FromSeconds(2);

            // The initial expiration time calculated is (current UTC time - sas TTL supplied).
            // The actual expiration time associated with a sas token is recalculated during token generation, but relies on the same sas TTL supplied.
            var expectedExpirationTime = startTime.Add(-sasTokenTimeToLive);
            authMethod.ExpiresOn.Should().BeCloseTo(expectedExpirationTime, (int)buffer.TotalMilliseconds);

            int expectedBufferSeconds = (int)(sasTokenTimeToLive.TotalSeconds * ((float)sasTokenRenewalBuffer / 100));
            var expectedRefreshTime = expectedExpirationTime.AddSeconds(-expectedBufferSeconds);
            authMethod.RefreshesOn.Should().BeCloseTo(expectedRefreshTime, (int)buffer.TotalMilliseconds);
        }

        [TestMethod]
        public void IotHubDeviceClient_CreateFromAuthenticationMethod_SasTokenTimeToLiveRenewalConfigurable()
        {
            // arrange
            var sasTokenTimeToLive = TimeSpan.FromMinutes(20);
            int sasTokenRenewalBuffer = 50;
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                // Pass these values, but expect them to be ignored, given the use of AuthenticationWithTokenRefresh below.
                SasTokenTimeToLive = sasTokenTimeToLive,
                SasTokenRenewalBuffer = sasTokenRenewalBuffer,
            };
            var pipelineBuilderSubstitute = Substitute.For<IDeviceClientPipelineBuilder>();

            // This authentication method relies on the default sas token time to live and renewal buffer set by the SDK.
            // These values are 1 hour for sas token expiration and renewed when 15% or less of its lifespan is left.
            var authMethod1 = new TestDeviceAuthenticationWithTokenRefresh();
            TimeSpan sasTokenTimeToLiveSdkDefault = s_defaultSasTimeToLive;
            int sasTokenRenewalBufferSdkDefault = DefaultSasRenewalBufferPercentage;

            // act
            DateTime startTime = DateTime.UtcNow;
            InternalClient internalClient = ClientFactory.CreateInternal(
                pipelineBuilderSubstitute,
                new IotHubConnectionCredentials(authMethod1, FakeHostName),
                options);

            // assert
            // Clients created with their own specific AuthenticationWithTokenRefresh IAuthenticationMethod will ignore the sas token renewal options specified in ClientOptions.
            // Those options are configurable from the AuthenticationWithTokenRefresh implementation directly.
            var authMethod = internalClient.IotHubConnectionCredentials.SasTokenRefresher;

            // The calculation of the sas token expiration will begin once the AuthenticationWithTokenRefresh object has been initialized.
            // Since the initialization is internal to the ClientFactory logic and is not observable, we will allow a buffer period to our assertions.
            var buffer = TimeSpan.FromSeconds(2);

            // The initial expiration time calculated is (current UTC time - sas TTL supplied).
            // The actual expiration time associated with a sas token is recalculated during token generation, but relies on the same sas TTL supplied.

            DateTime sasExpirationTimeFromClientOptions = startTime.Add(-sasTokenTimeToLive);
            authMethod.ExpiresOn.Should().NotBeCloseTo(sasExpirationTimeFromClientOptions, (int)buffer.TotalMilliseconds);

            DateTime sasExpirationTimeFromSdkDefault = startTime.Add(-sasTokenTimeToLiveSdkDefault);
            authMethod.ExpiresOn.Should().BeCloseTo(sasExpirationTimeFromSdkDefault, (int)buffer.TotalMilliseconds);

            // Validate the sas token renewal buffer
            int expectedRenewalBufferSecondsFromClientOptions = (int)(sasTokenTimeToLive.TotalSeconds * ((float)sasTokenRenewalBuffer / 100));
            DateTime expectedRefreshTimeFromClientOptions = sasExpirationTimeFromSdkDefault.AddSeconds(-expectedRenewalBufferSecondsFromClientOptions);
            authMethod.RefreshesOn.Should().NotBeCloseTo(expectedRefreshTimeFromClientOptions, (int)buffer.TotalMilliseconds);

            int expectedRenewalBufferSecondsFromSdkDefault = (int)(sasTokenTimeToLiveSdkDefault.TotalSeconds * ((float)sasTokenRenewalBufferSdkDefault / 100));
            DateTime expectedRefreshTimeFromSdkDefault = sasExpirationTimeFromSdkDefault.AddSeconds(-expectedRenewalBufferSecondsFromSdkDefault);
            authMethod.RefreshesOn.Should().BeCloseTo(expectedRefreshTimeFromSdkDefault, (int)buffer.TotalMilliseconds);
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
                .Invoking(() => { using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString, clientOptions); })
                .Should()
                .NotThrow();
        }

        [TestMethod]
        public void IotHubDeviceClient_ReceiveAsync_Cancelled_ThrowsOperationCanceledException()
        {
            //arrange
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
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

            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
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

            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
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

            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
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
        public void IotHubDeviceClient_OpenAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange

            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
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

            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
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

            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
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

            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
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

            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
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

            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
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
            public TestDeviceAuthenticationWithTokenRefresh() : base("someTestDevice")
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
