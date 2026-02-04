// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Demonstrates how to provision a device using DPS with a Certificate Signing Request (CSR).
    /// The device receives an issued certificate from DPS which can then be used to authenticate with IoT Hub.
    /// </summary>
    internal class ProvisioningDeviceClientCsrSample
    {
        private readonly Parameters _parameters;

        public ProvisioningDeviceClientCsrSample(Parameters parameters)
        {
            _parameters = parameters;
        }

        public async Task RunSampleAsync()
        {
            Console.WriteLine("=== DPS CSR Provisioning Sample ===\n");

            // Step 1: Generate key pair and CSR
            Console.WriteLine("Step 1: Generating key pair and Certificate Signing Request (CSR)...");
            var (csrBase64, privateKey) = GenerateCsr(_parameters.RegistrationId);
            Console.WriteLine($"  CSR generated successfully.");
            Console.WriteLine($"  Key type: {_parameters.CsrKeyType}");

            // Save the private key
            SavePrivateKey(privateKey, _parameters.OutputKeyPath);
            Console.WriteLine($"  Private key saved to: {_parameters.OutputKeyPath}");

            // Step 2: Create security provider based on authentication type
            Console.WriteLine($"\nStep 2: Creating security provider ({_parameters.AuthType})...");
            using SecurityProvider security = CreateSecurityProvider();
            Console.WriteLine($"  Registration ID: {security.GetRegistrationID()}");

            // Step 3: Create provisioning client
            Console.WriteLine("\nStep 3: Creating provisioning client...");
            using ProvisioningTransportHandler transportHandler = GetTransportHandler();
            ProvisioningDeviceClient provisioningClient = ProvisioningDeviceClient.Create(
                _parameters.GlobalDeviceEndpoint,
                _parameters.IdScope,
                security,
                transportHandler);

            Console.WriteLine($"  Global endpoint: {_parameters.GlobalDeviceEndpoint}");
            Console.WriteLine($"  ID Scope: {_parameters.IdScope}");
            Console.WriteLine($"  Transport: {_parameters.TransportType}");

            // Step 4: Register with CSR
            Console.WriteLine("\nStep 4: Registering with DPS (including CSR)...");
            var additionalData = new ProvisioningRegistrationAdditionalData
            {
                ClientCertificateSigningRequest = csrBase64
            };

            DeviceRegistrationResult result = await provisioningClient.RegisterAsync(additionalData);

            Console.WriteLine($"  Registration status: {result.Status}");

            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                Console.WriteLine($"  Error: Registration failed. Status: {result.Status}");
                Console.WriteLine($"  Error code: {result.ErrorCode}");
                Console.WriteLine($"  Error message: {result.ErrorMessage}");
                return;
            }

            Console.WriteLine($"  Device ID: {result.DeviceId}");
            Console.WriteLine($"  Assigned hub: {result.AssignedHub}");

            // Step 5: Process issued certificate
            Console.WriteLine("\nStep 5: Processing issued certificate...");
            if (result.IssuedClientCertificate == null || result.IssuedClientCertificate.Count == 0)
            {
                Console.WriteLine("  Error: No certificate was issued by DPS.");
                Console.WriteLine("  Ensure that your enrollment is configured for certificate issuance.");
                return;
            }

            Console.WriteLine($"  Received certificate chain with {result.IssuedClientCertificate.Count} certificate(s).");

            // Convert to PEM and save
            string pemChain = CertificateHelper.ConvertToPem(result.IssuedClientCertificate);
            File.WriteAllText(_parameters.OutputCertPath, pemChain);
            Console.WriteLine($"  Certificate chain saved to: {_parameters.OutputCertPath}");

            // Create X509Certificate2 with private key for IoT Hub authentication
            Console.WriteLine("\nStep 6: Creating certificate with private key for IoT Hub authentication...");
            using X509Certificate2 deviceCertTemp = CreateCertificateWithPrivateKey(result.IssuedClientCertificate, privateKey);
            
            // Export and reimport with Exportable flag to ensure it works with SChannel
            byte[] pfxBytes = deviceCertTemp.Export(X509ContentType.Pfx);
            using X509Certificate2 deviceCert = new X509Certificate2(pfxBytes, (string?)null, X509KeyStorageFlags.Exportable);
            
            Console.WriteLine($"  Certificate subject: {deviceCert.Subject}");
            Console.WriteLine($"  Certificate thumbprint: {deviceCert.Thumbprint}");
            Console.WriteLine($"  Valid from: {deviceCert.NotBefore}");
            Console.WriteLine($"  Valid until: {deviceCert.NotAfter}");
            Console.WriteLine($"  Has private key: {deviceCert.HasPrivateKey}");

            // Step 7: Connect to IoT Hub using issued certificate
            if (_parameters.SendTelemetry)
            {
                Console.WriteLine("\nStep 7: Connecting to IoT Hub using issued certificate...");
                
                // Build the certificate chain for authentication (leaf + intermediates)
                var chainCerts = new X509Certificate2Collection();
                for (int i = 1; i < result.IssuedClientCertificate.Count; i++)
                {
                    byte[] certBytes = Convert.FromBase64String(result.IssuedClientCertificate[i]);
                    chainCerts.Add(new X509Certificate2(certBytes));
                }

                var auth = new DeviceAuthenticationWithX509Certificate(
                    result.DeviceId,
                    deviceCert,
                    chainCerts);

                // Certificate chains are only supported on Amqp_Tcp_Only and Mqtt_Tcp_Only
                // Convert the transport type to TCP-only variant for IoT Hub connection
                TransportType iotHubTransportType = GetTcpOnlyTransportType(_parameters.TransportType);
                Console.WriteLine($"  Using transport type: {iotHubTransportType} (TCP-only required for cert chains)");

                using DeviceClient deviceClient = DeviceClient.Create(
                    result.AssignedHub,
                    auth,
                    iotHubTransportType);

                // Open connection explicitly to catch any TLS errors early
                await deviceClient.OpenAsync();
                Console.WriteLine("  Connected to IoT Hub successfully.");

                // Send a test telemetry message
                Console.WriteLine("\nStep 8: Sending test telemetry message...");
                string messagePayload = $"{{\"message\": \"Hello from CSR-provisioned device\", \"timestamp\": \"{DateTime.UtcNow:O}\"}}";
                using var message = new Message(Encoding.UTF8.GetBytes(messagePayload))
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8"
                };

                await deviceClient.SendEventAsync(message);
                Console.WriteLine("  Telemetry message sent successfully.");

                await deviceClient.CloseAsync();
                
                // Dispose chain certificates
                foreach (var cert in chainCerts)
                {
                    cert.Dispose();
                }
            }

            Console.WriteLine("\n=== Sample completed successfully ===");
            Console.WriteLine($"\nYou can now use the following files to authenticate with IoT Hub:");
            Console.WriteLine($"  Certificate: {_parameters.OutputCertPath}");
            Console.WriteLine($"  Private key: {_parameters.OutputKeyPath}");
        }

        private (string csrBase64, AsymmetricAlgorithm privateKey) GenerateCsr(string registrationId)
        {
            if (_parameters.CsrKeyType == CsrKeyType.ECC)
            {
                var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                var request = new CertificateRequest(
                    $"CN={registrationId}",
                    ecdsa,
                    HashAlgorithmName.SHA256);

                byte[] csrDer = request.CreateSigningRequest();
                return (Convert.ToBase64String(csrDer), ecdsa);
            }
            else
            {
                var rsa = RSA.Create(_parameters.RsaKeySize);
                var request = new CertificateRequest(
                    $"CN={registrationId}",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                byte[] csrDer = request.CreateSigningRequest();
                return (Convert.ToBase64String(csrDer), rsa);
            }
        }

        private void SavePrivateKey(AsymmetricAlgorithm privateKey, string path)
        {
            byte[] privateKeyBytes = privateKey switch
            {
                ECDsa ecdsa => ecdsa.ExportPkcs8PrivateKey(),
                RSA rsa => rsa.ExportPkcs8PrivateKey(),
                _ => throw new NotSupportedException($"Unsupported key type: {privateKey.GetType()}")
            };

            var sb = new StringBuilder();
            sb.AppendLine("-----BEGIN PRIVATE KEY-----");
            sb.AppendLine(Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks));
            sb.AppendLine("-----END PRIVATE KEY-----");

            File.WriteAllText(path, sb.ToString());
        }

        private X509Certificate2 CreateCertificateWithPrivateKey(
            System.Collections.Generic.IReadOnlyList<string> certificateChain,
            AsymmetricAlgorithm privateKey)
        {
            return privateKey switch
            {
                ECDsa ecdsa => CertificateHelper.CreateCertificateWithPrivateKey(certificateChain, ecdsa),
                RSA rsa => CertificateHelper.CreateCertificateWithPrivateKey(certificateChain, rsa),
                _ => throw new NotSupportedException($"Unsupported key type: {privateKey.GetType()}")
            };
        }

        private SecurityProvider CreateSecurityProvider()
        {
            return _parameters.AuthType switch
            {
                AuthenticationType.SymmetricKey => new SecurityProviderSymmetricKey(
                    _parameters.RegistrationId,
                    _parameters.SymmetricKey!,
                    null),

                AuthenticationType.X509 => CreateX509SecurityProvider(),

                _ => throw new NotSupportedException($"Unsupported authentication type: {_parameters.AuthType}")
            };
        }

        private SecurityProviderX509Certificate CreateX509SecurityProvider()
        {
            X509Certificate2 certificate;
            if (!string.IsNullOrEmpty(_parameters.X509CertPassword))
            {
                certificate = new X509Certificate2(
                    _parameters.X509CertPath!,
                    _parameters.X509CertPassword,
                    X509KeyStorageFlags.Exportable);
            }
            else
            {
                certificate = new X509Certificate2(
                    _parameters.X509CertPath!,
                    (string?)null,
                    X509KeyStorageFlags.Exportable);
            }

            return new SecurityProviderX509Certificate(certificate);
        }

        private ProvisioningTransportHandler GetTransportHandler()
        {
            Console.WriteLine($"  Using transport type: {_parameters.TransportType}");
            return _parameters.TransportType switch
            {
                TransportType.Mqtt => new ProvisioningTransportHandlerMqtt(),
                TransportType.Mqtt_Tcp_Only => new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly),
                TransportType.Mqtt_WebSocket_Only => new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly),
                TransportType.Amqp => new ProvisioningTransportHandlerAmqp(),
                TransportType.Amqp_Tcp_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly),
                TransportType.Amqp_WebSocket_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.WebSocketOnly),
                TransportType.Http1 => new ProvisioningTransportHandlerHttp(),
                _ => throw new NotSupportedException($"Unsupported transport type: {_parameters.TransportType}"),
            };
        }

        /// <summary>
        /// Converts a transport type to its TCP-only variant.
        /// Certificate chains are only supported on Amqp_Tcp_Only and Mqtt_Tcp_Only.
        /// </summary>
        private static TransportType GetTcpOnlyTransportType(TransportType transportType)
        {
            return transportType switch
            {
                TransportType.Mqtt => TransportType.Mqtt_Tcp_Only,
                TransportType.Mqtt_Tcp_Only => TransportType.Mqtt_Tcp_Only,
                TransportType.Mqtt_WebSocket_Only => TransportType.Mqtt_Tcp_Only, // Fallback to TCP
                TransportType.Amqp => TransportType.Amqp_Tcp_Only,
                TransportType.Amqp_Tcp_Only => TransportType.Amqp_Tcp_Only,
                TransportType.Amqp_WebSocket_Only => TransportType.Amqp_Tcp_Only, // Fallback to TCP
                TransportType.Http1 => TransportType.Mqtt_Tcp_Only, // HTTP doesn't support cert chains, use MQTT
                _ => TransportType.Mqtt_Tcp_Only,
            };
        }
    }
}
