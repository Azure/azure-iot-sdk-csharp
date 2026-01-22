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
/// Demonstrates certificate-based authentication and certificate renewal via the credential management API.
/// 
/// This sample:
/// 1. Provisions a device with Azure DPS using symmetric key + CSR to get an issued certificate
/// 2. Connects to IoT Hub using the issued certificate
/// 3. Creates a new CSR and requests certificate renewal from IoT Hub
/// 4. Reconnects with the renewed certificate
/// 5. Sends telemetry messages
/// </summary>
public class CertificateSigningRequestSample : IDisposable
{
    private const int MessageSize = 256;
    private const string ApiVersion = "2025-08-01-preview";

    private readonly Parameters _parameters;
    private readonly CancellationTokenSource _cts;
    private DeviceClient? _deviceClient;
    private ECDsa? _privateKey;
    private bool _disposed;

    public CertificateSigningRequestSample(Parameters parameters)
    {
        _parameters = parameters;
        _cts = new CancellationTokenSource();

        // Handle Ctrl+C for graceful shutdown
        Console.CancelKeyPress += (sender, eventArgs) =>
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
            // Step 1: Load existing credentials or provision device with DPS
            PrintStep(1, "Loading credentials");
            var credentials = await LoadOrProvisionCredentialsAsync();

            // Step 2: Connect to IoT Hub with DPS-issued certificate
            PrintStep(2, "Connecting to IoT Hub with DPS certificate");
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
            var csrRequest = new CertificateSigningRequest
            {
                Id = credentials.DeviceId,
                Csr = Convert.ToBase64String(csrData),
                Replace = "*", // Replace any active credential operation
            };

            // Send CSR request with keepalive messages running in parallel
            // This matches the Python reference implementation behavior
            CertificateSigningResponse response = await SendCsrWithKeepaliveAsync(csrRequest);
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

            await Task.Delay(TimeSpan.FromSeconds(1), _cts.Token); // Brief pause before reconnecting

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

    private async Task<DeviceCredentials> LoadOrProvisionCredentialsAsync()
    {
        DeviceCredentials? credentials = TryLoadExistingCredentials();

        if (credentials != null)
        {
            Console.WriteLine("Using existing credentials, skipping DPS provisioning");
            return credentials;
        }

        Console.WriteLine("No existing credentials found, provisioning with DPS...");
        return await ProvisionDeviceAsync();
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
        _privateKey = ECDsa.Create();
        _privateKey.ImportFromPem(keyPem);

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

    private async Task<DeviceCredentials> ProvisionDeviceAsync()
    {
        Console.WriteLine($"Provisioning device '{_parameters.DeviceName}' with DPS...");

        // Derive device key from SAS key
        string deviceKey = ComputeDerivedSymmetricKey(_parameters.SasKey, _parameters.DeviceName);

        // Create security provider
        using var security = new SecurityProviderSymmetricKey(
            _parameters.DeviceName,
            deviceKey,
            null);

        // Generate EC private key (prime256v1 = SECP256R1)
        Console.WriteLine("Generating EC private key...");
        _privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Generate CSR
        Console.WriteLine("Creating certificate signing request...");
        byte[] csrData = CreateCsr(_privateKey, _parameters.DeviceName);

        // Create provisioning client with CSR
        using ProvisioningTransportHandler transportHandler = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly);
        ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
            _parameters.ProvisioningHost,
            _parameters.IdScope,
            security,
            transportHandler);

        // Set the CSR on the provisioning data
        var additionalData = new ProvisioningRegistrationAdditionalData
        {
            ClientCertificateSigningRequest = Convert.ToBase64String(csrData),
        };

        Console.WriteLine("Registering with DPS...");
        DeviceRegistrationResult result = await provClient.RegisterAsync(additionalData, _cts.Token);

        if (result.Status != ProvisioningRegistrationStatusType.Assigned)
        {
            throw new InvalidOperationException($"Registration failed with status: {result.Status}");
        }

        Console.WriteLine($"Device assigned to hub: {result.AssignedHub}");
        Console.WriteLine($"Device ID: {result.DeviceId}");

        // Get issued certificate
        if (result.IssuedClientCertificate == null || result.IssuedClientCertificate.Count == 0)
        {
            throw new InvalidOperationException("No certificate issued by DPS");
        }

        // Convert certificate list to PEM
        string certPem = CertificateListToPem(result.IssuedClientCertificate);

        // Serialize private key to PEM
        string keyPem = ExportPrivateKeyToPem(_privateKey);

        // Create output directory if it doesn't exist
        Directory.CreateDirectory(_parameters.OutputDir);

        // Save certificate and key to disk
        string certPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.cert.pem");
        string keyPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.key.pem");

        Console.WriteLine($"Saving certificate to: {certPath}");
        File.WriteAllText(certPath, certPem);

        Console.WriteLine($"Saving private key to: {keyPath}");
        File.WriteAllText(keyPath, keyPem);

        // Save metadata
        string metadataPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.json");
        var metadata = new { assigned_hub = result.AssignedHub, device_id = result.DeviceId };
        Console.WriteLine($"Saving metadata to: {metadataPath}");
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

        return new DeviceCredentials
        {
            AssignedHub = result.AssignedHub,
            DeviceId = result.DeviceId,
            CertificatePath = certPath,
            KeyPath = keyPath,
        };
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
        var auth = new DeviceAuthenticationWithX509Certificate(deviceId, exportedCert);

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
        var testPayload = JsonSerializer.Serialize(new
        {
            type = "pre-csr-test",
            timestamp = DateTime.UtcNow.ToString("o"),
            message = "Verifying connectivity before certificate renewal"
        });

        using var message = new Message(Encoding.UTF8.GetBytes(testPayload))
        {
            ContentEncoding = "utf-8",
            ContentType = "application/json",
        };

        await _deviceClient!.SendEventAsync(message, _cts.Token);
    }

