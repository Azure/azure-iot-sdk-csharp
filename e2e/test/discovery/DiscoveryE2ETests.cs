// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Discovery.Client;
using Microsoft.Azure.Devices.Discovery.Client.Transport;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Azure.Devices.E2ETests.Provisioning.ProvisioningServiceClientE2ETests;

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("DPS")]
    public class DiscoveryE2ETests : E2EMsTestBase
    {
        private const int PassingTimeoutMiliseconds = 10 * 60 * 1000;
        private const int FailingTimeoutMiliseconds = 10 * 1000;
        private const int MaxTryCount = 10;
        private const string InvalidGlobalAddress = "httpbin.org";
        private static readonly string s_globalDeviceEndpoint = TestConfiguration.Provisioning.GlobalDeviceEndpoint;
        private static readonly string s_globalDiscoveryEndpoint = TestConfiguration.Discovery.GlobalDeviceEndpoint;
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;
        private static readonly string s_certificatePassword = TestConfiguration.Provisioning.CertificatePassword;

        private static readonly HashSet<Type> s_retryableExceptions = new() { typeof(ProvisioningServiceClientHttpException) };
        private static readonly IRetryPolicy s_provisioningServiceRetryPolicy = new ProvisioningServiceRetryPolicy();

        private readonly string _idPrefix = $"e2e-{nameof(DiscoveryE2ETests).ToLower()}-";

        [ClassInitialize]
        public static void TestClassSetup(TestContext _)
        {
            // setup for tests
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Onboard_Ok()
        {
            await ClientValidOnboardingAsyncOk(false).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Onboard_Proxy_Ok()
        {
            await ClientValidOnboardingAsyncOk(true).ConfigureAwait(false);
        }

        #region InvalidGlobalAddress



        #endregion InvalidGlobalAddress

        public async Task ClientValidOnboardingAsyncOk(
            TimeSpan timeout)
        {
            //Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            await ClientValidOnboardingAsyncOk(false, timeout, s_proxyServerAddress).ConfigureAwait(false);
        }

        public async Task ClientValidOnboardingAsyncOk(
            bool setCustomProxy,
            string proxyServerAddress = null)
        {
            // Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            await ClientValidOnboardingAsyncOk(
                    setCustomProxy,
                    TimeSpan.MaxValue,
                    proxyServerAddress)
                .ConfigureAwait(false);
        }

        public async Task ClientValidOnboardingAsyncOk(
            bool setCustomProxy,
            DeviceCapabilities capabilities,
            string proxyServerAddress = null)
        {
            await ClientValidOnboardingAsyncOk(
                    setCustomProxy,
                    TimeSpan.MaxValue,
                    proxyServerAddress)
                .ConfigureAwait(false);
        }

        private async Task ClientValidOnboardingAsyncOk(
            bool setCustomProxy,
            TimeSpan timeout,
            string proxyServerAddress = null)
        {
            // The range of valid combinations of configuration is very limited, there are fewer cases for us to test

            string registrationId = $"{Guid.NewGuid()}";

            using DiscoveryTransportHandler transport = new DiscoveryTransportHandlerHttp();
            using SecurityProvider security = new SecurityProviderTpmSimulator(registrationId);

            if (setCustomProxy)
            {
                transport.Proxy = proxyServerAddress == null
                    ? null
                    : new WebProxy(s_proxyServerAddress);
            }

            var client = DiscoveryDeviceClient.Create(
                s_globalDeviceEndpoint,
                security,
                transport);

            using var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

            Console.WriteLine("Getting nonce for challenge... ");
            string nonce = await client.IssueChallengeAsync(cts.Token);

            Console.WriteLine($"Received nonce");

            OnboardingInfo onboardingInfo = await client.GetOnboardingInfoAsync(nonce, cts.Token);

            using ProvisioningTransportHandler provTranspot = new ProvisioningTransportHandlerHttp();
            using SecurityProvider provSecurity = new SecurityProviderX509Certificate(onboardingInfo.ProvisioningCertificate);

            if (setCustomProxy)
            {
                provTranspot.Proxy = proxyServerAddress == null
                    ? null
                    : new WebProxy(s_proxyServerAddress);
            }

            var provClient = ProvisioningDeviceClient.Create(
                s_globalDeviceEndpoint,
                provSecurity,
                provTranspot);

            DeviceOnboardingResult onboardingResult = await provClient.OnboardAsync("random string", cts.Token);

            Console.WriteLine($"Successfully onboarded {onboardingResult.Id} {onboardingResult.Result.RegistrationId}");
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            // cleanup
        }
    }
}
