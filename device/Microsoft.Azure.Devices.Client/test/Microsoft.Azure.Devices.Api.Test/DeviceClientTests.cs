namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Common;
    using Microsoft.Azure.Devices.Client.Transport;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using NSubstitute;
    using NSubstitute.ExceptionExtensions;

    [TestClass]
    public class DeviceClientTests
    {
        /* Tests_SRS_DEVICECLIENT_28_002: [This property shall be defaulted to 240000 (4 minutes).] */
        [TestMethod]
        [TestCategory("DevClient")]
        public void DeviceClient_OperationTimeoutInMilliseconds_Property_DefaultValue()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            Assert.AreEqual((uint)(4 * 60 * 1000), deviceClient.OperationTimeoutInMilliseconds);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        public void DeviceClient_OperationTimeoutInMilliseconds_Property_GetSet()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
            deviceClient.OperationTimeoutInMilliseconds = 9999;

            Assert.AreEqual((uint)9999, deviceClient.OperationTimeoutInMilliseconds);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        public async Task DeviceClient_OperationTimeoutInMilliseconds_Equals_0_Open()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
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
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
            deviceClient.OperationTimeoutInMilliseconds = 0;

            var innerHandler = Substitute.For<IDelegatingHandler>();
            innerHandler.ReceiveAsync(Arg.Is<CancellationToken>(ct => ct.CanBeCanceled == false)).Returns(new Task<Message>(() => new Message()));
            deviceClient.InnerHandler = innerHandler;

            Task<Message> t = deviceClient.ReceiveAsync();
            
            await innerHandler.Received().ReceiveAsync(Arg.Is<CancellationToken>(ct => ct.CanBeCanceled == false));
        }
        
        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_10_012: [** If the given method argument is null, fail silently **]**
        public async Task DeviceClient_OnMethodCalled_NullMethodRequest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
            bool isMethodHandlerCalled = false;
            deviceClient.SetMethodHandler("testMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return new MethodCallbackReturn(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200);
            }, "custom data");
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            await deviceClient.OnMethodCalled(null);

            await innerHandler.Received(0).SendMethodResponseAsync(Arg.Any<MethodResponse>(), Arg.Any<CancellationToken>());
            Assert.IsFalse(isMethodHandlerCalled);
        }
        
        [TestMethod]
        [TestCategory("DeviceClient")]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasEmptyBody()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
            bool isMethodHandlerCalled = false;
            deviceClient.SetMethodHandler("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return new MethodCallbackReturn(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200);
            }, "custom data");
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var methodRequest = new MethodRequest("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(new byte[0]));

            await deviceClient.OnMethodCalled(methodRequest);
            
            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponse>(), Arg.Any<CancellationToken>());
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_28_020: [** If the given methodRequest data is not valid json, fail silently **]**
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasInvalidJson()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
            bool isMethodHandlerCalled = false;
            deviceClient.SetMethodHandler("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return new MethodCallbackReturn(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200);
            }, "custom data");
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var methodRequest = new MethodRequest("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{key")));

            await deviceClient.OnMethodCalled(methodRequest);

            await innerHandler.DidNotReceive().SendMethodResponseAsync(Arg.Any<MethodResponse>(), Arg.Any<CancellationToken>());
            Assert.IsFalse(isMethodHandlerCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_10_011: [ The OnMethodCalled shall invoke the specified delegate. ]
        public async Task DeviceClient_OnMethodCalled_MethodRequestHasValidJson()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
            bool isMethodHandlerCalled = false;
            deviceClient.SetMethodHandler("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return new MethodCallbackReturn(Encoding.UTF8.GetBytes("{\"name\":\"ABC\"}"), 200);
            }, "custom data");
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var methodRequest = new MethodRequest("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")));

            await deviceClient.OnMethodCalled(methodRequest);

            await innerHandler.Received().SendMethodResponseAsync(Arg.Any<MethodResponse>(), Arg.Any<CancellationToken>());
            Assert.IsTrue(isMethodHandlerCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_28_021: [** If the MethodCallbackReturn from the MethodHandler is not valid json, JsonReaderException shall be throw **]**
        public async Task DeviceClient_OnMethodCalled_MethodResponseHasInvalidJson()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
            bool isMethodHandlerCalled = false;
            deviceClient.SetMethodHandler("TestMethodName", (payload, context) =>
            {
                isMethodHandlerCalled = true;
                return new MethodCallbackReturn(Encoding.UTF8.GetBytes("{\"name\"\"ABC\"}"), 200);
            }, "custom data");
            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var methodRequest = new MethodRequest("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")));

            await TestAssert.ThrowsAsync<JsonReaderException>(() => deviceClient.OnMethodCalled(methodRequest));
            Assert.IsTrue(isMethodHandlerCalled);
            await innerHandler.DidNotReceive().SendMethodResponseAsync(Arg.Any<MethodResponse>(), Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_10_013: [** If the given method does not have an associated delegate, failed silently **]**
        public async Task DeviceClient_OnMethodCalled_NoMethodHandler()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;
            var methodRequest = new MethodRequest("TestMethodName", "4B810AFC-CF5B-4AE8-91EB-245F7C7751F9", new MemoryStream(Encoding.UTF8.GetBytes("{\"grade\":\"good\"}")));

            await deviceClient.OnMethodCalled(methodRequest);

            await innerHandler.DidNotReceive().SendMethodResponseAsync(Arg.Any<MethodResponse>(), Arg.Any<CancellationToken>());
        }

    }
}
