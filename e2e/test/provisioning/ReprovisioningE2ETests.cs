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
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
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
        private static readonly string s_proxyServerAddress = TestConfiguration.IoTHub.ProxyServerAddress;
        private static readonly X509Certificate2 s_groupEnrollmentCertificate = TestConfiguration.Provisioning.GetGroupEnrollmentCertificate();

        private readonly string _idPrefix = $"E2E-{nameof(ReprovisioningE2ETests).ToLower()}-";
        private readonly VerboseTestLogger _verboseLog = VerboseTestLogger.GetInstance();

        private static readonly HashSet<Type> s_retryableExceptions = new HashSet<Type> { typeof(ProvisioningServiceClientHttpException) };
        private static readonly IRetryPolicy s_provisioningServiceRetryPolicy = new ProvisioningServiceRetryPolicy();

        private static DirectoryInfo s_dpsClientCertificateFolder;
        private static DirectoryInfo s_selfSignedCertificatesFolder;

        [ClassInitialize]
        public static void TestClassSetup(TestContext _)
        {
            // Create a folder to hold the DPS client certificates and X509 self-signed certificates. If a folder by the same name already exists, it will be used.
            s_dpsClientCertificateFolder = Directory.CreateDirectory("DpsClientCertificates");
            s_selfSignedCertificatesFolder = s_dpsClientCertificateFolder.CreateSubdirectory("SelfSignedCertificates");
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_MqttWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_Mqtt_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_AmqpWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_Amqp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_MqttWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_Mqtt_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_AmqpWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_Amqp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningWorks_Http_SymmetricKey_RegisterOk_Individual()
        {
            //twin is irrelevant since HTTP Device Clients can't use twin
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_Http_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_Mqtt_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_Amqp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_AmqpWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_MqttWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_MqttWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_Mqtt_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_AmqpWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_Amqp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_MqttWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_Mqtt_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_AmqpWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_Amqp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningWorks_Http_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_Http_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_Mqtt_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_Amqp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_AmqpWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_MqttWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        /// <summary>
        /// This test flow reprovisions a device after that device created some twin updates on its original hub.
        /// The expected behaviour is that, with ReprovisionPolicy set to not migrate data, the twin updates from the original hub are not present at the new hub
        /// </summary>
        private async Task ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType transportProtocol, AttestationMechanismType attestationType, EnrollmentType enrollmentType, bool setCustomProxy, string customServerProxy = null)
        {
            var connectionString = IotHubConnectionStringBuilder.Create(TestConfiguration.IoTHub.ConnectionString);
            ICollection<string> iotHubsToStartAt = new List<string>() { TestConfiguration.Provisioning.FarAwayIotHubHostName };
            ICollection<string> iotHubsToReprovisionTo = new List<string>() { connectionString.HostName };
            await ProvisioningDeviceClient_ReprovisioningFlow(transportProtocol, attestationType, enrollmentType, setCustomProxy, new ReprovisionPolicy { MigrateDeviceData = false, UpdateHubAssignment = true }, AllocationPolicy.Hashed, null, iotHubsToStartAt, iotHubsToReprovisionTo, customServerProxy).ConfigureAwait(false);
        }

        /// <summary>
        /// This test flow reprovisions a device after that device created some twin updates on its original hub.
        /// The expected behaviour is that, with ReprovisionPolicy set to migrate data, the twin updates from the original hub are present at the new hub
        /// </summary>
        private async Task ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType transportProtocol, AttestationMechanismType attestationType, EnrollmentType enrollmentType, bool setCustomProxy, string customServerProxy = null)
        {
            var connectionString = IotHubConnectionStringBuilder.Create(TestConfiguration.IoTHub.ConnectionString);
            ICollection<string> iotHubsToStartAt = new List<string>() { TestConfiguration.Provisioning.FarAwayIotHubHostName };
            ICollection<string> iotHubsToReprovisionTo = new List<string>() { connectionString.HostName };
            await ProvisioningDeviceClient_ReprovisioningFlow(transportProtocol, attestationType, enrollmentType, setCustomProxy, new ReprovisionPolicy { MigrateDeviceData = true, UpdateHubAssignment = true }, AllocationPolicy.Hashed, null, iotHubsToStartAt, iotHubsToReprovisionTo, customServerProxy).ConfigureAwait(false);
        }

        /// <summary>
        /// The expected behaviour is that, with ReprovisionPolicy set to never update hub, the a device is not reprovisioned, even when other settings would suggest it should
        /// </summary>
        private async Task ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType transportProtocol, AttestationMechanismType attestationType, EnrollmentType enrollmentType, bool setCustomProxy, string customServerProxy = null)
        {
            var connectionString = IotHubConnectionStringBuilder.Create(TestConfiguration.IoTHub.ConnectionString);
            ICollection<string> iotHubsToStartAt = new List<string>() { TestConfiguration.Provisioning.FarAwayIotHubHostName };
            ICollection<string> iotHubsToReprovisionTo = new List<string>() { connectionString.HostName };
            await ProvisioningDeviceClient_ReprovisioningFlow(transportProtocol, attestationType, enrollmentType, setCustomProxy, new ReprovisionPolicy { MigrateDeviceData = false, UpdateHubAssignment = false }, AllocationPolicy.Hashed, null, iotHubsToStartAt, iotHubsToReprovisionTo, customServerProxy).ConfigureAwait(false);
        }

        /// <summary>
        /// Provisions a device to a starting hub, tries to open a connection, send telemetry,
        /// and (if supported by the protocol) send a twin update. Then, this method updates the enrollment
        /// to provision the device to a different hub. Based on the provided reprovisioning settings, this
        /// method then checks that the device was/was not reprovisioned as expected, and that the device
        /// did/did not migrate twin data as expected.
        /// </summary>
        public async Task ProvisioningDeviceClient_ReprovisioningFlow(
            Client.TransportType transportProtocol,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            ICollection<string> iotHubsToStartAt,
            ICollection<string> iotHubsToReprovisionTo,
            string proxyServerAddress = null)
        {
            using ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(s_proxyServerAddress);
            string groupId = _idPrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();

            bool transportProtocolSupportsTwinOperations = transportProtocol != Client.TransportType.Http1;

            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol);
            using SecurityProvider security = await CreateSecurityProviderFromNameAsync(
                    attestationType,
                    enrollmentType,
                    groupId,
                    reprovisionPolicy,
                    allocationPolicy,
                    customAllocationDefinition,
                    iotHubsToStartAt)
                .ConfigureAwait(false);

            //Check basic provisioning
            if (ImplementsWebProxy(transportProtocol) && setCustomProxy)
            {
                transport.Proxy = (proxyServerAddress != null) ? new WebProxy(s_proxyServerAddress) : null;
            }

            var provClient = ProvisioningDeviceClient.Create(
                s_globalDeviceEndpoint,
                TestConfiguration.Provisioning.IdScope,
                security,
                transport);
            using var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);
            DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
            ValidateDeviceRegistrationResult(result);

