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
using Microsoft.Azure.Devices.E2ETests.Helpers;
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
        private const int MaxTryCount = 10;
        private const string InvalidIdScope = "0neFFFFFFFF";
        private const string PayloadJsonData = "{\"testKey\":\"testValue\"}";
        private const string InvalidGlobalAddress = "httpbin.org";
        private static readonly string s_globalDeviceEndpoint = TestConfiguration.Provisioning.GlobalDeviceEndpoint;
        private static readonly string s_proxyServerAddress = TestConfiguration.IoTHub.ProxyServerAddress;
        private static readonly X509Certificate2 s_groupEnrollmentCertificate = TestConfiguration.Provisioning.GetGroupEnrollmentCertificate();

        private readonly string _idPrefix = $"e2e-{nameof(ProvisioningE2ETests).ToLower()}-";
        private readonly VerboseTestLogger _verboseLog = VerboseTestLogger.GetInstance();

        private static DirectoryInfo s_dpsClientCertificateFolder;
        private static DirectoryInfo s_selfSignedCertificatesFolder;

        public enum EnrollmentType
        {
            Individual,
            Group,
        }

        [ClassInitialize]
        public static void TestClassSetup(TestContext _)
        {
            // Create a folder to hold the DPS client certificates and X509 self-signed certificates. If a folder by the same name already exists, it will be used.
            s_dpsClientCertificateFolder = Directory.CreateDirectory("DpsClientCertificates");
            s_selfSignedCertificatesFolder = s_dpsClientCertificateFolder.CreateSubdirectory("SelfSignedCertificates");
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task DPS_Registration_Http_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationMechanismType.Tpm, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Http_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationMechanismType.X509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Http_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationMechanismType.X509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Http_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_Http_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task DPS_Registration_HttpWithProxy_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationMechanismType.Tpm, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_HttpWithNullProxy_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationMechanismType.Tpm, EnrollmentType.Individual, true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task DPS_Registration_HttpWithProxy_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task DPS_Registration_HttpWithProxy_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task DPS_Registration_Amqp_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.Tpm, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task DPS_Registration_AmqpWs_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.Tpm, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Amqp_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.X509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_AmqpWs_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Amqp_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.X509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_AmqpWs_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Amqp_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_AmqpWs_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Amqp_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_AmqpWs_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task DPS_Registration_AmqpWsWithProxy_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task DPS_Registration_AmqpWsWithNullProxy_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Individual, true).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_AmqpWsWithProxy_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("Proxy")]
        [LoggedTestMethod]
        public async Task DPS_Registration_AmqpWsWithProxy_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Mqtt_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.X509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_MqttWs_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Mqtt_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.X509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_MqttWs_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Mqtt_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_MqttWs_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Mqtt_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_MqttWs_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task DPS_Registration_MqttWsWithProxy_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        public async Task DPS_Registration_MqttWsWithNullProxy_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Individual, true).ConfigureAwait(false);
        }

        [TestCategory("Proxy")]
        [LoggedTestMethod]
        public async Task DPS_Registration_MqttWsWithProxy_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("Proxy")]
        [LoggedTestMethod]
        public async Task DPS_Registration_MqttWsWithProxy_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, true, s_proxyServerAddress).ConfigureAwait(false);
        }

        #region DeviceCapabilities

        [LoggedTestMethod]
        public async Task DPS_Registration_Amqp_SymmetricKey_IndividualEnrollment_EdgeEnabled_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false, new DeviceCapabilities() { IotEdge = true }).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Amqp_SymmetricKey_GroupEnrollment_EdgeEnabled_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false, new DeviceCapabilities() { IotEdge = true }).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Mqtt_SymmetricKey_IndividualEnrollment_EdgeDisabled_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false, new DeviceCapabilities() { IotEdge = false }).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Mqtt_SymmetricKey_GroupEnrollment_EdgeDisabled_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false, new DeviceCapabilities() { IotEdge = false }).ConfigureAwait(false);
        }

        #endregion DeviceCapabilities

        #region CustomAllocationDefinition tests

        [LoggedTestMethod]
        public async Task DPS_Registration_Http_SymmetricKey_IndividualEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Http_SymmetricKey_GroupEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Amqp_SymmetricKey_IndividualEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_AmqpWs_SymmetricKey_IndividualEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Amqp_SymmetricKey_GroupEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_AmqpWs_SymmetricKey_GroupEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Mqtt_SymmetricKey_IndividualEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_MqttWs_SymmetricKey_IndividualEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Mqtt_SymmetricKey_GroupEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_MqttWs_SymmetricKey_GroupEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        #endregion CustomAllocationDefinition tests

        [LoggedTestMethod]
        public async Task DPS_Registration_Mqtt_SymmetricKey_IndividualEnrollment_TimeSpanTimeoutRespected()
        {
            try
            {
                await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, TimeSpan.Zero).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return; // expected exception was thrown, so exit the test
            }

            throw new AssertFailedException("Expected an OperationCanceledException to be thrown since the timeout was set to TimeSpan.Zero");
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Http_SymmetricKey_IndividualEnrollment_TimeSpanTimeoutRespected()
        {
            try
            {
                await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, TimeSpan.Zero).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return; // expected exception was thrown, so exit the test
            }

            throw new AssertFailedException("Expected an OperationCanceledException to be thrown since the timeout was set to TimeSpan.Zero");
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Amqp_SymmetricKey_IndividualEnrollment_TimeSpanTimeoutRespected()
        {
            try
            {
                await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.SymmetricKey, EnrollmentType.Individual, TimeSpan.Zero).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return; // expected exception was thrown, so exit the test
            }

            throw new AssertFailedException("Expected an OperationCanceledException to be thrown since the timeout was set to TimeSpan.Zero");
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task DPS_Registration_Http_Tpm_InvalidRegistrationId_RegisterFail()
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
        public async Task DPS_Registration_Amqp_Tpm_InvalidRegistrationId_RegisterFail()
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

        [LoggedTestMethod]
        public async Task DPS_Registration_Mqtt_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.X509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_MqttWs_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Mqtt_X509_GrouplEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.X509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_MqttWs_X509_GrouplEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task DPS_Registration_Http_Tpm_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Http1, AttestationMechanismType.Tpm, null, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Http_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Http1, AttestationMechanismType.X509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Http_X509_GroupEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Http1, AttestationMechanismType.X509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task DPS_Registration_Amqp_Tpm_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.Tpm, null, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time as tpm only accepts one connection at a time
        public async Task DPS_Registration_AmqpWs_Tpm_InvalidIdScope_Register_Fail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.Tpm, null, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Amqp_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.X509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_AmqpWs_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Amqp_X509_GroupEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.X509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_AmqpWs_X509_GroupEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        #region InvalidGlobalAddress

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_Mqtt_X509_IndividualEnrollment_InvalidGlobalAddress_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(Client.TransportType.Mqtt_Tcp_Only, AttestationMechanismType.X509, EnrollmentType.Individual).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_MqttWs_X509_IndividualEnrollment_InvalidGlobalAddress_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(Client.TransportType.Mqtt_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Individual).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_Http_X509_IndividualEnrollment_InvalidGlobalAddress_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(Client.TransportType.Http1, AttestationMechanismType.X509, EnrollmentType.Individual, null).ConfigureAwait(false);
        }

        // Note: This test takes 3 minutes.
        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_Amqp_X509_IndividualEnrollment_InvalidGlobalAddress_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(Client.TransportType.Amqp_Tcp_Only, AttestationMechanismType.X509, EnrollmentType.Individual).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task DPS_Registration_AmqpWs_X509_IndividualEnrollment_InvalidGlobalAddress_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(Client.TransportType.Amqp_WebSocket_Only, AttestationMechanismType.X509, EnrollmentType.Individual).ConfigureAwait(false);
        }

        #endregion InvalidGlobalAddress

        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            Client.TransportType transportType,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            TimeSpan timeout)
        {
            //Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            await ProvisioningDeviceClientValidRegistrationIdRegisterOkAsync(transportType, attestationType, enrollmentType, false, null, AllocationPolicy.Hashed, null, null, null, timeout, s_proxyServerAddress).ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            Client.TransportType transportType,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            string proxyServerAddress = null)
        {
            //Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            await ProvisioningDeviceClientValidRegistrationIdRegisterOkAsync(
                    transportType,
                    attestationType,
                    enrollmentType,
                    setCustomProxy,
                    null,
                    AllocationPolicy.Hashed,
                    null,
                    null,
                    null,
                    TimeSpan.MaxValue,
                    proxyServerAddress)
                .ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            Client.TransportType transportType,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            DeviceCapabilities capabilities,
            string proxyServerAddress = null)
        {
            //Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            var iothubs = new List<string>() { IotHubConnectionStringBuilder.Create(TestConfiguration.IoTHub.ConnectionString).HostName };
            await ProvisioningDeviceClientValidRegistrationIdRegisterOkAsync(
                    transportType,
                    attestationType,
                    enrollmentType,
                    setCustomProxy,
                    null,
                    AllocationPolicy.Hashed,
                    null,
                    iothubs,
                    capabilities,
                    TimeSpan.MaxValue,
                    proxyServerAddress)
                .ConfigureAwait(false);
        }

        private async Task ProvisioningDeviceClientValidRegistrationIdRegisterOkAsync(
            Client.TransportType transportType,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            ICollection<string> iothubs,
            DeviceCapabilities deviceCapabilities,
            TimeSpan timeout,
            string proxyServerAddress = null)
        {
            string groupId = _idPrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType);
            using SecurityProvider security = await CreateSecurityProviderFromNameAsync(
                    attestationType,
                    enrollmentType,
                    groupId,
                    reprovisionPolicy,
                    allocationPolicy,
                    customAllocationDefinition,
                    iothubs,
                    deviceCapabilities)
                .ConfigureAwait(false);
            _verboseLog.WriteLine("Creating device");

            if (ImplementsWebProxy(transportType) && setCustomProxy)
            {
                transport.Proxy = proxyServerAddress == null
                    ? null
                    : new WebProxy(s_proxyServerAddress);
            }

            var provClient = ProvisioningDeviceClient.Create(
                s_globalDeviceEndpoint,
                TestConfiguration.Provisioning.IdScope,
                security,
                transport);

            using var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

            DeviceRegistrationResult result = null;
            Client.IAuthenticationMethod auth = null;

            Logger.Trace($"ProvisioningDeviceClient RegisterAsync for group {groupId} . . . ");

            try
            {
                // Trying to register simultaneously can cause conflicts (409). Retry in those scenarios to succeed.
                int tryCount = 0;
                while (true)
                {
                    try
                    {
                        result = timeout != TimeSpan.MaxValue
                            ? await provClient.RegisterAsync(timeout).ConfigureAwait(false)
                            : await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
                        break;
                    }
                    // Catching all ProvisioningTransportException as the status code is not the same for Mqtt, Amqp and Http.
                    // It should be safe to retry on any non-transient exception just for E2E tests as we have concurrency issues.
                    catch (ProvisioningTransportException ex) when (++tryCount < MaxTryCount)
                    {
                        Logger.Trace($"ProvisioningDeviceClient RegisterAsync failed because: {ex.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                    }
                }

                ValidateDeviceRegistrationResult(false, result);

#pragma warning disable CA2000 // Dispose objects before losing scope
                // The certificate instance referenced in the DeviceAuthenticationWithX509Certificate instance is common for all tests in this class. It is disposed during class cleanup.
                auth = CreateAuthenticationMethodFromSecurityProvider(security, result.DeviceId);
#pragma warning restore CA2000 // Dispose objects before losing scope

                await ConfirmRegisteredDeviceWorksAsync(result, auth, transportType, false).ConfigureAwait(false);
                await ConfirmExpectedDeviceCapabilitiesAsync(result, auth, deviceCapabilities).ConfigureAwait(false);
            }
            finally
            {
                if (attestationType == AttestationMechanismType.X509 && enrollmentType == EnrollmentType.Group)
                {
                    Logger.Trace($"The test enrollment type {attestationType}-{enrollmentType} with registration Id {security.GetRegistrationID()} is currently hardcoded - do not delete.");
                }
                else
                {
                    Logger.Trace($"Deleting test enrollment type {attestationType}-{enrollmentType} with registration Id {security.GetRegistrationID()}.");
                    await DeleteCreatedEnrollmentAsync(enrollmentType, security, groupId).ConfigureAwait(false);
                }

                if (security is SecurityProviderX509 x509Security && enrollmentType == EnrollmentType.Individual)
                {
                    X509Certificate2 publicPrivateCertificate = x509Security.GetAuthenticationCertificate();
                    publicPrivateCertificate?.Dispose();
                }

                if (auth != null && auth is IDisposable disposableAuth)
                {
                    disposableAuth?.Dispose();
                }
            }
        }

        /// <summary>
        /// This test flow uses a custom allocation policy to decide which of the two hubs a device should be provisioned to.
        /// The custom allocation policy has a webhook to an Azure function, and that function will always dictate to provision
        /// the device to the hub with the longest host name. This test verifies that an enrollment with a custom allocation policy
        /// pointing to that Azure function will always enroll to the hub with the longest name
        /// </summary>
        private async Task ProvisioningDeviceClientCustomAllocationPolicyAsync(
            Client.TransportType transportProtocol,
            AttestationMechanismType attestationType,
            EnrollmentType enrollmentType,
            bool setCustomProxy,
            string customServerProxy = null)
        {
            string closeHostName = IotHubConnectionStringBuilder.Create(TestConfiguration.IoTHub.ConnectionString).HostName;

            ICollection<string> iotHubsToProvisionTo = new List<string>() { closeHostName, TestConfiguration.Provisioning.FarAwayIotHubHostName };
            string expectedDestinationHub = "";
            if (closeHostName.Length > TestConfiguration.Provisioning.FarAwayIotHubHostName.Length)
            {
                expectedDestinationHub = closeHostName;
            }
            else if (closeHostName.Length < TestConfiguration.Provisioning.FarAwayIotHubHostName.Length)
            {
                expectedDestinationHub = TestConfiguration.Provisioning.FarAwayIotHubHostName;
            }
            else
            {
                //custom endpoint for this test allocates the device to the hub with the longer hostname. If both hubs
                // have the same length, then the test is no longer determenistic, as the device could be allocated to either hub
                Assert.Fail("Configuration failure: far away hub hostname cannot be the same length as the close hub hostname");
            }

            await ProvisioningDeviceClientProvisioningFlowCustomAllocationAllocateToHubWithLongestHostNameAsync(
                    transportProtocol,
                    attestationType,
                    enrollmentType,
                    setCustomProxy,
                    iotHubsToProvisionTo,
                    expectedDestinationHub,
                    customServerProxy)
                .ConfigureAwait(false);
        }

        private async Task ProvisioningDeviceClientProvisioningFlowCustomAllocationAllocateToHubWithLongestHostNameAsync(
            Client.TransportType transportProtocol,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            ICollection<string> iotHubsToProvisionTo,
            string expectedDestinationHub,
            string proxyServerAddress = null)
        {
            using ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(s_proxyServerAddress);
            string groupId = _idPrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();

            var customAllocationDefinition = new CustomAllocationDefinition
            {
                WebhookUrl = TestConfiguration.Provisioning.CustomAllocationPolicyWebhook,
                ApiVersion = "2019-03-31",
            };

            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol);
            using SecurityProvider security = await CreateSecurityProviderFromNameAsync(
                    attestationType,
                    enrollmentType,
                    groupId,
                    null,
                    AllocationPolicy.Custom,
                    customAllocationDefinition,
                    iotHubsToProvisionTo)
                .ConfigureAwait(false);

            // Check basic provisioning
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

            // Test registering with valid additional data payload
            DeviceRegistrationResult result = await provClient
                .RegisterAsync(new ProvisioningRegistrationAdditionalData { JsonData = PayloadJsonData }, cts.Token)
                .ConfigureAwait(false);
            ValidateDeviceRegistrationResult(true, result);
            Assert.AreEqual(expectedDestinationHub, result.AssignedHub);

            // Test registering without additional data
            result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
            ValidateDeviceRegistrationResult(false, result);

            try
            {
                if (attestationType == AttestationMechanismType.X509 && enrollmentType == EnrollmentType.Group)
                {
                    Logger.Trace($"The test enrollment type {attestationType}-{enrollmentType} with registration Id {security.GetRegistrationID()} is currently hardcoded - do not delete.");
                }
                else
                {
                    Logger.Trace($"Deleting test enrollment type {attestationType}-{enrollmentType} with registration Id {security.GetRegistrationID()}.");
                    await DeleteCreatedEnrollmentAsync(enrollmentType, security, groupId).ConfigureAwait(false);
                }

                if (security is SecurityProviderX509 x509Security && enrollmentType == EnrollmentType.Individual)
                {
                    X509Certificate2 publicPrivateCertificate = x509Security.GetAuthenticationCertificate();
                    publicPrivateCertificate?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cleanup of enrollment failed due to {ex}");
            }
        }

        public async Task ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(Client.TransportType transportProtocol)
        {
            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol);
            using SecurityProvider security = new SecurityProviderTpmSimulator("invalidregistrationid");
            var provClient = ProvisioningDeviceClient.Create(
                s_globalDeviceEndpoint,
                TestConfiguration.Provisioning.IdScope,
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

        private async Task ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
            Client.TransportType transportProtocol,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            string groupId)
        {
            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol);
            using SecurityProvider security = await CreateSecurityProviderFromNameAsync(
                    attestationType,
                    enrollmentType,
                    groupId,
                    null,
                    AllocationPolicy.Hashed,
                    null,
                    null)
            .ConfigureAwait(false);

            var provClient = ProvisioningDeviceClient.Create(
                s_globalDeviceEndpoint,
                InvalidIdScope,
                security,
                transport);

            using var cts = new CancellationTokenSource(FailingTimeoutMiliseconds);

            try
            {
                ProvisioningTransportException exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                () => provClient.RegisterAsync(cts.Token)).ConfigureAwait(false);

                Logger.Trace($"Exception: {exception}");
            }
            finally
            {
                if (attestationType == AttestationMechanismType.X509 && enrollmentType == EnrollmentType.Group)
                {
                    Logger.Trace($"The test enrollment type {attestationType}-{enrollmentType} with registration Id {security.GetRegistrationID()} is currently hardcoded - do not delete.");
                }
                else
                {
                    Logger.Trace($"Deleting test enrollment type {attestationType}-{enrollmentType} with registration Id {security.GetRegistrationID()}.");
                    await DeleteCreatedEnrollmentAsync(enrollmentType, security, groupId).ConfigureAwait(false);
                }

                if (security is SecurityProviderX509 x509Security && enrollmentType == EnrollmentType.Individual)
                {
                    X509Certificate2 publicPrivateCertificate = x509Security.GetAuthenticationCertificate();
                    publicPrivateCertificate?.Dispose();
                }
            }
        }

        private async Task ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(
            Client.TransportType transportProtocol,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            string groupId = "")
        {
            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol);
            using SecurityProvider security = await CreateSecurityProviderFromNameAsync(
                        attestationType,
                        enrollmentType,
                        groupId,
                        null,
                        AllocationPolicy.Hashed,
                        null,
                        null)
            .ConfigureAwait(false);

            var provClient = ProvisioningDeviceClient.Create(
                InvalidGlobalAddress,
                TestConfiguration.Provisioning.IdScope,
                security,
                transport);

            using var cts = new CancellationTokenSource(FailingTimeoutMiliseconds);

            Logger.Trace("ProvisioningDeviceClient RegisterAsync . . . ");

            try
            {
                ProvisioningTransportException exception = await Assert.
                ThrowsExceptionAsync<ProvisioningTransportException>(() => provClient.RegisterAsync(cts.Token))
                .ConfigureAwait(false);

                Logger.Trace($"Exception: {exception}");
            }
            finally
            {
                if (attestationType == AttestationMechanismType.X509 && enrollmentType == EnrollmentType.Group)
                {
                    Logger.Trace($"The test enrollment type {attestationType}-{enrollmentType} with registration Id {security.GetRegistrationID()} is currently hardcoded - do not delete.");
                }
                else
                {
                    Logger.Trace($"Deleting test enrollment type {attestationType}-{enrollmentType} with registration Id {security.GetRegistrationID()}.");
                    await DeleteCreatedEnrollmentAsync(enrollmentType, security, groupId).ConfigureAwait(false);
                }

                if (security is SecurityProviderX509 x509Security && enrollmentType == EnrollmentType.Individual)
                {
                    X509Certificate2 publicPrivateCertificate = x509Security.GetAuthenticationCertificate();
                    publicPrivateCertificate?.Dispose();
                }
            }
        }

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
        /// </summary
        private async Task ConfirmRegisteredDeviceWorksAsync(
            DeviceRegistrationResult result,
            Client.IAuthenticationMethod auth,
            Client.TransportType transportProtocol,
            bool sendReportedPropertiesUpdate)
        {
            using var iotClient = DeviceClient.Create(result.AssignedHub, auth, transportProtocol);
            Logger.Trace("DeviceClient OpenAsync.");
            await iotClient.OpenAsync().ConfigureAwait(false);
            Logger.Trace("DeviceClient SendEventAsync.");

            using var testMessage = new Client.Message(Encoding.UTF8.GetBytes("TestMessage"));
            await iotClient.SendEventAsync(testMessage).ConfigureAwait(false);

            if (sendReportedPropertiesUpdate)
            {
                Logger.Trace("DeviceClient updating desired properties.");
                Twin twin = await iotClient.GetTwinAsync().ConfigureAwait(false);
                await iotClient.UpdateReportedPropertiesAsync(new TwinCollection($"{{\"{new Guid()}\":\"{new Guid()}\"}}")).ConfigureAwait(false);
            }

            Logger.Trace("DeviceClient CloseAsync.");
            await iotClient.CloseAsync().ConfigureAwait(false);
        }

        private static async Task ConfirmExpectedDeviceCapabilitiesAsync(DeviceRegistrationResult result, Client.IAuthenticationMethod auth, DeviceCapabilities capabilities)
        {
            if (capabilities != null && capabilities.IotEdge)
            {
                //If device is edge device, it should be able to connect to iot hub as its edgehub module identity
                var connectionStringBuilder = Client.IotHubConnectionStringBuilder.Create(result.AssignedHub, auth);
                string edgehubConnectionString = connectionStringBuilder.ToString() + ";ModuleId=$edgeHub";
                using var moduleClient = ModuleClient.CreateFromConnectionString(edgehubConnectionString);
                await moduleClient.OpenAsync().ConfigureAwait(false);
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
            DeviceCapabilities capabilities = null)
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
                            X509Certificate2 publicPrivateCertificate = CreateX509CertificateWithPublicPrivateKey(registrationId);
#pragma warning restore CA2000 // Dispose objects before losing scope

                            using (X509Certificate2 publicCertificate = CreateX509CertificateWithPublicKey(registrationId))
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
                if (PayloadJsonData != null)
                {
                    Assert.IsNotNull(result.JsonPayload);
                    Assert.AreEqual(PayloadJsonData, result.JsonPayload);
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

        public static async Task DeleteCreatedEnrollmentAsync(
            EnrollmentType? enrollmentType,
            SecurityProvider security,
            string groupId = "")
        {
            using ProvisioningServiceClient dpsClient = CreateProvisioningService();

            if (enrollmentType == EnrollmentType.Individual)
            {
                await dpsClient.DeleteIndividualEnrollmentAsync(security.GetRegistrationID()).ConfigureAwait(false);
            }
            else if (enrollmentType == EnrollmentType.Group)
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
            using var hmac = new HMACSHA256(masterKey);
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationId)));
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

        private X509Certificate2 CreateX509CertificateWithPublicKey(string registrationId)
        {
            return new X509Certificate2($"{s_selfSignedCertificatesFolder}\\{registrationId}.crt");
        }

        private X509Certificate2 CreateX509CertificateWithPublicPrivateKey(string registrationId)
        {
            return new X509Certificate2($"{s_selfSignedCertificatesFolder}\\{registrationId}.pfx", TestConfiguration.Provisioning.CertificatePassword);
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