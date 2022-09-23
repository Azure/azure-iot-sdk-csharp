// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Azure.Devices.E2ETests.Provisioning.ProvisioningE2ETests;
using static Microsoft.Azure.Devices.E2ETests.Provisioning.ProvisioningServiceClientE2ETests;

namespace Microsoft.Azure.Devices.E2ETests.Provisioning
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("DPS")]
    [TestCategory("LongRunning")]
    public class ReprovisioningE2ETests : E2EMsTestBase
    {
        private const int PassingTimeoutMiliseconds = 10 * 60 * 1000;
        private static readonly string s_globalDeviceEndpoint = TestConfiguration.Provisioning.GlobalDeviceEndpoint;
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;
        private static readonly string s_certificatePassword = TestConfiguration.Provisioning.CertificatePassword;

        private readonly string _idPrefix = $"E2E-{nameof(ReprovisioningE2ETests).ToLower()}-";
        private readonly VerboseTestLogger _verboseLog = VerboseTestLogger.GetInstance();

        private static readonly HashSet<Type> s_retryableExceptions = new HashSet<Type> { typeof(DeviceProvisioningServiceException) };
        private static readonly IRetryPolicy s_provisioningServiceRetryPolicy = new ProvisioningServiceRetryPolicy();

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

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_MqttWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_MqttTcp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_AmqpWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_AmqpTcp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_MqttWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_MqttTcp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_AmqpWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_AmqpTcp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_MqttTcp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_AmqpTcp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_AmqpWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_MqttWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_MqttWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_MqttTcp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_AmqpWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_AmqpTcp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_MqttWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_MqttTcp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_AmqpWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_AmqpTcp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_MqttTcp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_AmqpTcp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_AmqpWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_MqttWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group, false)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// This test flow reprovisions a device after that device created some twin updates on its original hub.
        /// The expected behaviour is that, with ReprovisionPolicy set to not migrate data, the twin updates from the original hub are not present at the new hub
        /// </summary>
        private async Task ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(
            IotHubClientTransportSettings transportSettings,
            AttestationMechanismType attestationType,
            EnrollmentType enrollmentType,
            bool setCustomProxy,
            string customServerProxy = null)
        {
            var connectionString = IotHubConnectionStringBuilder.Create(TestConfiguration.IotHub.ConnectionString);
            var iotHubsToStartAt = new List<string> { TestConfiguration.Provisioning.FarAwayIotHubHostName };
            var iotHubsToReprovisionTo = new List<string> { connectionString.HostName };
            await ProvisioningDeviceClient_ReprovisioningFlow(
                    transportSettings,
                    attestationType,
                    enrollmentType,
                    setCustomProxy,
                    new ReprovisionPolicy { MigrateDeviceData = false, UpdateHubAssignment = true },
                    AllocationPolicy.Hashed,
                    null,
                    iotHubsToStartAt,
                    iotHubsToReprovisionTo,
                    customServerProxy)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// This test flow reprovisions a device after that device created some twin updates on its original hub.
        /// The expected behaviour is that, with ReprovisionPolicy set to migrate data, the twin updates from the original hub are present at the new hub
        /// </summary>
        private async Task ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(
            IotHubClientTransportSettings transportSettings,
            AttestationMechanismType attestationType,
            EnrollmentType enrollmentType,
            bool setCustomProxy,
            string customServerProxy = null)
        {
            var connectionString = IotHubConnectionStringBuilder.Create(TestConfiguration.IotHub.ConnectionString);
            var iotHubsToStartAt = new List<string> { TestConfiguration.Provisioning.FarAwayIotHubHostName };
            var iotHubsToReprovisionTo = new List<string> { connectionString.HostName };
            await ProvisioningDeviceClient_ReprovisioningFlow(
                    transportSettings,
                    attestationType,
                    enrollmentType,
                    setCustomProxy,
                    new ReprovisionPolicy { MigrateDeviceData = true, UpdateHubAssignment = true },
                    AllocationPolicy.Hashed,
                    null,
                    iotHubsToStartAt,
                    iotHubsToReprovisionTo,
                    customServerProxy)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// The expected behavior is that, with ReprovisionPolicy set to never update hub, the a device is not reprovisioned, even when other settings would suggest it should
        /// </summary>
        private async Task ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(
            IotHubClientTransportSettings transportSettings,
            AttestationMechanismType attestationType,
            EnrollmentType enrollmentType,
            bool setCustomProxy,
            string customServerProxy = null)
        {
            var connectionString = IotHubConnectionStringBuilder.Create(TestConfiguration.IotHub.ConnectionString);
            var iotHubsToStartAt = new List<string>() { TestConfiguration.Provisioning.FarAwayIotHubHostName };
            var iotHubsToReprovisionTo = new List<string>() { connectionString.HostName };
            await ProvisioningDeviceClient_ReprovisioningFlow(
                    transportSettings,
                    attestationType,
                    enrollmentType,
                    setCustomProxy,
                    new ReprovisionPolicy { MigrateDeviceData = false, UpdateHubAssignment = false },
                    AllocationPolicy.Hashed,
                    null,
                    iotHubsToStartAt,
                    iotHubsToReprovisionTo,
                    customServerProxy)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Provisions a device to a starting hub, tries to open a connection, send telemetry,
        /// and (if supported by the protocol) send a twin update. Then, this method updates the enrollment
        /// to provision the device to a different hub. Based on the provided reprovisioning settings, this
        /// method then checks that the device was/was not reprovisioned as expected, and that the device
        /// did/did not migrate twin data as expected.
        /// </summary>
        public async Task ProvisioningDeviceClient_ReprovisioningFlow(
            IotHubClientTransportSettings transportSettings,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            IList<string> iotHubsToStartAt,
            IList<string> iotHubsToReprovisionTo,
            string proxyServerAddress = null)
        {
            using ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(s_proxyServerAddress);

            string groupId = null;
            if (enrollmentType == EnrollmentType.Group)
            {
                groupId = attestationType == AttestationMechanismType.X509
                    ? TestConfiguration.Provisioning.X509GroupEnrollmentName
                    : _idPrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            }

            bool transportProtocolSupportsTwinOperations = transportSettings is not IotHubClientHttpSettings;

            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportSettings);
            AuthenticationProvider auth = await CreateAuthenticationProviderFromNameAsync(
                    attestationType,
                    enrollmentType,
                    groupId,
                    reprovisionPolicy,
                    allocationPolicy,
                    customAllocationDefinition,
                    iotHubsToStartAt)
                .ConfigureAwait(false);

            //Check basic provisioning
            if (ImplementsWebProxy(transportSettings) && setCustomProxy)
            {
                transport.Proxy = proxyServerAddress == null
                    ? null
                    : new WebProxy(s_proxyServerAddress);
            }

            var provClient = new ProvisioningDeviceClient(
                s_globalDeviceEndpoint,
                TestConfiguration.Provisioning.IdScope,
                auth,
                new ProvisioningClientOptions(transport));
            using var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);
            DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
            ValidateDeviceRegistrationResult(result);

#pragma warning disable CA2000 // Dispose objects before losing scope
            // The certificate instance referenced in the DeviceAuthenticationWithX509Certificate instance is common for all tests in this class. It is disposed during class cleanup.
            Client.IAuthenticationMethod authMethod = CreateAuthenticationMethodFromAuthenticationProvider(auth, result.DeviceId);
#pragma warning restore CA2000 // Dispose objects before losing scope

            await ConfirmRegisteredDeviceWorksAsync(result, authMethod, transportSettings, transportProtocolSupportsTwinOperations).ConfigureAwait(false);

            // Check reprovisioning
            await UpdateEnrollmentToForceReprovisionAsync(enrollmentType, provisioningServiceClient, iotHubsToReprovisionTo, auth, groupId).ConfigureAwait(false);
            result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
            ConfirmDeviceInExpectedHub(result, reprovisionPolicy, iotHubsToStartAt, iotHubsToReprovisionTo, allocationPolicy);
            await ConfirmDeviceWorksAfterReprovisioningAsync(result, authMethod, transportSettings, reprovisionPolicy, transportProtocolSupportsTwinOperations).ConfigureAwait(false);

            if (attestationType == AttestationMechanismType.X509 && enrollmentType == EnrollmentType.Group)
            {
                Logger.Trace($"The test enrollment type {attestationType}-{enrollmentType} with group Id {groupId} is currently hardcoded - do not delete.");
            }
            else
            {
                Logger.Trace($"Deleting test enrollment type {attestationType}-{enrollmentType} with registration Id {auth.GetRegistrationId()}.");
                await DeleteCreatedEnrollmentAsync(enrollmentType, auth, groupId, Logger).ConfigureAwait(false);
            }

            if (auth is AuthenticationProviderX509 x509Auth)
            {
                X509Certificate2 deviceCertificate = x509Auth.GetAuthenticationCertificate();
                deviceCertificate?.Dispose();
            }

            if (authMethod is IDisposable disposableAuth)
            {
                disposableAuth?.Dispose();
            }
        }

        /// <summary>
        /// Attempt to create device client instance from provided arguments, ensure that it can open a
        /// connection, ensure that it can send telemetry, and (optionally) send a reported property update
        /// </summary>
        private async Task ConfirmRegisteredDeviceWorksAsync(
            DeviceRegistrationResult result,
            Client.IAuthenticationMethod auth,
            IotHubClientTransportSettings transportSettings,
            bool transportProtocolSupportsTwinOperations)
        {
            using var iotClient = new IotHubDeviceClient(result.AssignedHub, auth, new IotHubClientOptions(transportSettings));
            Logger.Trace("DeviceClient OpenAsync.");
            await iotClient.OpenAsync().ConfigureAwait(false);
            Logger.Trace("DeviceClient SendEventAsync.");

            var message = new Client.Message(Encoding.UTF8.GetBytes("TestMessage"));
            await iotClient.SendEventAsync(message).ConfigureAwait(false);

            if (transportProtocolSupportsTwinOperations)
            {
                Logger.Trace("DeviceClient updating reported property.");
                Client.Twin twin = await iotClient.GetTwinAsync().ConfigureAwait(false);
                await iotClient.UpdateReportedPropertiesAsync(new Client.TwinCollection($"{{\"{new Guid()}\":\"{new Guid()}\"}}")).ConfigureAwait(false);
            }

            Logger.Trace("DeviceClient CloseAsync.");
            await iotClient.CloseAsync().ConfigureAwait(false);
        }

        private static async Task ConfirmExpectedDeviceCapabilities(
            DeviceRegistrationResult result,
            Client.IAuthenticationMethod auth,
            DeviceCapabilities capabilities)
        {
            if (capabilities != null)
            {
                //hardcoding amqp since http does not support twin, but tests that call into this may use http
                using var iotClient = new IotHubDeviceClient(result.AssignedHub, auth, new IotHubClientOptions(new IotHubClientAmqpSettings()));
                //Confirm that the device twin reflects what the enrollment dictated
                Client.Twin twin = await iotClient.GetTwinAsync().ConfigureAwait(false);
                twin.Capabilities.IotEdge.Should().Be(capabilities.IsIotEdge);
            }
        }

        private async Task<AuthenticationProvider> CreateAuthenticationProviderFromNameAsync(
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            string groupId,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            IList<string> iothubs,
            Devices.Provisioning.Service.DeviceCapabilities capabilities = null)
        {
            _verboseLog.WriteLine($"{nameof(CreateAuthenticationProviderFromNameAsync)}({attestationType})");

            string registrationId = AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            using var provisioningServiceClient = new ProvisioningServiceClient(TestConfiguration.Provisioning.ConnectionString);

            switch (attestationType)
            {
                case AttestationMechanismType.Tpm:
                    IndividualEnrollment tpmEnrollment = await CreateIndividualEnrollmentAsync(
                            provisioningServiceClient,
                            registrationId,
                            AttestationMechanismType.Tpm,
                            null,
                            reprovisionPolicy,
                            allocationPolicy,
                            customAllocationDefinition,
                            iothubs,
                            capabilities,
                            Logger)
                        .ConfigureAwait(false);

                    return new AuthenticationProviderTpmSimulator(tpmEnrollment.RegistrationId);

                case AttestationMechanismType.X509:
                    X509Certificate2 certificate = null;
                    X509Certificate2Collection collection = null;
                    switch (enrollmentType)
                    {
                        case EnrollmentType.Individual:
                            X509Certificate2Helper.GenerateSelfSignedCertificateFiles(registrationId, s_x509CertificatesFolder, Logger);

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
                                        capabilities,
                                        Logger)
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
                                s_x509CertificatesFolder,
                                Logger);

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

                    return new AuthenticationProviderX509Certificate(certificate, collection);

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
                                    capabilities,
                                    Logger)
                                .ConfigureAwait(false);
                            symmetricKeyEnrollmentGroup.Attestation.Should().BeOfType(typeof(SymmetricKeyAttestation));
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
                                    capabilities,
                                    Logger)
                                .ConfigureAwait(false);

                            symmetricKeyEnrollment.Attestation.Should().BeOfType(typeof(SymmetricKeyAttestation));
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

        private Client.IAuthenticationMethod CreateAuthenticationMethodFromAuthenticationProvider(
            AuthenticationProvider provisioningAuth,
            string deviceId)
        {
            _verboseLog.WriteLine($"{nameof(CreateAuthenticationMethodFromAuthenticationProvider)}({deviceId})");

            Client.IAuthenticationMethod auth;
            if (provisioningAuth is AuthenticationProviderTpm tpmAuth)
            {
                auth = new DeviceAuthenticationWithTpm(deviceId, tpmAuth);
            }
            else if (provisioningAuth is AuthenticationProviderX509 x509Auth)
            {
                X509Certificate2 cert = x509Auth.GetAuthenticationCertificate();
                auth = new DeviceAuthenticationWithX509Certificate(deviceId, cert);
            }
            else if (provisioningAuth is AuthenticationProviderSymmetricKey symmetricKeyAuth)
            {
                auth = new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, symmetricKeyAuth.GetPrimaryKey());
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
        private void ValidateDeviceRegistrationResult(DeviceRegistrationResult result)
        {
            result.Should().NotBeNull();
            Logger.Trace($"{result.Status} (Error Code: {result.ErrorCode}; Error Message: {result.ErrorMessage})");
            Logger.Trace($"ProvisioningDeviceClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

            result.Status.Should().Be(ProvisioningRegistrationStatusType.Assigned, $"Unexpected provisioning status, substatus: {result.Substatus}, error code: {result.ErrorCode}, error message: {result.ErrorMessage}");
            result.AssignedHub.Should().NotBeNull();
            result.DeviceId.Should().NotBeNull();
        }

        /// <summary>
        /// Update the enrollment under test such that it forces it to re-provision to the hubs within <paramref name="iotHubsToReprovisionTo"/>
        /// </summary>
        private async Task UpdateEnrollmentToForceReprovisionAsync(
            EnrollmentType? enrollmentType,
            ProvisioningServiceClient provisioningServiceClient,
            IList<string> iotHubsToReprovisionTo,
            AuthenticationProvider auth,
            string groupId)
        {
            if (enrollmentType == EnrollmentType.Individual)
            {
                IndividualEnrollment retrievedEnrollment = null;
                await RetryOperationHelper
                    .RetryOperationsAsync(
                        async () =>
                        {
                            retrievedEnrollment = await provisioningServiceClient
                                .GetIndividualEnrollmentAsync(auth.GetRegistrationId())
                                .ConfigureAwait(false);
                        },
                        s_provisioningServiceRetryPolicy,
                        s_retryableExceptions,
                        Logger,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                if (retrievedEnrollment == null)
                {
                    throw new ArgumentException($"The individual enrollment entry with registration Id {auth.GetRegistrationId()} could not be retrieved; exiting test.");
                }

                retrievedEnrollment.IotHubs = iotHubsToReprovisionTo;

                IndividualEnrollment updatedEnrollment = null;

                await RetryOperationHelper
                    .RetryOperationsAsync(
                        async () =>
                        {
                            updatedEnrollment = await provisioningServiceClient
                                .CreateOrUpdateIndividualEnrollmentAsync(retrievedEnrollment)
                                .ConfigureAwait(false);
                        },
                        s_provisioningServiceRetryPolicy,
                        s_retryableExceptions,
                        Logger,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                if (updatedEnrollment == null)
                {
                    throw new ArgumentException($"The individual enrollment entry with registration Id {auth.GetRegistrationId()} could not be updated; exiting test.");
                }
            }
            else
            {
                EnrollmentGroup retrievedEnrollmentGroup = null;
                await RetryOperationHelper
                    .RetryOperationsAsync(
                        async () =>
                        {
                            retrievedEnrollmentGroup = await provisioningServiceClient.GetEnrollmentGroupAsync(groupId).ConfigureAwait(false);
                        },
                        s_provisioningServiceRetryPolicy,
                        s_retryableExceptions,
                        Logger,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                if (retrievedEnrollmentGroup == null)
                {
                    throw new ArgumentException($"The enrollment group entry with group Id {groupId} could not be retrieved; exiting test.");
                }

                retrievedEnrollmentGroup.IotHubs = iotHubsToReprovisionTo;

                EnrollmentGroup updatedEnrollmentGroup = null;

                await RetryOperationHelper
                    .RetryOperationsAsync(
                        async () =>
                        {
                            updatedEnrollmentGroup = await provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(retrievedEnrollmentGroup).ConfigureAwait(false);
                        },
                        s_provisioningServiceRetryPolicy,
                        s_retryableExceptions,
                        Logger,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                if (updatedEnrollmentGroup == null)
                {
                    throw new ArgumentException($"The enrollment group entry with group Id {groupId} could not be updated; exiting test.");
                }
            }
        }

        /// <summary>
        /// Confirm that the hub the device belongs to did or did not change, depending on the reprovision policy
        /// </summary>
        private static void ConfirmDeviceInExpectedHub(
            DeviceRegistrationResult result,
            ReprovisionPolicy reprovisionPolicy,
            ICollection<string> iotHubsToStartAt,
            ICollection<string> iotHubsToReprovisionTo,
            AllocationPolicy allocationPolicy)
        {
            if (reprovisionPolicy.UpdateHubAssignment)
            {
                iotHubsToReprovisionTo.Should().Contain(result.AssignedHub);
                iotHubsToStartAt.Should().NotContain(result.AssignedHub);

                if (allocationPolicy == AllocationPolicy.GeoLatency)
                {
                    result.AssignedHub.Should().NotBe(TestConfiguration.Provisioning.FarAwayIotHubHostName);
                }
            }
            else
            {
                iotHubsToReprovisionTo.Should().NotContain(result.AssignedHub);
                iotHubsToStartAt.Should().Contain(result.AssignedHub);
            }
        }

        private async Task ConfirmDeviceWorksAfterReprovisioningAsync(
            DeviceRegistrationResult result,
            Client.IAuthenticationMethod auth,
            IotHubClientTransportSettings transportSettings,
            ReprovisionPolicy reprovisionPolicy,
            bool transportProtocolSupportsTwinOperations)
        {
            using var iotClient = new IotHubDeviceClient(result.AssignedHub, auth, new IotHubClientOptions(transportSettings));
            Logger.Trace("DeviceClient OpenAsync.");
            await iotClient.OpenAsync().ConfigureAwait(false);
            Logger.Trace("DeviceClient SendEventAsync.");

            var testMessage = new Client.Message(Encoding.UTF8.GetBytes("TestMessage"));
            await iotClient.SendEventAsync(testMessage).ConfigureAwait(false);

            // Twin can be configured to revert back to default twin when provisioned, or to keep twin
            // from previous hub's records.
            if (transportProtocolSupportsTwinOperations)
            {
                Client.Twin twin = await iotClient.GetTwinAsync().ConfigureAwait(false);

                // Reprovision
                if (reprovisionPolicy.UpdateHubAssignment)
                {
                    // Migrate data
                    if (reprovisionPolicy.MigrateDeviceData)
                    {
                        // The reprovisioned twin should have an entry for the reprorted property updated previously in the test
                        // as a part of ConfirmRegisteredDeviceWorks.
                        // On device creation the twin reported property version starts at 1. For this scenario the reported property update
                        // operation should increment the version to 2.
                        twin.Properties.Reported.Count.Should().Be(1);
                        twin.Properties.Reported.Version.Should().Be(2);
                        result.Substatus.Should().Be(ProvisioningRegistrationSubstatusType.DeviceDataMigrated);
                    }
                    // Reset to initial configuration
                    else
                    {
                        // The reprovisioned twin should not have an entry for the reprorted property updated previously in the test
                        // as a part of ConfirmRegisteredDeviceWorks.
                        // On device creation the twin reported property version starts at 1.
                        twin.Properties.Reported.Count.Should().Be(0);
                        twin.Properties.Reported.Version.Should().Be(1);
                        result.Substatus.Should().Be(ProvisioningRegistrationSubstatusType.DeviceDataReset);
                    }
                }
                // Do not reprovision
                else
                {
                    // The reprovisioned twin should have an entry for the reprorted property updated previously in the test
                    // as a part of ConfirmRegisteredDeviceWorks.
                    // On device creation the twin reported property version starts at 1. For this scenario the reported property update
                    // operation should increment the version to 2.
                    twin.Properties.Reported.Count.Should().Be(1);
                    twin.Properties.Reported.Version.Should().Be(2);
                    result.Substatus.Should().Be(ProvisioningRegistrationSubstatusType.InitialAssignment);
                }
            }

            Logger.Trace("DeviceClient CloseAsync.");
            await iotClient.CloseAsync().ConfigureAwait(false);
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
