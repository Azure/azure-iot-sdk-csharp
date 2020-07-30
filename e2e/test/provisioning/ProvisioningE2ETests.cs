// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
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
        private static string s_globalDeviceEndpoint = Configuration.Provisioning.GlobalDeviceEndpoint;
        private const string InvalidIDScope = "0neFFFFFFFF";
        private const string payloadJsonData = "{\"testKey\":\"testValue\"}";
        private const string InvalidGlobalAddress = "httpbin.org";
        private static string s_proxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private readonly string _idPrefix = $"e2e-{nameof(ProvisioningE2ETests).ToLower()}-";

        private readonly VerboseTestLogger _verboseLog = VerboseTestLogger.GetInstance();

        public enum EnrollmentType
        {
            Individual,
            Group
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.Tpm, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.x509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.x509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_Tcp_Only, AttestationType.Tpm, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_Tcp_Only, AttestationType.x509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_Tcp_Only, AttestationType.x509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.Tpm, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_Tcp_Only, AttestationType.x509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_Tcp_Only, AttestationType.x509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_X509Group_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.x509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task ProvisioningDeviceClient_ValidRegistrationId_HttpWithProxy_Tpm_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.Tpm, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        [TestCategory("LongRunning")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_HttpWithNullProxy_Tpm_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.Tpm, EnrollmentType.Individual, true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWsWithProxy_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWsWithNullProxy_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWsWithProxy_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWsWithNullProxy_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_SymmetricKey_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_HttpWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Group, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Group, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("Proxy")]
        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Group, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("Proxy")]
        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWsWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("Proxy")]
        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWsWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        #region DeviceCapabilities

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_SymmetricKey_RegisterOk_EdgeEnabled_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Group, false, new DeviceCapabilities() { IotEdge = true }).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_SymmetricKey_RegisterOk_EdgeEnabled_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Individual, false, new DeviceCapabilities() { IotEdge = true }).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_SymmetricKey_RegisterOk_EdgeDisabled_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Individual, false, new DeviceCapabilities() { IotEdge = false }).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_SymmetricKey_RegisterOk_EdgeDisabled_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Group, false, new DeviceCapabilities() { IotEdge = false }).ConfigureAwait(false);
        }

        #endregion DeviceCapabilities

        #region CustomAllocationDefinition tests

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_Http_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_Mqtt_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Mqtt_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_Amqp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Amqp_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_AmqpWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_MqttWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_Http_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_Mqtt_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Mqtt_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_Amqp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Amqp_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_AmqpWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_MqttWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        /// <summary>
        /// This test flow uses a custom allocation policy to decide which of the two hubs a device should be provisioned to.
        /// The custom allocation policy has a webhook to an Azure function, and that function will always dictate to provision
        /// the device to the hub with the longest host name. This test verifies that an enrollment with a custom allocation policy
        /// pointing to that Azure function will always enroll to the hub with the longest name
        /// </summary>
        private async Task ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType transportProtocol, AttestationType attestationType, EnrollmentType enrollmentType, bool setCustomProxy, string customServerProxy = null)
        {
            var closeHostName = IotHubConnectionStringBuilder.Create(Configuration.IoTHub.ConnectionString).HostName;

            ICollection<string> iotHubsToProvisionTo = new List<string>() { closeHostName, Configuration.Provisioning.FarAwayIotHubHostName };
            string expectedDestinationHub = "";
            if (closeHostName.Length > Configuration.Provisioning.FarAwayIotHubHostName.Length)
            {
                expectedDestinationHub = closeHostName;
            }
            else if (closeHostName.Length < Configuration.Provisioning.FarAwayIotHubHostName.Length)
            {
                expectedDestinationHub = Configuration.Provisioning.FarAwayIotHubHostName;
            }
            else
            {
                //custom endpoint for this test allocates the device to the hub with the longer hostname. If both hubs
                // have the same length, then the test is no longer determenistic, as the device could be allocated to either hub
                Assert.Fail("Configuration failure: far away hub hostname cannot be the same length as the close hub hostname");
            }

            await ProvisioningDeviceClient_ProvisioningFlow_CustomAllocation_AllocateToHubWithLongestHostName(transportProtocol, attestationType, enrollmentType, setCustomProxy, iotHubsToProvisionTo, expectedDestinationHub, customServerProxy).ConfigureAwait(false);
        }

        #endregion CustomAllocationDefinition tests

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_HttpWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWsWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWsWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            Client.TransportType transportType,
            AttestationType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            string proxyServerAddress = null)
        {
            //Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(transportType, attestationType, enrollmentType, setCustomProxy, null, AllocationPolicy.Hashed, null, null, null, s_proxyServerAddress).ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            Client.TransportType transportType,
            AttestationType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            DeviceCapabilities capabilities,
            string proxyServerAddress = null)
        {
            //Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            List<String> iothubs = new List<string>() { IotHubConnectionStringBuilder.Create(Configuration.IoTHub.ConnectionString).HostName };
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(transportType, attestationType, enrollmentType, setCustomProxy, null, AllocationPolicy.Hashed, null, iothubs, capabilities, s_proxyServerAddress).ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            Client.TransportType transportType,
            AttestationType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            ICollection<string> iothubs,
            DeviceCapabilities deviceCapabilities,
            string proxyServerAddress = null)
        {
            string groupId = _idPrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType))
            using (SecurityProvider security = await CreateSecurityProviderFromName(attestationType, enrollmentType, groupId, reprovisionPolicy, allocationPolicy, customAllocationDefinition, iothubs, deviceCapabilities).ConfigureAwait(false))
            {
                _verboseLog.WriteLine("Creating device");

                if (ImplementsWebProxy(transportType) && setCustomProxy)
                {
                    transport.Proxy = (proxyServerAddress != null) ? new WebProxy(s_proxyServerAddress) : null;
                }

                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    s_globalDeviceEndpoint,
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);

                using var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

                Logger.Trace("ProvisioningDeviceClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);

                ValidateDeviceRegistrationResult(false, result);

                Client.IAuthenticationMethod auth = CreateAuthenticationMethodFromSecurityProvider(security, result.DeviceId);

                await ConfirmRegisteredDeviceWorks(result, auth, transportType, false).ConfigureAwait(false);
                await ConfirmExpectedDeviceCapabilities(result, auth, deviceCapabilities).ConfigureAwait(false);

                if (attestationType != AttestationType.x509) //x509 enrollments are hardcoded, should never be deleted
                {
                    await DeleteCreatedEnrollment(enrollmentType, CreateProvisioningService(proxyServerAddress), security, groupId).ConfigureAwait(false);
                }
            }
        }

        public async Task ProvisioningDeviceClient_ProvisioningFlow_CustomAllocation_AllocateToHubWithLongestHostName(
            Client.TransportType transportProtocol,
            AttestationType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            ICollection<string> iotHubsToProvisionTo,
            string expectedDestinationHub,
            string proxyServerAddress = null)
        {
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(s_proxyServerAddress);
            string groupId = _idPrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();

            var customAllocationDefinition = new CustomAllocationDefinition
            {
                WebhookUrl = Configuration.Provisioning.CustomAllocationPolicyWebhook,
                ApiVersion = "2019-03-31",
            };

            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol))
            using (SecurityProvider security = await CreateSecurityProviderFromName(
                    attestationType,
                    enrollmentType,
                    groupId,
                    null,
                    AllocationPolicy.Custom,
                    customAllocationDefinition,
                    iotHubsToProvisionTo)
                .ConfigureAwait(false))
            {
                //Check basic provisioning
                if (ImplementsWebProxy(transportProtocol) && setCustomProxy)
                {
                    transport.Proxy = (proxyServerAddress != null) ? new WebProxy(s_proxyServerAddress) : null;
                }

                var provClient = ProvisioningDeviceClient.Create(
                    s_globalDeviceEndpoint,
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);
                using var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

                //Test registering with valid additional data payload
                DeviceRegistrationResult result = await provClient
                    .RegisterAsync(new ProvisioningRegistrationAdditionalData { JsonData = payloadJsonData }, cts.Token)
                    .ConfigureAwait(false);
                ValidateDeviceRegistrationResult(true, result);
                Assert.AreEqual(expectedDestinationHub, result.AssignedHub);

                //Test registering without additional data
                result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
                ValidateDeviceRegistrationResult(false, result);

                if (attestationType != AttestationType.x509) //x509 enrollments are hardcoded, should never be deleted
                {
                    try
                    {
                        await DeleteCreatedEnrollment(enrollmentType, provisioningServiceClient, security, groupId).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Cleanup of enrollment failed due to {ex}");
                    }
                }
            }
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Http_Fail()
        {
            try
            {
                await ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(Client.TransportType.Http1).ConfigureAwait(false);
                Assert.Fail("Expected exception not thrown");
            }
            catch (ProvisioningTransportException ex)
            {
                // Exception message must contain the errorCode value as below
                Assert.IsTrue(ex.Message.Contains("404201"));
            }
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Amqp_Fail()
        {
            try
            {
                await ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
                Assert.Fail("Expected exception not thrown");
            }
            catch (ProvisioningTransportException ex)
            {
                // Exception message must contain the errorCode value as below
                Assert.IsTrue(ex.Message.Contains("404201"));
            }
        }

        public async Task ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(Client.TransportType transportProtocol)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol))
            using (SecurityProvider security = new SecurityProviderTpmSimulator("invalidregistrationid"))
            {
                var provClient = ProvisioningDeviceClient.Create(
                    s_globalDeviceEndpoint,
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);

                using var cts = new CancellationTokenSource(FailingTimeoutMiliseconds);

                Logger.Trace("ProvisioningDeviceClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);

                Logger.Trace($"{result.Status}");

                Assert.AreEqual(ProvisioningRegistrationStatusType.Failed, result.Status);
                Assert.IsNull(result.AssignedHub);
                Assert.IsNull(result.DeviceId);
                // Exception message must contain the errorCode value as below
                Assert.AreEqual(404201, result.ErrorCode);
            }
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Http_Tpm_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Http1, AttestationType.Tpm, null, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Http_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Http1, AttestationType.x509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Http_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Http1, AttestationType.x509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Amqp_Tpm_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Amqp_Tcp_Only, AttestationType.Tpm, null, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Amqp_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Amqp_Tcp_Only, AttestationType.x509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Amqp_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Amqp_Tcp_Only, AttestationType.x509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time as tpm only accepts one connection at a time
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_AmqpWs_Tpm_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Amqp_WebSocket_Only, AttestationType.Tpm, null, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_AmqpWs_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_AmqpWs_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Mqtt_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Mqtt_Tcp_Only, AttestationType.x509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Mqtt_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Mqtt_Tcp_Only, AttestationType.x509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_MqttWs_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_MqttWs_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.x509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Fail(
            Client.TransportType transportProtocol,
            AttestationType attestationType,
            EnrollmentType? enrollmentType,
            string groupId)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol))
            using (SecurityProvider security = await CreateSecurityProviderFromName(attestationType, enrollmentType, groupId, null, AllocationPolicy.Hashed, null, null).ConfigureAwait(false))
            {
                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    s_globalDeviceEndpoint,
                    InvalidIDScope,
                    security,
                    transport);

                using var cts = new CancellationTokenSource(FailingTimeoutMiliseconds);

                var exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                    () => provClient.RegisterAsync(cts.Token)).ConfigureAwait(false);

                Logger.Trace($"Exception: {exception}");
            }
        }

        #region InvalidGlobalAddress

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Http_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(Client.TransportType.Http1, AttestationType.x509, EnrollmentType.Individual, null).ConfigureAwait(false);
        }

        // Note: This test takes 3 minutes.
        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Amqp_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(Client.TransportType.Amqp_Tcp_Only, AttestationType.x509, EnrollmentType.Individual).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_AmqpWs_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Mqtt_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(Client.TransportType.Mqtt_Tcp_Only, AttestationType.x509, EnrollmentType.Individual).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_MqttWs_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual).ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(
            Client.TransportType transportProtocol,
            AttestationType attestationType,
            EnrollmentType? enrollmentType,
            string groupId = "")
        {
            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol);
            using SecurityProvider security = await CreateSecurityProviderFromName(attestationType, enrollmentType, groupId, null, AllocationPolicy.Hashed, null, null).ConfigureAwait(false);

            ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
