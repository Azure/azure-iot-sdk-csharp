// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Messaging;
using Microsoft.Azure.Devices.Shared;
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
        private static X509Certificate2 s_selfSignedCertificateWithPrivateKey = TestConfiguration.IotHub.GetCertificateWithPrivateKey();
        private static X509Certificate2 s_chainCertificateWithPrivateKey = TestConfiguration.IotHub.GetChainDeviceCertificateWithPrivateKey();
        private readonly string _hostName = GetHostName(TestConfiguration.IotHub.ConnectionString);

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_AmqpTcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(DeviceTransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_AmqpWs()
        {
            await X509InvalidDeviceIdOpenAsyncTest(DeviceTransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

// TODO: there is a problem with DotNetty on net6.0 with gatewayv2. Wait for transition to MqttNet to re-enable and try this test out.
#if !NET6_0_OR_GREATER
        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_MqttTcp()
        {
            await X509InvalidDeviceIdOpenAsyncTest(DeviceTransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }
#endif

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_MqttWs()
        {
            await X509InvalidDeviceIdOpenAsyncTest(DeviceTransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck_Http()
        {
            ITransportSettings transportSetting = CreateHttpTransportSettingWithCertificateRevocationCheck();
            await SendMessageTestAsync(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck_MqttTcp()
        {
            ITransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(DeviceTransportType.Mqtt_Tcp_Only);
            await SendMessageTestAsync(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck_MqttWs()
        {
            ITransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(DeviceTransportType.Mqtt_WebSocket_Only);
            await SendMessageTestAsync(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck_AmqpTcp()
        {
            ITransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(DeviceTransportType.Amqp_Tcp_Only);
            await SendMessageTestAsync(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task X509_Enable_CertificateRevocationCheck_AmqpWs()
        {
            ITransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(DeviceTransportType.Amqp_WebSocket_Only);
            await SendMessageTestAsync(transportSetting).ConfigureAwait(false);
        }

        [LoggedTestMethod]
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
            using var auth = new DeviceAuthenticationWithX509Certificate(
                TestConfiguration.IotHub.X509ChainDeviceName,
                s_chainCertificateWithPrivateKey,
                chainCerts);
            using var deviceClient = DeviceClient.Create(
                _hostName,
                auth,
                DeviceTransportType.Mqtt_Tcp_Only);

            // act
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);

            // assert
            ValidateCertsAreInstalled(chainCerts);
        }

        [LoggedTestMethod]
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
            using var auth = new DeviceAuthenticationWithX509Certificate(
                TestConfiguration.IotHub.X509ChainDeviceName,
                s_chainCertificateWithPrivateKey,
                chainCerts);
            using var deviceClient = DeviceClient.Create(
                _hostName,
                auth,
                DeviceTransportType.Amqp_Tcp_Only);

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

#if !NET451
            store?.Dispose();
#endif
        }

        private async Task SendMessageTestAsync(ITransportSettings transportSetting)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, s_devicePrefix, TestDeviceType.X509).ConfigureAwait(false);

            using DeviceClient deviceClient = testDevice.CreateDeviceClient(new[] { transportSetting });
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await MessageSendE2ETests.SendSingleMessageAsync(deviceClient, testDevice.Id, Logger).ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private static ITransportSettings CreateHttpTransportSettingWithCertificateRevocationCheck()
        {
            TlsVersions.Instance.CertificateRevocationCheck = true;
            return new Http1TransportSettings();
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

        private async Task X509InvalidDeviceIdOpenAsyncTest(DeviceTransportType transportType)
        {
            string deviceName = $"DEVICE_NOT_EXIST_{Guid.NewGuid()}";
            using var auth = new DeviceAuthenticationWithX509Certificate(deviceName, s_selfSignedCertificateWithPrivateKey);
            using var deviceClient = DeviceClient.Create(_hostName, auth, transportType);

            Func<Task> act = async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1000));
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);
            };
            await act.Should().ThrowAsync<UnauthorizedException>();

            // Manual check option: check TCP connection to verify there is no connection leak.
            // Uncomment this to give you enough time:
            //await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
            // To run:
            // netstat -na | find "[Your Hub IP]" | find "ESTABLISHED"
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // X509Certificate needs to be disposed for implementations !NET451 (NET451 doesn't implement X509Certificates as IDisposable).
            if (s_selfSignedCertificateWithPrivateKey is IDisposable disposableSelfSignedCertificate)
            {
                disposableSelfSignedCertificate?.Dispose();
            }
            s_selfSignedCertificateWithPrivateKey = null;

            // X509Certificate needs to be disposed for implementations !NET451 (NET451 doesn't implement X509Certificates as IDisposable).
            if (s_chainCertificateWithPrivateKey is IDisposable disposableChainedCertificate)
            {
                disposableChainedCertificate?.Dispose();
            }
            s_chainCertificateWithPrivateKey = null;
        }
    }
}
