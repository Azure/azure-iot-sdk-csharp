// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Security;
using Microsoft.Azure.Devices.Provisioning.Security.Samples;
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
    public class ProvisioningTests
    {
        private const int PassingTimeoutMiliseconds = 30 * 1000;
        private const int FailingTimeoutMiliseconds = 10 * 1000;
        private const string s_globalDeviceEndpoint = "global.azure-devices-provisioning.net";
        private const string InvalidIDScope = "0neFFFFFFFF";
        private const string InvalidGlobalAddress = "httpbin.org";

        private readonly VerboseTestLogging _verboseLog = VerboseTestLogging.GetInstance();
        private readonly TestLogging _log = TestLogging.GetInstance();

        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "This routes all logging to Console and Debug log.")]
        private static ConsoleEventListener s_listener = new ConsoleEventListener();

        public enum X509EnrollmentType
        {
            Individual,
            Group
        }

        [TestMethod]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderTpmHsm), null, null)]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, null)]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, null)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderTpmHsm), null, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.TcpOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderTpmHsm), null, TransportFallbackType.WebSocketOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.TcpOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly)]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            string transportType, 
            string securityType,
            X509EnrollmentType? x509EnrollmentType,
            TransportFallbackType? transportFallback)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, transportFallback))
            using (SecurityProvider security = CreateSecurityProviderFromName(securityType, x509EnrollmentType))
            {
                _verboseLog.WriteLine("Creating device");

                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    s_globalDeviceEndpoint,
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);

                var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

                _log.WriteLine("ProvisioningClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);

                Assert.IsNotNull(result);
                _log.WriteLine($"{result.Status} (Error Code: {result.ErrorCode}; Error Message: {result.ErrorMessage})");
                _log.WriteLine($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

                Assert.AreEqual(ProvisioningRegistrationStatusType.Assigned, result.Status);
                Assert.IsNotNull(result.AssignedHub);
                Assert.IsNotNull(result.DeviceId);

                Client.IAuthenticationMethod auth = CreateAuthenticationMethodFromSecurityProvider(
                    security, 
                    result.DeviceId);

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
        [DataRow(nameof(ProvisioningTransportHandlerHttp))]
        // TODO: throws UnauthorizedAccessException [DataRow(nameof(ProvisioningTransportHandlerAmqp))]
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

                _log.WriteLine("ProvisioningClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token).ConfigureAwait(false);

                _log.WriteLine($"{result.Status}");

                Assert.AreEqual(ProvisioningRegistrationStatusType.Failed, result.Status);
                Assert.IsNull(result.AssignedHub);
                Assert.IsNull(result.DeviceId);
                Assert.AreEqual("Not Found", result.ErrorMessage);
                Assert.AreEqual(0x00062ae9, result.ErrorCode);
            }
        }

        [TestMethod]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderTpmHsm), null, null)]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, null)]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, null)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderTpmHsm), null, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderTpmHsm), null, TransportFallbackType.WebSocketOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.TcpOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly)]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Fail(
            string transportType,
            string securityType,
            X509EnrollmentType? x509EnrollmentType,
            TransportFallbackType? transportFallback)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, transportFallback))
            using (SecurityProvider security = CreateSecurityProviderFromName(securityType, x509EnrollmentType))
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
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, null)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityProviderX509Certificate), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly)]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(
            string transportType,
            string securityType,
            X509EnrollmentType? x509EnrollmentType,
            TransportFallbackType? transportFallback)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, transportFallback))
            using (SecurityProvider security = CreateSecurityProviderFromName(securityType, x509EnrollmentType))
            {
                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    InvalidGlobalAddress,
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);

                var cts = new CancellationTokenSource(FailingTimeoutMiliseconds);

                _log.WriteLine("ProvisioningClient RegisterAsync . . . ");
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

        private SecurityProvider CreateSecurityProviderFromName(string name, X509EnrollmentType? x509Type)
        {
            _verboseLog.WriteLine($"{nameof(CreateSecurityProviderFromName)}({name})");

            switch (name)
            {
                case nameof(SecurityProviderTpmHsm):
                    var tpmSim = new SecurityProviderTpmSimulator(Configuration.Provisioning.TpmDeviceRegistrationId);
                    SecurityProviderTpmSimulator.StartSimulatorProcess();

                    _log.WriteLine(
                        $"RegistrationID = {Configuration.Provisioning.TpmDeviceRegistrationId} " + 
                        $"EK = '{Convert.ToBase64String(tpmSim.GetEndorsementKey())}'");

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
    }
}
