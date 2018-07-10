// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    public class ProvisioningTests : IDisposable
    {
        private const int PassingTimeoutMiliseconds = 30 * 1000;
        private const int FailingTimeoutMiliseconds = 10 * 1000;
        private static string s_globalDeviceEndpoint = Configuration.Provisioning.GlobalDeviceEndpoint;
        private const string InvalidIDScope = "0neFFFFFFFF";
        private const string InvalidGlobalAddress = "httpbin.org";

        private readonly VerboseTestLogging _verboseLog = VerboseTestLogging.GetInstance();
        private readonly TestLogging _log = TestLogging.GetInstance();
        private readonly ConsoleEventListener _listener;

        public ProvisioningTests()
        {
            _listener = new ConsoleEventListener("Microsoft-Azure-");
        }

        public enum X509EnrollmentType
        {
            Individual,
            Group
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderTpmHsm), null, null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Http_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderTpmHsm), null, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Amqp_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [Ignore] //TODO
        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_Tpm_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderTpmHsm), null, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_AmqpWs_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Mqtt_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_X509Individual_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_MqttWs_X509Group_RegisterOk()
        {
            await ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            string transportType, 
            string securityType,
            X509EnrollmentType? x509EnrollmentType,
            TransportFallbackType? transportFallback)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, transportFallback))
            using (SecurityProvider security = await CreateSecurityProviderFromName(securityType, x509EnrollmentType).ConfigureAwait(false))
            {
                _verboseLog.WriteLine("Creating device");

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

                using (DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth, Client.TransportType.Mqtt_Tcp_Only))
                {
                    _log.WriteLine("DeviceClient OpenAsync.");
                    await iotClient.OpenAsync().ConfigureAwait(false);
                    _log.WriteLine("DeviceClient SendEventAsync.");
                    await iotClient.SendEventAsync(
                        new Client.Message(Encoding.UTF8.GetBytes("TestMessage"))).ConfigureAwait(false);
                    _log.WriteLine("DeviceClient CloseAsync.");
                    await iotClient.CloseAsync().ConfigureAwait(false);
                }
            }
        }
        
        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Http_Fail()
        {
            await ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(nameof(ProvisioningTransportHandlerHttp)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Amqp_Fail()
        {
            await ProvisioningDeviceClient_InvalidRegistrationId_TpmRegister_Fail(nameof(ProvisioningTransportHandlerAmqp)).ConfigureAwait(false);
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
                Assert.AreEqual("Enrollment not found", result.ErrorMessage);
                Assert.AreEqual(0x00062ae9, result.ErrorCode);
            }
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Http_Tpm_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderTpmHsm), null, null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Http_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Http_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Amqp_Tpm_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderTpmHsm), null, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Amqp_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Amqp_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [Ignore] //TODO
        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_AmqpWs_Tpm_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderTpmHsm), null, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_AmqpWs_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_AmqpWs_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Mqtt_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Mqtt_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_MqttWs_X509Individual_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_MqttWs_X509Group_Fail()
        {
            await ProvisioningDeviceClient_InvalidIdScope_Register_Fail(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Fail(
            string transportType,
            string securityType,
            X509EnrollmentType? x509EnrollmentType,
            TransportFallbackType? transportFallback)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, transportFallback))
            using (SecurityProvider security = await CreateSecurityProviderFromName(securityType, x509EnrollmentType).ConfigureAwait(false))
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
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, null).ConfigureAwait(false);
        }

        [Ignore] //TODO
        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Amqp_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_AmqpWs_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Mqtt_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_MqttWs_Fail()
        {
            await ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly).ConfigureAwait(false);
        }

        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(
            string transportType,
            string securityType,
            X509EnrollmentType? x509EnrollmentType,
            TransportFallbackType? transportFallback)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, transportFallback))
            using (SecurityProvider security = await CreateSecurityProviderFromName(securityType, x509EnrollmentType).ConfigureAwait(false))
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

        private async Task<SecurityProvider> CreateSecurityProviderFromName(string name, X509EnrollmentType? x509Type)
        {
            _verboseLog.WriteLine($"{nameof(CreateSecurityProviderFromName)}({name})");

            switch (name)
            {
                case nameof(SecurityProviderTpmHsm):
                    var tpmSim = new SecurityProviderTpmSimulator(Configuration.Provisioning.TpmDeviceRegistrationId);

                    string base64Ek = Convert.ToBase64String(tpmSim.GetEndorsementKey());
                    string registrationId = Configuration.Provisioning.TpmDeviceRegistrationId;


                    var provisioningService = ProvisioningServiceClient.CreateFromConnectionString(Configuration.Provisioning.ConnectionString);

                    _log.WriteLine($"Getting enrollment: RegistrationID = {registrationId}");
                    IndividualEnrollment enrollment = await provisioningService.GetIndividualEnrollmentAsync(registrationId).ConfigureAwait(false);
                    var attestation = new TpmAttestation(base64Ek);
                    enrollment.Attestation = attestation;
                    _log.WriteLine($"Updating enrollment: RegistrationID = {registrationId} EK = '{base64Ek}'");
                    await provisioningService.CreateOrUpdateIndividualEnrollmentAsync(enrollment).ConfigureAwait(false);

                    return tpmSim;

                case nameof(SecurityProviderX509Certificate):

                    X509Certificate2 certificate = null;
                    X509Certificate2Collection collection = null;
                    switch (x509Type)
                    {
                        case X509EnrollmentType.Individual:
                            certificate = Configuration.Provisioning.GetIndividualEnrollmentCertificate();
                            break;
                        case X509EnrollmentType.Group:
                            certificate = Configuration.Provisioning.GetGroupEnrollmentCertificate();
                            collection = Configuration.Provisioning.GetGroupEnrollmentChain();
                            break;
                        default:
                            throw new NotSupportedException($"Unknown X509 type: '{x509Type}'");
                    }

                    return new SecurityProviderX509Certificate(certificate, collection);
            }

            throw new NotSupportedException($"Unknown security type: '{name}'.");
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

            throw new NotSupportedException($"Unknown provisioningSecurity type.");
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listener.Dispose();
            }
        }
    }
}
