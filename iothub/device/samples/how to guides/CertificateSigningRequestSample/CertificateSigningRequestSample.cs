// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;

namespace CertificateSigningRequestSample;

/// <summary>
/// Demonstrates certificate renewal via the IoT Hub credential management API.
/// 
/// This sample supports two modes of operation:
/// 
/// Mode 1: Using existing credentials
/// - Device must already be provisioned with IoT Hub
/// - Existing certificate and private key files must be available
/// - A metadata JSON file with assigned_hub and device_id
/// 
/// Mode 2: Initial provisioning via DPS with CSR (when credentials don't exist)
/// - Requires DPS IdScope and SymmetricKey parameters
/// - Generates a new key pair and CSR
/// - Registers with DPS to obtain an issued certificate
/// - Saves credentials to disk for future use
/// 
/// After loading/obtaining credentials, the sample:
/// 1. Connects to IoT Hub using the certificate
/// 2. Sends a test message to verify connectivity
/// 3. Creates a new CSR and requests certificate renewal from IoT Hub
/// 4. Saves the renewed certificate
/// 5. Reconnects with the renewed certificate
/// 6. Sends telemetry messages to verify the new certificate works
/// </summary>
public sealed class CertificateSigningRequestSample : IDisposable
{
    private const int MessageSize = 256;

    private readonly Parameters _parameters;
    private readonly CancellationTokenSource _cts;
    private DeviceClient? _deviceClient;
    private AsymmetricAlgorithm? _privateKey;
    private bool _disposed;

