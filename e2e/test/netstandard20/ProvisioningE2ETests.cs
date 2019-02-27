// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.Devices.E2ETests.ProvisioningServiceClientE2ETests;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("Provisioning-E2E")]
    public class ProvisioningE2ETests : IDisposable
    {
        private const int PassingTimeoutMiliseconds = 10 * 60 * 1000;
        private const int FailingTimeoutMiliseconds = 10 * 1000;
        private static string s_globalDeviceEndpoint = Configuration.Provisioning.GlobalDeviceEndpoint;
        private const string InvalidIDScope = "0neFFFFFFFF";
        private const string InvalidGlobalAddress = "httpbin.org";
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;

        private readonly VerboseTestLogging _verboseLog = VerboseTestLogging.GetInstance();
        private readonly TestLogging _log = TestLogging.GetInstance();
        private readonly ConsoleEventListener _listener;

        public ProvisioningE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        public enum EnrollmentType
        {
            Individual,
            Group
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.Tpm, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.x509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.x509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_Tcp_Only, AttestationType.Tpm, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_Tcp_Only, AttestationType.x509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_Tcp_Only, AttestationType.x509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.Tpm, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_Tcp_Only, AttestationType.x509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_Tcp_Only, AttestationType.x509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_X509Group_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.x509, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_HttpWithProxy_Tpm_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.Tpm, EnrollmentType.Individual, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_HttpWithNullProxy_Tpm_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.Tpm, EnrollmentType.Individual, true).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWsWithProxy_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWsWithNullProxy_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, true).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWsWithProxy_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWsWithNullProxy_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_SymmetricKey_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestCategory("ProxyE2ETests")]
        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_HttpWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Group, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("ProxyE2ETests")]
        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Group, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("ProxyE2ETests")]
        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Group, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("ProxyE2ETests")]
        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWsWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("ProxyE2ETests")]
        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWsWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_MqttWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_Mqtt_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_AmqpWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_Amqp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Amqp_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_MqttWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_Mqtt_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_AmqpWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_Amqp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Amqp_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningWorks_Http_SymmetricKey_RegisterOk_Individual()
        {
            //twin is irrelevant since HTTP Device Clients can't use twin
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_Http_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_Mqtt_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Mqtt_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_Amqp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Amqp_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_AmqpWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_MqttWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_Http_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_Mqtt_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Mqtt_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_Amqp_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Amqp_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_AmqpWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_MqttWs_SymmetricKey_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Individual, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_MqttWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_Mqtt_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_AmqpWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceResetsTwin_Amqp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Amqp_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_MqttWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_Mqtt_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType.Mqtt_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_AmqpWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisionedDeviceKeepsTwin_Amqp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Amqp_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningWorks_Http_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_Http_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_Mqtt_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Mqtt_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_Amqp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Amqp_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_AmqpWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ReprovisioningBlockingWorks_MqttWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_Http_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_Mqtt_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Mqtt_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_Amqp_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Amqp_Tcp_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_CustomAllocationPolicy_AmqpWs_SymmetricKey_RegisterOk_Group()
        {
            await ProvisioningDeviceClient_CustomAllocationPolicy(Client.TransportType.Amqp_WebSocket_Only, AttestationType.SymmetricKey, EnrollmentType.Group, false).ConfigureAwait(false);
        }

        [TestMethod]
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

        /// <summary>
        /// This test flow reprovisions a device after that device created some twin updates on its original hub.
        /// The expected behaviour is that, with ReprovisionPolicy set to not migrate data, the twin updates from the original hub are not present at the new hub
        /// </summary>
        private async Task ProvisioningDeviceClient_ReprovisioningFlow_ResetTwin(Client.TransportType transportProtocol, AttestationType attestationType, EnrollmentType enrollmentType, bool setCustomProxy, string customServerProxy = null)
        {
            var connectionString = IotHubConnectionStringBuilder.Create(Configuration.IoTHub.ConnectionString);
            ICollection<string> iotHubsToStartAt = new List<string>() { Configuration.Provisioning.FarAwayIotHubHostName };
            ICollection<string> iotHubsToReprovisionTo = new List<string>() { connectionString.HostName };
            await ProvisioningDeviceClient_ReprovisioningFlow(transportProtocol, attestationType, enrollmentType, setCustomProxy, new ReprovisionPolicy { MigrateDeviceData = false, UpdateHubAssignment = true }, AllocationPolicy.Hashed, null, iotHubsToStartAt, iotHubsToReprovisionTo, customServerProxy).ConfigureAwait(false);
        }

        /// <summary>
        /// This test flow reprovisions a device after that device created some twin updates on its original hub.
        /// The expected behaviour is that, with ReprovisionPolicy set to migrate data, the twin updates from the original hub are present at the new hub
        /// </summary>
        private async Task ProvisioningDeviceClient_ReprovisioningFlow_KeepTwin(Client.TransportType transportProtocol, AttestationType attestationType, EnrollmentType enrollmentType, bool setCustomProxy, string customServerProxy = null)
        {
            var connectionString = IotHubConnectionStringBuilder.Create(Configuration.IoTHub.ConnectionString);
            ICollection<string> iotHubsToStartAt = new List<string>() { Configuration.Provisioning.FarAwayIotHubHostName };
            ICollection<string> iotHubsToReprovisionTo = new List<string>() { connectionString.HostName };
            await ProvisioningDeviceClient_ReprovisioningFlow(transportProtocol, attestationType, enrollmentType, setCustomProxy, new ReprovisionPolicy { MigrateDeviceData = true, UpdateHubAssignment = true }, AllocationPolicy.Hashed, null, iotHubsToStartAt, iotHubsToReprovisionTo, customServerProxy).ConfigureAwait(false);
        }

        /// <summary>
        /// The expected behaviour is that, with ReprovisionPolicy set to never update hub, the a device is not reprovisioned, even when other settings would suggest it should
        /// </summary>
        private async Task ProvisioningDeviceClient_ReprovisioningFlow_DoNotReprovision(Client.TransportType transportProtocol, AttestationType attestationType, EnrollmentType enrollmentType, bool setCustomProxy, string customServerProxy = null)
        {
            var connectionString = IotHubConnectionStringBuilder.Create(Configuration.IoTHub.ConnectionString);
            ICollection<string> iotHubsToStartAt = new List<string>() { Configuration.Provisioning.FarAwayIotHubHostName };
            ICollection<string> iotHubsToReprovisionTo = new List<string>() { connectionString.HostName };
            await ProvisioningDeviceClient_ReprovisioningFlow(transportProtocol, attestationType, enrollmentType, setCustomProxy, new ReprovisionPolicy { MigrateDeviceData = false, UpdateHubAssignment = false }, AllocationPolicy.Hashed, null, iotHubsToStartAt, iotHubsToReprovisionTo, customServerProxy).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_HttpWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Http1, AttestationType.SymmetricKey, EnrollmentType.Individual, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Individual, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWsWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Amqp, AttestationType.SymmetricKey, EnrollmentType.Individual, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Individual, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWsWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(Client.TransportType.Mqtt, AttestationType.SymmetricKey, EnrollmentType.Individual, true, ProxyServerAddress).ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            Client.TransportType transportType,
            AttestationType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            string proxyServerAddress = null)
        {
            //Default reprovisioning settings: Hashed allocation, no reprovision policy, hub names, or custom allocation policy
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(transportType, attestationType, enrollmentType, setCustomProxy, null, AllocationPolicy.Hashed, null, null, ProxyServerAddress).ConfigureAwait(false);
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
            string proxyServerAddress = null)
        {
            string groupId = "some-valid-group-id-" + attestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType))
            using (SecurityProvider security = await CreateSecurityProviderFromName(attestationType, enrollmentType, groupId, reprovisionPolicy, allocationPolicy, customAllocationDefinition, iothubs).ConfigureAwait(false))
            {

                _verboseLog.WriteLine("Creating device");

                if (ImplementsWebProxy(transportType) && setCustomProxy)
                {
                    transport.Proxy = (proxyServerAddress != null) ? new WebProxy(ProxyServerAddress) : null;
                }

                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    s_globalDeviceEndpoint,
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);

                var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

                _log.WriteLine("ProvisioningDeviceClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);

                ValidateDeviceRegistrationResult(result);

                Client.IAuthenticationMethod auth = CreateAuthenticationMethodFromSecurityProvider(security, result.DeviceId);

                await ConfirmRegisteredDeviceWorks(result, auth, transportType, false).ConfigureAwait(false);

                if (attestationType != AttestationType.x509) //x509 enrollments are hardcoded, should never be deleted
                {
                    await DeleteCreatedEnrollment(enrollmentType, CreateProvisioningService(proxyServerAddress), security, groupId).ConfigureAwait(false);
                }
            }
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
            AttestationType attestationType,
            EnrollmentType? enrollmentType,
            bool setCustomProxy,
            ReprovisionPolicy reprovisionPolicy,
            AllocationPolicy allocationPolicy,
            CustomAllocationDefinition customAllocationDefinition,
            ICollection<string> iotHubsToStartAt,
            ICollection<string> iotHubsToReprovisionTo,
            string proxyServerAddress = null)
        {
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(ProxyServerAddress);
            string groupId = "some-valid-group-id-" + attestationTypeToString(attestationType) + "-" + Guid.NewGuid();

            bool twinOperationsAllowed = transportProtocol != Client.TransportType.Http1;

            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol))
            using (SecurityProvider security = await CreateSecurityProviderFromName(attestationType, enrollmentType, groupId, reprovisionPolicy, allocationPolicy, customAllocationDefinition, iotHubsToStartAt).ConfigureAwait(false))
            {
                //Check basic provisioning
                if (ImplementsWebProxy(transportProtocol) && setCustomProxy)
                {
                    transport.Proxy = (proxyServerAddress != null) ? new WebProxy(ProxyServerAddress) : null;
                }

                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    s_globalDeviceEndpoint,
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);
                var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);
                DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
                ValidateDeviceRegistrationResult(result);
                Client.IAuthenticationMethod auth = CreateAuthenticationMethodFromSecurityProvider(security, result.DeviceId);
                await ConfirmRegisteredDeviceWorks(result, auth, transportProtocol, twinOperationsAllowed).ConfigureAwait(false);

                //Check reprovisioning
                await UpdateEnrollmentToForceReprovision(enrollmentType, provisioningServiceClient, iotHubsToReprovisionTo, security, groupId).ConfigureAwait(false);
                result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
                ConfirmDeviceInExpectedHub(result, reprovisionPolicy, iotHubsToStartAt, iotHubsToReprovisionTo, allocationPolicy);
                await ConfirmDeviceWorksAfterReprovisioning(result, auth, transportProtocol, reprovisionPolicy, twinOperationsAllowed).ConfigureAwait(false);

                if (attestationType != AttestationType.x509) //x509 enrollments are hardcoded, should never be deleted
                {
                    await DeleteCreatedEnrollment(enrollmentType, provisioningServiceClient, security, groupId).ConfigureAwait(false);
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
            ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(ProxyServerAddress);
            string groupId = "some-valid-group-id-" + attestationTypeToString(attestationType) + "-" + Guid.NewGuid();

            CustomAllocationDefinition customAllocationDefinition = new CustomAllocationDefinition() { WebhookUrl = Configuration.Provisioning.CustomAllocationPolicyWebhook, ApiVersion = "2018-11-01" };

            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol))
            using (SecurityProvider security = await CreateSecurityProviderFromName(attestationType, enrollmentType, groupId, null, AllocationPolicy.Custom, customAllocationDefinition, iotHubsToProvisionTo).ConfigureAwait(false))
            {
                //Check basic provisioning
                if (ImplementsWebProxy(transportProtocol) && setCustomProxy)
                {
                    transport.Proxy = (proxyServerAddress != null) ? new WebProxy(ProxyServerAddress) : null;
                }

                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    s_globalDeviceEndpoint,
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);
                var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

                DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
                ValidateDeviceRegistrationResult(result);
                Assert.AreEqual(expectedDestinationHub, result.AssignedHub);

                if (attestationType != AttestationType.x509) //x509 enrollments are hardcoded, should never be deleted
                {
                    await DeleteCreatedEnrollment(enrollmentType, provisioningServiceClient, security, groupId).ConfigureAwait(false);
                }
            }
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Http_Fail()
        {
            try
            {
                await ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(Client.TransportType.Http1).ConfigureAwait(false);
                Assert.Fail("Expected exception not thrown");
            }
            catch (ProvisioningTransportException ex)
            {
                Assert.IsTrue(ex.Message.Contains("404201"));
            }
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Amqp_Fail()
        {
            try
            {
                await ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
                Assert.Fail("Expected exception not thrown");
            }
            catch (ProvisioningTransportException ex)
            {
                Assert.IsTrue(ex.Message.Contains("404201"));
            }
        }

        public async Task ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(Client.TransportType transportProtocol)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol))
            using (SecurityProvider security = new SecurityProviderTpmSimulator("invalidregistrationid"))
            {
                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    s_globalDeviceEndpoint,
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);

                var cts = new CancellationTokenSource(FailingTimeoutMiliseconds);

                _log.WriteLine("ProvisioningDeviceClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);

                _log.WriteLine($"{result.Status}");

                Assert.AreEqual(ProvisioningRegistrationStatusType.Failed, result.Status);
                Assert.IsNull(result.AssignedHub);
                Assert.IsNull(result.DeviceId);
                Assert.AreEqual(404201, result.ErrorCode);
            }
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Http_Tpm_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Http1, AttestationType.Tpm, null, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Http_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Http1, AttestationType.x509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Http_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Http1, AttestationType.x509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Amqp_Tpm_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Amqp_Tcp_Only, AttestationType.Tpm, null, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Amqp_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Amqp_Tcp_Only, AttestationType.x509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Amqp_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Amqp_Tcp_Only, AttestationType.x509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_AmqpWs_Tpm_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Amqp_WebSocket_Only, AttestationType.Tpm, null, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_AmqpWs_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_AmqpWs_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Mqtt_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Mqtt_Tcp_Only, AttestationType.x509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Mqtt_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Mqtt_Tcp_Only, AttestationType.x509, EnrollmentType.Group, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_MqttWs_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(Client.TransportType.Mqtt_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual, "").ConfigureAwait(false);
        }

        [TestMethod]
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

                var cts = new CancellationTokenSource(FailingTimeoutMiliseconds);

                var exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                    () => provClient.RegisterAsync(cts.Token)).ConfigureAwait(false);

                _log.WriteLine($"Exception: {exception}");
            }
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Http_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(Client.TransportType.Http1, AttestationType.x509, EnrollmentType.Individual, null).ConfigureAwait(false);
        }

        // Note: This test takes 3 minutes.
        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Amqp_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(Client.TransportType.Amqp_Tcp_Only, AttestationType.x509, EnrollmentType.Individual).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_AmqpWs_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(Client.TransportType.Amqp_WebSocket_Only, AttestationType.x509, EnrollmentType.Individual).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Mqtt_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(Client.TransportType.Mqtt_Tcp_Only, AttestationType.x509, EnrollmentType.Individual).ConfigureAwait(false);
        }

        [TestMethod]
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
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportProtocol))
            using (SecurityProvider security = await CreateSecurityProviderFromName(attestationType, enrollmentType, groupId, null, AllocationPolicy.Hashed, null, null).ConfigureAwait(false))
            {
                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    InvalidGlobalAddress,
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);

                var cts = new CancellationTokenSource(FailingTimeoutMiliseconds);

                _log.WriteLine("ProvisioningDeviceClient RegisterAsync . . . ");
                var exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                    () => provClient.RegisterAsync(cts.Token)).ConfigureAwait(false);

                _log.WriteLine($"Exception: {exception}");
            }
        }
        
        private ProvisioningTransportHandler CreateTransportHandlerFromName(Client.TransportType transportType)
        {
            _verboseLog.WriteLine($"{nameof(CreateTransportHandlerFromName)}({transportType})");

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
                _log.WriteLine("DeviceClient OpenAsync.");
                await iotClient.OpenAsync().ConfigureAwait(false);
                _log.WriteLine("DeviceClient SendEventAsync.");
                await iotClient.SendEventAsync(
                    new Client.Message(Encoding.UTF8.GetBytes("TestMessage"))).ConfigureAwait(false);

                if (sendReportedPropertiesUpdate)
                {
                    _log.WriteLine("DeviceClient updating desired properties.");
                    Twin twin = await iotClient.GetTwinAsync().ConfigureAwait(false);
                    await iotClient.UpdateReportedPropertiesAsync(new TwinCollection("{someTwinProperty:\"someTwinPropertyValue\"}")).ConfigureAwait(false);
                }

                _log.WriteLine("DeviceClient CloseAsync.");
                await iotClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task<SecurityProvider> CreateSecurityProviderFromName(AttestationType attestationType, EnrollmentType? enrollmentType, string groupId, ReprovisionPolicy reprovisionPolicy, AllocationPolicy allocationPolicy, CustomAllocationDefinition customAllocationDefinition, ICollection<string> iothubs)
        {
            _verboseLog.WriteLine($"{nameof(CreateSecurityProviderFromName)}({attestationType})");

            var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(Configuration.Provisioning.ConnectionString);

            switch (attestationType)
            {
                case AttestationType.Tpm:
                    string registrationId = attestationTypeToString(attestationType) + "-registration-id-" + Guid.NewGuid();
                    var tpmSim = new SecurityProviderTpmSimulator(registrationId);

                    string base64Ek = Convert.ToBase64String(tpmSim.GetEndorsementKey());


                    var provisioningService = ProvisioningServiceClient.CreateFromConnectionString(Configuration.Provisioning.ConnectionString);

                    _log.WriteLine($"Getting enrollment: RegistrationID = {registrationId}");
                    IndividualEnrollment individualEnrollment = new IndividualEnrollment(registrationId, new TpmAttestation(base64Ek)) { AllocationPolicy = allocationPolicy, ReprovisionPolicy = reprovisionPolicy, IotHubs = iothubs, CustomAllocationDefinition = customAllocationDefinition };
                    IndividualEnrollment enrollment = await provisioningService.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                    var attestation = new TpmAttestation(base64Ek);
                    enrollment.Attestation = attestation;
                    _log.WriteLine($"Updating enrollment: RegistrationID = {registrationId} EK = '{base64Ek}'");
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
                            EnrollmentGroup symmetricKeyEnrollmentGroup = await CreateEnrollmentGroup(provisioningServiceClient, AttestationType.SymmetricKey, groupId, reprovisionPolicy, allocationPolicy, customAllocationDefinition, iothubs).ConfigureAwait(false);
                            Assert.IsTrue(symmetricKeyEnrollmentGroup.Attestation is SymmetricKeyAttestation);
                            SymmetricKeyAttestation symmetricKeyAttestation = (SymmetricKeyAttestation)symmetricKeyEnrollmentGroup.Attestation;
                            string registrationIdSymmetricKey = "someregistrationid-" + Guid.NewGuid();
                            string primaryKeyEnrollmentGroup = symmetricKeyAttestation.PrimaryKey;
                            string secondaryKeyEnrollmentGroup = symmetricKeyAttestation.SecondaryKey;

                            string primaryKeyIndividual = ComputeDerivedSymmetricKey(Convert.FromBase64String(primaryKeyEnrollmentGroup), registrationIdSymmetricKey);
                            string secondaryKeyIndividual = ComputeDerivedSymmetricKey(Convert.FromBase64String(secondaryKeyEnrollmentGroup), registrationIdSymmetricKey);

                            return new SecurityProviderSymmetricKey(registrationIdSymmetricKey, primaryKeyIndividual, secondaryKeyIndividual);
                        case EnrollmentType.Individual:
                            IndividualEnrollment symmetricKeyEnrollment = await CreateIndividualEnrollment(provisioningServiceClient, AttestationType.SymmetricKey, reprovisionPolicy, allocationPolicy, customAllocationDefinition, iothubs).ConfigureAwait(false);

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
        private void ValidateDeviceRegistrationResult(DeviceRegistrationResult result)
        {
            Assert.IsNotNull(result);
            _log.WriteLine($"{result.Status} (Error Code: {result.ErrorCode}; Error Message: {result.ErrorMessage})");
            _log.WriteLine($"ProvisioningDeviceClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

            Assert.AreEqual(ProvisioningRegistrationStatusType.Assigned, result.Status);
            Assert.IsNotNull(result.AssignedHub);
            Assert.IsNotNull(result.DeviceId);
        }

        /// <summary>
        /// Update the enrollment under test such that it forces it to reprovision to the hubs within <paramref name="iotHubsToReprovisionTo"/>
        /// </summary>
        private async Task UpdateEnrollmentToForceReprovision(EnrollmentType? enrollmentType, ProvisioningServiceClient provisioningServiceClient, ICollection<String> iotHubsToReprovisionTo, SecurityProvider security, string groupId)
        {
            if (enrollmentType == EnrollmentType.Individual)
            {
                IndividualEnrollment individualEnrollment = await provisioningServiceClient.GetIndividualEnrollmentAsync(security.GetRegistrationID()).ConfigureAwait(false);
                individualEnrollment.IotHubs = iotHubsToReprovisionTo;
                IndividualEnrollment individualEnrollmentResult = await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
            }
            else
            {
                EnrollmentGroup enrollmentGroup = await provisioningServiceClient.GetEnrollmentGroupAsync(groupId).ConfigureAwait(false);
                enrollmentGroup.IotHubs = iotHubsToReprovisionTo;
                EnrollmentGroup enrollmentGroupResult = await provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Confirm that the hub the device belongs to did or did not change, depending on the reprovision policy
        /// </summary>
        private void ConfirmDeviceInExpectedHub(DeviceRegistrationResult result, ReprovisionPolicy reprovisionPolicy, ICollection<string> iotHubsToStartAt, ICollection<string> iotHubsToReprovisionTo, AllocationPolicy allocationPolicy)
        {
            if (reprovisionPolicy.UpdateHubAssignment)
            {
                Assert.IsTrue(iotHubsToReprovisionTo.Contains(result.AssignedHub));
                Assert.IsFalse(iotHubsToStartAt.Contains(result.AssignedHub));

                if (allocationPolicy == AllocationPolicy.GeoLatency)
                {
                    Assert.AreNotEqual(result.AssignedHub, Configuration.Provisioning.FarAwayIotHubHostName);
                }
            }
            else
            {
                Assert.IsFalse(iotHubsToReprovisionTo.Contains(result.AssignedHub));
                Assert.IsTrue(iotHubsToStartAt.Contains(result.AssignedHub));
            }
        }

        private async Task ConfirmDeviceWorksAfterReprovisioning(DeviceRegistrationResult result, Client.IAuthenticationMethod auth, Client.TransportType transportProtocol, ReprovisionPolicy reprovisionPolicy, bool twinOperationsAllowed)
        {
            using (DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth, transportProtocol))
            {
                _log.WriteLine("DeviceClient OpenAsync.");
                await iotClient.OpenAsync().ConfigureAwait(false);
                _log.WriteLine("DeviceClient SendEventAsync.");
                await iotClient.SendEventAsync(
                    new Client.Message(Encoding.UTF8.GetBytes("TestMessage"))).ConfigureAwait(false);

                //twin can be configured to revert back to default twin when provisioned, or to keep twin
                // from previous hub's records.
                if (twinOperationsAllowed)
                {
                    Twin twin = await iotClient.GetTwinAsync().ConfigureAwait(false);

                    if (reprovisionPolicy.MigrateDeviceData)
                    {
                        Assert.AreNotEqual(1, twin.Properties.Desired.Count);
                        Assert.AreNotEqual(0, twin.Properties.Desired.Version);
                        Assert.AreEqual(ProvisioningRegistrationSubstatusType.DeviceDataMigrated, result.Substatus);
                    }
                    else if (reprovisionPolicy.UpdateHubAssignment)
                    {
                        Assert.AreEqual(twin.Properties.Desired.Count, 0);
                        Assert.AreEqual(ProvisioningRegistrationSubstatusType.DeviceDataReset, result.Substatus);
                    }
                    else
                    {
                        Assert.AreNotEqual(twin.Properties.Desired.Count, 1);
                        Assert.AreEqual(ProvisioningRegistrationSubstatusType.InitialAssignment, result.Substatus);
                    }
                }

                _log.WriteLine("DeviceClient CloseAsync.");
                await iotClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task DeleteCreatedEnrollment(EnrollmentType? enrollmentType, ProvisioningServiceClient provisioningServiceClient, SecurityProvider security, string groupId)
        {
            if (enrollmentType == EnrollmentType.Individual)
            {
                await provisioningServiceClient.DeleteIndividualEnrollmentAsync(security.GetRegistrationID()).ConfigureAwait(false);
            }
            else
            {
                await provisioningServiceClient.DeleteEnrollmentGroupAsync(groupId).ConfigureAwait(false);
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

        private bool ImplementsWebProxy(Client.TransportType transportProtocol)
        {
            _verboseLog.WriteLine($"{nameof(ImplementsWebProxy)}({transportProtocol})");

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
