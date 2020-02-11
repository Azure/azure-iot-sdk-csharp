using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class DeviceClientX509AuthenticationE2ETests : IDisposable
    {
        private static readonly string DevicePrefix = $"E2E_{nameof(DeviceClientX509AuthenticationE2ETests)}_";
        private static readonly TestLogging _log = TestLogging.GetInstance();
        private static readonly TimeSpan TIMESPAN_ONE_MINUTE = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan TIMESPAN_ONE_SECOND = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan TIMESPAN_FIVE_SECONDS = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan TIMESPAN_TWENDY_SECONDS = TimeSpan.FromSeconds(20);

        private readonly ConsoleEventListener _listener;
        private readonly string _hostName;

        public DeviceClientX509AuthenticationE2ETests()
        {
            _listener = TestConfig.StartEventListener();
            _hostName = TestDevice.GetHostName(Configuration.IoTHub.ConnectionString);
        }
                
        [TestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Amqp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Amqp_Tcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Amqp_WebSocket()
        {
            await X509InvalidDeviceIdOpenAsyncTest(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Mqtt()
        {
            await X509InvalidDeviceIdOpenAsyncTest(Client.TransportType.Mqtt).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Mqtt_Tcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Mqtt_WebSocket()
        {
            await X509InvalidDeviceIdOpenAsyncTest(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Amqp()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Amqp_TCP()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Amqp_WebSocket()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Mqtt()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType.Mqtt).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Mqtt_Tcp()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Mqtt_WebSocket()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_Disable_CertificateRevocationCheck_Mqtt_Tcp()
        {
            await MqttWithCertificateRevocationCheck(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_Disable_CertificateRevocationCheck_Mqtt_WebSocket()
        {
            await MqttWithCertificateRevocationCheck(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        private async Task MqttWithCertificateRevocationCheck(Client.TransportType transportType)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, TestDeviceType.X509).ConfigureAwait(false);

            var mqttTransportSettings = new MqttTransportSettings(transportType)
            {
                CertificateRevocationCheck = true
            };

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(new[] { mqttTransportSettings }))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await MessageSendE2ETests.SendSingleMessageAndVerifyAsync(deviceClient, testDevice.Id).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }


        private async Task X509InvalidDeviceIdOpenAsyncTest(Client.TransportType transportType)
        {
            var deviceClient = CreateDeviceClientWithInvalidId(transportType);
            using (deviceClient)
            {
                try
                {
                    await deviceClient.OpenAsync().ConfigureAwait(false);
                    Assert.Fail("Should throw UnauthorizedException but didn't.");
                }
                catch (UnauthorizedException)
                {
                    // It should always throw UnauthorizedException
                }

                // Check TCP connection to verify there is no connection leak
                // netstat -na | find "[Your Hub IP]" | find "ESTABLISHED" 
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
        }

        private async Task X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType transportType)
        {
            var deviceClient = CreateDeviceClientWithInvalidId(transportType);
            using (deviceClient)
            {
                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        await deviceClient.OpenAsync().ConfigureAwait(false);
                        Assert.Fail("Should throw UnauthorizedException but didn't.");
                    }
                    catch (UnauthorizedException)
                    {
                        // It should always throw UnauthorizedException
                    }
                }

                // Check TCP connection to verify there is no connection leak
                // netstat -na | find "[Your Hub IP]" | find "ESTABLISHED" 
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
        }

        private DeviceClient CreateDeviceClientWithInvalidId(Client.TransportType transportType)
        {
            string deviceName = $"DEVICE_NOT_EXIST_{Guid.NewGuid()}";
            var auth = new DeviceAuthenticationWithX509Certificate(deviceName, Configuration.IoTHub.GetCertificateWithPrivateKey());
            return DeviceClient.Create(_hostName, auth, transportType);
        }

        public void Dispose()
        {
           
        }
    }
}