    public CertificateSigningRequestSample(Parameters parameters)
    {
        _parameters = parameters;
        _cts = new CancellationTokenSource();

        // Handle Ctrl+C for graceful shutdown
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            Console.WriteLine("\nShutdown requested...");
            eventArgs.Cancel = true;
            _cts.Cancel();
        };
    }

    public async Task<int> RunSampleAsync()
    {
        try
        {
            // Step 1: Load existing credentials (certificate, key, metadata)
            PrintStep(1, "Loading credentials");
            DeviceCredentials credentials = await LoadCredentialsAsync();
            Console.WriteLine("Credentials loaded successfully.");

            // Step 2: Connect to IoT Hub with existing certificate
            PrintStep(2, "Connecting to IoT Hub with certificate");
            _deviceClient = await ConnectWithCertificateAsync(
                credentials.AssignedHub,
                credentials.DeviceId,
                credentials.CertificatePath,
                credentials.KeyPath);
            Console.WriteLine("Connected to IoT Hub successfully!");

            // Step 3: Send test message to verify connectivity before CSR request
            PrintStep(3, "Sending test message to verify connectivity");
            await SendTestMessageAsync();
            Console.WriteLine("Test message sent successfully - connection verified!");

            // Step 4: Create new CSR using the same private key
            PrintStep(4, "Creating new CSR for certificate renewal");
            byte[] csrData = CreateCsr(_privateKey!, _parameters.DeviceName);
            Console.WriteLine($"CSR created (length: {csrData.Length} bytes)");

            // Step 5: Request new certificate via MQTT
            PrintStep(5, "Requesting new certificate from IoT Hub");
            var csrRequest = new CertificateSigningRequest(
                credentials.DeviceId,
                Convert.ToBase64String(csrData)) 
                { Replace = "*" }; // Replace any active credential operation for this device

            Console.WriteLine("Sending CSR request...");
            CertificateSigningResponse response = await _deviceClient!.SendCertificateSigningRequestAsync(csrRequest, _cts.Token);
            Console.WriteLine($"Received certificate response with {response.Certificates?.Count ?? 0} certificate(s)");

            if (response.Certificates == null || response.Certificates.Count == 0)
            {
                Console.WriteLine("ERROR: No certificate received in response");
                return 1;
            }

            PrintCertificateChain(response.Certificates);

            // Save the renewed certificate
            string renewedCertPath = SaveRenewedCertificate(response.Certificates, _parameters.OutputDir, _parameters.DeviceName);
            Console.WriteLine($"Saved renewed certificate to: {renewedCertPath}");

            await Task.Delay(TimeSpan.FromSeconds(5), _cts.Token); // Pause to ensure final ack is sent

            // Step 6: Disconnect and reconnect with renewed certificate
            PrintStep(6, "Reconnecting with renewed certificate");
            Console.WriteLine("Disconnecting from IoT Hub...");
            await _deviceClient.CloseAsync(_cts.Token);
            _deviceClient.Dispose();

            _deviceClient = await ConnectWithCertificateAsync(
                credentials.AssignedHub,
                credentials.DeviceId,
                renewedCertPath,
                credentials.KeyPath);
            Console.WriteLine("Reconnected with renewed certificate successfully!");

            // Step 7: Send telemetry messages
            PrintStep(7, "Sending telemetry messages");
            await SendTelemetryAsync(_parameters.MessageCount);

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation was cancelled.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
        finally
        {
            if (_deviceClient != null)
            {
                Console.WriteLine("Disconnecting from IoT Hub...");
                try
                {
                    await _deviceClient.CloseAsync(CancellationToken.None);
                    Console.WriteLine("Disconnected successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during disconnect: {ex.Message}");
                }
            }
        }
    }

    private async Task<DeviceCredentials> LoadCredentialsAsync()
    {
        DeviceCredentials? credentials = TryLoadExistingCredentials();

        if (credentials != null)
        {
            return credentials;
        }

        // Credentials not found - check if DPS parameters are provided
        if (_parameters.HasDpsParameters)
        {
            Console.WriteLine("Credentials not found. DPS parameters provided - provisioning via DPS with CSR...");
            return await ProvisionWithDpsAsync();
        }

        // No credentials and no DPS parameters - provide helpful error message
        string certPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.cert.pem");
        string keyPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.key.pem");
        string metadataPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.json");

        throw new InvalidOperationException(
            $"Required credential files not found.\n\n" +
            $"Expected files:\n" +
            $"  - Certificate: {certPath}\n" +
            $"  - Private Key: {keyPath}\n" +
            $"  - Metadata:    {metadataPath}\n\n" +
            $"The metadata JSON file should contain:\n" +
            $"  {{\n" +
            $"    \"assigned_hub\": \"your-hub.azure-devices.net\",\n" +
            $"    \"device_id\": \"{_parameters.DeviceName}\"\n" +
            $"  }}\n\n" +
            $"Alternatively, provide DPS parameters to provision the device:\n" +
            $"  --idScope <DPS_ID_SCOPE> --symmetricKey <SYMMETRIC_KEY>");
    }

    private DeviceCredentials? TryLoadExistingCredentials()
    {
        string certPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.cert.pem");
        string keyPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.key.pem");
        string metadataPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.json");

        if (!File.Exists(certPath) || !File.Exists(keyPath) || !File.Exists(metadataPath))
        {
            return null;
        }

        Console.WriteLine($"Found existing credentials in {_parameters.OutputDir}");
        Console.WriteLine($"Loading metadata from: {metadataPath}");

        string metadataJson = File.ReadAllText(metadataPath);
        using JsonDocument doc = JsonDocument.Parse(metadataJson);
        string assignedHub = doc.RootElement.GetProperty("assigned_hub").GetString()!;
        string deviceId = doc.RootElement.GetProperty("device_id").GetString()!;

        Console.WriteLine($"Loading private key from: {keyPath}");
        string keyPem = File.ReadAllText(keyPath);
        _privateKey = LoadPrivateKeyFromPem(keyPem);

        Console.WriteLine($"Device ID: {deviceId}");
        Console.WriteLine($"Assigned hub: {assignedHub}");

        return new DeviceCredentials
        {
            AssignedHub = assignedHub,
            DeviceId = deviceId,
            CertificatePath = certPath,
            KeyPath = keyPath,
        };
    }

    private static AsymmetricAlgorithm LoadPrivateKeyFromPem(string keyPem)
    {
        // Try ECC first, then RSA
        if (keyPem.Contains("EC PRIVATE KEY") || keyPem.Contains("PRIVATE KEY"))
        {
            try
            {
                var ecdsa = ECDsa.Create();
                ecdsa.ImportFromPem(keyPem);
                return ecdsa;
            }
            catch (CryptographicException)
            {
                // Not an ECC key, try RSA
            }
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(keyPem);
        return rsa;
    }

    private async Task<DeviceClient> ConnectWithCertificateAsync(
        string hostname,
        string deviceId,
        string certPath,
        string keyPath)
    {
        Console.WriteLine($"Connecting to IoT Hub via MQTT: {hostname}...");
        Console.WriteLine($"  Certificate file: {Path.GetFullPath(certPath)}");
        Console.WriteLine($"  Key file: {Path.GetFullPath(keyPath)}");

        // Load certificate and key
        string certPem = File.ReadAllText(certPath);
        string keyPem = File.ReadAllText(keyPath);

        // Create X509Certificate2 from PEM
        using var cert = X509Certificate2.CreateFromPem(certPem, keyPem);

        // Note: On Windows, we need to export and reimport to allow ephemeral key use
        using var exportedCert = new X509Certificate2(cert.Export(X509ContentType.Pfx));

        // Create authentication using the certificate
        using var auth = new DeviceAuthenticationWithX509Certificate(deviceId, exportedCert);

        // Create device client
        var deviceClient = DeviceClient.Create(
            hostname,
            auth,
            _parameters.TransportType);

        // Open connection
        await deviceClient.OpenAsync(_cts.Token);

        return deviceClient;
    }

    private async Task SendTestMessageAsync()
    {
        // Send a test message to verify connectivity before CSR request
        // This matches the Python reference implementation behavior
        string testPayload = JsonSerializer.Serialize(new
        {
            type = "pre-csr-test",
            timestamp = DateTime.UtcNow.ToString("o"),
            message = "Verifying connectivity before certificate renewal"
        });

        using var message = new Message(Encoding.UTF8.GetBytes(testPayload));
        message.ContentEncoding = "utf-8";
        message.ContentType = "application/json";

        await _deviceClient!.SendEventAsync(message, _cts.Token);
    }


    private async Task SendTelemetryAsync(int messageCount)
    {
        Console.WriteLine($"\nSending {messageCount} messages...");

        int sentCount = 0;
        for (int i = 0; i < messageCount && !_cts.IsCancellationRequested; i++)
        {
            try
            {
                string messageData = CreateMessage(MessageSize);
                using var message = new Message(Encoding.UTF8.GetBytes(messageData));
                message.ContentEncoding = "utf-8";
                message.ContentType = "application/json";

                await _deviceClient!.SendEventAsync(message, _cts.Token);
                sentCount++;
                Console.WriteLine($"Message {sentCount}/{messageCount} sent ({messageData.Length} bytes)");

                // Brief pause between messages (except after the last one)
                if (i < messageCount - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        Console.WriteLine($"\nTotal messages sent: {sentCount}");
    }

    private static byte[] CreateCsr(AsymmetricAlgorithm privateKey, string deviceName)
    {
        CertificateRequest request = privateKey switch
        {
            ECDsa ecdsa => new CertificateRequest($"CN={deviceName}", ecdsa, HashAlgorithmName.SHA256),
            RSA rsa => new CertificateRequest($"CN={deviceName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            _ => throw new NotSupportedException($"Unsupported key type: {privateKey.GetType()}")
        };

        return request.CreateSigningRequest();
    }

    private static string CertificateListToPem(IList<string> certList)
    {
        const string beginHeader = "-----BEGIN CERTIFICATE-----\r\n";
        const string endFooter = "\r\n-----END CERTIFICATE-----";
        string separator = endFooter + "\r\n" + beginHeader;
        return beginHeader + string.Join(separator, certList) + endFooter;
    }

    private static string SaveRenewedCertificate(IList<string> certificates, string outputDir, string deviceName)
    {
        string certPem = CertificateListToPem(certificates);
        string renewedCertPath = Path.Combine(outputDir, $"{deviceName}_renewed.cert.pem");
        File.WriteAllText(renewedCertPath, certPem);
        return renewedCertPath;
    }

    private static void PrintCertificateChain(IList<string> certList)
    {
        Console.WriteLine($"\n=== Certificate Chain ({certList.Count} certificate(s)) ===");

        for (int i = 0; i < certList.Count; i++)
        {
            try
            {
                byte[] certDer = Convert.FromBase64String(certList[i]);
                using var cert = new X509Certificate2(certDer);

                Console.WriteLine($"\n  Certificate [{i + 1}]:");
                Console.WriteLine($"    Subject CN: {cert.GetNameInfo(X509NameType.SimpleName, false)}");
                Console.WriteLine($"    Not Before: {cert.NotBefore:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"    Not After:  {cert.NotAfter:yyyy-MM-dd HH:mm:ss} UTC");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n  Certificate [{i + 1}]: Failed to parse - {ex.Message}");
            }
        }

        Console.WriteLine();
    }

    private static string CreateMessage(int size)
    {
        string timestamp = DateTime.UtcNow.ToString("o");
        string padding = new string('A', size);
        return JsonSerializer.Serialize(new { date = timestamp, val = padding });
    }

    private static void PrintStep(int stepNumber, string description)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"STEP {stepNumber}: {description}");
        Console.WriteLine(new string('=', 70));
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _deviceClient?.Dispose();
                _privateKey?.Dispose();
                _cts.Dispose();
            }
            _disposed = true;
        }
    }

    private class DeviceCredentials
    {
        public string AssignedHub { get; init; } = null!;
        public string DeviceId { get; init; } = null!;
        public string CertificatePath { get; init; } = null!;
        public string KeyPath { get; init; } = null!;
    }

    #region DPS Provisioning

    private async Task<DeviceCredentials> ProvisionWithDpsAsync()
    {
        Console.WriteLine("\n--- DPS CSR Provisioning ---");

        // Step 1: Generate key pair and CSR
        Console.WriteLine("Generating key pair and Certificate Signing Request (CSR)...");
        var (csrBase64, privateKey) = GenerateKeyPairAndCsr(_parameters.DeviceName);
        _privateKey = privateKey;
        Console.WriteLine($"  CSR generated successfully. Key type: {_parameters.CsrKeyType}");

        // Step 2: Determine symmetric key (derive if enrollment group key provided)
        string symmetricKey;
        if (!string.IsNullOrEmpty(_parameters.EnrollmentGroupKey))
        {
            Console.WriteLine("Deriving device symmetric key from enrollment group key...");
            symmetricKey = DeriveSymmetricKey(_parameters.EnrollmentGroupKey, _parameters.DeviceName);
            Console.WriteLine($"  Derived key: {symmetricKey.Substring(0, Math.Min(20, symmetricKey.Length))}...");
        }
        else if (!string.IsNullOrEmpty(_parameters.SymmetricKey))
        {
            symmetricKey = _parameters.SymmetricKey;
        }
        else
        {
            throw new InvalidOperationException("Either --symmetricKey or --enrollmentGroupKey must be provided for DPS provisioning.");
        }

        // Step 3: Create security provider
        Console.WriteLine($"Creating security provider (SymmetricKey)...");
        using var security = new SecurityProviderSymmetricKey(
            _parameters.DeviceName,
            symmetricKey,
            null);
        Console.WriteLine($"  Registration ID: {security.GetRegistrationID()}");

        // Step 4: Create provisioning client (MQTT only for CSR)
        Console.WriteLine("Creating provisioning client...");
        using var transportHandler = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly);
        ProvisioningDeviceClient provisioningClient = ProvisioningDeviceClient.Create(
            _parameters.GlobalDeviceEndpoint,
            _parameters.IdScope!,
            security,
            transportHandler);

        Console.WriteLine($"  Global endpoint: {_parameters.GlobalDeviceEndpoint}");
        Console.WriteLine($"  ID Scope: {_parameters.IdScope}");
        Console.WriteLine($"  Transport: MQTT (TCP only)");

        // Step 5: Register with CSR
        Console.WriteLine("Registering with DPS (including CSR)...");
        var additionalData = new ProvisioningRegistrationAdditionalData
        {
            ClientCertificateSigningRequest = csrBase64
        };

        DeviceRegistrationResult result = await provisioningClient.RegisterAsync(additionalData, _cts.Token);

        Console.WriteLine($"  Registration status: {result.Status}");

        if (result.Status != ProvisioningRegistrationStatusType.Assigned)
        {
            throw new InvalidOperationException(
                $"DPS registration failed. Status: {result.Status}, " +
                $"Error code: {result.ErrorCode}, Error message: {result.ErrorMessage}");
        }

        Console.WriteLine($"  Device ID: {result.DeviceId}");
        Console.WriteLine($"  Assigned hub: {result.AssignedHub}");

        // Step 6: Process issued certificate
        Console.WriteLine("Processing issued certificate...");
        if (result.IssuedClientCertificate == null || result.IssuedClientCertificate.Count == 0)
        {
            throw new InvalidOperationException(
                "No certificate was issued by DPS. Ensure that your enrollment is configured for certificate issuance.");
        }

        Console.WriteLine($"  Received certificate chain with {result.IssuedClientCertificate.Count} certificate(s).");

        // Save credentials to disk
        string certPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.cert.pem");
        string keyPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.key.pem");
        string metadataPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.json");

        // Ensure output directory exists
        Directory.CreateDirectory(_parameters.OutputDir);

        // Save certificate chain
        string pemChain = CertificateHelper.ConvertToPem(result.IssuedClientCertificate);
        File.WriteAllText(certPath, pemChain);
        Console.WriteLine($"  Certificate chain saved to: {certPath}");

        // Save private key
        SavePrivateKey(_privateKey, keyPath);
        Console.WriteLine($"  Private key saved to: {keyPath}");

        // Save metadata
        SaveMetadataJson(result.AssignedHub!, result.DeviceId!, metadataPath);
        Console.WriteLine($"  Metadata saved to: {metadataPath}");

        Console.WriteLine("--- DPS Provisioning Complete ---\n");

        return new DeviceCredentials
        {
            AssignedHub = result.AssignedHub!,
            DeviceId = result.DeviceId!,
            CertificatePath = certPath,
            KeyPath = keyPath,
        };
    }

    private (string csrBase64, AsymmetricAlgorithm privateKey) GenerateKeyPairAndCsr(string registrationId)
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

    private static void SavePrivateKey(AsymmetricAlgorithm privateKey, string path)
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

    private static void SaveMetadataJson(string assignedHub, string deviceId, string path)
    {
        var metadata = new { assigned_hub = assignedHub, device_id = deviceId };
        string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Derives a device-specific symmetric key from an enrollment group key using HMAC-SHA256.
    /// This is required for group enrollments where each device needs its own derived key.
    /// </summary>
    /// <param name="enrollmentGroupKey">The primary or secondary key from the enrollment group.</param>
    /// <param name="registrationId">The device registration ID (typically the device name).</param>
    /// <returns>A base64-encoded derived symmetric key for the device.</returns>
    private static string DeriveSymmetricKey(string enrollmentGroupKey, string registrationId)
    {
        using var hmac = new HMACSHA256(Convert.FromBase64String(enrollmentGroupKey));
        byte[] derivedKeyBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationId));
        return Convert.ToBase64String(derivedKeyBytes);
    }

    #endregion
}
