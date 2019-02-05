﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Security;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
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
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerHttp), AttestationType.Tpm, EnrollmentType.Individual, null, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerHttp), AttestationType.x509, EnrollmentType.Individual, null, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerHttp), AttestationType.x509, EnrollmentType.Group, null, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.Tpm, EnrollmentType.Individual, TransportFallbackType.TcpOnly, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.TcpOnly, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.x509, EnrollmentType.Group, TransportFallbackType.TcpOnly, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.Tpm, EnrollmentType.Individual, TransportFallbackType.WebSocketOnly, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.WebSocketOnly, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.x509, EnrollmentType.Group, TransportFallbackType.WebSocketOnly, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.TcpOnly, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.x509, EnrollmentType.Group, TransportFallbackType.TcpOnly, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.WebSocketOnly, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_X509Group_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.x509, EnrollmentType.Group, TransportFallbackType.WebSocketOnly, false).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_HttpWithProxy_Tpm_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerHttp), AttestationType.Tpm, EnrollmentType.Individual, null, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_HttpWithNullProxy_Tpm_RegisterOk_Individual()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerHttp), AttestationType.Tpm, EnrollmentType.Individual, null, true, null).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWsWithProxy_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.WebSocketOnly, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWsWithNullProxy_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.WebSocketOnly, true, null).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWsWithProxy_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.WebSocketOnly, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWsWithNullProxy_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.WebSocketOnly, true, null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_SymmetricKey_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerHttp), AttestationType.SymmetricKey, EnrollmentType.Individual, null, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerHttp), AttestationType.SymmetricKey, EnrollmentType.Group, null, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.SymmetricKey, EnrollmentType.Group, null, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.SymmetricKey, EnrollmentType.Group, null, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.SymmetricKey, EnrollmentType.Group, TransportFallbackType.WebSocketOnly, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.SymmetricKey, EnrollmentType.Group, TransportFallbackType.WebSocketOnly, false).ConfigureAwait(false);
        }

        [TestCategory("ProxyE2ETests")]
        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_HttpWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerHttp), AttestationType.SymmetricKey, EnrollmentType.Group, null, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("ProxyE2ETests")]
        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.SymmetricKey, EnrollmentType.Group, null, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("ProxyE2ETests")]
        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.SymmetricKey, EnrollmentType.Group, null, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("ProxyE2ETests")]
        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWsWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.SymmetricKey, EnrollmentType.Group, TransportFallbackType.WebSocketOnly, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestCategory("ProxyE2ETests")]
        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWsWithProxy_SymmetricKey_RegisterOk_GroupEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.SymmetricKey, EnrollmentType.Group, TransportFallbackType.WebSocketOnly, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.SymmetricKey, EnrollmentType.Individual, null, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.SymmetricKey, EnrollmentType.Individual, null, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.SymmetricKey, EnrollmentType.Individual, null, false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.SymmetricKey, EnrollmentType.Individual, null, false).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_HttpWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerHttp), AttestationType.SymmetricKey, EnrollmentType.Individual, null, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.SymmetricKey, EnrollmentType.Individual, null, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWsWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), AttestationType.SymmetricKey, EnrollmentType.Individual, null, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.SymmetricKey, EnrollmentType.Individual, null, true, ProxyServerAddress).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWsWithProxy_SymmetricKey_RegisterOk_IndividualEnrollment()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), AttestationType.SymmetricKey, EnrollmentType.Individual, null, true, ProxyServerAddress).ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            string transportType, 
            AttestationType attestationType,
            EnrollmentType? enrollmentType,
            TransportFallbackType? transportFallback,
            bool setCustomProxy,
            string proxyServerAddress = null)
        {
            string groupId = "some-valid-group-id-" + attestationTypeToString(attestationType) + "-" + Guid.NewGuid();
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, transportFallback))
            using (SecurityProvider security = await CreateSecurityProviderFromName(attestationType, enrollmentType, groupId).ConfigureAwait(false))
            {
                
                _verboseLog.WriteLine("Creating device");

                if (ImplementsWebProxy(transportType, transportFallback) && setCustomProxy)
                {
                    transport.Proxy = (proxyServerAddress != null) ? new WebProxy(proxyServerAddress) : null;
                }

                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    s_globalDeviceEndpoint,
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);

                var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

                _log.WriteLine("ProvisioningDeviceClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);
                
                Assert.IsNotNull(result);
                _log.WriteLine($"{result.Status} (Error Code: {result.ErrorCode}; Error Message: {result.ErrorMessage})");
                _log.WriteLine($"ProvisioningDeviceClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

                Assert.AreEqual(ProvisioningRegistrationStatusType.Assigned, result.Status);
                Assert.IsNotNull(result.AssignedHub);
                Assert.IsNotNull(result.DeviceId);

                Client.IAuthenticationMethod auth = CreateAuthenticationMethodFromSecurityProvider(security, result.DeviceId);

                using (DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth, GetDeviceTransportType(transportType, transportFallback)))
                {
                    _log.WriteLine("DeviceClient OpenAsync.");
                    await iotClient.OpenAsync().ConfigureAwait(false);
                    _log.WriteLine("DeviceClient SendEventAsync.");
                    await iotClient.SendEventAsync(
                        new Client.Message(Encoding.UTF8.GetBytes("TestMessage"))).ConfigureAwait(false);
                    _log.WriteLine("DeviceClient CloseAsync.");
                    await iotClient.CloseAsync().ConfigureAwait(false);
                }

                //delete the created enrollment
                if (attestationType != AttestationType.x509) //x509 enrollments are hardcoded, should never be deleted
                {
                    ProvisioningServiceClient provisioningServiceClient = CreateProvisioningService(proxyServerAddress);
                    if (enrollmentType == EnrollmentType.Individual)
                    {
                        await provisioningServiceClient.DeleteIndividualEnrollmentAsync(security.GetRegistrationID()).ConfigureAwait(false);
                    }
                    else
                    {
                        await provisioningServiceClient.DeleteEnrollmentGroupAsync(groupId).ConfigureAwait(false);
                    }
                }
            }
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Http_Fail()
        {
            try
            {
                await ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(nameof(ProvisioningTransportHandlerHttp)).ConfigureAwait(false);
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
                await ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(nameof(ProvisioningTransportHandlerAmqp)).ConfigureAwait(false);
                Assert.Fail("Expected exception not thrown");
            }
            catch (ProvisioningTransportException ex)
            {
                Assert.IsTrue(ex.Message.Contains("404201"));
            }
        }

        public async Task ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(string transportType)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, TransportFallbackType.TcpOnly))
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
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerHttp), AttestationType.Tpm, null, null, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Http_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerHttp), AttestationType.x509, EnrollmentType.Individual, null, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Http_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerHttp), AttestationType.x509, EnrollmentType.Group, null, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Amqp_Tpm_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), AttestationType.Tpm, null, TransportFallbackType.TcpOnly, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Amqp_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.TcpOnly, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Amqp_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), AttestationType.x509, EnrollmentType.Group, TransportFallbackType.TcpOnly, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_AmqpWs_Tpm_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), AttestationType.Tpm, null, TransportFallbackType.WebSocketOnly, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_AmqpWs_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.WebSocketOnly, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_AmqpWs_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), AttestationType.x509, EnrollmentType.Group, TransportFallbackType.WebSocketOnly, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Mqtt_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerMqtt), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.TcpOnly, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Mqtt_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerMqtt), AttestationType.x509, EnrollmentType.Group, TransportFallbackType.TcpOnly, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_MqttWs_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerMqtt), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.WebSocketOnly, "").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_MqttWs_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerMqtt), AttestationType.x509, EnrollmentType.Group, TransportFallbackType.WebSocketOnly, "").ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Fail(
            string transportType,
            AttestationType attestationType,
            EnrollmentType? enrollmentType,
            TransportFallbackType? transportFallback,
            string groupId)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, transportFallback))
            using (SecurityProvider security = await CreateSecurityProviderFromName(attestationType, enrollmentType, groupId).ConfigureAwait(false))
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
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(nameof(ProvisioningTransportHandlerHttp), AttestationType.x509, EnrollmentType.Individual, null).ConfigureAwait(false);
        }

        // Note: This test takes 3 minutes.
        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Amqp_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_AmqpWs_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Mqtt_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(nameof(ProvisioningTransportHandlerMqtt), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_MqttWs_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(nameof(ProvisioningTransportHandlerMqtt), AttestationType.x509, EnrollmentType.Individual, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(
            string transportType,
            AttestationType attestationType,
            EnrollmentType? enrollmentType,
            TransportFallbackType? transportFallback,
            string groupId = "")
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, transportFallback))
            using (SecurityProvider security = await CreateSecurityProviderFromName(attestationType, enrollmentType, groupId).ConfigureAwait(false))
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

        private ProvisioningTransportHandler CreateTransportHandlerFromName(string name, TransportFallbackType? fallbackType)
        {
            _verboseLog.WriteLine($"{nameof(CreateTransportHandlerFromName)}({name})");

            switch (name)
            {
                case nameof(ProvisioningTransportHandlerHttp):
                    return new ProvisioningTransportHandlerHttp();

                case nameof(ProvisioningTransportHandlerAmqp):
                    return new ProvisioningTransportHandlerAmqp(fallbackType ?? TransportFallbackType.TcpOnly);

                case nameof(ProvisioningTransportHandlerMqtt):
                    return new ProvisioningTransportHandlerMqtt(fallbackType ?? TransportFallbackType.TcpOnly);
            }

            throw new NotSupportedException($"Unknown transport: '{name}'.");
        }

        private Client.TransportType GetDeviceTransportType(string provisioningTransport, TransportFallbackType? fallbackType)
        {
            switch (provisioningTransport)
            {
                case nameof(ProvisioningTransportHandlerHttp):
                    return Client.TransportType.Http1;

                case nameof(ProvisioningTransportHandlerAmqp):
                    if (!fallbackType.HasValue) return Client.TransportType.Amqp;
                    switch (fallbackType)
                    {
                        case TransportFallbackType.TcpWithWebSocketFallback:
                            return Client.TransportType.Amqp;
                        case TransportFallbackType.WebSocketOnly:
                            return Client.TransportType.Amqp_WebSocket_Only;
                        case TransportFallbackType.TcpOnly:
                            return Client.TransportType.Amqp_Tcp_Only;
                        default:
                            break;
                    }
                    break;

                case nameof(ProvisioningTransportHandlerMqtt):
                    if (!fallbackType.HasValue) return Client.TransportType.Mqtt;
                    switch (fallbackType)
                    {
                        case TransportFallbackType.TcpWithWebSocketFallback:
                            return Client.TransportType.Mqtt;
                        case TransportFallbackType.WebSocketOnly:
                            return Client.TransportType.Mqtt_WebSocket_Only;
                        case TransportFallbackType.TcpOnly:
                            return Client.TransportType.Mqtt_Tcp_Only;
                        default:
                            break;
                    }
                    break;

                default:
                    break;
            }

            throw new NotSupportedException($"Unknown transport: '{provisioningTransport}'.");
        }

        private async Task<SecurityProvider> CreateSecurityProviderFromName(AttestationType attestationType, EnrollmentType? enrollmentType, string groupId)
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
                    IndividualEnrollment individualEnrollment = new IndividualEnrollment(registrationId, new TpmAttestation(base64Ek));
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
                            EnrollmentGroup symmetricKeyEnrollmentGroup = await CreateEnrollmentGroup(provisioningServiceClient, AttestationType.SymmetricKey, groupId).ConfigureAwait(false);
                            Assert.IsTrue(symmetricKeyEnrollmentGroup.Attestation is SymmetricKeyAttestation);
                            SymmetricKeyAttestation symmetricKeyAttestation = (SymmetricKeyAttestation)symmetricKeyEnrollmentGroup.Attestation;
                            string registrationIdSymmetricKey = "someregistrationid-" + Guid.NewGuid();
                            string primaryKeyEnrollmentGroup = symmetricKeyAttestation.PrimaryKey;
                            string secondaryKeyEnrollmentGroup = symmetricKeyAttestation.SecondaryKey;

                            string primaryKeyIndividual = ComputeDerivedSymmetricKey(Convert.FromBase64String(primaryKeyEnrollmentGroup), registrationIdSymmetricKey);
                            string secondaryKeyIndividual = ComputeDerivedSymmetricKey(Convert.FromBase64String(secondaryKeyEnrollmentGroup), registrationIdSymmetricKey);

                            return new SecurityProviderSymmetricKey(registrationIdSymmetricKey, primaryKeyIndividual, secondaryKeyIndividual);
                        case EnrollmentType.Individual:
                            IndividualEnrollment symmetricKeyEnrollment = await CreateIndividualEnrollment(provisioningServiceClient, AttestationType.SymmetricKey).ConfigureAwait(false);

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

        private bool ImplementsWebProxy(string name, TransportFallbackType? fallbackType)
        {
            _verboseLog.WriteLine($"{nameof(ImplementsWebProxy)}({name})");

            switch (name)
            {
                case nameof(ProvisioningTransportHandlerHttp):
                    return true;

                case nameof(ProvisioningTransportHandlerAmqp):
                case nameof(ProvisioningTransportHandlerMqtt):
                    return (fallbackType == TransportFallbackType.WebSocketOnly);
            }

            throw new NotSupportedException($"Unknown transport: '{name}'.");
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
