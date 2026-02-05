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

namespace CertificateSigningRequestSample;

/// <summary>
/// Demonstrates certificate renewal via the IoT Hub credential management API.
/// 
/// Prerequisites:
/// - Device must already be provisioned with IoT Hub
/// - Existing certificate and private key files must be available
/// - A metadata JSON file with assigned_hub and device_id
/// 
/// This sample:
/// 1. Loads existing credentials from disk (certificate, key, and metadata)
/// 2. Connects to IoT Hub using the existing certificate
/// 3. Sends a test message to verify connectivity
/// 4. Creates a new CSR and requests certificate renewal from IoT Hub
/// 5. Saves the renewed certificate
/// 6. Reconnects with the renewed certificate
/// 7. Sends telemetry messages to verify the new certificate works
/// 
/// Note: DPS provisioning with CSR is not yet supported in this SDK.
/// Use the Python SDK or Azure CLI for initial device provisioning with CSR.
/// </summary>
public sealed class CertificateSigningRequestSample : IDisposable
{
    private const int MessageSize = 256;

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

    private Task<DeviceCredentials> LoadCredentialsAsync()
    {
        DeviceCredentials? credentials = TryLoadExistingCredentials();

        if (credentials != null)
        {
            return Task.FromResult(credentials);
        }

        // Credentials not found - provide helpful error message
        string certPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.cert.pem");
        string keyPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.key.pem");
        string metadataPath = Path.Combine(_parameters.OutputDir, $"{_parameters.DeviceName}.json");

        throw new InvalidOperationException(
            $"Required credential files not found. This sample requires pre-existing credentials.\n\n" +
            $"Expected files:\n" +
            $"  - Certificate: {certPath}\n" +
            $"  - Private Key: {keyPath}\n" +
            $"  - Metadata:    {metadataPath}\n\n" +
            $"The metadata JSON file should contain:\n" +
            $"  {{\n" +
            $"    \"assigned_hub\": \"your-hub.azure-devices.net\",\n" +
            $"    \"device_id\": \"{_parameters.DeviceName}\"\n" +
            $"  }}\n\n" +
            $"Note: DPS provisioning with CSR is not yet supported in this SDK.\n" +
            $"Use the Python SDK (cert_test.py) or Azure CLI for initial device provisioning.");
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

    private static byte[] CreateCsr(ECDsa privateKey, string deviceName)
    {
        var request = new CertificateRequest(
            $"CN={deviceName}",
            privateKey,
            HashAlgorithmName.SHA256);

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
}
