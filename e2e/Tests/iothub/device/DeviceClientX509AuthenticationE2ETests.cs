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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Azure.Devices.E2ETests.Helpers.HostNameHelper;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class DeviceClientX509AuthenticationE2ETests : E2EMsTestBase
    {
        private const string Amqpwsb10 = "AMQPWSB10";
        private static readonly string s_devicePrefix = $"{nameof(DeviceClientX509AuthenticationE2ETests)}_";
        private static X509Certificate2 s_selfSignedCertificateWithPrivateKey = TestConfiguration.IotHub.GetCertificateWithPrivateKey();
        private static X509Certificate2 s_chainCertificateWithPrivateKey = TestConfiguration.IotHub.GetChainDeviceCertificateWithPrivateKey();
        private readonly string _hostName = GetHostName(TestConfiguration.IotHub.ConnectionString);

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientAmqpSettings(protocol), ct).ConfigureAwait(false);
        }

        [DataTestMethod]
        // TODO: there is a problem with DotNetty on net6.0 with gatewayv2. Wait for transition to MqttNet to re-enable and try this test out.
#if !NET6_0_OR_GREATER
        [DataRow(IotHubClientTransportProtocol.Tcp)]
#endif
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task X509_InvalidDeviceId_Throw_UnauthorizedException_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await X509InvalidDeviceIdOpenAsyncTest(new IotHubClientMqttSettings(protocol), ct).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task X509_Enable_CertificateRevocationCheck_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            IotHubClientTransportSettings transportSetting = CreateMqttTransportSettingWithCertificateRevocationCheck(protocol);
            await SendMessageTestAsync(transportSetting, ct).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task X509_Enable_CertificateRevocationCheck_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            IotHubClientTransportSettings transportSetting = CreateAmqpTransportSettingWithCertificateRevocationCheck(protocol);
            await SendMessageTestAsync(transportSetting, ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task X509_CustomWebSocket_AmqpWs()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            IotHubClientTransportSettings transportSetting = CreateAmqpTransportSettingWithCustomWebSocket(IotHubClientTransportProtocol.WebSocket);
            await SendMessageTestAsync(transportSetting, ct).ConfigureAwait(false);
        }

        private static async Task SendMessageTestAsync(IotHubClientTransportSettings transportSetting, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, TestDeviceType.X509, ct).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSetting));
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            TelemetryMessage message = TelemetryMessageHelper.ComposeTestMessage(out string _, out string _);
            await deviceClient.SendTelemetryAsync(message, ct).ConfigureAwait(false);
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

        private async Task X509InvalidDeviceIdOpenAsyncTest(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            string deviceName = $"DEVICE_NOT_EXIST_{Guid.NewGuid()}";
            var auth = new ClientAuthenticationWithX509Certificate(s_selfSignedCertificateWithPrivateKey, deviceName);
            await using var deviceClient = new IotHubDeviceClient(_hostName, auth, new IotHubClientOptions(transportSettings));

            try
            {
                await deviceClient.OpenAsync(ct).ConfigureAwait(false);
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

        private async Task X509InvalidDeviceIdOpenAsyncTwiceTest(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            string deviceName = $"DEVICE_NOT_EXIST_{Guid.NewGuid()}";
            var auth = new ClientAuthenticationWithX509Certificate(s_selfSignedCertificateWithPrivateKey, deviceName);
            await using var deviceClient = new IotHubDeviceClient(_hostName, auth, new IotHubClientOptions(transportSettings));

            for (int i = 0; i < 2; i++)
            {
                try
                {
                    await deviceClient.OpenAsync(ct).ConfigureAwait(false);
                    Assert.Fail("Should throw UnauthorizedException but didn't.");
                }
                catch (IotHubClientException ex) when (ex.ErrorCode is IotHubClientErrorCode.Unauthorized)
                {
                    // It should always throw IotHubClientException with status code Unauthorized
                }
            }

            // Check TCP connection to verify there is no connection leak
            // netstat -na | find "[Your Hub IP]" | find "ESTABLISHED"
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
