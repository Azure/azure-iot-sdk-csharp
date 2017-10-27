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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    public class ProvisioningTests
    {
        private const int PassingTimeoutMiliseconds = 10 * 1000;
        private const int FailingTimeoutMiliseconds = 1 * 1000;
        private const string InvalidIDScope = "0neFFFFFFFF";
        private const string InvalidGlobalAddress = "https://httpbin.org";

        private readonly VerboseTestLogging _verboseLog = VerboseTestLogging.GetInstance();
        private readonly TestLogging _log = TestLogging.GetInstance();

        public enum X509EnrollmentType
        {
            Individual,
            Group
        }

        [TestMethod]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityClientTpm), null, null)]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityClientX509), X509EnrollmentType.Individual, null)]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityClientX509), X509EnrollmentType.Group, null)]
//        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientTpm), null, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientX509), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientX509), X509EnrollmentType.Group, TransportFallbackType.TcpOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientTpm), null, TransportFallbackType.WebSocketOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientX509), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientX509), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientTpm), null, TransportFallbackType.TcpOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientX509), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientX509), X509EnrollmentType.Group, TransportFallbackType.TcpOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientTpm), null, TransportFallbackType.WebSocketOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientX509), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly)]
//        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientX509), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly)]
        public async Task ProvisioningDeviceClient_ValidRegistrationId_Register_Ok(
            string transportType, 
            string securityType,
            X509EnrollmentType? x509EnrollmentType,
            TransportFallbackType? transportFallback)
        {
            if (string.IsNullOrWhiteSpace(Configuration.Provisioning.IdScope))
            {
                Assert.Inconclusive();
            }

            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, transportFallback))
            using (SecurityClient security = CreateSecurityClientFromName(securityType, x509EnrollmentType))
            {
                _verboseLog.WriteLine("Creating device");

                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);

                var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

                _log.WriteLine("ProvisioningClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token);

                _log.WriteLine($"{result.Status}");
                _log.WriteLine($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

                Assert.AreEqual(ProvisioningRegistrationStatusType.Assigned, result.Status);
                Assert.IsNotNull(result.AssignedHub);
                Assert.IsNotNull(result.DeviceId);

                Client.IAuthenticationMethod auth =
                    await CreateAuthenticationMethodFromSecurityClient(security, result.DeviceId, result.AssignedHub);

                using (DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth, Client.TransportType.Mqtt_Tcp_Only))
                {
                    _log.WriteLine("DeviceClient OpenAsync.");
                    await iotClient.OpenAsync();
                    _log.WriteLine("DeviceClient SendEventAsync.");
                    await iotClient.SendEventAsync(new Client.Message(Encoding.UTF8.GetBytes("TestMessage")));
                    _log.WriteLine("DeviceClient CloseAsync.");
                    await iotClient.CloseAsync();
                }
            }
        }

        [TestMethod]
        [DataRow(nameof(ProvisioningTransportHandlerHttp))]
        // TODO: throws UnauthorizedAccessException [DataRow(nameof(ProvisioningTransportHandlerAmqp))]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt))]
        public async Task ProvisioningDeviceClient_InvalidRegistrationId_Register_Fail(string transportType)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, TransportFallbackType.TcpOnly))
            using (SecurityClient security = new SecurityClientTpmSimulator("invalidregistrationid"))
            {
                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    Configuration.Provisioning.IdScope,
                    security,
                    transport);

                var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

                _log.WriteLine("ProvisioningClient RegisterAsync . . . ");
                DeviceRegistrationResult result = await provClient.RegisterAsync(cts.Token);

                _log.WriteLine($"{result.Status}");

                Assert.AreEqual(ProvisioningRegistrationStatusType.Failed, result.Status);
                Assert.IsNull(result.AssignedHub);
                Assert.IsNull(result.DeviceId);
                Assert.AreEqual("Not Found", result.ErrorMessage);
                Assert.AreEqual(0x00062ae9, result.ErrorCode);
            }
        }

        [TestMethod]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityClientTpm), null, null)]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityClientX509), X509EnrollmentType.Individual, null)]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityClientX509), X509EnrollmentType.Group, null)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientTpm), null, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientX509), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientX509), X509EnrollmentType.Group, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientTpm), null, TransportFallbackType.WebSocketOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientX509), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientX509), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly)]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientTpm), null, TransportFallbackType.TcpOnly)]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientX509), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly)]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientX509), X509EnrollmentType.Group, TransportFallbackType.TcpOnly)]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientTpm), null, TransportFallbackType.WebSocketOnly)]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientX509), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly)]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientX509), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly)]
        public async Task ProvisioningDeviceClient_InvalidIdScope_Register_Fail(
            string transportType,
            string securityType,
            X509EnrollmentType? x509EnrollmentType,
            TransportFallbackType? transportFallback)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, transportFallback))
            using (SecurityClient security = CreateSecurityClientFromName(securityType, x509EnrollmentType))
            {
                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    InvalidIDScope,
                    security,
                    transport);

                var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

                var exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                    () => provClient.RegisterAsync(cts.Token));

                _log.WriteLine($"Exception: {exception}");
            }
        }

        [TestMethod]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityClientTpm), null, null)]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityClientX509), X509EnrollmentType.Individual, null)]
        [DataRow(nameof(ProvisioningTransportHandlerHttp), nameof(SecurityClientX509), X509EnrollmentType.Group, null)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientTpm), null, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientX509), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientX509), X509EnrollmentType.Group, TransportFallbackType.TcpOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientTpm), null, TransportFallbackType.WebSocketOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientX509), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly)]
        [DataRow(nameof(ProvisioningTransportHandlerAmqp), nameof(SecurityClientX509), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly)]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientTpm), null, TransportFallbackType.TcpOnly)]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientX509), X509EnrollmentType.Individual, TransportFallbackType.TcpOnly)]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientX509), X509EnrollmentType.Group, TransportFallbackType.TcpOnly)]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientTpm), null, TransportFallbackType.WebSocketOnly)]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientX509), X509EnrollmentType.Individual, TransportFallbackType.WebSocketOnly)]
        //        [DataRow(nameof(ProvisioningTransportHandlerMqtt), nameof(SecurityClientX509), X509EnrollmentType.Group, TransportFallbackType.WebSocketOnly)]
        public async Task ProvisioningDeviceClient_InvalidGlobalAddress_Register_Fail(
            string transportType,
            string securityType,
            X509EnrollmentType? x509EnrollmentType,
            TransportFallbackType? transportFallback)
        {
            using (ProvisioningTransportHandler transport = CreateTransportHandlerFromName(transportType, transportFallback))
            using (SecurityClient security = CreateSecurityClientFromName(securityType, x509EnrollmentType))
            {
                ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                    InvalidIDScope,
                    security,
                    transport);

                var cts = new CancellationTokenSource(PassingTimeoutMiliseconds);

                _log.WriteLine("ProvisioningClient RegisterAsync . . . ");
                var exception = await Assert.ThrowsExceptionAsync<ProvisioningTransportException>(
                    () => provClient.RegisterAsync(cts.Token));

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
                    // TODO: Enable after Mqtt is implemented
                    Assert.Inconclusive("MQTT is not implemented.");
                    return new ProvisioningTransportHandlerMqtt(fallbackType ?? TransportFallbackType.TcpOnly);
            }

            throw new NotSupportedException($"Unknown transport: '{name}'.");
        }

        private SecurityClient CreateSecurityClientFromName(string name, X509EnrollmentType? x509Type)
        {
            _verboseLog.WriteLine($"{nameof(CreateSecurityClientFromName)}({name})");

            switch (name)
            {
                case nameof(SecurityClientTpm):
                    var tpmSim = new SecurityClientTpmSimulator(Configuration.Provisioning.TpmDeviceRegistrationId);
                    SecurityClientTpmSimulator.StartSimulatorProcess();

                    _log.WriteLine(
                        $"RegistrationID = {Configuration.Provisioning.TpmDeviceRegistrationId} " + 
                        $"EK = '{Convert.ToBase64String(tpmSim.GetEndorsementKey())}'");

                    return tpmSim;

                case nameof(SecurityClientX509):

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

                    return new SecurityClientX509(certificate, collection);
            }

            throw new NotSupportedException($"Unknown security type: '{name}'.");
        }

        private async Task<Client.IAuthenticationMethod> CreateAuthenticationMethodFromSecurityClient(
            SecurityClient provisioningSecurity,
            string deviceId,
            string iotHub)
        {
            _verboseLog.WriteLine($"{nameof(CreateAuthenticationMethodFromSecurityClient)}({deviceId})");

            if (provisioningSecurity is SecurityClientHsmTpm)
            {
                var security = (SecurityClientHsmTpm)provisioningSecurity;
                var auth = new DeviceAuthenticationWithTpm(deviceId, security);
                
                // TODO: workaround to populate Token.
                await auth.GetTokenAsync(iotHub);

                return auth;
            }
            else if (provisioningSecurity is SecurityClientHsmX509)
            {
                var security = (SecurityClientHsmX509)provisioningSecurity;
                X509Certificate2 cert = security.GetAuthenticationCertificate();

                return new DeviceAuthenticationWithX509Certificate(deviceId, cert);
            }

            throw new NotSupportedException($"Unknown provisioningSecurity type.");
        }
    }
}