#pragma warning disable CA2000 // Dispose objects before losing scope
            // The certificate instance referenced in the DeviceAuthenticationWithX509Certificate instance is common for all tests in this class. It is disposed during class cleanup.
            Client.IAuthenticationMethod auth = CreateAuthenticationMethodFromSecurityProvider(security, result.DeviceId);
#pragma warning restore CA2000 // Dispose objects before losing scope

            await ConfirmRegisteredDeviceWorks(result, auth, transportProtocol, transportProtocolSupportsTwinOperations).ConfigureAwait(false);

            //Check reprovisioning
            await UpdateEnrollmentToForceReprovision(enrollmentType, provisioningServiceClient, iotHubsToReprovisionTo, security, groupId).ConfigureAwait(false);
            result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
            ReprovisioningE2ETests.ConfirmDeviceInExpectedHub(result, reprovisionPolicy, iotHubsToStartAt, iotHubsToReprovisionTo, allocationPolicy);
            await ConfirmDeviceWorksAfterReprovisioning(result, auth, transportProtocol, reprovisionPolicy, transportProtocolSupportsTwinOperations).ConfigureAwait(false);

            if (attestationType != AttestationMechanismType.X509) //x509 enrollments are hardcoded, should never be deleted
            {
                await DeleteCreatedEnrollmentAsync(enrollmentType, security, groupId).ConfigureAwait(false);
            }

            if (auth is IDisposable disposableAuth)
            {
                disposableAuth?.Dispose();
            }
        }

        /// <summary>
        /// Attempt to create device client instance from provided arguments, ensure that it can open a
        /// connection, ensure that it can send telemetry, and (optionally) send a reported property update
        /// </summary>
        private async Task ConfirmRegisteredDeviceWorks(DeviceRegistrationResult result, Client.IAuthenticationMethod auth, Client.TransportType transportProtocol, bool transportProtocolSupportsTwinOperations)
        {
            using var iotClient = DeviceClient.Create(result.AssignedHub, auth, transportProtocol);
            Logger.Trace("DeviceClient OpenAsync.");
            await iotClient.OpenAsync().ConfigureAwait(false);
            Logger.Trace("DeviceClient SendEventAsync.");

            using var message = new Client.Message(Encoding.UTF8.GetBytes("TestMessage"));
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

        private static async Task ConfirmExpectedDeviceCapabilities(DeviceRegistrationResult result, Client.IAuthenticationMethod auth, DeviceCapabilities capabilities)
        {
            if (capabilities != null)
            {
                //hardcoding amqp since http does not support twin, but tests that call into this may use http
                using var iotClient = DeviceClient.Create(result.AssignedHub, auth, Client.TransportType.Amqp);
                //Confirm that the device twin reflects what the enrollment dictated
                Client.Twin twin = await iotClient.GetTwinAsync().ConfigureAwait(false);
                twin.Capabilities.IotEdge.Should().Be(capabilities.IotEdge);
            }
        }

        private async Task<SecurityProvider> CreateSecurityProviderFromNameAsync(
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            string groupId,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            ICollection<string> iothubs,
            Devices.Provisioning.Service.DeviceCapabilities capabilities = null)
        {
            _verboseLog.WriteLine($"{nameof(CreateSecurityProviderFromNameAsync)}({attestationType})");

            string registrationId = AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            using var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(TestConfiguration.Provisioning.ConnectionString);

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
                        Logger).ConfigureAwait(false);

                    return new SecurityProviderTpmSimulator(tpmEnrollment.RegistrationId);

                case AttestationMechanismType.X509:
                    X509Certificate2 certificate = null;
                    X509Certificate2Collection collection = null;
                    switch (enrollmentType)
                    {
                        case EnrollmentType.Individual:
                            X509Certificate2Generator.GenerateSelfSignedCertificates(registrationId, s_selfSignedCertificatesFolder, Logger);

#pragma warning disable CA2000 // Dispose objects before losing scope
                            // This certificate is used for authentication with IoT hub, it is disposed at the end of the test method.
                            X509Certificate2 publicPrivateCertificate = ReprovisioningE2ETests.CreateX509CertificateWithPublicPrivateKey(registrationId);
#pragma warning restore CA2000 // Dispose objects before losing scope

                            using (X509Certificate2 publicCertificate = ReprovisioningE2ETests.CreateX509CertificateWithPublicKey(registrationId))
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
                                    Logger).ConfigureAwait(false);

                                x509IndividualEnrollment.Attestation.Should().BeAssignableTo<X509Attestation>();
                            }

                            return new SecurityProviderX509Certificate(publicPrivateCertificate);

                        case EnrollmentType.Group:
                            certificate = s_groupEnrollmentCertificate;
                            collection = TestConfiguration.Provisioning.GetGroupEnrollmentChain();
                            break;

                        default:
                            throw new NotSupportedException($"Unknown X509 type: '{enrollmentType}'");
                    }

                    return new SecurityProviderX509Certificate(certificate, collection);

                case AttestationMechanismType.SymmetricKey:
                    switch (enrollmentType)
                    {
                        case EnrollmentType.Group:
                            EnrollmentGroup symmetricKeyEnrollmentGroup = await CreateEnrollmentGroup(
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
                            Assert.IsTrue(symmetricKeyEnrollmentGroup.Attestation is SymmetricKeyAttestation);
                            var symmetricKeyAttestation = (SymmetricKeyAttestation)symmetricKeyEnrollmentGroup.Attestation;
                            string registrationIdSymmetricKey = _idPrefix + Guid.NewGuid();
                            string primaryKeyEnrollmentGroup = symmetricKeyAttestation.PrimaryKey;
                            string secondaryKeyEnrollmentGroup = symmetricKeyAttestation.SecondaryKey;

                            string primaryKeyIndividual = ComputeDerivedSymmetricKey(Convert.FromBase64String(primaryKeyEnrollmentGroup), registrationIdSymmetricKey);
                            string secondaryKeyIndividual = ComputeDerivedSymmetricKey(Convert.FromBase64String(secondaryKeyEnrollmentGroup), registrationIdSymmetricKey);

                            return new SecurityProviderSymmetricKey(registrationIdSymmetricKey, primaryKeyIndividual, secondaryKeyIndividual);

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
                                Logger).ConfigureAwait(false);

                            Assert.IsTrue(symmetricKeyEnrollment.Attestation is SymmetricKeyAttestation);
                            symmetricKeyAttestation = (SymmetricKeyAttestation)symmetricKeyEnrollment.Attestation;

                            registrationIdSymmetricKey = symmetricKeyEnrollment.RegistrationId;
                            string primaryKey = symmetricKeyAttestation.PrimaryKey;
                            string secondaryKey = symmetricKeyAttestation.SecondaryKey;
                            return new SecurityProviderSymmetricKey(registrationIdSymmetricKey, primaryKey, secondaryKey);

                        default:
                            throw new NotSupportedException("Unrecognized enrollment type");
                    }
                default:
                    throw new NotSupportedException("Unrecognized attestation type");
            }

            throw new NotSupportedException($"Unknown security type: '{attestationType}'.");
        }

        private Client.IAuthenticationMethod CreateAuthenticationMethodFromSecurityProvider(
            SecurityProvider provisioningSecurity,
            string deviceId)
        {
            _verboseLog.WriteLine($"{nameof(CreateAuthenticationMethodFromSecurityProvider)}({deviceId})");

            Client.IAuthenticationMethod auth;
            if (provisioningSecurity is SecurityProviderTpm tpmSecurity)
            {
                auth = new DeviceAuthenticationWithTpm(deviceId, tpmSecurity);
            }
            else if (provisioningSecurity is SecurityProviderX509 x509Security)
            {
                X509Certificate2 cert = x509Security.GetAuthenticationCertificate();
                auth = new DeviceAuthenticationWithX509Certificate(deviceId, cert);
            }
            else if (provisioningSecurity is SecurityProviderSymmetricKey symmetricKeySecurity)
            {
                auth = new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, symmetricKeySecurity.GetPrimaryKey());
            }
            else
            {
                throw new NotSupportedException($"Unknown provisioningSecurity type.");
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
        private async Task UpdateEnrollmentToForceReprovision(EnrollmentType? enrollmentType, ProvisioningServiceClient provisioningServiceClient, ICollection<string> iotHubsToReprovisionTo, SecurityProvider security, string groupId)
        {
            if (enrollmentType == EnrollmentType.Individual)
            {
                IndividualEnrollment retrievedEnrollment = null;
                await RetryOperationHelper
                            .RetryOperationsAsync(
                                async () =>
                                {
                                    retrievedEnrollment = await provisioningServiceClient.GetIndividualEnrollmentAsync(security.GetRegistrationID()).ConfigureAwait(false);
                                },
                                s_provisioningServiceRetryPolicy,
                                s_retryableExceptions,
                                Logger)
                            .ConfigureAwait(false);

                if (retrievedEnrollment == null)
                {
                    throw new ArgumentException($"The individual enrollment entry with registration Id {security.GetRegistrationID()} could not be retrieved, exiting test.");
                }

                retrievedEnrollment.IotHubs = iotHubsToReprovisionTo;
                IndividualEnrollment updatedEnrollment = null;

                await RetryOperationHelper
                            .RetryOperationsAsync(
                                async () =>
                                {
                                    updatedEnrollment = await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(retrievedEnrollment).ConfigureAwait(false);
                                },
                                s_provisioningServiceRetryPolicy,
                                s_retryableExceptions,
                                Logger)
                            .ConfigureAwait(false);

                if (updatedEnrollment == null)
                {
                    throw new ArgumentException($"The individual enrollment entry with registration Id {security.GetRegistrationID()} could not be updated, exiting test.");
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
                                Logger)
                            .ConfigureAwait(false);

                if (retrievedEnrollmentGroup == null)
                {
                    throw new ArgumentException($"The enrollment group entry with group Id {groupId} could not be retrieved, exiting test.");
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
                                Logger)
                            .ConfigureAwait(false);

                if (updatedEnrollmentGroup == null)
                {
                    throw new ArgumentException($"The enrollment group entry with group Id {groupId} could not be updated, exiting test.");
                }
            }
        }

        /// <summary>
        /// Confirm that the hub the device belongs to did or did not change, depending on the reprovision policy
        /// </summary>
        private static void ConfirmDeviceInExpectedHub(DeviceRegistrationResult result, ReprovisionPolicy reprovisionPolicy, ICollection<string> iotHubsToStartAt, ICollection<string> iotHubsToReprovisionTo, AllocationPolicy allocationPolicy)
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

        private async Task ConfirmDeviceWorksAfterReprovisioning(
            DeviceRegistrationResult result,
            Client.IAuthenticationMethod auth,
            Client.TransportType transportProtocol,
            ReprovisionPolicy reprovisionPolicy,
            bool transportProtocolSupportsTwinOperations)
        {
            using var iotClient = DeviceClient.Create(result.AssignedHub, auth, transportProtocol);
            Logger.Trace("DeviceClient OpenAsync.");
            await iotClient.OpenAsync().ConfigureAwait(false);
            Logger.Trace("DeviceClient SendEventAsync.");

            using var testMessage = new Client.Message(Encoding.UTF8.GetBytes("TestMessage"));
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

        private static X509Certificate2 CreateX509CertificateWithPublicKey(string registrationId)
        {
            return new X509Certificate2(Path.Combine(s_selfSignedCertificatesFolder.FullName, $"{registrationId}.crt"));
        }

        private static X509Certificate2 CreateX509CertificateWithPublicPrivateKey(string registrationId)
        {
            return new X509Certificate2(Path.Combine(s_selfSignedCertificatesFolder.FullName, $"{registrationId}.pfx"));
        }

        [ClassCleanup]
        public static void CleanupCertificates()
        {
            s_groupEnrollmentCertificate?.Dispose();

            // Delete all the test client certificates created
            try
            {
                s_dpsClientCertificateFolder.Delete(true);
            }
            catch (Exception)
            {
                // In case of an exception, silently exit. All systems images on Microsoft hosted agents will be cleaned up by the system.
            }
        }
    }
}
