// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    [TestCategory("AppContext")]
    public class AppContextTests
    {
        private readonly string DevicePrefix = $"E2E_{nameof(AppContextTests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public AppContextTests()
        {
#if !NET451
            AppContext.SetSwitch(AppContextConstants.DisableObjectDisposedExceptionForReceiveAsync, true);
#endif

            _listener = TestConfig.StartEventListener();
        }

        #region ReceiveAsyncAfterDispose

        [TestMethod]
        public async Task AppConfig_ReceiveAsyncAfterDispose_Sasl_Amqp()
        {

            bool isFlagSet = false;
            bool flag = false;

#if !NET451
            isFlagSet = AppContext.TryGetSwitch(AppContextConstants.DisableObjectDisposedExceptionForReceiveAsync, out flag);
#endif

            if (isFlagSet)
            {
                await ReceiveAsyncAfterDispose(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, false).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("AppContext flag DisableObjectDisposedExceptionForReceiveAsync not set");
            }
        }

        [TestMethod]
        public async Task AppConfig_ReceiveAsyncWithCancellationTokenAfterDispose_Sasl_Amqp()
        {

            bool isFlagSet = false;
            bool flag = false;

#if !NET451
            isFlagSet = AppContext.TryGetSwitch(AppContextConstants.DisableObjectDisposedExceptionForReceiveAsync, out flag);
#endif

            if (isFlagSet)
            {
                await ReceiveAsyncAfterDispose(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, true).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("AppContext flag DisableObjectDisposedExceptionForReceiveAsync not set");
            }
        }

        private async Task ReceiveAsyncAfterDispose(TestDeviceType testDeviceType, Client.TransportType transportType, bool expectException)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, testDeviceType).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transportType))
            {
                Exception exceptionCaught = null;
                Client.Message message = null;
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

                try
                {
                    await deviceClient.OpenAsync().ConfigureAwait(false);

                    Task<Client.Message> t;

                    if (expectException)
                    {
                        // ReceiveAsync with cancellation token should always throw exception.
                        t = deviceClient.ReceiveAsync(cts.Token);
                    }
                    else
                    {
                        t = deviceClient.ReceiveAsync();
                    }

                    deviceClient.Dispose();

                    message = await t.ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _log.WriteLine("Received exception:" + exception);
                    exceptionCaught = exception;
                }

                if (expectException)
                {
                    if (exceptionCaught == null)
                    {
                        Assert.Fail("Expected exception but got none.");
                    }
                }
                else
                {
                    if (exceptionCaught != null)
                    {
                        Assert.Fail("Unexpected exception was thrown (was disabled in AppConfig).");
                    }
                    else if (message != null)
                    {
                        Assert.Fail("ReceiveAsync did not return null");
                    }
                }
            }
        }

        #endregion ReceiveAsyncAfterDispose

        #region ReceiveAsyncAfterCloseAsync
        
        [TestMethod]
        public async Task AppConfig_ReceiveAsyncAfterCloseAsync_x509_AmqpWs()
        {
            bool isFlagSet = false;
            bool flag = false;

#if !NET451
            isFlagSet = AppContext.TryGetSwitch(AppContextConstants.DisableObjectDisposedExceptionForReceiveAsync, out flag);
#endif

            if (isFlagSet)
            {
                await ReceiveAsyncAfterCloseAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only, false).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("AppContext flag DisableObjectDisposedExceptionForReceiveAsync not set");
            }
        }

        [TestMethod]
        public async Task AppConfig_ReceiveAsyncWithCancellationTokenAfterCloseAsync_x509_AmqpWs()
        {
            bool isFlagSet = false;
            bool flag = false;

#if !NET451
            isFlagSet = AppContext.TryGetSwitch(AppContextConstants.DisableObjectDisposedExceptionForReceiveAsync, out flag);
#endif

            if (isFlagSet)
            {
                await ReceiveAsyncAfterCloseAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only, true).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("AppContext flag DisableObjectDisposedExceptionForReceiveAsync not set");
            }
        }

        private async Task ReceiveAsyncAfterCloseAsync(TestDeviceType testDeviceType, Client.TransportType transportType, bool expectException)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, testDeviceType).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transportType))
            {
                Exception exceptionCaught = null;
                Client.Message message = null;
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

                try
                {
                    await deviceClient.OpenAsync().ConfigureAwait(false);

                    Task<Client.Message> t;
                    
                    if (expectException)
                    {
                        // ReceiveAsync with cancellation token should always throw exception.
                        t = deviceClient.ReceiveAsync(cts.Token);
                    }
                    else
                    {
                        t = deviceClient.ReceiveAsync();
                    }

                    await deviceClient.CloseAsync().ConfigureAwait(false);

                    message = await t.ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _log.WriteLine("Received exception:" + exception);
                    exceptionCaught = exception;
                }

                if (expectException)
                {
                    if (exceptionCaught == null)
                    {
                        Assert.Fail("Expected exception but got none.");
                    }
                }
                else
                {
                    if (exceptionCaught != null)
                    {
                        Assert.Fail("Unexpected exception was thrown (was disabled in AppConfig).");
                    }
                    else if (message != null)
                    {
                        Assert.Fail("ReceiveAsync did not return null");
                    }
                }
            }
        }

        #endregion ReceiveAsyncAfterCloseAsync

        private Client.Message GetMeAMessage()
        {
            return new Client.Message(Encoding.ASCII.GetBytes(DateTime.Now.ToLongDateString()));
        }
    }
}
