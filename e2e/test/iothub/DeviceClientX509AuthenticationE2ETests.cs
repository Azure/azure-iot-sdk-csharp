// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Azure.Devices.E2ETests.Helpers.HostNameHelper;

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

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_AmqpTcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_AmqpWs()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket)).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_MqttTcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException__MqttWs()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck_MqttTcp()
        {
            IotHubClientTransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(IotHubClientTransportProtocol.Tcp);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck__MqttWs()
        {
            IotHubClientTransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(IotHubClientTransportProtocol.WebSocket);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck_AmqpTcp()
        {
            IotHubClientTransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(IotHubClientTransportProtocol.Tcp);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck_AmqpWs()
        {
            IotHubClientTransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(IotHubClientTransportProtocol.WebSocket);
            await SendMessageTest(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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
            DeviceClientX509AuthenticationE2ETests.ValidateCertsAreInstalled(chainCerts);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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

        private static void ValidateCertsAreInstalled(X509Certificate2Collection certificates)
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

        private async Task SendMessageTest(IotHubClientTransportSettings transportSetting)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, TestDeviceType.X509).ConfigureAwait(false);

            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSetting));
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await MessageSendE2ETests.SendSingleMessageAsync(deviceClient, Logger).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private static IotHubClientTransportSettings CreateHttpTransportSettingWithCertificateRevocationCheck()
        {
            return new IotHubClientHttpSettings { CertificateRevocationCheck = true };
        }

        private static IotHubClientTransportSettings CreateMqttTransportSettingWithCertificateRevocationCheck(IotHubClientTransportProtocol transportProtocol)
        {
            return new IotHubClientMqttSettings(transportProtocol) { CertificateRevocationCheck = true };
        }

        private static IotHubClientTransportSettings CreateAmqpTransportSettingWithCertificateRevocationCheck(IotHubClientTransportProtocol transportProtocol)
        {
            return new IotHubClientAmqpSettings(transportProtocol) { CertificateRevocationCheck = true };
        }

        private async Task X509InvalidDeviceIdOpenAsyncTest(IotHubClientTransportSettings transportSettings)
        {
            string deviceName = $"DEVICE_NOT_EXIST_{Guid.NewGuid()}";
            using var auth = new DeviceAuthenticationWithX509Certificate(deviceName, s_selfSignedCertificateWithPrivateKey);
            using var deviceClient = IotHubDeviceClient.Create(_hostName, auth, new IotHubClientOptions(transportSettings));

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);
                Assert.Fail("Should throw UnauthorizedException but didn't.");
            }
            catch (IotHubClientException ex) when (ex.StatusCode is IotHubStatusCode.Unauthorized)
            {
                // It should always throw IotHubClientException with status code Unauthorized
            }
            catch (IotHubClientException ex) when (ex.StatusCode is IotHubStatusCode.NetworkErrors && ex.InnerException is TaskCanceledException)
            {
                Assert.Fail("Call to OpenAsync timed out.");
            }

            // Check TCP connection to verify there is no connection leak
            // netstat -na | find "[Your Hub IP]" | find "ESTABLISHED"
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }

        private async Task X509InvalidDeviceIdOpenAsyncTwiceTest(IotHubClientTransportSettings transportSettings)
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
                catch (IotHubClientException ex) when (ex.StatusCode is IotHubStatusCode.Unauthorized)
                {
                    // It should always throw IotHubClientException with status code Unauthorized
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
