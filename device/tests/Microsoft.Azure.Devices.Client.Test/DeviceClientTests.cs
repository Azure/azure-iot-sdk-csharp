namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using NSubstitute.ExceptionExtensions;

    [TestClass]
    public class DeviceClientTests
    {
        static string fakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";

        /* Tests_SRS_DEVICECLIENT_28_002: [This property shall be defaulted to 240000 (4 minutes).] */
        [TestMethod]
        [TestCategory("DevClient")]
        public void DeviceClient_OperationTimeoutInMilliseconds_Property_DefaultValue()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            Assert.AreEqual((uint)(4 * 60 * 1000), deviceClient.OperationTimeoutInMilliseconds);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        public void DeviceClient_OperationTimeoutInMilliseconds_Property_GetSet()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            deviceClient.OperationTimeoutInMilliseconds = 9999;

            Assert.AreEqual((uint)9999, deviceClient.OperationTimeoutInMilliseconds);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        public async Task DeviceClient_OperationTimeoutInMilliseconds_Equals_0_Open()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            deviceClient.OperationTimeoutInMilliseconds = 0;

            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.OpenAsync(Arg.Any<bool>(), Arg.Is<CancellationToken>(ct => ct.CanBeCanceled == false)).Returns(TaskConstants.Completed);
            deviceClient.InnerHandler = innerHandler;

            Task t = deviceClient.OpenAsync();

            await innerHandler.Received().OpenAsync(Arg.Any<bool>(), Arg.Is<CancellationToken>(ct => ct.CanBeCanceled == false));
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        public async Task DeviceClient_OperationTimeoutInMilliseconds_Equals_0_Receive()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);
            deviceClient.OperationTimeoutInMilliseconds = 0;

            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.ReceiveAsync(Arg.Is<CancellationToken>(ct => ct.CanBeCanceled == false)).Returns(new Task<Message>(() => new Message()));
            deviceClient.InnerHandler = innerHandler;

            Task<Message> t = deviceClient.ReceiveAsync();
            
            await innerHandler.Received().ReceiveAsync(Arg.Is<CancellationToken>(ct => ct.CanBeCanceled == false));
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
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
            }, "custom data");

            await deviceClient.OnMethodCalled(null);
            await innerHandler.Received(0).SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>());
            Assert.IsFalse(isMethodHandlerCalled);
        }
        
        [TestMethod]
        [TestCategory("DeviceClient")]
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
            }, "custom data");

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(new byte[0]));

            await deviceClient.OnMethodCalled(methodRequestInternal);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>());
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
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
            }, "custom data");

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{key")));

            await deviceClient.OnMethodCalled(methodRequestInternal);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<MethodResponseInternal>(resp => resp.Status == 400), Arg.Any<CancellationToken>());
            Assert.IsFalse(isMethodHandlerCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
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
            }, "custom data");

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")));

            await deviceClient.OnMethodCalled(methodRequestInternal);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>());
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
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
            }, "custom data");

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")));

            await deviceClient.OnMethodCalled(methodRequestInternal);
            Assert.IsTrue(isMethodHandlerCalled);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<MethodResponseInternal>(resp => resp.Status == 500), Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
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

            await deviceClient.OnMethodCalled(null);
            await innerHandler.Received(0).SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>());
            Assert.IsFalse(isMethodHandlerCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
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

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(new byte[0]));

            await deviceClient.OnMethodCalled(methodRequestInternal);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>());
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
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

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")));

            await deviceClient.OnMethodCalled(methodRequestInternal);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>());
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
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

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")));

            await deviceClient.OnMethodCalled(methodRequestInternal);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponseInternal>(), Arg.Any<CancellationToken>());
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_28_021: [** If the MethodResponse from the MethodHandler is not valid json, respond with status code 500 (USER CODE EXCEPTION) **]**
        public async Task DeviceClient_OnMethodCalled_MethodResponseHasInvalidJson_With_SetMethodHandler()
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

            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")));

            await deviceClient.OnMethodCalled(methodRequestInternal);
            Assert.IsTrue(isMethodHandlerCalled);
            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<MethodResponseInternal>(resp => resp.Status == 500), Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_10_013: [** If the given method does not have an associated delegate, respond with status code 501 (METHOD NOT IMPLEMENTED) **]**
        public async Task DeviceClient_OnMethodCalled_NoMethodHandler()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var methodRequestInternal = new MethodRequestInternal("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")));

            await deviceClient.OnMethodCalled(methodRequestInternal);

            await innerHandler.Received().SendMethodResponseAsync(Arg.Is<MethodResponseInternal>(resp => resp.Status == 501), Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_10_001: [ The SetMethodHandler shall lazy-initialize the deviceMethods property. ]
        // Tests_SRS_DEVICECLIENT_10_003: [ The given delegate will only be added if it is not null. ]
        public async Task DeviceClient_SetMethodHandler_SetFirstMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
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
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext);
            await deviceClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody))));

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>());
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_10_002: [ If the given methodName already has an associated delegate, the existing delegate shall be removed. ]
        public async Task DeviceClient_SetMethodHandler_OverwriteExistingDelegate()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
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
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext);
            await deviceClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody))));

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>());
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
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback2, methodUserContext2);
            await deviceClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId2", new MemoryStream(Encoding.UTF8.GetBytes(methodBody2))));

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>());
            Assert.IsTrue(methodCallbackCalled2);
            Assert.AreEqual(methodName, actualMethodName2);
            Assert.AreEqual(methodBody2, actualMethodBody2);
            Assert.AreEqual(methodUserContext2, actualMethodUserContext2);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_10_004: [ The deviceMethods property shall be deleted if the last delegate has been removed. ]
        // Tests_SRS_DEVICECLIENT_10_006: [ The SetMethodHandler shall DisableMethodsAsync when the last delegate has been removed. ]
        public async Task DeviceClient_SetMethodHandler_UnsetLastMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
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
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback, methodUserContext);
            await deviceClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody))));

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>());
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, null, null);
            await deviceClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody))));

            await innerHandler.Received().DisableMethodsAsync(Arg.Any<CancellationToken>());
            Assert.IsFalse(methodCallbackCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        public async Task DeviceClient_SetMethodHandler_UnsetWhenNoMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            await deviceClient.SetMethodHandlerAsync("TestMethodName", null, null);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_10_001: [ The SetMethodHandler shall lazy-initialize the deviceMethods property. ]
        // Tests_SRS_DEVICECLIENT_10_003: [ The given delegate will only be added if it is not null. ]
        public async Task DeviceClient_SetMethodHandler_SetFirstMethodHandler_With_SetMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
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
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            deviceClient.SetMethodHandler(methodName, methodCallback, methodUserContext);
            await deviceClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody))));

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>());
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_10_002: [ If the given methodName already has an associated delegate, the existing delegate shall be removed. ]
        public async Task DeviceClient_SetMethodHandler_OverwriteExistingDelegate_With_SetMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
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
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            deviceClient.SetMethodHandler(methodName, methodCallback, methodUserContext);
            await deviceClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody))));

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>());
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
            await deviceClient.SetMethodHandlerAsync(methodName, methodCallback2, methodUserContext2);
            await deviceClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId2", new MemoryStream(Encoding.UTF8.GetBytes(methodBody2))));

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>());
            Assert.IsTrue(methodCallbackCalled2);
            Assert.AreEqual(methodName, actualMethodName2);
            Assert.AreEqual(methodBody2, actualMethodBody2);
            Assert.AreEqual(methodUserContext2, actualMethodUserContext2);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_10_004: [ The deviceMethods property shall be deleted if the last delegate has been removed. ]
        // Tests_SRS_DEVICECLIENT_10_006: [ The SetMethodHandler shall DisableMethodsAsync when the last delegate has been removed. ]
        public async Task DeviceClient_SetMethodHandler_UnsetLastMethodHandler_With_SetMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
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
                return Task.FromResult(new MethodResponse(new byte[0], 200));
            };

            string methodName = "TestMethodName";
            string methodUserContext = "UserContext";
            string methodBody = "{\"grade\":\"good\"}";
            deviceClient.SetMethodHandler(methodName, methodCallback, methodUserContext);
            await deviceClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody))));

            await innerHandler.Received().EnableMethodsAsync(Arg.Any<CancellationToken>());
            Assert.IsTrue(methodCallbackCalled);
            Assert.AreEqual(methodName, actualMethodName);
            Assert.AreEqual(methodBody, actualMethodBody);
            Assert.AreEqual(methodUserContext, actualMethodUserContext);

            methodCallbackCalled = false;
            await deviceClient.SetMethodHandlerAsync(methodName, null, null);
            await deviceClient.OnMethodCalled(new MethodRequestInternal(methodName, "fakeRequestId", new MemoryStream(Encoding.UTF8.GetBytes(methodBody))));

            await innerHandler.Received().DisableMethodsAsync(Arg.Any<CancellationToken>());
            Assert.IsFalse(methodCallbackCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        public async Task DeviceClient_SetMethodHandler_UnsetWhenNoMethodHandler_With_SetMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            deviceClient.SetMethodHandler("TestMethodName", null, null);
            await innerHandler.DidNotReceive().DisableMethodsAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_28_22: [** The OnConnectionClosed shall invoke the RecoverConnections operation. **]**
        public async Task DeviceClient_OnConnectionClosed_Recover()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var sender = new object();

            deviceClient.OnConnectionClosed(sender, null);

            await innerHandler.Received().RecoverConnections(sender, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_28_023: [** If RecoverConnections operations throw exception, the OnConnectionClosed shall failed silently **]**
        public void DeviceClient_OnConnectionClosed_RecoverThrowsExceptionWillEatUp()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            innerHandler.RecoverConnections(Arg.Any<object>(), Arg.Any<CancellationToken>()).Throws<InvalidOperationException>();
            
            deviceClient.OnConnectionClosed(null, null);

            // Expected: exception should be eat up
        }
    }
}