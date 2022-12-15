// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
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
        private const string Amqpwsb10 = "AMQPWSB10";
        private static readonly string s_devicePrefix = $"{nameof(DeviceClientX509AuthenticationE2ETests)}_";
        private static X509Certificate2 s_selfSignedCertificateWithPrivateKey = TestConfiguration.IotHub.GetCertificateWithPrivateKey();
        private static X509Certificate2 s_chainCertificateWithPrivateKey = TestConfiguration.IotHub.GetChainDeviceCertificateWithPrivateKey();
        private readonly string _hostName = GetHostName(TestConfiguration.IotHub.ConnectionString);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_AmqpTcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_AmqpWs()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket)).ConfigureAwait(false);
        }

        // TODO: there is a problem with DotNetty on net6.0 with gatewayv2. Wait for transition to MqttNet to re-enable and try this test out.
#if !NET6_0_OR_GREATER
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_MqttTcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientMqttSettings()).ConfigureAwait(false);
        }
#endif

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_MqttWs()
        {
            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck_MqttTcp()
        {
            IotHubClientTransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(IotHubClientTransportProtocol.Tcp);
            await DeviceClientX509AuthenticationE2ETests.SendMessageTestAsync(transportSetting).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck_MqttWs()
        {
            IotHubClientTransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(IotHubClientTransportProtocol.WebSocket);
            await DeviceClientX509AuthenticationE2ETests.SendMessageTestAsync(transportSetting).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck_AmqpTcp()
        {
            IotHubClientTransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(IotHubClientTransportProtocol.Tcp);
            await DeviceClientX509AuthenticationE2ETests.SendMessageTestAsync(transportSetting).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck_AmqpWs()
        {
            IotHubClientTransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(IotHubClientTransportProtocol.WebSocket);
            await DeviceClientX509AuthenticationE2ETests.SendMessageTestAsync(transportSetting).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_CustomWebSocket_AmqpWs()
        {
            IotHubClientTransportSettings transportSetting = CreateAmqpTransportSettingWithCustomWebSocket(IotHubClientTransportProtocol.WebSocket);
            await DeviceClientX509AuthenticationE2ETests.SendMessageTestAsync(transportSetting).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Cert_Chain_Install_Test_MqttTcp()
        {
            // arrange
            var chainCerts = new X509Certificate2Collection
            {
                TestConfiguration.CommonCertificates.GetRootCaCertificate(),
                TestConfiguration.CommonCertificates.GetIntermediate1Certificate(),
                TestConfiguration.CommonCertificates.GetIntermediate2Certificate()
            };
            var auth = new ClientAuthenticationWithX509Certificate(
                s_chainCertificateWithPrivateKey,
                chainCerts,
                TestConfiguration.IotHub.X509ChainDeviceName);
            await using var deviceClient = new IotHubDeviceClient(
                _hostName,
                auth,
                new IotHubClientOptions(new IotHubClientMqttSettings()));

            // act
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);

            // assert
            ValidateCertsAreInstalled(chainCerts);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Cert_Chain_Install_Test_AmqpTcp()
        {
            // arrange
            var chainCerts = new X509Certificate2Collection
            {
                TestConfiguration.CommonCertificates.GetRootCaCertificate(),
                TestConfiguration.CommonCertificates.GetIntermediate1Certificate(),
                TestConfiguration.CommonCertificates.GetIntermediate2Certificate(),
            };
            var auth = new ClientAuthenticationWithX509Certificate(
                s_chainCertificateWithPrivateKey,
                chainCerts,
                TestConfiguration.IotHub.X509ChainDeviceName);
            await using var deviceClient = new IotHubDeviceClient(
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
                results.Count.Should().NotBe(0, $"{certificate.SubjectName} was not found");
            }

            store?.Dispose();
        }

        private static async Task SendMessageTestAsync(IotHubClientTransportSettings transportSetting)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, TestDeviceType.X509).ConfigureAwait(false);

            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSetting));
            await deviceClient.OpenAsync().ConfigureAwait(false);
            TelemetryMessage message = TelemetryE2ETests.ComposeD2cTestMessage(out string _, out string _);
            await deviceClient.SendTelemetryAsync(message).ConfigureAwait(false);
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

        private static IotHubClientTransportSettings CreateAmqpTransportSettingWithCustomWebSocket(IotHubClientTransportProtocol transportProtocol)
        {
            var websocket = new ClientWebSocket();
            websocket.Options.AddSubProtocol(Amqpwsb10);
            websocket.Options.Proxy = new WebProxy(TestConfiguration.IotHub.ProxyServerAddress);
            websocket.Options.ClientCertificates.Add(s_selfSignedCertificateWithPrivateKey);

            return new IotHubClientAmqpSettings(transportProtocol)
            {
                ClientWebSocket = websocket,
            };
        }

        private static IotHubClientTransportSettings CreateAmqpTransportSettingWithCertificateRevocationCheck(IotHubClientTransportProtocol transportProtocol)
        {
            return new IotHubClientAmqpSettings(transportProtocol) { CertificateRevocationCheck = true };
        }

        private async Task X509InvalidDeviceIdOpenAsyncTest(IotHubClientTransportSettings transportSettings)
        {
            string deviceName = $"DEVICE_NOT_EXIST_{Guid.NewGuid()}";
            var auth = new ClientAuthenticationWithX509Certificate(s_selfSignedCertificateWithPrivateKey, deviceName);
            await using var deviceClient = new IotHubDeviceClient(_hostName, auth, new IotHubClientOptions(transportSettings));

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);
                Assert.Fail("Should throw UnauthorizedException but didn't.");
            }
            catch (IotHubClientException ex) when (ex.ErrorCode is IotHubClientErrorCode.Unauthorized)
            {
                // It should always throw IotHubClientException with status code Unauthorized
            }

            // Manual check option: check TCP connection to verify there is no connection leak.
            // Uncomment this to give you enough time:
            //await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
            // To run:
            // netstat -na | find "[Your Hub IP]" | find "ESTABLISHED"
        }

        private async Task X509InvalidDeviceIdOpenAsyncTwiceTest(IotHubClientTransportSettings transportSettings)
        {
            string deviceName = $"DEVICE_NOT_EXIST_{Guid.NewGuid()}";
            var auth = new ClientAuthenticationWithX509Certificate(s_selfSignedCertificateWithPrivateKey, deviceName);
            await using var deviceClient = new IotHubDeviceClient(_hostName, auth, new IotHubClientOptions(transportSettings));

            for (int i = 0; i < 2; i++)
            {
                try
                {
                    await deviceClient.OpenAsync().ConfigureAwait(false);
                    Assert.Fail("Should throw UnauthorizedException but didn't.");
                }
                catch (IotHubClientException ex) when (ex.ErrorCode is IotHubClientErrorCode.Unauthorized)
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
