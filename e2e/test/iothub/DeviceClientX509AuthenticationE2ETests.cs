// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Azure.Devices.E2ETests.Helpers.HostNameHelper;
using DeviceTransportType = Microsoft.Azure.Devices.Client;

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
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_AmqpTcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_AmqpWs()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientAmqpSettings(TransportProtocol.WebSocket)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_MqttTcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException__MqttWs()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientMqttSettings(TransportProtocol.WebSocket)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_AmqpTcp()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_AmqpWs()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(new IotHubClientAmqpSettings(TransportProtocol.WebSocket)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice_MqttTcp()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Twice__MqttWs()
        {
            await X509InvalidDeviceIdOpenAsyncTwiceTest(new IotHubClientMqttSettings(TransportProtocol.WebSocket)).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck_Httt_Tcp()
        {
            TransportSettings transportSetting
                = CreateHttpTransportSettingWithCertificateRevocationCheck();
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck_MqttTcp()
        {
            TransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(TransportProtocol.Tcp);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck__MqttWs()
        {
            TransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(TransportProtocol.WebSocket);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck_AmqpTcp()
        {
            TransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(TransportProtocol.Tcp);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Enable_CertificateRevocationCheck_AmqpWs()
        {
            TransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(TransportProtocol.WebSocket);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task X509_Cert_Chain_Install_Test_MqttTcp()
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
            using var deviceClient = IotHubDeviceClient.Create(
                _hostName,
                auth,
                new IotHubClientOptions(new IotHubClientMqttSettings()));

            // act
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);

            // assert
            ValidateCertsAreInstalled(chainCerts);
        }

        [LoggedTestMethod]
        public async Task X509_Cert_Chain_Install_Test_AmqpTcp()
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
            using var deviceClient = IotHubDeviceClient.Create(
                _hostName,
                auth,
                new IotHubClientOptions(new IotHubClientAmqpSettings()));

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

        private async Task SendMessageTest(TransportSettings transportSetting)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, TestDeviceType.X509).ConfigureAwait(false);

            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSetting));
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await MessageSendE2ETests.SendSingleMessageAsync(deviceClient, Logger).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private static TransportSettings CreateHttpTransportSettingWithCertificateRevocationCheck()
        {
            TlsVersions.Instance.CertificateRevocationCheck = true;
            return new Client.IotHubClientHttpSettings();
        }

        private static TransportSettings CreateMqttTransportSettingWithCertificateRevocationCheck(TransportProtocol transportProtocol)
        {
            TlsVersions.Instance.CertificateRevocationCheck = true;
            return new IotHubClientMqttSettings(transportProtocol);
        }

        private static TransportSettings CreateAmqpTransportSettingWithCertificateRevocationCheck(TransportProtocol transportProtocol)
        {
            TlsVersions.Instance.CertificateRevocationCheck = true;
            return new IotHubClientAmqpSettings(transportProtocol);
        }

        private async Task X509InvalidDeviceIdOpenAsyncTest(TransportSettings transportSettings)
        {
            string deviceName = $"DEVICE_NOT_EXIST_{Guid.NewGuid()}";
            using var auth = new DeviceAuthenticationWithX509Certificate(deviceName, s_selfSignedCertificateWithPrivateKey);
            using var deviceClient = IotHubDeviceClient.Create(_hostName, auth, new IotHubClientOptions(transportSettings));

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

        private async Task X509InvalidDeviceIdOpenAsyncTwiceTest(TransportSettings transportSettings)
        {
            string deviceName = $"DEVICE_NOT_EXIST_{Guid.NewGuid()}";
            using var auth = new DeviceAuthenticationWithX509Certificate(deviceName, s_selfSignedCertificateWithPrivateKey);
            using var deviceClient = IotHubDeviceClient.Create(_hostName, auth, new IotHubClientOptions(transportSettings));

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