    /// <summary>
    /// Sends a CSR request while running keepalive messages in parallel.
    /// This matches the Python reference implementation behavior where keepalive
    /// messages are sent every 5 seconds while waiting for the certificate response.
    /// </summary>
    private async Task<CertificateSigningResponse> SendCsrWithKeepaliveAsync(CertificateSigningRequest csrRequest)
    {
        const int KeepaliveIntervalSeconds = 5;

        // Create a cancellation token source that will be cancelled when the CSR response arrives
        using var keepaliveCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);

        // Start the keepalive task
        Task keepaliveTask = SendKeepaliveMessagesAsync(KeepaliveIntervalSeconds, keepaliveCts.Token);

        try
        {
            // Send the CSR request and wait for the response
            Console.WriteLine("Sending CSR request (keepalive messages will be sent every 5 seconds)...");
            CertificateSigningResponse response = await _deviceClient!.SendCertificateSigningRequestAsync(csrRequest, _cts.Token);

            // Cancel the keepalive task now that we have the response
            keepaliveCts.Cancel();

            // Wait for the keepalive task to complete gracefully
            try
            {
                await keepaliveTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when we cancel the keepalive
            }

            return response;
        }
        catch
        {
            // Cancel the keepalive task on error
            keepaliveCts.Cancel();

            try
            {
                await keepaliveTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            throw;
        }
    }

    /// <summary>
    /// Sends keepalive messages at the specified interval until cancelled.
    /// </summary>
    private async Task SendKeepaliveMessagesAsync(int intervalSeconds, CancellationToken cancellationToken)
    {
        int keepaliveCount = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken);

                keepaliveCount++;
                var keepalivePayload = JsonSerializer.Serialize(new
                {
                    type = "keepalive",
                    seq = keepaliveCount,
                    timestamp = DateTime.UtcNow.ToString("o"),
                    message = "Waiting for certificate response"
                });

                using var message = new Message(Encoding.UTF8.GetBytes(keepalivePayload))
                {
                    ContentEncoding = "utf-8",
                    ContentType = "application/json",
                };

                await _deviceClient!.SendEventAsync(message, cancellationToken);
                Console.WriteLine($"  [Keepalive #{keepaliveCount}] Sent at {DateTime.UtcNow:HH:mm:ss}");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"  [Keepalive] Stopped after {keepaliveCount} message(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [Keepalive] Error: {ex.Message}");
        }
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
                using var message = new Message(Encoding.UTF8.GetBytes(messageData))
                {
                    ContentEncoding = "utf-8",
                    ContentType = "application/json",
                };

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

    private static byte[] CreateCsr(ECDsa privateKey, string deviceName)
    {
        var request = new CertificateRequest(
            $"CN={deviceName}",
            privateKey,
            HashAlgorithmName.SHA256);

        return request.CreateSigningRequest();
    }

    private static string ComputeDerivedSymmetricKey(string enrollmentGroupKey, string deviceId)
    {
        byte[] keyBytes = Convert.FromBase64String(enrollmentGroupKey);
        using var hmac = new HMACSHA256(keyBytes);
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(deviceId));
        return Convert.ToBase64String(hash);
    }

    private static string CertificateListToPem(IList<string> certList)
    {
        const string beginHeader = "-----BEGIN CERTIFICATE-----\r\n";
        const string endFooter = "\r\n-----END CERTIFICATE-----";
        string separator = endFooter + "\r\n" + beginHeader;
        return beginHeader + string.Join(separator, certList) + endFooter;
    }

    private static string ExportPrivateKeyToPem(ECDsa key)
    {
        byte[] privateKeyBytes = key.ExportPkcs8PrivateKey();
        return new StringBuilder()
            .AppendLine("-----BEGIN PRIVATE KEY-----")
            .AppendLine(Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks))
            .AppendLine("-----END PRIVATE KEY-----")
            .ToString();
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
                Console.WriteLine($"    Subject CN: {cert.GetNameInfo(X509NameType.SimpleName, false) ?? "N/A"}");
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
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
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
}
