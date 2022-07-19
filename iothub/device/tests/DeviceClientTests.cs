// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class DeviceClientTests
    {
        private const string FakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=";
        private const string FakeConnectionStringWithModuleId = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=fake;SharedAccessKey=dGVzdFN0cmluZzE=;ModuleId=mod1";
        private const string TestModelId = "dtmi:com:example:testModel;1";

        private static readonly IotHubConnectionStringBuilder s_csBuilder = IotHubConnectionStringBuilder.Create(FakeConnectionString);
        private static readonly IotHubConnectionString s_cs = new IotHubConnectionString(s_csBuilder);

        [TestMethod]
        public void DeviceAuthenticationWithX509Certificate_NullCertificate_Throws()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", null);

            Action act = () => DeviceClient.Create(hostName, authMethod, new ClientOptions { TransportType = TransportType.Amqp_WebSocket_Only });
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void DeviceClient_ParamsHostNameAuthMethod_Works()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", s_cs);

            using var deviceClient = DeviceClient.Create(hostName, authMethod);
        }

        [TestMethod]
        public void DeviceClient_ParamsHostNameAuthMethodTransportType_Works()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", s_cs);

            var deviceClient = DeviceClient.Create(hostName, authMethod, new ClientOptions { TransportType = TransportType.Amqp_WebSocket_Only });
        }

        [TestMethod]
        public void DeviceClient_ParamsHostNameAuthMethodTransportArray_Works()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", s_cs);

            using var deviceClient = DeviceClient.Create(
                hostName,
                authMethod,
                new ITransportSettings[]
                {
                    new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                });
        }

        [TestMethod]
        public void DeviceClient_ParamsHostNameGatewayAuthMethod_Works()
        {
            string hostName = "acme.azure-devices.net";
            string gatewayHostname = "gateway.acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", s_cs);

            using var deviceClient = DeviceClient.Create(hostName, gatewayHostname, authMethod);
        }

        [TestMethod]
        public void DeviceClient_ParamsHostNameGatewayAuthMethodTransport_Works()
        {
            string hostName = "acme.azure-devices.net";
            string gatewayHostname = "gateway.acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", s_cs);

            using var deviceClient = DeviceClient.Create(
                hostName,
                gatewayHostname,
                authMethod,
                new ClientOptions { TransportType = TransportType.Amqp_WebSocket_Only });
        }

        [TestMethod]
        public void DeviceClient_ParsmHostNameGatewayAuthMethodTransportArray_Works()
        {
            string hostName = "acme.azure-devices.net";
            string gatewayHostname = "gateway.acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", s_cs);

            using var deviceClient = DeviceClient.Create(
                hostName,
                gatewayHostname,
                authMethod,
                new ITransportSettings[]
                {
                    new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                });
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void DeviceClient_Params_GatewayAuthMethod_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", s_cs);

            using var deviceClient = DeviceClient.Create(gatewayHostname, authMethod);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void DeviceClient_ParamsGatewayAuthMethodTransport_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", s_cs);

            using var deviceClient = DeviceClient.Create(gatewayHostname, authMethod, new ClientOptions { TransportType = TransportType.Amqp_WebSocket_Only });
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void DeviceClient_ParamsGatewayAuthMethodTransportArray_Works()
        {
            string gatewayHostname = "myGatewayDevice";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", s_cs);

            using var deviceClient = DeviceClient.Create(
                gatewayHostname,
                authMethod,
                new ITransportSettings[]
                {
                    new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                });
        }

        [TestMethod]
        public void DeviceClient_CreateFromConnectionString_WithModuleIdThrows()
        {
            Action act = () => DeviceClient.CreateFromConnectionString(FakeConnectionStringWithModuleId);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void DeviceClient_DefaultDiagnosticSamplingPercentage_Ok()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            const int DefaultPercentage = 0;
            Assert.AreEqual(deviceClient.DiagnosticSamplingPercentage, DefaultPercentage);
        }

        [TestMethod]
        public void DeviceClient_SetDiagnosticSamplingPercentageInRange_Ok()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            const int ValidPercentage = 80;
            deviceClient.DiagnosticSamplingPercentage = ValidPercentage;
            Assert.AreEqual(deviceClient.DiagnosticSamplingPercentage, ValidPercentage);
        }

        [TestMethod]
        public void DeviceClient_SetDiagnosticSamplingPercentageOutOfRange_Fail()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
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
        public void DeviceClient_StartDiagLocallyThatDoNotSupport_ThrowException()
        {
            var options = new ClientOptions { TransportType = TransportType.Http1 };
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString, options);
            try
            {
                deviceClient.DiagnosticSamplingPercentage = 100;
                Assert.Fail();
            }
            catch (NotSupportedException e)
            {
                Assert.AreEqual($"{TransportType.Http1} protocol doesn't support E2E diagnostic.", e.Message);
            }
        }

        [TestMethod]
        public void DeviceClient_StartDiagLocallyWithMutipleProtocolThatDoNotSupport_ThrowException()
        {
            var transportSettings = new ITransportSettings[]
            {
                new AmqpTransportSettings(TransportType.Amqp_Tcp_Only),
                new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only),
                new Http1TransportSettings(),
            };

            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString, transportSettings);
            try
            {
                deviceClient.DiagnosticSamplingPercentage = 100;
                Assert.Fail();
            }
            catch (NotSupportedException e)
            {
                Assert.AreEqual($"{TransportType.Http1} protocol doesn't support E2E diagnostic.", e.Message);
            }
        }

        [TestMethod]
        public async Task DeviceClient_OnMethodCalled_Unsubscribe()
        {
            // arrange
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
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
        public async Task DeviceClient_OnMethodCalled_NullMethodRequest()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
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
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasEmptyBody()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(new byte[0]), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasInvalidJson()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{key")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<MethodResponseInternal>(resp => resp.Status == 400), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(isMethodHandlerCalled);
        }

        [TestMethod]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasValidJson()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        public async Task DeviceClient_OnMethodCalled_MethodResponseHasInvalidJson()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            bool isMethodHandlerCalled = false;
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\"\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<MethodResponseInternal>(resp => resp.Status == 500), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_SetMethodDefaultHandler()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodDefaultHandlerCalled = false;
            await deviceClient.SetMethodDefaultHandlerAsync((payload, context) =>
            {
                isMethodDefaultHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodDefaultHandlerCalled);
        }

        [TestMethod]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_SetMethodHandlerNotMatchedAndDefaultHandler()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
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

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(isMethodHandlerCalled);
            Assert.IsTrue(isMethodDefaultHandlerCalled);
        }

        [TestMethod]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_SetMethodHandlerAndDefaultHandler()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
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

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
            Assert.IsFalse(isMethodDefaultHandlerCalled);
        }

        [TestMethod]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_No_Result()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        public async Task DeviceClientOnMethodCalledNoMethodHandler()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalledAsync(methodRequestInternal).ConfigureAwait(false);

            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<MethodResponseInternal>(resp => resp.Status == 501), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClientSetMethodHandlerSetFirstMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            MethodCallback methodCallback = (methodRequest, userContext) =>
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
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            innerHandler.ClearReceivedCalls();
            methodCallbackCalled = false;
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.DidNotReceive().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);
        }

        [TestMethod]
        public async Task DeviceClientSetMethodHandlerSetFirstMethodDefaultHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            MethodCallback methodCallback = (methodRequest, userContext) =>
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
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            innerHandler.ClearReceivedCalls();
            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.DidNotReceive().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);
        }

        [TestMethod]
        public async Task DeviceClientSetMethodHandlerOverwriteExistingDelegate()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            MethodCallback methodCallback = (methodRequest, userContext) =>
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
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            bool methodCallbackCalled2 = false;
            string actualMethodName2 = string.Empty;
            string actualMethodBody2 = string.Empty;
            object actualMethodUserContext2 = null;
            MethodCallback methodCallback2 = (methodRequest, userContext) =>
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
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId2", new MemoryStream(Encoding.UTF8.GetBytes(methodBody2)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled2);
            Assert.AreEqual(methodName, actualMethodName2);
            Assert.AreEqual(methodBody2, actualMethodBody2);
            Assert.AreEqual(methodUserContext2, actualMethodUserContext2);
        }

        [TestMethod]
        public async Task DeviceClientSetMethodHandlerOverwriteExistingDefaultDelegate()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            MethodCallback methodCallback = (methodRequest, userContext) =>
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
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            bool methodCallbackCalled2 = false;
            string actualMethodName2 = string.Empty;
            string actualMethodBody2 = string.Empty;
            object actualMethodUserContext2 = null;
            MethodCallback methodCallback2 = (methodRequest, userContext) =>
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
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId2", new MemoryStream(Encoding.UTF8.GetBytes(methodBody2)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled2);
            Assert.AreEqual(methodName, actualMethodName2);
            Assert.AreEqual(methodBody2, actualMethodBody2);
            Assert.AreEqual(methodUserContext2, actualMethodUserContext2);
        }

        [TestMethod]
        public async Task DeviceClientSetMethodHandlerUnsetLastMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            MethodCallback methodCallback = (methodRequest, userContext) =>
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
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(methodCallbackCalled);
        }

        [TestMethod]
        public async Task DeviceClientSetMethodHandlerUnsetLastMethodHandlerWithDefaultHandlerSet()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            MethodCallback methodCallback = (methodRequest, userContext) =>
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
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            innerHandler.ClearReceivedCalls();
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.DidNotReceive().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodDefaultHandlerAsync(null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(methodCallbackCalled);
        }

        [TestMethod]
        public async Task DeviceClientSetMethodHandlerUnsetDefaultHandlerSet()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            string actualMethodBody = string.Empty;
            object actualMethodUserContext = null;
            MethodCallback methodCallback = (methodRequest, userContext) =>
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
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            innerHandler.ClearReceivedCalls();
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.DidNotReceive().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);

            methodCallbackCalled = false;
            await deviceClient.SetMethodDefaultHandlerAsync(null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalledAsync(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(methodCallbackCalled);
        }

        [TestMethod]
        public async Task DeviceClientSetMethodHandlerUnsetWhenNoMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            await deviceClient.SetMethodHandlerAsync("TestMethodName", null, null).ConfigureAwait(false);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public void DeviceClientOnConnectionOpenedInvokeHandlerForStatusChange()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            bool handlerCalled = false;
            ConnectionStatus? status = null;
            ConnectionStatusChangeReason? statusChangeReason = null;
            ConnectionStatusChangesHandler statusChangeHandler = (s, r) =>
            {
                handlerCalled = true;
                status = s;
                statusChangeReason = r;
            };
            deviceClient.SetConnectionStatusChangesHandler(statusChangeHandler);

            // Connection status changes from disconnected to connected
            deviceClient.InternalClient.OnConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);

            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(ConnectionStatus.Connected, status);
            Assert.AreEqual(ConnectionStatusChangeReason.Connection_Ok, statusChangeReason);
        }

        [TestMethod]
        public void DeviceClientOnConnectionOpenedWithNullHandler()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            bool handlerCalled = false;
            ConnectionStatus? status = null;
            ConnectionStatusChangeReason? statusChangeReason = null;
            ConnectionStatusChangesHandler statusChangeHandler = (s, r) =>
            {
                handlerCalled = true;
                status = s;
                statusChangeReason = r;
            };
            deviceClient.SetConnectionStatusChangesHandler(statusChangeHandler);
            deviceClient.SetConnectionStatusChangesHandler(null);

            // Connection status changes from disconnected to connected
            deviceClient.InternalClient.OnConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);

            Assert.IsFalse(handlerCalled);
        }

        [TestMethod]
        public void DeviceClientOnConnectionOpenedNotInvokeHandlerWithoutStatusChange()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            bool handlerCalled = false;
            ConnectionStatus? status = null;
            ConnectionStatusChangeReason? statusChangeReason = null;
            ConnectionStatusChangesHandler statusChangeHandler = (s, r) =>
            {
                handlerCalled = true;
                status = s;
                statusChangeReason = r;
            };
            deviceClient.SetConnectionStatusChangesHandler(statusChangeHandler);
            // current status = disabled

            deviceClient.InternalClient.OnConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);

            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(ConnectionStatus.Connected, status);
            Assert.AreEqual(ConnectionStatusChangeReason.Connection_Ok, statusChangeReason);
            handlerCalled = false;

            // current status = connected
            deviceClient.InternalClient.OnConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);

            Assert.IsFalse(handlerCalled);
        }

        [TestMethod]
        public void DeviceClientOnConnectionClosedInvokeHandlerAndRecoveryForStatusChange()
        {
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var sender = new object();
            bool handlerCalled = false;
            ConnectionStatus? status = null;
            ConnectionStatusChangeReason? statusChangeReason = null;
            ConnectionStatusChangesHandler statusChangeHandler = (s, r) =>
            {
                handlerCalled = true;
                status = s;
                statusChangeReason = r;
            };
            deviceClient.SetConnectionStatusChangesHandler(statusChangeHandler);

            // current status = disabled
            deviceClient.InternalClient.OnConnectionStatusChanged(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);

            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(ConnectionStatus.Connected, status);
            Assert.AreEqual(ConnectionStatusChangeReason.Connection_Ok, statusChangeReason);
            handlerCalled = false;

            // current status = connected
            deviceClient.InternalClient.OnConnectionStatusChanged(ConnectionStatus.Disconnected_Retrying, ConnectionStatusChangeReason.No_Network);

            Assert.IsTrue(handlerCalled);
            Assert.AreEqual(ConnectionStatus.Disconnected_Retrying, status);
            Assert.AreEqual(ConnectionStatusChangeReason.No_Network, statusChangeReason);
        }

        [TestMethod]
        public void ProductInfoStoresProductInfoOk()
        {
            const string userAgent = "name/version (runtime; os; arch)";
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);
            client.ProductInfo = userAgent;
            Assert.AreEqual(userAgent, client.ProductInfo);
        }

        [TestMethod]
        public void CompleteAsyncThrowsForNullMessage()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.CompleteMessageAsync((Message)null);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CompleteAsyncWithCancellationTokenThrowsForNullMessage()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.CompleteMessageAsync((Message)null, CancellationToken.None);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CompleteAsyncThrowsForNullLockToken()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.CompleteMessageAsync((string)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CompleteAsyncWithCancellationTokenThrowsForNullLockToken()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.CompleteMessageAsync((string)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void RejectAsyncThrowsForNullMessage()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.RejectMessageAsync((Message)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void RejectAsyncWithCancellationTokenThrowsForNullMessage()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.RejectMessageAsync((Message)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void RejectAsyncThrowsForNullLockToken()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.RejectMessageAsync((string)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void RejectAsyncWithCancellationTokenThrowsForNullLockToken()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.RejectMessageAsync((string)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AbandonAsyncThrowsForNullMessage()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.AbandonMessageAsync((Message)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AbandonAsyncWithCancellationTokenThrowsForNullMessage()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.AbandonMessageAsync((Message)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AbandonAsyncThrowsForNullLockToken()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.AbandonMessageAsync((string)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AbandonAsyncWithCancellationTokenThrowsForNullLockToken()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            Func<Task> act = async () => await client.AbandonMessageAsync((string)null, CancellationToken.None);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task MessageIdDefaultNotSet_SendEventDoesNotSetMessageId()
        {
            // arrange
            var messageId = Guid.NewGuid().ToString();
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

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
            var options = new ClientOptions
            {
                SdkAssignsMessageId = SdkAssignsMessageId.Never,
            };
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString, options);

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
            var options = new ClientOptions
            {
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString, options);

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
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

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
            var options = new ClientOptions
            {
                SdkAssignsMessageId = SdkAssignsMessageId.Never,
            };
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString, options);

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
            var options = new ClientOptions
            {
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString, options);

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
        public void DeviceClient_CreateFromConnectionString_InvalidSasTimeToLive_ThrowsException()
        {
            // arrange
            var options = new ClientOptions
            {
                SasTokenTimeToLive = TimeSpan.FromSeconds(-60),
            };

            // act
            Action createDeviceClient = () => { DeviceClient.CreateFromConnectionString(FakeConnectionString, options); };

            // assert
            createDeviceClient.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void DeviceClient_CreateFromConnectionString_InvalidSasRenewalBuffer_ThrowsException()
        {
            // arrange
            var options = new ClientOptions
            {
                SasTokenRenewalBuffer = 200,
            };

            // act
            Action createDeviceClient = () => { DeviceClient.CreateFromConnectionString(FakeConnectionString, options); };

            // assert
            createDeviceClient.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void DeviceClient_CreateFromConnectionString_SasTokenTimeToLiveRenewalConfigurable()
        {
            // arrange
            var sasTokenTimeToLive = TimeSpan.FromMinutes(20);
            int sasTokenRenewalBuffer = 50;
            var options = new ClientOptions
            {
                TransportType = TransportType.Mqtt,
                SasTokenTimeToLive = sasTokenTimeToLive,
                SasTokenRenewalBuffer = sasTokenRenewalBuffer,
            };
            var pipelineBuilderSubstitute = Substitute.For<IDeviceClientPipelineBuilder>();

            // act
            DateTime startTime = DateTime.UtcNow;
            InternalClient internalClient = ClientFactory.CreateFromConnectionString(
                FakeConnectionString,
                null,
                pipelineBuilderSubstitute,
                options);

            // assert
            var authMethod = internalClient.IotHubConnectionString.TokenRefresher;
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
        public void DeviceClient_CreateFromAuthenticationMethod_SasTokenTimeToLiveRenewalConfigurable()
        {
            // arrange
            var sasTokenTimeToLive = TimeSpan.FromMinutes(20);
            int sasTokenRenewalBuffer = 50;
            var options = new ClientOptions
            {
                TransportType = TransportType.Mqtt,
                SasTokenTimeToLive = sasTokenTimeToLive,
                SasTokenRenewalBuffer = sasTokenRenewalBuffer,
            };
            var pipelineBuilderSubstitute = Substitute.For<IDeviceClientPipelineBuilder>();

            // This authentication method relies on the default sas token time to live and renewal buffer set by the SDK.
            // These values are 1 hour for sas token expiration and renewed when 15% or less of its lifespan is left.
            var authMethod1 = new TestDeviceAuthenticationWithTokenRefresh();
            int sasExpirationTimeInSecondsSdkDefault = DeviceAuthenticationWithTokenRefresh.DefaultTimeToLiveSeconds;
            int sasRenewalBufferSdkDefault = DeviceAuthenticationWithTokenRefresh.DefaultBufferPercentage;

            // act
            DateTime startTime = DateTime.UtcNow;
            InternalClient internalClient = ClientFactory.CreateFromConnectionString(
                FakeConnectionString,
                authMethod1,
                pipelineBuilderSubstitute,
                options);

            // assert
            // Clients created with their own specific AuthenticationWithTokenRefresh IAuthenticationMethod will ignore the sas token renewal options specified in ClientOptions.
            // Those options are configurable from the AuthenticationWithTokenRefresh implementation directly.
            var authMethod = internalClient.IotHubConnectionString.TokenRefresher;

            // The calculation of the sas token expiration will begin once the AuthenticationWithTokenRefresh object has been initialized.
            // Since the initialization is internal to the ClientFactory logic and is not observable, we will allow a buffer period to our assertions.
            var buffer = TimeSpan.FromSeconds(2);

            // The initial expiration time calculated is (current UTC time - sas TTL supplied).
            // The actual expiration time associated with a sas token is recalculated during token generation, but relies on the same sas TTL supplied.

            var sasExpirationTimeFromClientOptions = startTime.Add(-sasTokenTimeToLive);
            authMethod.ExpiresOn.Should().NotBeCloseTo(sasExpirationTimeFromClientOptions, (int)buffer.TotalMilliseconds);

            var sasExpirationTimeFromSdkDefault = startTime.AddSeconds(-sasExpirationTimeInSecondsSdkDefault);
            authMethod.ExpiresOn.Should().BeCloseTo(sasExpirationTimeFromSdkDefault, (int)buffer.TotalMilliseconds);

            // Validate the sas token renewal buffer
            int expectedRenewalBufferSecondsFromClientOptions = (int)(sasExpirationTimeInSecondsSdkDefault * ((float)sasTokenRenewalBuffer / 100));
            var expectedRefreshTimeFromClientOptions = sasExpirationTimeFromSdkDefault.AddSeconds(-expectedRenewalBufferSecondsFromClientOptions);
            authMethod.RefreshesOn.Should().NotBeCloseTo(expectedRefreshTimeFromClientOptions, (int)buffer.TotalMilliseconds);

            int expectedRenewalBufferSecondsFromSdkDefault = (int)(sasExpirationTimeInSecondsSdkDefault * ((float)sasRenewalBufferSdkDefault / 100));
            var expectedRefreshTimeFromSdkDefault = sasExpirationTimeFromSdkDefault.AddSeconds(-expectedRenewalBufferSecondsFromSdkDefault);
            authMethod.RefreshesOn.Should().BeCloseTo(expectedRefreshTimeFromSdkDefault, (int)buffer.TotalMilliseconds);
        }

        [TestMethod]
        public void DeviceClient_InitWithTransportAndModelId_ThrowsWhenHttp()
        {
            // arrange

            var clientOptions = new ClientOptions
            {
                TransportType = TransportType.Http1,
                ModelId = TestModelId,
            };

            // act

            Action act = () => DeviceClient.CreateFromConnectionString(FakeConnectionString, clientOptions);

            // assert

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*Plug and Play*")
                .WithMessage("*HTTP*");
        }

        [TestMethod]
        public void DeviceClient_InitWithTransportArrayAndModelId_ThrowsWhenHttp()
        {
            // arrange

            var clientOptions = new ClientOptions
            {
                ModelId = TestModelId,
            };

            var allTransportTypes = new ITransportSettings[]
            {
                new AmqpTransportSettings(TransportType.Amqp_Tcp_Only),
                new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only),
                new MqttTransportSettings(TransportType.Mqtt_Tcp_Only),
                new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only),
                new Http1TransportSettings(),
            };

            // act

            Action act = () => DeviceClient.CreateFromConnectionString(FakeConnectionString, allTransportTypes, clientOptions);

            // assert

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*Plug and Play*")
                .WithMessage("*HTTP*");
        }

        [TestMethod]
        public void Deviceclient_InitWithNonHttpTransportAndModelId_DoesNotThrow()
        {
            // arrange

            var clientOptions = new ClientOptions
            {
                ModelId = TestModelId,
            };

            var allTransportTypes = new ITransportSettings[]
            {
                new AmqpTransportSettings(TransportType.Amqp_Tcp_Only),
                new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only),
                new MqttTransportSettings(TransportType.Mqtt_Tcp_Only),
                new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only),
            };

            // act and assert
            FluentActions
                .Invoking(() => { using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString, allTransportTypes, clientOptions); })
                .Should()
                .NotThrow();
        }

        [TestMethod]
        public void DeviceClient_InitWithHttpTransportButNoModelId_DoesNotThrow()
        {
            var options = new ClientOptions { TransportType = TransportType.Http1 };
            // act and assert
            FluentActions
                .Invoking(() => { using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString, options); })
                .Should()
                .NotThrow();
        }

        [TestMethod]
        public void DeviceClient_ReceiveAsync_Cancelled_ThrowsOperationCanceledException()
        {
            //arrange
            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

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
        public void DeviceClient_CompleteAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange

            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
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
        public void DeviceClient_RejectAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange

            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.RejectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
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
        public void DeviceClient_SendEventAsync_Cancelled_ThrowsOperationCanceledException()
        {
            //arrange

            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

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

            using var message = new Message();
            Func<Task> act = async () => await deviceClient.SendEventAsync(message, cts.Token);

            // assert

            act.Should().Throw<OperationCanceledException>();
        }

        [TestMethod]
        public void DeviceClient_OpenAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange

            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

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
        public void DeviceClient_AbandonAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange

            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            var mainProtocolHandler = Substitute.For<IDelegatingHandler>();

            // We will setup the main handler which can be either MQTT or AMQP or HTTP handler to throw
            // a cancellation token expiry exception (OperationCancelledException) To ensure that we mimic when a token expires.
            mainProtocolHandler
                .When(x => x.AbandonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()))
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
        public void DeviceClient_UpdateReportedPropertiesAsync_Cancelled_ThrowsOperationCanceledException()
        {
            //arrange

            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

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
        public void DeviceClient_GetTwinAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange

            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

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
        public void DeviceClient_CloseAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange

            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

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
        public void DeviceClient_SetDesiredPropertyCallbackAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange

            using var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

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
            protected override Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive)
            {
                return Task.FromResult<string>("someToken");
            }
        }
    }
}
