using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
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
        public async Task X509_InvalidDeviceId_Amqp()
        {
            var deviceClient = CreateDeviceClientWithInvalidId(Client.TransportType.Amqp_Tcp_Only);
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
                // netstat -na | find "4567" | find "ESTABLISHED" 
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            } 
        }

        [TestMethod]
        public async Task X509_InvalidDeviceId_Twice_Amqp()
        {
            var deviceClient = CreateDeviceClientWithInvalidId(Client.TransportType.Amqp_Tcp_Only);
            using (deviceClient)
            {
                for (int i=0; i < 2; i++)
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
                // netstat -na | find "4567" | find "ESTABLISHED" 
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
        }

        private DeviceClient CreateDeviceClientWithInvalidId(Client.TransportType transport)
        {
            string deviceName = $"DEVICE_NOT_EXIST_{Guid.NewGuid()}";
            var auth = new DeviceAuthenticationWithX509Certificate(deviceName, Configuration.IoTHub.GetCertificateWithPrivateKey());
            return DeviceClient.Create(_hostName, auth, transport);
        }

        public void Dispose()
        {
           
        }
    }
}
