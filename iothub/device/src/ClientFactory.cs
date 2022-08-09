// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace Microsoft.Azure.Devices.Client
{
    internal static class ClientFactory
    {
        /// <summary>
        /// Create an instance of InternalClient with the given parameters.
        /// </summary>
        /// <param name="connectionString">The device connection string.</param>
        /// <param name="options">The optional client settings.</param>
        /// <returns>An instance of InternalClient.</returns>
        internal static InternalClient CreateFromConnectionString(
            string connectionString,
            IotHubClientOptions options)
        {
            Argument.AssertNotNullOrWhiteSpace(connectionString, nameof(connectionString));

            if (options == default)
            {
                options = new();
            }

            var csBuilder = new IotHubConnectionStringBuilder(connectionString);
            if (csBuilder.UsingX509Cert)
            {
                throw new ArgumentException("To use X509 certificates, use the initializer with the IAuthenticationMethod parameter.", nameof(connectionString));
            }

            return CreateInternal(null, csBuilder, options);
        }

        /// <summary>
        /// Create an instance of InternalClient with the given parameters.
        /// </summary>
        /// <param name="hostName">The fully-qualified DNS hostname of IoT hub</param>
        /// <param name="authenticationMethod">The authentication method.</param>
        /// <param name="options">The optional client settings.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient Create(string hostName, IAuthenticationMethod authenticationMethod, IotHubClientOptions options = default)
        {
            Argument.AssertNotNullOrWhiteSpace(hostName, nameof(hostName));
            Argument.AssertNotNull(authenticationMethod, nameof(authenticationMethod));

            var csBuilder = new IotHubConnectionStringBuilder(authenticationMethod, hostName, options?.GatewayHostName);

            // Make sure client options is initialized with the correct transport setting.
            EnsureOptionsIsSetup(csBuilder.Certificate, ref options);

            // Validate certs.
            if (authenticationMethod is DeviceAuthenticationWithX509Certificate)
            {
                // Prep for certificate auth.
                if (csBuilder.AuthenticationMethod is DeviceAuthenticationWithX509Certificate
                    && csBuilder.Certificate == null)
                {
                    throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.");
                }

                if (csBuilder.AuthenticationMethod is DeviceAuthenticationWithX509Certificate certificate
                    && certificate.ChainCertificates != null
                    && (options.TransportSettings is not IotHubClientAmqpSettings
                    && options.TransportSettings is not IotHubClientMqttSettings
                    || options.TransportSettings.Protocol != IotHubClientTransportProtocol.Tcp))
                {
                    throw new ArgumentException("Certificate chains are only supported on MQTT and AMQP over TCP.");
                }

                // Install all the intermediate certificates in the chain if specified.
                if (csBuilder.ChainCertificates != null)
                {
                    try
                    {
                        CertificateInstaller.EnsureChainIsInstalled(csBuilder.ChainCertificates);
                    }
                    catch (Exception ex)
                    {
                        if (Logging.IsEnabled)
                            Logging.Error(null, $"{nameof(CertificateInstaller)} failed to read or write to cert store due to: {ex}");

                        throw new UnauthorizedException($"Failed to provide certificates in the chain - {ex.Message}", ex);
                    }
                }
            }

            InternalClient internalClient = CreateInternal(null, csBuilder, options);
            internalClient.Certificate = csBuilder.Certificate;

            return internalClient;
        }

        /// <summary>
        /// This initializer exists only for unit tests to override the pipeline creation
        /// </summary>
        /// <param name="pipelineBuilder">Should only be specified by SDK tests to override the operation pipeline.</param>
        /// <param name="csBuilder">The device connection string builder.</param>
        /// <param name="options">The optional client settings.</param>
#if DEBUG
        internal
#else
        private
#endif
        static InternalClient CreateInternal(
            IDeviceClientPipelineBuilder pipelineBuilder,
            IotHubConnectionStringBuilder csBuilder,
            IotHubClientOptions options)
        {
            Argument.AssertNotNull(csBuilder, nameof(csBuilder));
            Argument.AssertNotNull(options, nameof(options));

            // Clients that derive their authentication method from AuthenticationWithTokenRefresh will need to specify
            // the token time to live and renewal buffer values through the corresponding AuthenticationWithTokenRefresh
            // implementation constructors instead.
            if (csBuilder.AuthenticationMethod is not AuthenticationWithTokenRefresh
                && csBuilder.AuthenticationMethod is not DeviceAuthenticationWithX509Certificate)
            {
                csBuilder.SasTokenTimeToLive = options?.SasTokenTimeToLive ?? default;
                csBuilder.SasTokenRenewalBuffer = options?.SasTokenRenewalBuffer ?? default;
            }

            var clientConfiguration = new ClientConfiguration(csBuilder, options);
            var client = new InternalClient(clientConfiguration, pipelineBuilder);

            if (Logging.IsEnabled)
                Logging.CreateFromConnectionString(
                    client,
                    $"HostName={clientConfiguration.GatewayHostName};DeviceId={clientConfiguration.DeviceId};ModuleId={clientConfiguration.ModuleId}",
                    options);

            return client;
        }

        /// <summary>
        /// Ensures that the client options are configured and initialized.
        /// If a certificate is provided, the fileUploadTransportSettings will use it during initialization.
        /// </summary>
        private static void EnsureOptionsIsSetup(X509Certificate2 cert, ref IotHubClientOptions options)
        {
            if (options == null)
            {
                options = new();
            }

            if (options.FileUploadTransportSettings == null)
            {
                options.FileUploadTransportSettings = new();
            }

            if (cert != null)
            {
                if (options.FileUploadTransportSettings.ClientCertificate == null)
                {
                    options.FileUploadTransportSettings.ClientCertificate = cert;
                }

                if (options.TransportSettings.ClientCertificate == null)
                {
                    options.TransportSettings.ClientCertificate = cert;
                }
            }
        }
    }
}
