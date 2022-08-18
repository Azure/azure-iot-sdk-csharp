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
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
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
        private static readonly string s_proxyServerAddress = TestConfiguration.IoTHub.ProxyServerAddress;
        private static readonly string s_certificatePassword = TestConfiguration.Provisioning.CertificatePassword;

        private static readonly HashSet<Type> s_retryableExceptions = new HashSet<Type> { typeof(ProvisioningServiceClientHttpException) };
        private static readonly IRetryPolicy s_provisioningServiceRetryPolicy = new ProvisioningServiceRetryPolicy();

        private readonly string _idPrefix = $"e2e-{nameof(ProvisioningE2ETests).ToLower()}-";
        private readonly VerboseTestLogger _verboseLog = VerboseTestLogger.GetInstance();

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

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task DPS_Registration_Amqp_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.Tpm,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task DPS_Registration_AmqpWs_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.Tpm,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWs_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWs_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWs_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWs_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
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
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_X509_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_X509_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_SymmetricKey_IndividualEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_SymmetricKey_GroupEnrollment_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_SymmetricKey_IndividualEnrollment_EdgeEnabled_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false,
                    new Devices.Provisioning.Service.DeviceCapabilities() { IotEdge = true })
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_SymmetricKey_GroupEnrollment_EdgeEnabled_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false,
                    new Devices.Provisioning.Service.DeviceCapabilities() { IotEdge = true })
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_SymmetricKey_IndividualEnrollment_EdgeDisabled_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false,
                    new Devices.Provisioning.Service.DeviceCapabilities() { IotEdge = false })
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_SymmetricKey_GroupEnrollment_EdgeDisabled_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false,
                    new Devices.Provisioning.Service.DeviceCapabilities() { IotEdge = false })
                .ConfigureAwait(false);
        }

        #endregion DeviceCapabilities

        #region CustomAllocationDefinition tests

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_SymmetricKey_IndividualEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_AmqpWs_SymmetricKey_IndividualEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_SymmetricKey_GroupEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_AmqpWs_SymmetricKey_GroupEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_SymmetricKey_IndividualEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_MqttWs_SymmetricKey_IndividualEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Individual,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_SymmetricKey_GroupEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_SymmetricKey_GroupEnrollment_CustomAllocationPolicy_RegisterOk()
        {
            await ProvisioningDeviceClientCustomAllocationPolicyAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.SymmetricKey,
                    EnrollmentType.Group,
                    false)
                .ConfigureAwait(false);
        }

        #endregion CustomAllocationDefinition tests

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task DPS_Registration_Amqp_Tpm_InvalidRegistrationId_RegisterFail()
        {
            try
            {
                await ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(new IotHubClientAmqpSettings()).ConfigureAwait(false);
                Assert.Fail("Expected exception not thrown");
            }
            catch (ProvisioningTransportException ex)
            {
                // Exception message must contain the errorCode value as below
                Assert.IsTrue(ex.Message.Contains("404201"));
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Mqtt_X509_GrouplEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_X509_GrouplEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time
        public async Task DPS_Registration_Amqp_Tpm_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.Tpm,
                    null,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DoNotParallelize] //TPM tests need to execute in serial as tpm only accepts one connection at a time as tpm only accepts one connection at a time
        public async Task DPS_Registration_AmqpWs_Tpm_InvalidIdScope_Register_Fail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.Tpm,
                    null,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_AmqpWs_X509_IndividualEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_Amqp_X509_GroupEnrollment_InvalidIdScope_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidIdScopeRegisterFailAsync(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Group,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_Mqtt_X509_IndividualEnrollment_InvalidGlobalAddress_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(
                    new IotHubClientMqttSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DPS_Registration_MqttWs_X509_IndividualEnrollment_InvalidGlobalAddress_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual)
                .ConfigureAwait(false);
        }

        // Note: This test takes 3 minutes.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task DPS_Registration_Amqp_X509_IndividualEnrollment_InvalidGlobalAddress_RegisterFail()
        {
            await ProvisioningDeviceClientInvalidGlobalAddressRegisterFailAsync(
                    new IotHubClientAmqpSettings(),
                    AttestationMechanismType.X509,
                    EnrollmentType.Individual)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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
            Devices.Provisioning.Service.DeviceCapabilities capabilities,
            string proxyServerAddress = null)
        {
            //Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            var iothubs = new List<string>() { IotHubConnectionStringBuilder.Create(TestConfiguration.IoTHub.ConnectionString).HostName };
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
            Devices.Provisioning.Service.DeviceCapabilities deviceCapabilities,
            string proxyServerAddress = null)
        {
            string groupId = null;
            if (enrollmentType == EnrollmentType.Group)
            {
                groupId = attestationType == AttestationMechanismType.X509
                    ? TestConfiguration.Provisioning.X509GroupEnrollmentName
                    : _idPrefix + AttestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            }

            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportSettings);
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
            _verboseLog.WriteLine("Creating device");

            if (ImplementsWebProxy(transportSettings) && setCustomProxy)
            {
                transport.Proxy = proxyServerAddress == null
                    ? null
                    : new WebProxy(s_proxyServerAddress);
            }

            var provClient = ProvisioningDeviceClient.Create(
                s_globalDeviceEndpoint,
                TestConfiguration.Provisioning.IdScope,
                auth,
                new ProvisioningClientOptions(transport));

            using var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

            DeviceRegistrationResult result = null;
            Client.IAuthenticationMethod authMethod = null;

            Logger.Trace($"ProvisioningDeviceClient RegisterAsync for group {groupId} . . . ");

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
                authMethod = CreateAuthenticationMethodFromAuthProvider(auth, result.DeviceId);
#pragma warning restore CA2000 // Dispose objects before losing scope

                await ConfirmRegisteredDeviceWorksAsync(result, authMethod, transportSettings, false).ConfigureAwait(false);
                await ConfirmExpectedDeviceCapabilitiesAsync(result, authMethod, deviceCapabilities).ConfigureAwait(false);
            }
            finally
            {
                if (attestationType == AttestationMechanismType.X509
                    && enrollmentType == EnrollmentType.Group)
                {
                    Logger.Trace($"The test enrollment type {attestationType}-{enrollmentType} with group Id {groupId} is currently hardcoded - do not delete.");
                }
                else
                {
                    Logger.Trace($"Deleting test enrollment type {attestationType}-{enrollmentType} with registration Id {auth.GetRegistrationId()}.");
                    await DeleteCreatedEnrollmentAsync(enrollmentType, auth, groupId, Logger).ConfigureAwait(false);
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

        /// <summary>
        /// This test flow uses a custom allocation policy to decide which of the two hubs a device should be provisioned to.
        /// The custom allocation policy has a webhook to an Azure function, and that function will always dictate to provision
        /// the device to the hub with the longest host name. This test verifies that an enrollment with a custom allocation policy
        /// pointing to that Azure function will always enroll to the hub with the longest name
        /// </summary>
        private async Task ProvisioningDeviceClientCustomAllocationPolicyAsync(
            IotHubClientTransportSettings transportSettings,
            AttestationMechanismType attestationType,
            EnrollmentType enrollmentType,
            bool setCustomProxy,
            string customServerProxy = null)
        {
            string closeHostName = IotHubConnectionStringBuilder.Create(TestConfiguration.IoTHub.ConnectionString).HostName;

            var iotHubsToProvisionTo = new List<string>() { closeHostName, TestConfiguration.Provisioning.FarAwayIotHubHostName };
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
                    transportSettings,
                    attestationType,
                    enrollmentType,
                    setCustomProxy,
                    iotHubsToProvisionTo,
                    expectedDestinationHub,
                    customServerProxy)
                .ConfigureAwait(false);
        }

        private async Task ProvisioningDeviceClientProvisioningFlowCustomAllocationAllocateToHubWithLongestHostNameAsync(
            IotHubClientTransportSettings transportSettings,
            AttestationMechanismType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            IList<string> iotHubsToProvisionTo,
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

            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportSettings);
            AuthenticationProvider auth = await CreateAuthProviderFromNameAsync(
                    attestationType,
                    enrollmentType,
                    groupId,
                    null,
                    AllocationPolicy.Custom,
                    customAllocationDefinition,
                    iotHubsToProvisionTo)
                .ConfigureAwait(false);

            // Check basic provisioning
            if (ImplementsWebProxy(transportSettings) && setCustomProxy)
            {
                transport.Proxy = (proxyServerAddress != null) ? new WebProxy(s_proxyServerAddress) : null;
            }

            var provClient = ProvisioningDeviceClient.Create(
                s_globalDeviceEndpoint,
                TestConfiguration.Provisioning.IdScope,
                auth,
                new ProvisioningClientOptions(transport));
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
                if (attestationType == AttestationMechanismType.X509
                    && enrollmentType == EnrollmentType.Group)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cleanup of enrollment failed due to {ex}");
            }
            finally
            {
                if (auth is IDisposable disposableAuth)
                {
                    disposableAuth?.Dispose();
                }
            }
        }

        public async Task ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(IotHubClientTransportSettings transportSettings)
        {
            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportSettings);
            using var auth = new AuthenticationProviderTpmSimulator("invalidregistrationid");

            try
            {
                var provClient = ProvisioningDeviceClient.Create(
                    s_globalDeviceEndpoint,
                    TestConfiguration.Provisioning.IdScope,
                    auth,
                    new ProvisioningClientOptions(transport));

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
            finally
            {
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
            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportSettings);
            AuthenticationProvider auth = await CreateAuthProviderFromNameAsync(
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
                auth,
                new ProvisioningClientOptions(transport));

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
            using ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportSettings);
            AuthenticationProvider auth = await CreateAuthProviderFromNameAsync(
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
                auth,
                new ProvisioningClientOptions(transport));

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

                if (auth is IDisposable disposableAuth)
                {
                    disposableAuth?.Dispose();
                }
            }
        }

        public static ProvisioningTransportHandler CreateTransportHandlerFromName(IotHubClientTransportSettings transportSettings)
        {
            if (transportSettings is IotHubClientAmqpSettings)
            {
                return transportSettings.Protocol == IotHubClientTransportProtocol.Tcp
                    ? new ProvisioningTransportHandlerAmqp(ProvisioningClientTransportProtocol.Tcp)
                    : new ProvisioningTransportHandlerAmqp(ProvisioningClientTransportProtocol.WebSocket);
            }

            if (transportSettings is IotHubClientMqttSettings)
            {
                return transportSettings.Protocol == IotHubClientTransportProtocol.Tcp
                    ? new ProvisioningTransportHandlerMqtt(ProvisioningClientTransportProtocol.Tcp)
                    : new ProvisioningTransportHandlerMqtt(ProvisioningClientTransportProtocol.WebSocket);
            }

            if (transportSettings is IotHubClientHttpSettings)
            {
                return new ProvisioningTransportHandlerHttp();
            }

            throw new NotSupportedException($"Unknown transport: '{transportSettings}'.");
        }

        /// <summary>
        /// Attempt to create device client instance from provided arguments, ensure that it can open a
        /// connection, ensure that it can send telemetry, and (optionally) send a reported property update
        /// </summary
        private async Task ConfirmRegisteredDeviceWorksAsync(
            DeviceRegistrationResult result,
            Client.IAuthenticationMethod auth,
            IotHubClientTransportSettings transportSettings,
            bool sendReportedPropertiesUpdate)
        {
            using var iotClient = IotHubDeviceClient.Create(result.AssignedHub, auth, new IotHubClientOptions(transportSettings));
            Logger.Trace("DeviceClient OpenAsync.");
            await iotClient.OpenAsync().ConfigureAwait(false);
            Logger.Trace("DeviceClient SendEventAsync.");

            var testMessage = new Client.Message(Encoding.UTF8.GetBytes("TestMessage"));
            await iotClient.SendEventAsync(testMessage).ConfigureAwait(false);

            if (sendReportedPropertiesUpdate)
            {
                Logger.Trace("DeviceClient updating desired properties.");
                Client.Twin twin = await iotClient.GetTwinAsync().ConfigureAwait(false);
                await iotClient.UpdateReportedPropertiesAsync(new Client.TwinCollection($"{{\"{new Guid()}\":\"{new Guid()}\"}}")).ConfigureAwait(false);
            }

            Logger.Trace("DeviceClient CloseAsync.");
            await iotClient.CloseAsync().ConfigureAwait(false);
        }

        private static async Task ConfirmExpectedDeviceCapabilitiesAsync(
            DeviceRegistrationResult result,
            Client.IAuthenticationMethod auth,
            Devices.Provisioning.Service.DeviceCapabilities capabilities)
        {
            if (capabilities != null && capabilities.IotEdge)
            {
                //If device is edge device, it should be able to connect to iot hub as its edgehub module identity
                var csBuilder = new Client.IotHubConnectionStringBuilder(auth, result.AssignedHub);
                string edgehubConnectionString = csBuilder.ToString() + ";ModuleId=$edgeHub";
                using var moduleClient = IotHubModuleClient.CreateFromConnectionString(edgehubConnectionString);
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
            Devices.Provisioning.Service.DeviceCapabilities capabilities = null)
        {
            _verboseLog.WriteLine($"{nameof(CreateAuthProviderFromNameAsync)}({attestationType})");

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
                                capabilities,
                                Logger).ConfigureAwait(false);

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

        private Client.IAuthenticationMethod CreateAuthenticationMethodFromAuthProvider(
            AuthenticationProvider provisioningAuth,
            string deviceId)
        {
            _verboseLog.WriteLine($"{nameof(CreateAuthenticationMethodFromAuthProvider)}({deviceId})");

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
        private void ValidateDeviceRegistrationResult(bool validatePayload, DeviceRegistrationResult result)
        {
            Assert.IsNotNull(result);
            Logger.Trace($"{result.Status} (Error Code: {result.ErrorCode}; Error Message: {result.ErrorMessage})");
            Logger.Trace($"ProvisioningDeviceClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

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
            string groupId,
            MsTestLogger logger)
        {
            using ProvisioningServiceClient dpsClient = CreateProvisioningService();

            try
            {
                if (enrollmentType == EnrollmentType.Individual)
                {
                    await RetryOperationHelper
                        .RetryOperationsAsync(
                            async () =>
                            {
                                await dpsClient.DeleteIndividualEnrollmentAsync(authProvider.GetRegistrationId()).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy,
                            s_retryableExceptions,
                            logger)
                        .ConfigureAwait(false);
                }
                else if (enrollmentType == EnrollmentType.Group)
                {
                    await RetryOperationHelper
                        .RetryOperationsAsync(
                            async () =>
                            {
                                await dpsClient.DeleteEnrollmentGroupAsync(groupId).ConfigureAwait(false);
                            },
                            s_provisioningServiceRetryPolicy,
                            s_retryableExceptions,
                            logger)
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
