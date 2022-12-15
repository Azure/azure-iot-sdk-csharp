// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Demonstrates how to register a device with the device provisioning service using a certificate, and then
    /// use the registration information to authenticate to IoT Hub.
    /// </summary>
    internal class ProvisioningDeviceClientSample
    {
        private readonly ILogger _logger;
        private static volatile ProvisioningDeviceClient s_provClient;
        private static volatile DeviceRegistrationResult s_provResult;
        private static ProvisioningClientOptions s_clientOptions;
        private static IotHubClientTransportSettings s_hubTransportSettings;
        private static CancellationTokenSource s_appCancellation;
        private readonly string _idScope;
        private readonly string _globalDeviceEndpoint;
        private string _certificatePassword;
        private readonly string _certificatePath;
        private readonly string _certificateName;

        public ProvisioningDeviceClientSample(Parameters parameters, ILogger logger)
        {
            s_clientOptions = parameters.GetClientOptions();
            s_hubTransportSettings = parameters.GetHubTransportSettings();
            _certificatePath = parameters.GetCertificatePath();
            _logger = logger;
            _idScope = parameters.IdScope;
            _globalDeviceEndpoint = parameters.GlobalDeviceEndpoint;
            _certificateName = parameters.CertificateName;
            _certificatePassword = parameters.CertificatePassword;
        }

        public async Task RunSampleAsync()
        {
            s_appCancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                s_appCancellation.Cancel();
                _logger.LogWarning("Sample execution cancellation requested; will exit.");
            };

            _logger.LogInformation("Loading the certificate...");
            using X509Certificate2 certificate = LoadProvisioningCertificate();
            var security = new AuthenticationProviderX509(certificate);

            // Perform retry up to 10 times every 2 seconds
            s_clientOptions.RetryPolicy = new ProvisioningClientFixedDelayRetryPolicy(10, TimeSpan.FromSeconds(2));

            _logger.LogInformation("Initializing the device provisioning client...");

            try
            {
                s_provClient = new ProvisioningDeviceClient(
                    _globalDeviceEndpoint,
                    _idScope,
                    security,
                    s_clientOptions);
            }
            catch (ProvisioningClientException ex)
            {
                _logger.LogError($"ProvioningClientException encountered. Reason: [{ex.GetType()}: {ex.Message}]");
            }

            _logger.LogInformation($"Initialized for registration Id '{security.GetRegistrationId()}'.");

            _logger.LogInformation("Registering with the device provisioning service... ");
            s_provResult = await s_provClient.RegisterAsync(s_appCancellation.Token);

            _logger.LogInformation($"Registration status: {s_provResult.Status}.");
            if (s_provResult.Status != ProvisioningRegistrationStatus.Assigned)
            {
                _logger.LogWarning("Registration status did not assign a hub, so exiting this sample.");
                return;
            }

            _logger.LogInformation($"Device '{s_provResult.DeviceId}' registered to IoT hub hostname '{s_provResult.AssignedHub}'.");

            _logger.LogInformation("Creating X509 authentication for IoT Hub...");
            var auth = new ClientAuthenticationWithX509Certificate(
                certificate,
                s_provResult.DeviceId);

            _logger.LogInformation("Testing the provisioned device with IoT Hub...");
            var hubOptions = new IotHubClientOptions(s_hubTransportSettings);
            await using var iotClient = new IotHubDeviceClient(s_provResult.AssignedHub, auth, hubOptions);

            await iotClient.OpenAsync(s_appCancellation.Token);
            _logger.LogInformation($"Sending a telemetry message...");
            var message = new TelemetryMessage("TestMessage");
            await iotClient.SendTelemetryAsync(message, s_appCancellation.Token);

            await iotClient.CloseAsync(s_appCancellation.Token);
            _logger.LogInformation("Finished.");
        }

        private X509Certificate2 LoadProvisioningCertificate()
        {
            ReadCertificatePassword();

            var certificateCollection = new X509Certificate2Collection();
            
            certificateCollection.Import(
                _certificatePath,
                _certificatePassword,
                X509KeyStorageFlags.UserKeySet);

            X509Certificate2 certificate = null;

            foreach (X509Certificate2 element in certificateCollection)
            {
                _logger.LogInformation($"Found certificate: {element?.Thumbprint} {element?.Subject}; PrivateKey: {element?.HasPrivateKey}");
                if (certificate == null && element.HasPrivateKey)
                {
                    certificate = element;
                }
                else
                {
                    element.Dispose();
                }
            }

            if (certificate == null)
            {
                throw new FileNotFoundException($"{_certificateName} did not contain any certificate with a private key.");
            }

            _logger.LogInformation($"Using certificate {certificate.Thumbprint} {certificate.Subject}");

            return certificate;
        }

        private void ReadCertificatePassword()
        {
            if (!string.IsNullOrWhiteSpace(_certificatePassword))
            {
                return;
            }

            var password = new StringBuilder();
            Console.WriteLine($"Enter the PFX password for {_certificateName}:");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else
                {
                    Console.Write('*');
                    password.Append(key.KeyChar);
                }
            }

            _certificatePassword = password.ToString();
        }
    }
}
