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
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Azure.Devices.E2ETests.Provisioning.ProvisioningServiceClientE2ETests;

namespace Microsoft.Azure.Devices.E2ETests.Provisioning
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("DPS")]
    public class ProvisioningE2ETests : E2EMsTestBase
    {
        private const int PassingTimeoutMiliseconds = 10 * 60 * 1000;
        private const int FailingTimeoutMiliseconds = 10 * 1000;
        private const int MaxTryCount = 10;
        private const string InvalidIdScope = "0neFFFFFFFF";
        private const string PayloadJsonData = "{\"testKey\":\"testValue\"}";
        private const string InvalidGlobalAddress = "httpbin.org";
        private static readonly string s_globalDeviceEndpoint = TestConfiguration.Provisioning.GlobalDeviceEndpoint;
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;
        private static readonly string s_certificatePassword = TestConfiguration.Provisioning.CertificatePassword;

        private static readonly IRetryPolicy s_provisioningServiceRetryPolicy = new ProvisioningServiceRetryPolicy();

        private readonly string _idPrefix = $"e2e-{nameof(ProvisioningE2ETests).ToLower()}-";

        private static DirectoryInfo s_x509CertificatesFolder;
        private static string s_intermediateCertificateSubject;

        [ClassInitialize]
        public static void TestClassSetup(TestContext _)
        {
            // Create a folder to hold the DPS client certificates and X509 self-signed certificates. If a folder by the same name already exists, it will be used.
            s_x509CertificatesFolder = Directory.CreateDirectory($"x509Certificates-{nameof(ProvisioningE2ETests)}-{Guid.NewGuid()}");

            // Extract the public certificate and private key information from the intermediate certificate pfx file.
            // These keys will be used to sign the test leaf device certificates.
            s_intermediateCertificateSubject = X509Certificate2Helper.ExtractPublicCertificateAndPrivateKeyFromPfxAndReturnSubject(
                TestConfiguration.Provisioning.GetGroupEnrollmentIntermediatePfxCertificateBase64(),
                s_certificatePassword,
                s_x509CertificatesFolder);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWs_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWs_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWs_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWs_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        public async Task DPS_Registration_AmqpWsWithProxy_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    true,
                    s_proxyServerAddress)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        public async Task DPS_Registration_AmqpWsWithNullProxy_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    true)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_AmqpWsWithProxy_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    true,
                    s_proxyServerAddress)
                .ConfigureAwait(false);
        }

        [TestCategory("Proxy")]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWsWithProxy_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    true,
                    s_proxyServerAddress)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        public async Task DPS_Registration_MqttWsWithProxy_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    true,
                    s_proxyServerAddress)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        public async Task DPS_Registration_MqttWsWithNullProxy_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    true)
                .ConfigureAwait
                (false);
        }

        [TestCategory("Proxy")]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWsWithProxy_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    true,
                    s_proxyServerAddress)
                .ConfigureAwait(false);
        }

        [TestCategory("Proxy")]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWsWithProxy_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    true,
                    s_proxyServerAddress)
                .ConfigureAwait(false);
        }

        #region DeviceCapabilities

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_SymmetricKey_IndividualEnrollment_EdgeEnabled_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false,
                    new Devices.Provisioning.Service.ProvisioningClientCapabilities() { IotEdge = true })
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_SymmetricKey_GroupEnrollment_EdgeEnabled_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false,
                    new Devices.Provisioning.Service.ProvisioningClientCapabilities() { IotEdge = true })
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_SymmetricKey_IndividualEnrollment_EdgeDisabled_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false,
                    new Devices.Provisioning.Service.ProvisioningClientCapabilities() { IotEdge = false })
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_SymmetricKey_GroupEnrollment_EdgeDisabled_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false,
                    new Devices.Provisioning.Service.ProvisioningClientCapabilities() { IotEdge = false })
                .ConfigureAwait(false);
        }

        #endregion DeviceCapabilities

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_X509_GrouplEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_X509_GrouplEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWs_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_X509_GroupEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWs_X509_GroupEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    "")
                .ConfigureAwait(false);
        }

        #region InvalidGlobalAddress

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_Mqtt_X509_IndividualEnrollment_InvalidGlobalAddress_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_X509_IndividualEnrollment_InvalidGlobalAddress_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual)
                .ConfigureAwait(false);
        }

        // Note: This test takes 3 minutes.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_Amqp_X509_IndividualEnrollment_InvalidGlobalAddress_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWs_X509_IndividualEnrollment_InvalidGlobalAddress_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual)
                .ConfigureAwait(false);
        }

        #endregion InvalidGlobalAddress

        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            IotHubClientTransportSettings transportSettings,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            string proxyServerAddress = null)
        {
            //Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            await ProvisioningDeviceClientValidRegistrationIdRegisterOkAsync(
                    transportSettings,
                    attestationType,
                    enrollmentType,
                    setCustomProxy,
                    null,
                    AllocationPolicy.Hashed,
                    null,
                    null,
                    null,
                    proxyServerAddress)
                .ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            IotHubClientTransportSettings transportSettings,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            ProvisioningClientCapabilities capabilities,
            string proxyServerAddress = null)
        {
            //Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            var iothubs = new List<string>() { HostNameHelper.GetHostName(TestConfiguration.IotHub.ConnectionString) };
            await ProvisioningDeviceClientValidRegistrationIdRegisterOkAsync(
                    transportSettings,
                    attestationType,
                    enrollmentType,
                    setCustomProxy,
                    null,
                    AllocationPolicy.Hashed,
                    null,
                    iothubs,
                    capabilities,
                    proxyServerAddress)
                .ConfigureAwait(false);
        }

        private async Task ProvisioningDeviceClientValidRegistrationIdRegisterOkAsync(
            IotHubClientTransportSettings transportSettings,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            IList<string> iothubs,
            ProvisioningClientCapabilities deviceCapabilities,
            string proxyServerAddress = null)
        {
            string groupId = null;
            if (enrollmentType == EnrollmentType.Group)
            {
                groupId = attestationType == AttestationMechanismType.X509
                    ? TestConfiguration.Provisioning.X509GroupEnrollmentName
                    : _idPrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            }

            bool shouldCleanupEnrollment = groupId == null || groupId != TestConfiguration.Provisioning.X509GroupEnrollmentName;

            ProvisioningClientOptions clientOptions = CreateProvisioningClientOptionsFromName(transportSettings);
            AuthenticationProvider auth = await CreateAuthProviderFromNameAsync(
                    attestationType,
                    enrollmentType,
                    groupId,
                    reprovisionPolicy,
                    allocationPolicy,
                    customAllocationDefinition,
                    iothubs,
                    deviceCapabilities)
                .ConfigureAwait(false);
            VerboseTestLogger.WriteLine("Creating device");

            if (ImplementsWebProxy(transportSettings) && setCustomProxy)
            {
                clientOptions.TransportSettings.Proxy = proxyServerAddress == null
                    ? null
                    : new WebProxy(s_proxyServerAddress);
            }

            var provClient = new ProvisioningDeviceClient(
                s_globalDeviceEndpoint,
                TestConfiguration.Provisioning.IdScope,
                auth,
                clientOptions);

            using var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

            DeviceRegistrationResult result = null;
            IAuthenticationMethod authMethod = null;

            VerboseTestLogger.WriteLine($"ProvisioningDeviceClient RegisterAsync for group {groupId} . . . ");

            try
            {
                // Trying to register simultaneously can cause conflicts (409). Retry in those scenarios to succeed.
                int tryCount = 0;
                while (true)
                {
                    try
                    {
                        result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
                        break;
                    }
                    // Catching all DeviceProvisioningClientException as the status code is not the same for Mqtt, Amqp and Http.
                    // It should be safe to retry on any non-transient exception just for E2E tests as we have concurrency issues.
                    catch (DeviceProvisioningClientException ex) when (++tryCount < MaxTryCount)
                    {
                        VerboseTestLogger.WriteLine($"ProvisioningDeviceClient RegisterAsync failed because: {ex.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                    }
                }

                ProvisioningE2ETests.ValidateDeviceRegistrationResult(false, result);

#pragma warning disable CA2000 // Dispose objects before losing scope
                // The certificate instance referenced in the ClientAuthenticationWithX509Certificate instance is common for all tests in this class. It is disposed during class cleanup.
                authMethod = CreateAuthenticationMethodFromAuthProvider(auth, result.DeviceId);
#pragma warning restore CA2000 // Dispose objects before losing scope

                await ConfirmRegisteredDeviceWorksAsync(result, authMethod, transportSettings, false).ConfigureAwait(false);
                await ConfirmExpectedDeviceCapabilitiesAsync(result, authMethod, deviceCapabilities).ConfigureAwait(false);
            }
            finally
            {
                if (shouldCleanupEnrollment)
                {
                    VerboseTestLogger.WriteLine($"Deleting test enrollment type {attestationType}-{enrollmentType} with registration Id {auth.GetRegistrationId()}.");
                    await DeleteCreatedEnrollmentAsync(enrollmentType, auth, groupId).ConfigureAwait(false);
                }
                else
                {
                    VerboseTestLogger.WriteLine($"The test enrollment type {attestationType}-{enrollmentType} with group Id {groupId} is currently hardcoded - do not delete.");
                }

                if (authMethod is AuthenticationProviderX509 x509Auth)
                {
                    X509Certificate2 deviceCertificate = x509Auth.GetAuthenticationCertificate();
                    deviceCertificate?.Dispose();
                }

                if (authMethod is IDisposable disposableAuthMethod)
                {
                    disposableAuthMethod?.Dispose();
                }

                if (auth is IDisposable disposableAuth)
                {
                    disposableAuth?.Dispose();
                }
            }
        }

        private async Task ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
            IotHubClientTransportSettings transportSettings,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            string groupId)
        {
            ProvisioningClientOptions clientOptions = CreateProvisioningClientOptionsFromName(transportSettings);
            AuthenticationProvider auth = null;

            try
            {
                auth = await CreateAuthProviderFromNameAsync(
                        attestationType,
                        enrollmentType,
                        groupId,
                        null,
                        AllocationPolicy.Hashed,
                        null,
                        null)
                    .ConfigureAwait(false);

                var provClient = new ProvisioningDeviceClient(
                    s_globalDeviceEndpoint,
                    InvalidIdScope,
                    auth,
                    clientOptions);

                using var cts = new CancellationTokenSource(FailingTimeoutMiliseconds);
                Func<Task> act = async () => await provClient.RegisterAsync(cts.Token);
                var exception = await act.Should().ThrowAsync<DeviceProvisioningClientException>().ConfigureAwait(false);
                VerboseTestLogger.WriteLine($"Exception: {exception}");
            }
            finally
            {
                if (auth == null
                    || attestationType == AttestationMechanismType.X509
                    && enrollmentType == EnrollmentType.Group)
                {
                    VerboseTestLogger.WriteLine($"The test enrollment type {attestationType}-{enrollmentType} with group Id {groupId} is currently hardcoded - do not delete.");
                }
                else
                {
                    VerboseTestLogger.WriteLine($"Deleting test enrollment type {attestationType}-{enrollmentType} with registration Id {auth.GetRegistrationId()}.");
                    await DeleteCreatedEnrollmentAsync(enrollmentType, auth, groupId).ConfigureAwait(false);
                }

                if (auth is AuthenticationProviderX509 x509Auth)
                {
                    X509Certificate2 deviceCertificate = x509Auth.GetAuthenticationCertificate();
                    deviceCertificate?.Dispose();
                }

                if (auth is IDisposable disposableAuth)
                {
                    disposableAuth?.Dispose();
                }
            }
        }

        private async Task ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(
            IotHubClientTransportSettings transportSettings,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            string groupId = "")
        {
            ProvisioningClientOptions clientOptions = CreateProvisioningClientOptionsFromName(transportSettings);
            AuthenticationProvider auth = null;

            try
            {
                auth = await CreateAuthProviderFromNameAsync(
                        attestationType,
                        enrollmentType,
                        groupId,
                        null,
                        AllocationPolicy.Hashed,
                        null,
                        null)
                    .ConfigureAwait(false);

                var provClient = new ProvisioningDeviceClient(
                    InvalidGlobalAddress,
                    TestConfiguration.Provisioning.IdScope,
                    auth,
                    clientOptions);

                using var cts = new CancellationTokenSource(FailingTimeoutMiliseconds);

                VerboseTestLogger.WriteLine("ProvisioningDeviceClient RegisterAsync . . . ");

                Func<Task> act = async () => await provClient.RegisterAsync(cts.Token);
                var exception = await act.Should().ThrowAsync<DeviceProvisioningClientException>().ConfigureAwait(false);

                VerboseTestLogger.WriteLine($"Exception: {exception}");
            }
            finally
            {
                if (auth == null
                    || attestationType == AttestationMechanismType.X509
                    && enrollmentType == EnrollmentType.Group)
                {
                    VerboseTestLogger.WriteLine($"The test enrollment type {attestationType}-{enrollmentType} with group Id {groupId} is currently hardcoded - do not delete.");
                }
                else
                {
                    VerboseTestLogger.WriteLine($"Deleting test enrollment type {attestationType}-{enrollmentType} with registration Id {auth.GetRegistrationId()}.");
                    await DeleteCreatedEnrollmentAsync(enrollmentType, auth, groupId).ConfigureAwait(false);
                }

                if (auth is AuthenticationProviderX509 x509Auth)
                {
                    X509Certificate2 deviceCertificate = x509Auth.GetAuthenticationCertificate();
                    deviceCertificate?.Dispose();
                }

                if (auth is IDisposable disposableAuth)
                {
                    disposableAuth?.Dispose();
                }
            }
        }

        public static ProvisioningClientOptions CreateProvisioningClientOptionsFromName(IotHubClientTransportSettings transportSettings)
        {
            if (transportSettings is IotHubClientAmqpSettings)
            {
                return transportSettings.Protocol == IotHubClientTransportProtocol.Tcp
                    ? new ProvisioningClientOptions(new ProvisioningClientAmqpSettings(ProvisioningClientTransportProtocol.Tcp))
                    : new ProvisioningClientOptions(new ProvisioningClientAmqpSettings(ProvisioningClientTransportProtocol.WebSocket));
            }

            if (transportSettings is IotHubClientMqttSettings)
            {
                return transportSettings.Protocol == IotHubClientTransportProtocol.Tcp
                    ? new ProvisioningClientOptions(new ProvisioningClientAmqpSettings(ProvisioningClientTransportProtocol.Tcp))
                    : new ProvisioningClientOptions(new ProvisioningClientAmqpSettings(ProvisioningClientTransportProtocol.WebSocket));
            }

            throw new NotSupportedException($"Unknown transport: '{transportSettings}'.");
        }

        /// <summary>
        /// Attempt to create device client instance from provided arguments, ensure that it can open a
        /// connection, ensure that it can send telemetry, and (optionally) send a reported property update
        /// </summary
        private async Task ConfirmRegisteredDeviceWorksAsync(
            DeviceRegistrationResult result,
            IAuthenticationMethod auth,
            IotHubClientTransportSettings transportSettings,
            bool sendReportedPropertiesUpdate)
        {
            using var iotClient = new IotHubDeviceClient(result.AssignedHub, auth, new IotHubClientOptions(transportSettings));
            VerboseTestLogger.WriteLine("DeviceClient OpenAsync.");
            await iotClient.OpenAsync().ConfigureAwait(false);
            VerboseTestLogger.WriteLine("DeviceClient SendTelemetryAsync.");

            var testMessage = new TelemetryMessage("TestMessage");
            await iotClient.SendTelemetryAsync(testMessage).ConfigureAwait(false);

            if (sendReportedPropertiesUpdate)
            {
                VerboseTestLogger.WriteLine("DeviceClient updating desired properties.");
                TwinProperties twin = await iotClient.GetTwinPropertiesAsync().ConfigureAwait(false);
                var propertiesToReport = new ReportedProperties
                {
                    [new Guid().ToString()] = new Guid().ToString(),
                };
                await iotClient.UpdateReportedPropertiesAsync(propertiesToReport).ConfigureAwait(false);
            }

            VerboseTestLogger.WriteLine("DeviceClient CloseAsync.");
            await iotClient.CloseAsync().ConfigureAwait(false);
        }

        private static async Task ConfirmExpectedDeviceCapabilitiesAsync(
            DeviceRegistrationResult result,
            IAuthenticationMethod auth,
            ProvisioningClientCapabilities capabilities)
        {
            if (capabilities != null && capabilities.IotEdge)
            {
                //If device is edge device, it should be able to connect to iot hub as its edgehub module identity
                var iotHubConnectionCredentials = new IotHubConnectionCredentials(auth, result.AssignedHub);
                string edgehubConnectionString = iotHubConnectionCredentials.GetIotHubConnectionString() + ";ModuleId=$edgeHub";
                using var moduleClient = new IotHubModuleClient(edgehubConnectionString);
                await moduleClient.OpenAsync().ConfigureAwait(false);
            }
        }

        private async Task<AuthenticationProvider> CreateAuthProviderFromNameAsync(
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            string groupId,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            IList<string> iothubs,
            ProvisioningClientCapabilities capabilities = null)
        {
            VerboseTestLogger.WriteLine($"{nameof(CreateAuthProviderFromNameAsync)}({attestationType})");

            string registrationId = AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);

            switch (attestationType)
            {
                case AttestationMechanismType.X509:
                    X509Certificate2 certificate = null;
                    X509Certificate2Collection collection = null;
                    switch (enrollmentType)
                    {
                        case EnrollmentType.Individual:
                            X509Certificate2Helper.GenerateSelfSignedCertificateFiles(registrationId, s_x509CertificatesFolder);

#pragma warning disable CA2000 // Dispose objects before losing scope
                            // This certificate is used for authentication with IoT hub and is returned to the caller of this method.
                            // It is disposed when the caller to this method is disposed, at the end of the test method.
                            certificate = X509Certificate2Helper.CreateX509Certificate2FromPfxFile(registrationId, s_x509CertificatesFolder);
#pragma warning restore CA2000 // Dispose objects before losing scope

                            using (X509Certificate2 publicCertificate = X509Certificate2Helper.CreateX509Certificate2FromCerFile(registrationId, s_x509CertificatesFolder))
                            {
                                IndividualEnrollment x509IndividualEnrollment = await CreateIndividualEnrollmentAsync(
                                        provisioningServiceClient,
                                        registrationId,
                                        AttestationMechanismType.X509,
                                        publicCertificate,
                                        reprovisionPolicy,
                                        allocationPolicy,
                                        customAllocationDefinition,
                                        iothubs,
                                        capabilities)
                                    .ConfigureAwait(false);

                                x509IndividualEnrollment.Attestation.Should().BeAssignableTo<X509Attestation>();
                            }

                            break;

                        case EnrollmentType.Group:
                            // The X509 enrollment group has been hardcoded for the purpose of E2E tests and the root certificate has been verified on DPS.
                            // Each device identity provisioning through the above enrollment group is created on-demand.

                            X509Certificate2Helper.GenerateIntermediateCertificateSignedCertificateFiles(
                                registrationId,
                                s_intermediateCertificateSubject,
                                s_x509CertificatesFolder);

#pragma warning disable CA2000 // Dispose objects before losing scope
                            // This certificate is used for authentication with IoT hub and is returned to the caller of this method.
                            // It is disposed when the caller to this method is disposed, at the end of the test method.
                            certificate = X509Certificate2Helper.CreateX509Certificate2FromPfxFile(registrationId, s_x509CertificatesFolder);
#pragma warning restore CA2000 // Dispose objects before losing scope

                            collection = new X509Certificate2Collection
                            {
                                TestConfiguration.CommonCertificates.GetRootCaCertificate(),
                                TestConfiguration.CommonCertificates.GetIntermediate1Certificate(),
                                TestConfiguration.CommonCertificates.GetIntermediate2Certificate(),
                                X509Certificate2Helper.CreateX509Certificate2FromCerFile(registrationId, s_x509CertificatesFolder)
                            };
                            break;

                        default:
                            throw new NotSupportedException($"Unknown X509 type: '{enrollmentType}'");
                    }

                    return new AuthenticationProviderX509(certificate, collection);

                case AttestationMechanismType.SymmetricKey:
                    switch (enrollmentType)
                    {
                        case EnrollmentType.Group:
                            EnrollmentGroup symmetricKeyEnrollmentGroup = await CreateEnrollmentGroupAsync(
                                    provisioningServiceClient,
                                    AttestationMechanismType.SymmetricKey,
                                    groupId,
                                    reprovisionPolicy,
                                    allocationPolicy,
                                    customAllocationDefinition,
                                    iothubs,
                                    capabilities)
                                .ConfigureAwait(false);
                            Assert.IsTrue(symmetricKeyEnrollmentGroup.Attestation is SymmetricKeyAttestation);
                            var symmetricKeyAttestation = (SymmetricKeyAttestation)symmetricKeyEnrollmentGroup.Attestation;
                            string registrationIdSymmetricKey = _idPrefix + Guid.NewGuid();
                            string primaryKeyEnrollmentGroup = symmetricKeyAttestation.PrimaryKey;
                            string secondaryKeyEnrollmentGroup = symmetricKeyAttestation.SecondaryKey;

                            string primaryKeyIndividual = ComputeDerivedSymmetricKey(Convert.FromBase64String(primaryKeyEnrollmentGroup), registrationIdSymmetricKey);
                            string secondaryKeyIndividual = ComputeDerivedSymmetricKey(Convert.FromBase64String(secondaryKeyEnrollmentGroup), registrationIdSymmetricKey);

                            return new AuthenticationProviderSymmetricKey(registrationIdSymmetricKey, primaryKeyIndividual, secondaryKeyIndividual);

                        case EnrollmentType.Individual:
                            IndividualEnrollment symmetricKeyEnrollment = await CreateIndividualEnrollmentAsync(
                                    provisioningServiceClient,
                                    registrationId,
                                    AttestationMechanismType.SymmetricKey,
                                    null,
                                    reprovisionPolicy,
                                    allocationPolicy,
                                    customAllocationDefinition,
                                    iothubs,
                                    capabilities)
                                .ConfigureAwait(false);

                            Assert.IsTrue(symmetricKeyEnrollment.Attestation is SymmetricKeyAttestation);
                            symmetricKeyAttestation = (SymmetricKeyAttestation)symmetricKeyEnrollment.Attestation;

                            registrationIdSymmetricKey = symmetricKeyEnrollment.RegistrationId;
                            string primaryKey = symmetricKeyAttestation.PrimaryKey;
                            string secondaryKey = symmetricKeyAttestation.SecondaryKey;
                            return new AuthenticationProviderSymmetricKey(registrationIdSymmetricKey, primaryKey, secondaryKey);

                        default:
                            throw new NotSupportedException("Unrecognized enrollment type");
                    }
                default:
                    throw new NotSupportedException("Unrecognized attestation type");
            }

            throw new NotSupportedException($"Unknown attestation type: '{attestationType}'.");
        }

        private IAuthenticationMethod CreateAuthenticationMethodFromAuthProvider(
            AuthenticationProvider provisioningAuth,
            string deviceId)
        {
            VerboseTestLogger.WriteLine($"{nameof(CreateAuthenticationMethodFromAuthProvider)}({deviceId})");

            IAuthenticationMethod auth;
            if (provisioningAuth is AuthenticationProviderX509 x509Auth)
            {
                X509Certificate2 cert = x509Auth.GetAuthenticationCertificate();
                auth = new ClientAuthenticationWithX509Certificate(cert, deviceId);
            }
            else if (provisioningAuth is AuthenticationProviderSymmetricKey symmetricKeyAuth)
            {
                auth = new ClientAuthenticationWithSharedAccessKeyRefresh(symmetricKeyAuth.GetPrimaryKey(), deviceId);
            }
            else
            {
                throw new NotSupportedException($"Unknown provisioning auth type.");
            }

            return auth;
        }

        /// <summary>
        /// Assert that the device registration result has not errors, and that it was assigned to a hub and has a device id
        /// </summary>
        private static void ValidateDeviceRegistrationResult(bool validatePayload, DeviceRegistrationResult result)
        {
            Assert.IsNotNull(result);
            VerboseTestLogger.WriteLine($"{result.Status} (Error Code: {result.ErrorCode}; Error Message: {result.ErrorMessage})");
            VerboseTestLogger.WriteLine($"ProvisioningDeviceClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

            result.Status.Should().Be(ProvisioningRegistrationStatusType.Assigned, $"Unexpected provisioning status, substatus: {result.Substatus}, error code: {result.ErrorCode}, error message: {result.ErrorMessage}");
            result.AssignedHub.Should().NotBeNull();
            result.DeviceId.Should().NotBeNull();

            if (validatePayload)
            {
                if (PayloadJsonData != null)
                {
                    ((object)result.Payload).Should().NotBeNull();
                    result.Payload.ToString().Should().Be(PayloadJsonData);
                }
                else
                {
#pragma warning disable CS0162 // Unreachable code detected
                    ((object)result.Payload).Should().BeNull();
#pragma warning restore CS0162 // Unreachable code detected
                }
            }
            else
            {
                ((object)result.Payload).Should().BeNull();
            }
        }

        public static async Task DeleteCreatedEnrollmentAsync(
            EnrollmentType? enrollmentType,
            AuthenticationProvider authProvider,
            string groupId)
        {
            using ProvisioningServiceClient dpsClient = CreateProvisioningService();

            try
            {
                if (enrollmentType == EnrollmentType.Individual)
                {
                    await RetryOperationHelper
                        .RunWithRetryAsync(
                            async () =>
                            {
                                await dpsClient.IndividualEnrollments.DeleteAsync(authProvider.GetRegistrationId()).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy,
                            CancellationToken.None)
                        .ConfigureAwait(false);
                }
                else if (enrollmentType == EnrollmentType.Group)
                {
                    await RetryOperationHelper
                        .RunWithRetryAsync(
                            async () =>
                            {
                                await dpsClient.EnrollmentGroups.DeleteAsync(groupId).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy,
                            CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cleanup of enrollment failed due to {ex}.");
            }
        }

        /// <summary>
        /// Generate the derived symmetric key for the provisioned device from the symmetric key used in attestation
        /// </summary>
        /// <param name="masterKey">Symmetric key enrollment group primary/secondary key value</param>
        /// <param name="registrationId">the registration id to create</param>
        /// <returns>the primary/secondary key for the member of the enrollment group</returns>
        public static string ComputeDerivedSymmetricKey(byte[] masterKey, string registrationId)
        {
            using var hmac = new HMACSHA256(masterKey);
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationId)));
        }

        public static bool ImplementsWebProxy(IotHubClientTransportSettings transportSettings)
        {
            return transportSettings is IotHubClientMqttSettings or IotHubClientAmqpSettings;
        }

        [ClassCleanup]
        public static void CleanupCertificates()
        {
            // Delete all the test client certificates created
            try
            {
                s_x509CertificatesFolder.Delete(true);
            }
            catch (Exception)
            {
                // In case of an exception, silently exit. All systems images on Microsoft hosted agents will be cleaned up by the system.
            }
        }
    }
}
