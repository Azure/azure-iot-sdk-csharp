using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class AppConfigTests
    {
        private readonly string DevicePrefix = $"E2E_{nameof(AppConfigTests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public AppConfigTests()
        {
#if !NETSTANDARD1_3 && !NETSTANDARD2_0 && !NET451
            AppContext.SetSwitch(AppConfigConstants.DisableObjectDisposedExceptionForReceiveAsync, true);
#endif

            _listener = TestConfig.StartEventListener();
        }

        #region ReceiveAsyncAfterDispose

        [TestMethod]
        public async Task AppConfig_ReceiveAsyncAfterDispose_Sasl_Amqp()
        {
            bool isFlagSet = false;
            bool flag = false;

#if !NETSTANDARD1_3 && !NETSTANDARD2_0 && !NET451
            isFlagSet = AppContext.TryGetSwitch(AppConfigConstants.DisableObjectDisposedExceptionForReceiveAsync, out flag);
#endif

            if (isFlagSet)
            {
                await DCLC_ReceiveAsyncAfterDispose(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
            }
        }

        private async Task DCLC_ReceiveAsyncAfterDispose(TestDeviceType testDeviceType, Client.TransportType transportType)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, testDeviceType).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transportType))
            {
                Exception exceptionCaught = null;
                Client.Message message = null;

                try
                {
                    await deviceClient.OpenAsync().ConfigureAwait(false);

                    Task<Client.Message> t = deviceClient.ReceiveAsync();

                    deviceClient.Dispose();

                    message = await t.ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _log.WriteLine("Received exception:" + exception);
                    exceptionCaught = exception;
                }

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

        #endregion ReceiveAsyncAfterDispose

        #region ReceiveAsyncAfterCloseAsync
        
        [TestMethod]
        public async Task AppConfig_ReceiveAsyncAfterCloseAsync_x509_AmqpWs()
        {
            bool isFlagSet = false;
            bool flag = false;

#if !NETSTANDARD1_3 && !NETSTANDARD2_0 && !NET451
            isFlagSet = AppContext.TryGetSwitch(AppConfigConstants.DisableObjectDisposedExceptionForReceiveAsync, out flag);
#endif

            if (isFlagSet)
            {
                await DCLC_ReceiveAsyncAfterCloseAsync(TestDeviceType.X509, Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
            }
        }

        private async Task DCLC_ReceiveAsyncAfterCloseAsync(TestDeviceType testDeviceType, Client.TransportType transportType)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, testDeviceType).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transportType))
            {
                Exception exceptionCaught = null;
                Client.Message message = null;

                try
                {
                    await deviceClient.OpenAsync().ConfigureAwait(false);

                    Task<Client.Message> t = deviceClient.ReceiveAsync();

                    await deviceClient.CloseAsync().ConfigureAwait(false);

                    message = await t.ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _log.WriteLine("Received exception:" + exception);
                    exceptionCaught = exception;
                }

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

        #endregion ReceiveAsyncAfterCloseAsync

        private Client.Message GetMeAMessage()
        {
            return new Client.Message(Encoding.ASCII.GetBytes(DateTime.Now.ToLongDateString()));
        }
    }
}