InvalidGlobalAddress,
Configuration.Provisioning.IdScope,
security,
transport);

            using var cts = new CancellationTokenSource(FailingTimeoutMiliseconds);

            Logger.Trace("ProvisioningDeviceClient RegisterAsync . . . ");
            ProvisioningTransportException exception = await Assert.
                ThrowsExceptionAsync<ProvisioningTransportException>(() => provClient.RegisterAsync(cts.Token))
                .ConfigureAwait(false);

            Logger.Trace($"Exception: {exception}");
        }

        #endregion InvalidGlobalAddress

        public static ProvisioningTransportHandler CreateTransportHandlerFromName(Client.TransportType transportType)
        {
            switch (transportType)
            {
                case Client.TransportType.Http1:
                    return new ProvisioningTransportHandlerHttp();

                case Client.TransportType.Amqp:
                case Client.TransportType.Amqp_Tcp_Only:
                    return new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly);

                case Client.TransportType.Amqp_WebSocket_Only:
                    return new ProvisioningTransportHandlerAmqp(TransportFallbackType.WebSocketOnly);

                case Client.TransportType.Mqtt:
                case Client.TransportType.Mqtt_Tcp_Only:
                    return new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly);

                case Client.TransportType.Mqtt_WebSocket_Only:
                    return new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly);
            }

            throw new NotSupportedException($"Unknown transport: '{transportType}'.");
        }

        /// <summary>
        /// Attempt to create device client instance from provided arguments, ensure that it can open a
        /// connection, ensure that it can send telemetry, and (optionally) send a reported property update
        /// </summary>
        private async Task ConfirmRegisteredDeviceWorks(DeviceRegistrationResult result, Client.IAuthenticationMethod auth, Client.TransportType transportProtocol, bool sendReportedPropertiesUpdate)
        {
            using (DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth, transportProtocol))
            {
                Logger.Trace("DeviceClient OpenAsync.");
                await iotClient.OpenAsync().ConfigureAwait(false);
                Logger.Trace("DeviceClient SendEventAsync.");
                await iotClient.SendEventAsync(
                    new Client.Message(Encoding.UTF8.GetBytes("TestMessage"))).ConfigureAwait(false);

                if (sendReportedPropertiesUpdate)
                {
                    Logger.Trace("DeviceClient updating desired properties.");
                    Twin twin = await iotClient.GetTwinAsync().ConfigureAwait(false);
                    await iotClient.UpdateReportedPropertiesAsync(new TwinCollection($"{{\"{new Guid()}\":\"{new Guid()}\"}}")).ConfigureAwait(false);
                }

                Logger.Trace("DeviceClient CloseAsync.");
                await iotClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task ConfirmExpectedDeviceCapabilities(DeviceRegistrationResult result, Client.IAuthenticationMethod auth, DeviceCapabilities capabilities)
        {
            if (capabilities != null && capabilities.IotEdge)
            {
                //If device is edge device, it should be able to connect to iot hub as its edgehub module identity
                Client.IotHubConnectionStringBuilder connectionStringBuilder = Client.IotHubConnectionStringBuilder.Create(result.AssignedHub, auth);
                string edgehubConnectionString = connectionStringBuilder.ToString() + ";ModuleId=$edgeHub";
                ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(edgehubConnectionString);
                await moduleClient.OpenAsync().ConfigureAwait(false);
            }
        }

        private async Task<SecurityProvider> CreateSecurityProviderFromName(AttestationType attestationType, EnrollmentType? enrollmentType, string groupId, ReprovisionPolicy reprovisionPolicy, AllocationPolicy allocationPolicy, CustomAllocationDefinition customAllocationDefinition, ICollection<string> iothubs, DeviceCapabilities capabilities = null)
        {
            _verboseLog.WriteLine($"{nameof(CreateSecurityProviderFromName)}({attestationType})");

            var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(Configuration.Provisioning.ConnectionString);

            switch (attestationType)
            {
                case AttestationType.Tpm:
                    string registrationId = AttestationTypeToString(attestationType) + "-registration-id-" + Guid.NewGuid();
                    var tpmSim = new SecurityProviderTpmSimulator(registrationId);

                    string base64Ek = Convert.ToBase64String(tpmSim.GetEndorsementKey());

                    var provisioningService = ProvisioningServiceClient.CreateFromConnectionString(Configuration.Provisioning.ConnectionString);

                    Logger.Trace($"Getting enrollment: RegistrationID = {registrationId}");
                    IndividualEnrollment individualEnrollment = new IndividualEnrollment(registrationId, new TpmAttestation(base64Ek)) { AllocationPolicy = allocationPolicy, ReprovisionPolicy = reprovisionPolicy, IotHubs = iothubs, CustomAllocationDefinition = customAllocationDefinition, Capabilities = capabilities };
                    IndividualEnrollment enrollment = await provisioningService.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                    var attestation = new TpmAttestation(base64Ek);
                    enrollment.Attestation = attestation;
                    Logger.Trace($"Updating enrollment: RegistrationID = {registrationId} EK = '{base64Ek}'");
                    await provisioningService.CreateOrUpdateIndividualEnrollmentAsync(enrollment).ConfigureAwait(false);

                    return tpmSim;

                case AttestationType.x509:

                    X509Certificate2 certificate = null;
                    X509Certificate2Collection collection = null;
                    switch (enrollmentType)
                    {
                        case EnrollmentType.Individual:
                            certificate = Configuration.Provisioning.GetIndividualEnrollmentCertificate();
                            break;

                        case EnrollmentType.Group:
                            certificate = Configuration.Provisioning.GetGroupEnrollmentCertificate();
                            collection = Configuration.Provisioning.GetGroupEnrollmentChain();
                            break;

                        default:
                            throw new NotSupportedException($"Unknown X509 type: '{enrollmentType}'");
                    }

                    return new SecurityProviderX509Certificate(certificate, collection);

                case AttestationType.SymmetricKey:
                    switch (enrollmentType)
                    {
                        case EnrollmentType.Group:
                            EnrollmentGroup symmetricKeyEnrollmentGroup = await CreateEnrollmentGroup(provisioningServiceClient, AttestationType.SymmetricKey, groupId, reprovisionPolicy, allocationPolicy, customAllocationDefinition, iothubs, capabilities).ConfigureAwait(false);
                            Assert.IsTrue(symmetricKeyEnrollmentGroup.Attestation is SymmetricKeyAttestation);
                            SymmetricKeyAttestation symmetricKeyAttestation = (SymmetricKeyAttestation)symmetricKeyEnrollmentGroup.Attestation;
                            string registrationIdSymmetricKey = _idPrefix + Guid.NewGuid();
                            string primaryKeyEnrollmentGroup = symmetricKeyAttestation.PrimaryKey;
                            string secondaryKeyEnrollmentGroup = symmetricKeyAttestation.SecondaryKey;

                            string primaryKeyIndividual = ComputeDerivedSymmetricKey(Convert.FromBase64String(primaryKeyEnrollmentGroup), registrationIdSymmetricKey);
                            string secondaryKeyIndividual = ComputeDerivedSymmetricKey(Convert.FromBase64String(secondaryKeyEnrollmentGroup), registrationIdSymmetricKey);

                            return new SecurityProviderSymmetricKey(registrationIdSymmetricKey, primaryKeyIndividual, secondaryKeyIndividual);

                        case EnrollmentType.Individual:
                            IndividualEnrollment symmetricKeyEnrollment = await CreateIndividualEnrollment(provisioningServiceClient, AttestationType.SymmetricKey, reprovisionPolicy, allocationPolicy, customAllocationDefinition, iothubs, capabilities).ConfigureAwait(false);

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

            if (provisioningSecurity is SecurityProviderTpm)
            {
                var security = (SecurityProviderTpm)provisioningSecurity;
                var auth = new DeviceAuthenticationWithTpm(deviceId, security);
                return auth;
            }
            else if (provisioningSecurity is SecurityProviderX509)
            {
                var security = (SecurityProviderX509)provisioningSecurity;
                X509Certificate2 cert = security.GetAuthenticationCertificate();
                return new DeviceAuthenticationWithX509Certificate(deviceId, cert);
            }
            else if (provisioningSecurity is SecurityProviderSymmetricKey)
            {
                var security = (SecurityProviderSymmetricKey)provisioningSecurity;
                var auth = new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, security.GetPrimaryKey());
                return auth;
            }

            throw new NotSupportedException($"Unknown provisioningSecurity type.");
        }

        /// <summary>
        /// Assert that the device registration result has not errors, and that it was assigned to a hub and has a device id
        /// </summary>
        private void ValidateDeviceRegistrationResult(bool validatePayload, DeviceRegistrationResult result)
        {
            Assert.IsNotNull(result);
            Logger.Trace($"{result.Status} (Error Code: {result.ErrorCode}; Error Message: {result.ErrorMessage})");
            Logger.Trace($"ProvisioningDeviceClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

            Assert.AreEqual(ProvisioningRegistrationStatusType.Assigned, result.Status, $"Unexpected provisioning status, substatus: {result.Substatus}, error code: {result.ErrorCode}, error message: {result.ErrorMessage}");
            Assert.IsNotNull(result.AssignedHub);
            Assert.IsNotNull(result.DeviceId);

            if (validatePayload)
            {
                if (payloadJsonData != null)
                {
                    Assert.IsNotNull(result.JsonPayload);
                    Assert.AreEqual(payloadJsonData, result.JsonPayload);
                }
                else
                {
#pragma warning disable CS0162 // Unreachable code detected
                    Assert.IsNull(result.JsonPayload);
#pragma warning restore CS0162 // Unreachable code detected
                }
            }
            else
            {
                Assert.IsNull(result.JsonPayload);
            }
        }

        public static async Task DeleteCreatedEnrollment(
            EnrollmentType? enrollmentType,
            ProvisioningServiceClient dpsClient,
            SecurityProvider security,
            string groupId)
        {
            if (enrollmentType == EnrollmentType.Individual)
            {
                await dpsClient.DeleteIndividualEnrollmentAsync(security.GetRegistrationID()).ConfigureAwait(false);
            }
            else
            {
                await dpsClient.DeleteEnrollmentGroupAsync(groupId).ConfigureAwait(false);
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
            using (var hmac = new HMACSHA256(masterKey))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationId)));
            }
        }

        public static bool ImplementsWebProxy(Client.TransportType transportProtocol)
        {
            switch (transportProtocol)
            {
                case Client.TransportType.Mqtt_WebSocket_Only:
                case Client.TransportType.Amqp_WebSocket_Only:
                    return true;

                case Client.TransportType.Amqp:
                case Client.TransportType.Amqp_Tcp_Only:
                case Client.TransportType.Mqtt:
                case Client.TransportType.Mqtt_Tcp_Only:
                case Client.TransportType.Http1:
                default:
                    return false;
            }

            throw new NotSupportedException($"Unknown transport: '{transportProtocol}'.");
        }
    }
}
