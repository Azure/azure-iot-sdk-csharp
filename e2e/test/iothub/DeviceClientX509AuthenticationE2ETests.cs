﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Azure.Devices.E2ETests.Helpers.HostNameHelper;
using DeviceTransportType = Microsoft.Azure.Devices.Client.TransportType;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class DeviceClientX509AuthenticationE2ETests : E2EMsTestBase
    {
        private static readonly string s_devicePrefix = $"{nameof(DeviceClientX509AuthenticationE2ETests)}_";
        private static X509Certificate2 s_selfSignedCertificateWithPrivateKey = TestConfiguration.IoTHub.GetCertificateWithPrivateKey();
        private static X509Certificate2 s_chainCertificateWithPrivateKey = TestConfiguration.IoTHub.GetChainDeviceCertificateWithPrivateKey();
        private readonly string _hostName;

        public DeviceClientX509AuthenticationE2ETests()
        {
            _hostName = GetHostName(TestConfiguration.IoTHub.ConnectionString);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Amqp_Tcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new AmqpTransportSettings(DeviceTransportType.Amqp_Tcp_Only)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Amqp_WebSocket()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new AmqpTransportSettings(DeviceTransportType.Amqp_WebSocket_Only)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Mqtt_Tcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new MqttTransportSettings(DeviceTransportType.Mqtt_Tcp_Only)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Mqtt_WebSocket()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new MqttTransportSettings(DeviceTransportType.Mqtt_WebSocket_Only)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Amqp_Tcp()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(new AmqpTransportSettings(DeviceTransportType.Amqp_Tcp_Only)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Amqp_WebSocket()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(new AmqpTransportSettings(DeviceTransportType.Amqp_WebSocket_Only)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Mqtt_Tcp()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(new MqttTransportSettings(DeviceTransportType.Mqtt_Tcp_Only)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_Mqtt_WebSocket()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(new MqttTransportSettings(DeviceTransportType.Mqtt_WebSocket_Only)).ConfigureAwait(false);
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
            ITransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(DeviceTransportType.Mqtt_Tcp_Only);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck_Mqtt_WebSocket()
        {
            ITransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(DeviceTransportType.Mqtt_WebSocket_Only);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck_Amqp_Tcp()
        {
            ITransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(DeviceTransportType.Amqp_Tcp_Only);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck_Amqp_WebSocket()
        {
            ITransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(DeviceTransportType.Amqp_WebSocket_Only);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Cert_Chain_Install_Test_Mqtt_Tcp()
        {
            // arrange
            var chainCerts = new X509Certificate2Collection
            {
                TestConfiguration.CommonCertificates.GetRootCaCertificate(),
                TestConfiguration.CommonCertificates.GetIntermediate1Certificate(),
                TestConfiguration.CommonCertificates.GetIntermediate2Certificate()
            };
            using var auth = new DeviceAuthenticationWithX509Certificate(
                TestConfiguration.IoTHub.X509ChainDeviceName,
                s_chainCertificateWithPrivateKey,
                chainCerts);
            using var deviceClient = DeviceClient.Create(
                _hostName,
                auth,
                new ClientOptions(new MqttTransportSettings(DeviceTransportType.Mqtt_Tcp_Only)));

            // act
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);

            // assert
            ValidateCertsAreInstalled(chainCerts);
        }

        [LoggedTestMethod]
        public async Task X509_Cert_Chain_Install_Test_Amqp_Tcp()
        {
            // arrange
            var chainCerts = new X509Certificate2Collection
            {
                TestConfiguration.CommonCertificates.GetRootCaCertificate(),
                TestConfiguration.CommonCertificates.GetIntermediate1Certificate(),
                TestConfiguration.CommonCertificates.GetIntermediate2Certificate()
            };
            using var auth = new DeviceAuthenticationWithX509Certificate(
                TestConfiguration.IoTHub.X509ChainDeviceName,
                s_chainCertificateWithPrivateKey,
                chainCerts);
            using var deviceClient = DeviceClient.Create(
                _hostName,
                auth,
                new ClientOptions(new AmqpTransportSettings(DeviceTransportType.Amqp_Tcp_Only)));

            // act
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);

            // assert
            ValidateCertsAreInstalled(chainCerts);
        }

        private void ValidateCertsAreInstalled(X509Certificate2Collection certificates)
        {
            var store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            foreach (X509Certificate2 certificate in certificates)
            {
                X509Certificate2Collection results = store.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    certificate.Thumbprint,
                    false);
                if (results.Count == 0)
                {
                    Assert.Fail($"{certificate.SubjectName} was not found");
                }
            }

            store?.Dispose();
        }

        private async Task SendMessageTest(ITransportSettings transportSetting)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, TestDeviceType.X509).ConfigureAwait(false);

            using DeviceClient deviceClient = testDevice.CreateDeviceClient(new ClientOptions(transportSetting));
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await MessageSendE2ETests.SendSingleMessageAsync(deviceClient, Logger).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private static ITransportSettings CreateHttpTransportSettingWithCertificateRevocationCheck()
        {
            TlsVersions.Instance.CertificateRevocationCheck = true;
            return new Client.HttpTransportSettings();
        }

        private static ITransportSettings CreateMqttTransportSettingWithCertificateRevocationCheck(DeviceTransportType transportType)
        {
            TlsVersions.Instance.CertificateRevocationCheck = true;
            return new MqttTransportSettings(transportType);
        }

        private static ITransportSettings CreateAmqpTransportSettingWithCertificateRevocationCheck(DeviceTransportType transportType)
        {
            TlsVersions.Instance.CertificateRevocationCheck = true;
            return new AmqpTransportSettings(transportType);
        }

        private async Task X509InvalidDeviceIdOpenAsyncTest(ITransportSettings transportSettings)
        {
            string deviceName = $"DEVICE_NOT_EXIST_{Guid.NewGuid()}";
            using var auth = new DeviceAuthenticationWithX509Certificate(deviceName, s_selfSignedCertificateWithPrivateKey);
            using var deviceClient = DeviceClient.Create(_hostName, auth, new ClientOptions(transportSettings));

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

        private async Task X509InvalidDeviceIdOpenAsyncTwiceTest(ITransportSettings transportSettings)
        {
            string deviceName = $"DEVICE_NOT_EXIST_{Guid.NewGuid()}";
            using var auth = new DeviceAuthenticationWithX509Certificate(deviceName, s_selfSignedCertificateWithPrivateKey);
            using var deviceClient = DeviceClient.Create(_hostName, auth, new ClientOptions(transportSettings));

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

        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (s_selfSignedCertificateWithPrivateKey is IDisposable disposableSelfSignedCertificate)
            {
                disposableSelfSignedCertificate?.Dispose();
            }
            s_selfSignedCertificateWithPrivateKey = null;

            if (s_chainCertificateWithPrivateKey is IDisposable disposableChainedCertificate)
            {
                disposableChainedCertificate?.Dispose();

            }
            s_chainCertificateWithPrivateKey = null;
        }
    }
}
