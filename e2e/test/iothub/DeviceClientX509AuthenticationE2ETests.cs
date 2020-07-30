// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Messaging;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Azure.Devices.E2ETests.Helpers.HostNameHelper;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class DeviceClientX509AuthenticationE2ETests : E2EMsTestBase
    {
        private static readonly string s_devicePrefix = $"E2E_{nameof(DeviceClientX509AuthenticationE2ETests)}_";

        private readonly string _hostName;

        public DeviceClientX509AuthenticationE2ETests()
        {
            _hostName = GetHostName(Configuration.IoTHub.ConnectionString);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Amqp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Amqp_Tcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Amqp_WebSocket()
        {
            await X509InvalidDeviceIdOpenAsyncTest(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Mqtt()
        {
            await X509InvalidDeviceIdOpenAsyncTest(Client.TransportType.Mqtt).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Mqtt_Tcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Mqtt_WebSocket()
        {
            await X509InvalidDeviceIdOpenAsyncTest(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Amqp()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType.Amqp).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Amqp_TCP()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Amqp_WebSocket()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Mqtt()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType.Mqtt).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Mqtt_Tcp()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Mqtt_WebSocket()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck_Httt_Tcp()
        {
            ITransportSettings transportSetting
                = CreateHttpTransportSettingWithCertificateRevocationCheck();
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck_Mqtt_Tcp()
        {
            ITransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(Client.TransportType.Mqtt_Tcp_Only);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck_Mqtt_WebSocket()
        {
            ITransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(Client.TransportType.Mqtt_WebSocket_Only);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck_Amqp_Tcp()
        {
            ITransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(Client.TransportType.Amqp_Tcp_Only);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck_Amqp_WebSocket()
        {
            ITransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(Client.TransportType.Amqp_WebSocket_Only);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        private async Task SendMessageTest(ITransportSettings transportSetting)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, TestDeviceType.X509).ConfigureAwait(false);

            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(new[] { transportSetting }))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await MessageSendE2ETests.SendSingleMessageAsync(deviceClient, testDevice.Id, Logger).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private ITransportSettings CreateHttpTransportSettingWithCertificateRevocationCheck()
        {
            TlsVersions.Instance.CertificateRevocationCheck = true;
            return new Http1TransportSettings();
        }

        private ITransportSettings CreateMqttTransportSettingWithCertificateRevocationCheck(Client.TransportType transportType)
        {
            TlsVersions.Instance.CertificateRevocationCheck = true;
            return new MqttTransportSettings(transportType);
        }

        private ITransportSettings CreateAmqpTransportSettingWithCertificateRevocationCheck(Client.TransportType transportType)
        {
            TlsVersions.Instance.CertificateRevocationCheck = true;
            return new AmqpTransportSettings(transportType);
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
    }
}
