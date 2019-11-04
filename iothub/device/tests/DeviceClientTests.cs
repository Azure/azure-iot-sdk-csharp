﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class DeviceClientTests
    {
        const string fakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
        const string fakeConnectionStringWithModuleId = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=;ModuleId=mod1";

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClient_CreateFromConnectionString_WithModuleIdThrows()
        {
            DeviceClient.CreateFromConnectionString(fakeConnectionStringWithModuleId);
        }

        /* Tests_SRS_DEVICECLIENT_28_002: [This property shall be defaulted to 240000 (4 minutes).] */
        [TestMethod]
        public void DeviceClient_OperationTimeoutInMilliseconds_Property_DefaultValue()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            Assert.AreEqual((uint)(4 * 60 * 1000), deviceClient.OperationTimeoutInMilliseconds);
        }

        [TestMethod]
        public void DeviceClient_DefaultDiagnosticSamplingPercentage_Ok()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            const int DefaultPercentage = 0;
            Assert.AreEqual(deviceClient.DiagnosticSamplingPercentage, DefaultPercentage);
        }

        [TestMethod]
        public void DeviceClient_SetDiagnosticSamplingPercentageInRange_Ok()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            const int ValidPercentage = 80;
            deviceClient.DiagnosticSamplingPercentage = ValidPercentage;
            Assert.AreEqual(deviceClient.DiagnosticSamplingPercentage, ValidPercentage);
        }

        [TestMethod]
        public void DeviceClient_SetDiagnosticSamplingPercentageOutOfRange_Fail()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            const int DefaultPercentage = 0;
            const int InvalidPercentageExceedUpperLimit = 200;
            const int InvalidPercentageExceedLowerLimit = -100;

            try
            {
                deviceClient.DiagnosticSamplingPercentage = InvalidPercentageExceedUpperLimit;
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException e)
            {
                Assert.AreEqual(deviceClient.DiagnosticSamplingPercentage, DefaultPercentage);
            }

            try
            {
                deviceClient.DiagnosticSamplingPercentage = InvalidPercentageExceedLowerLimit;
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException e)
            {
                Assert.AreEqual(deviceClient.DiagnosticSamplingPercentage, DefaultPercentage);
            }
        }

        [TestMethod]
        public void DeviceClient_StartDiagLocallyThatDoNotSupport_ThrowException()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString, TransportType.Http1);
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
                new Http1TransportSettings()
            };

            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString, transportSettings);
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
        public void DeviceClient_OperationTimeoutInMilliseconds_Property_GetSet()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            deviceClient.OperationTimeoutInMilliseconds = 9999;

            Assert.AreEqual((uint)9999, deviceClient.OperationTimeoutInMilliseconds);
        }

        [TestMethod]
        public async Task DeviceClient_OperationTimeoutInMilliseconds_Equals_0_Open()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            deviceClient.OperationTimeoutInMilliseconds = 0;

            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.OpenAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            deviceClient.InnerHandler = innerHandler;

            Task t = deviceClient.OpenAsync();

            await innerHandler.Received().OpenAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_OperationTimeoutInMilliseconds_Equals_0_Receive()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.ReceiveAsync(Arg.Any<TimeoutHelper>()).Returns(new Task<Message>(() => new Message()));
            deviceClient.InnerHandler = innerHandler;

            Task<Message> t = deviceClient.ReceiveAsync(TimeSpan.Zero);

            await innerHandler.Received().ReceiveAsync(Arg.Any<TimeoutHelper>()).ConfigureAwait(false);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_012: [** If the given methodRequestInternal argument is null, fail silently **]**
        public async Task DeviceClient_OnMethodCalled_NullMethodRequest()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("testMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            await deviceClient.InternalClient.OnMethodCalled(null).ConfigureAwait(false);
            await innerHandler.Received(0).SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(isMethodHandlerCalled);
        }

        [TestMethod]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasEmptyBody()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Array.Empty<byte>()), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalled(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_28_020: [** If the given methodRequestInternal data is not valid json, respond with status code 400 (BAD REQUEST) **]**
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasInvalidJson()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{key")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalled(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<MethodResponseInternal>(resp => resp.Status == 400), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(isMethodHandlerCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_011: [ The OnMethodCalled shall invoke the specified delegate. ]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasValidJson()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalled(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_28_021: [** If the MethodResponse from the MethodHandler is not valid json, respond with status code 500 (USER CODE EXCEPTION) **]**
        public async Task DeviceClient_OnMethodCalled_MethodResponseHasInvalidJson()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            bool isMethodHandlerCalled = false;
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            await deviceClient.SetMethodHandlerAsync("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\"\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalled(methodRequestInternal).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<MethodResponseInternal>(resp => resp.Status == 500), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_012: [** If the given methodRequestInternal argument is null, fail silently **]**
        public async Task DeviceClient_OnMethodCalled_NullMethodRequest_With_SetMethodHandler()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool isMethodHandlerCalled = false;
            deviceClient.SetMethodHandler("testMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data");

            await deviceClient.InternalClient.OnMethodCalled(null).ConfigureAwait(false);
            await innerHandler.Received(0).SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(isMethodHandlerCalled);
        }

        [TestMethod]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasEmptyBody_With_SetMethodHandler()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            bool isMethodHandlerCalled = false;
            deviceClient.SetMethodHandler("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data");

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Array.Empty<byte>()), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalled(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_011: [ The OnMethodCalled shall invoke the specified delegate. ]
        // Tests_SRS_DEVICECLIENT_03_013: [Otherwise, the MethodResponseInternal constructor shall be invoked with the result supplied.]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_SetMethodHandler()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            deviceClient.SetMethodHandler("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data");

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalled(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_24_002: [ The OnMethodCalled shall invoke the default delegate if there is no specified delegate for that method. ]
        // Tests_SRS_DEVICECLIENT_03_013: [Otherwise, the MethodResponseInternal constructor shall be invoked with the result supplied.]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_SetMethodDefaultHandler()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodDefaultHandlerCalled = false;
            await deviceClient.SetMethodDefaultHandlerAsync((payload, context) =>
            {
                isMethodDefaultHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200));
            }, "custom data").ConfigureAwait(false);

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalled(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodDefaultHandlerCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_24_002: [ The OnMethodCalled shall invoke the default delegate if there is no specified delegate for that method. ]
        // Tests_SRS_DEVICECLIENT_03_013: [Otherwise, the MethodResponseInternal constructor shall be invoked with the result supplied.]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_SetMethodHandlerNotMatchedAndDefaultHandler()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
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

            await deviceClient.InternalClient.OnMethodCalled(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(isMethodHandlerCalled);
            Assert.IsTrue(isMethodDefaultHandlerCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_011: [ The OnMethodCalled shall invoke the specified delegate. ]
        // Tests_SRS_DEVICECLIENT_03_013: [Otherwise, the MethodResponseInternal constructor shall be invoked with the result supplied.]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_SetMethodHandlerAndDefaultHandler()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
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

            await deviceClient.InternalClient.OnMethodCalled(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
            Assert.IsFalse(isMethodDefaultHandlerCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_011: [ The OnMethodCalled shall invoke the specified delegate. ]
        // Tests_SRS_DEVICECLIENT_03_012: [If the MethodResponse does not contain result, the MethodResponseInternal constructor shall be invoked with no results.]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasValidJson_With_SetMethodHandler_With_No_Result()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            bool isMethodHandlerCalled = false;
            deviceClient.SetMethodHandler("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(200));
            }, "custom data");

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalled(methodRequestInternal).ConfigureAwait(false);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_28_021: [** If the MethodResponse from the MethodHandler is not valid json, respond with status code 500 (USER CODE EXCEPTION) **]**
        public async Task DeviceClientOnMethodCalledMethodResponseHasInvalidJsonWithSetMethodHandler()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            bool isMethodHandlerCalled = false;
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            deviceClient.SetMethodHandler("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"name\"\"ABC\"}"), 200));
            }, "custom data");

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalled(methodRequestInternal).ConfigureAwait(false);
            Assert.IsTrue(isMethodHandlerCalled);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<MethodResponseInternal>(resp => resp.Status == 500), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_013: [** If the given method does not have an associated delegate and no default delegate was registered, respond with status code 501 (METHOD NOT IMPLEMENTED) **]**
        public async Task DeviceClientOnMethodCalledNoMethodHandler()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")), CancellationToken.None);

            await deviceClient.InternalClient.OnMethodCalled(methodRequestInternal).ConfigureAwait(false);

            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<MethodResponseInternal>(resp => resp.Status == 501), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_001: [ It shall lazy-initialize the deviceMethods property. ]
        // Tests_SRS_DEVICECLIENT_10_003: [ The given delegate will only be added if it is not null. ]
        public async Task DeviceClientSetMethodHandlerSetFirstMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            innerHandler.ClearReceivedCalls();
            methodCallbackCalled = false;
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.DidNotReceive().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_001: [ It shall lazy-initialize the deviceMethods property. ]
        // Tests_SRS_DEVICECLIENT_10_003: [ The given delegate will only be added if it is not null. ]
        public async Task DeviceClientSetMethodHandlerSetFirstMethodDefaultHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            innerHandler.ClearReceivedCalls();
            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.DidNotReceive().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_002: [ If the given methodName already has an associated delegate, the existing delegate shall be removed. ]
        public async Task DeviceClientSetMethodHandlerOverwriteExistingDelegate()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodUserContext2 = "UserContext2";
            string methodBody2 = "{\"grade\":\"bad\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback2, methodUserContext2).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId2", new MemoryStream(Encoding.UTF8.GetBytes(methodBody2)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled2);
            Assert.AreEqual(methodName, actualMethodName2);
            Assert.AreEqual(methodBody2, actualMethodBody2);
            Assert.AreEqual(methodUserContext2, actualMethodUserContext2);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_24_001: [ If the default callback has already been set, it is replaced with the new callback. ]
        public async Task DeviceClientSetMethodHandlerOverwriteExistingDefaultDelegate()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodUserContext2 = "UserContext2";
            string methodBody2 = "{\"grade\":\"bad\"}";
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback2, methodUserContext2).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId2", new MemoryStream(Encoding.UTF8.GetBytes(methodBody2)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled2);
            Assert.AreEqual(methodName, actualMethodName2);
            Assert.AreEqual(methodBody2, actualMethodBody2);
            Assert.AreEqual(methodUserContext2, actualMethodUserContext2);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_004: [ The deviceMethods property shall be deleted if the last delegate has been removed. ]
        // Tests_SRS_DEVICECLIENT_10_006: [ It shall DisableMethodsAsync when the last delegate has been removed. ]
        public async Task DeviceClientSetMethodHandlerUnsetLastMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            // TODO #890
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(methodCallbackCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_004: [ The deviceMethods property shall be deleted if the last delegate has been removed. ]
        // Tests_SRS_DEVICECLIENT_10_006: [ It shall DisableMethodsAsync when the last delegate has been removed. ]
        public async Task DeviceClientSetMethodHandlerUnsetLastMethodHandlerWithDefaultHandlerSet()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            innerHandler.ClearReceivedCalls();
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.DidNotReceive().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodDefaultHandlerAsync(null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            // TODO #890
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(methodCallbackCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_004: [ The deviceMethods property shall be deleted if the last delegate has been removed. ]
        // Tests_SRS_DEVICECLIENT_10_006: [ It shall DisableMethodsAsync when the last delegate has been removed. ]
        public async Task DeviceClientSetMethodHandlerUnsetDefaultHandlerSet()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            innerHandler.ClearReceivedCalls();
            await deviceClient.SetMethodDefaultHandlerAsync(methodCallback, methodUserContext).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.DidNotReceive().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);

            methodCallbackCalled = false;
            await deviceClient.SetMethodDefaultHandlerAsync(null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            // TODO #890
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(methodCallbackCalled);
        }

        [TestMethod]
        public async Task DeviceClientSetMethodHandlerUnsetWhenNoMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            await deviceClient.SetMethodHandlerAsync("TestMethodName", null, null).ConfigureAwait(false);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_001: [ It shall lazy-initialize the deviceMethods property. ]
        // Tests_SRS_DEVICECLIENT_10_003: [ The given delegate will only be added if it is not null. ]
        public async Task DeviceClientSetMethodHandlerSetFirstMethodHandlerWithSetMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            deviceClient.SetMethodHandler(methodName, methodCallback, methodUserContext);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_002: [ If the given methodName already has an associated delegate, the existing delegate shall be removed. ]
        public async Task DeviceClientSetMethodHandlerOverwriteExistingDelegateWithSetMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            deviceClient.SetMethodHandler(methodName, methodCallback, methodUserContext);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodUserContext2 = "UserContext2";
            string methodBody2 = "{\"grade\":\"bad\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback2, methodUserContext2).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId2", new MemoryStream(Encoding.UTF8.GetBytes(methodBody2)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled2);
            Assert.AreEqual(methodName, actualMethodName2);
            Assert.AreEqual(methodBody2, actualMethodBody2);
            Assert.AreEqual(methodUserContext2, actualMethodUserContext2);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_10_004: [ The deviceMethods property shall be deleted if the last delegate has been removed. ]
        // Tests_SRS_DEVICECLIENT_10_006: [ It shall DisableMethodsAsync when the last delegate has been removed. ]
        public async Task DeviceClientSetMethodHandlerUnsetLastMethodHandlerWithSetMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

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
                return Task.FromResult(new MethodResponse(Array.Empty<byte>(), 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            deviceClient.SetMethodHandler(methodName, methodCallback, methodUserContext);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, null, null).ConfigureAwait(false);
            await deviceClient.InternalClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody)), CancellationToken.None)).ConfigureAwait(false);

            //TODO #890
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsFalse(methodCallbackCalled);
        }

        [TestMethod]
        public async Task DeviceClientSetMethodHandlerUnsetWhenNoMethodHandlerWithSetMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            deviceClient.SetMethodHandler("TestMethodName", null, null);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_28_024: [** `OnConnectionOpened` shall invoke the connectionStatusChangesHandler if ConnectionStatus is changed **]**
        // Tests_SRS_DEVICECLIENT_28_025: [** `SetConnectionStatusChangesHandler` shall set connectionStatusChangesHandler **]**
        // Tests_SRS_DEVICECLIENT_28_026: [** `SetConnectionStatusChangesHandler` shall unset connectionStatusChangesHandler if `statusChangesHandler` is null **]**
        public void DeviceClientOnConnectionOpenedInvokeHandlerForStatusChange()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
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
        // Tests_SRS_DEVICECLIENT_28_026: [** `SetConnectionStatusChangesHandler` shall unset connectionStatusChangesHandler if `statusChangesHandler` is null **]**
        public void DeviceClientOnConnectionOpenedWithNullHandler()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
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
        // Tests_SRS_DEVICECLIENT_28_024: [** `OnConnectionOpened` shall invoke the connectionStatusChangesHandler if ConnectionStatus is changed **]**
        public void DeviceClientOnConnectionOpenedNotInvokeHandlerWithoutStatusChange()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
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
        // Tests_SRS_DEVICECLIENT_28_022: [** `OnConnectionClosed` shall invoke the RecoverConnections process. **]**
        // Tests_SRS_DEVICECLIENT_28_023: [** `OnConnectionClosed` shall invoke the connectionStatusChangesHandler if ConnectionStatus is changed. **]**
        public async Task DeviceClientOnConnectionClosedInvokeHandlerAndRecoveryForStatusChange()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
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
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            client.ProductInfo = userAgent;
            Assert.AreEqual(userAgent, client.ProductInfo);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CompleteAsyncThrowsForNullMessage()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            await client.CompleteAsync((Message)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CompleteAsyncWithCancellationTokenThrowsForNullMessage()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            await client.CompleteAsync((Message)null, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CompleteAsyncThrowsForNullLockToken()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            await client.CompleteAsync((string)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CompleteAsyncWithCancellationTokenThrowsForNullLockToken()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            await client.CompleteAsync((string)null, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RejectAsyncThrowsForNullMessage()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            await client.RejectAsync((Message)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RejectAsyncWithCancellationTokenThrowsForNullMessage()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            await client.RejectAsync((Message)null, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RejectAsyncThrowsForNullLockToken()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            await client.RejectAsync((string)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RejectAsyncWithCancellationTokenThrowsForNullLockToken()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            await client.RejectAsync((string)null, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AbandonAsyncThrowsForNullMessage()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            await client.AbandonAsync((Message)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AbandonAsyncWithCancellationTokenThrowsForNullMessage()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            await client.AbandonAsync((Message)null, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AbandonAsyncThrowsForNullLockToken()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            await client.AbandonAsync((string)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AbandonAsyncWithCancellationTokenThrowsForNullLockToken()
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            await client.AbandonAsync((string)null, CancellationToken.None);
        }
    }
}