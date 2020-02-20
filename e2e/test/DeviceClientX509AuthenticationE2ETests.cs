using System;
using System.Diagnostics.Tracing;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(Client.TransportType.Mqtt)]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException(Client.TransportType transport)
        {
            using (DeviceClient deviceClient = CreateDeviceClientWithUniqueInvalidId(transport))
            {
                try
                {
                    await deviceClient.OpenAsync().ConfigureAwait(false);
                    Assert.Fail("Should throw specific exception but didn't.");
                }
                catch (UnauthorizedException)
                {
                    // For some reason, with some NET 451 and netcoreapp2.1 the service returns UnauthorizedException
                    // TODO #1251
                }
                catch (DeviceNotFoundException)
                {
                    // This should be the real error
                }

                // Check TCP connection to verify there is no connection leak
                // netstat -na | find "[Your Hub IP]" | find "ESTABLISHED"
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
        }

        [DataTestMethod]
        [DataRow(Client.TransportType.Mqtt_Tcp_Only)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only)]
#if !NETCOREAPP1_1
        [DataRow(Client.TransportType.Mqtt_WebSocket_Only)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only)]
#endif
        public async Task X509_Enable_CertificateRevocationCheck(Client.TransportType transport)
        {
            ITransportSettings transportSetting = CreateTransportSettingWithCertificateRevocationCheck(transport);
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, TestDeviceType.X509).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(new[] { transportSetting }))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await MessageSendE2ETests.SendSingleMessageAndVerifyAsync(deviceClient, testDevice.Id).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private ITransportSettings CreateTransportSettingWithCertificateRevocationCheck(Client.TransportType transportType)
        {
            switch (transportType)
            {
                case Client.TransportType.Amqp:
                case Client.TransportType.Amqp_Tcp_Only:
                case Client.TransportType.Amqp_WebSocket_Only:
                    return new AmqpTransportSettings(transportType)
                    {
                        CertificateRevocationCheck = true
                    };

                case Client.TransportType.Mqtt:
                case Client.TransportType.Mqtt_Tcp_Only:
                case Client.TransportType.Mqtt_WebSocket_Only:
                    return new MqttTransportSettings(transportType)
                    {
                        CertificateRevocationCheck = true
                    };

                default:
                    Assert.Fail($"Unexpected transport type {transportType}.");
                    break;
            }

            return null;
        }

        private DeviceClient CreateDeviceClientWithUniqueInvalidId(Client.TransportType transportType)
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
